using System.Reflection;
using MegaCrit.Sts2.Core.Platform.Steam;

namespace STS2RitsuLib.RuntimeInput
{
    internal static class RitsuSteamInputInterop
    {
        private const int MaxControllers = 16;

        private static readonly Lazy<Type?> SteamInputType = new(() =>
            Type.GetType("Steamworks.SteamInput, Steamworks.NET", false));

        private static readonly Lazy<Type?> InputHandleType = new(() =>
            Type.GetType("Steamworks.InputHandle_t, Steamworks.NET", false));

        private static readonly Lazy<Type?> InputDigitalActionHandleType = new(() =>
            Type.GetType("Steamworks.InputDigitalActionHandle_t, Steamworks.NET", false));

        public static bool IsSteamAvailable => SteamInitializer.Initialized && SteamInputType.Value != null;

        public static bool SetInputActionManifestFilePath(string path)
        {
            return InvokeStatic<bool>("SetInputActionManifestFilePath", [path]);
        }

        public static bool TryGetFirstController(out object controllerHandle)
        {
            controllerHandle = null!;
            if (!IsSteamAvailable || InputHandleType.Value == null)
                return false;

            var controllers = Array.CreateInstance(InputHandleType.Value, MaxControllers);
            var count = InvokeStatic<int>("GetConnectedControllers", [controllers]);
            if (count <= 0)
                return false;

            controllerHandle = controllers.GetValue(0)!;
            return !IsDefaultStruct(controllerHandle);
        }

        public static bool TryGetDigitalActionHandle(string actionId, out object actionHandle)
        {
            actionHandle = null!;
            if (!IsSteamAvailable)
                return false;

            actionHandle = InvokeStatic<object?>("GetDigitalActionHandle", [actionId])!;
            return actionHandle != null && !IsDefaultStruct(actionHandle);
        }

        public static bool IsDigitalActionPressed(object controllerHandle, object actionHandle)
        {
            var data = InvokeStatic<object?>("GetDigitalActionData", [controllerHandle, actionHandle]);
            if (data == null)
                return false;

            var field = data.GetType().GetField("bState", BindingFlags.Instance | BindingFlags.Public);
            return field?.GetValue(data) switch
            {
                bool b => b,
                byte b => b != 0,
                int i => i != 0,
                _ => false,
            };
        }

        private static T InvokeStatic<T>(string methodName, object?[] args)
        {
            var method = ResolveMethod(methodName, args.Length);
            if (method == null)
                return default!;

            try
            {
                var value = method.Invoke(null, args);
                return value is T typed ? typed : default!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static MethodInfo? ResolveMethod(string methodName, int argumentCount)
        {
            return SteamInputType.Value?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == methodName && method.GetParameters().Length == argumentCount);
        }

        private static bool IsDefaultStruct(object value)
        {
            var type = value.GetType();
            if (!type.IsValueType)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return value == null;

            var defaultValue = Activator.CreateInstance(type);
            return Equals(value, defaultValue);
        }
    }
}
