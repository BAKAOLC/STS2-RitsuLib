using System.Reflection;
using HarmonyLib;

namespace STS2RitsuLib.Patching.Compat
{
    internal static class HarmonyPatchAllTypeLoadGuard
    {
        private const string HarmonyId = "OLC.STS2-RitsuLib.HarmonyPatchAllTypeLoadGuard";
        private static readonly Lock InstallLock = new();
        private static readonly Lock WarnedTypesLock = new();
        private static readonly HashSet<string> WarnedTypes = [];
        private static Action<string>? _warn;
        private static bool _installed;

        public static void Install(Action<string>? warn = null)
        {
            lock (InstallLock)
            {
                if (_installed)
                    return;

                _warn ??= warn;

                try
                {
                    var target = AccessTools.Method(typeof(HarmonyMethodExtensions),
                        nameof(HarmonyMethodExtensions.GetFromType), [typeof(Type)]);
                    if (target == null)
                    {
                        Warn("Cannot install Harmony PatchAll type-load guard: GetFromType(Type) was not found.");
                        _installed = true;
                        return;
                    }

                    if (Harmony.GetPatchInfo(target)?.Finalizers.Any(patch => patch.owner == HarmonyId) == true)
                    {
                        _installed = true;
                        return;
                    }

                    var finalizer = AccessTools.Method(typeof(HarmonyPatchAllTypeLoadGuard), nameof(Finalizer));
                    new Harmony(HarmonyId).Patch(target, finalizer: new(finalizer));
                    _installed = true;
                }
                catch (Exception ex)
                {
                    Warn(
                        "Cannot install Harmony PatchAll type-load guard; incompatible patch attributes may still abort PatchAll: " +
                        ex.Message);
                    _installed = true;
                }
            }
        }

        private static Exception? Finalizer(Type type, Exception? __exception, ref List<HarmonyMethod> __result)
        {
            if (__exception is null)
                return null;

            var root = Unwrap(__exception);
            if (root is not TypeLoadException)
                return __exception;

            __result = [];
            WarnOnce(type, root);
            return null;
        }

        private static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException or TypeInitializationException &&
                   exception.InnerException is { } inner)
                exception = inner;

            return exception;
        }

        private static void WarnOnce(Type type, Exception exception)
        {
            var key = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

            lock (WarnedTypesLock)
            {
                if (!WarnedTypes.Add(key))
                    return;
            }

            Warn(
                "Skipping Harmony patch type with unloadable attribute target: " +
                $"{type.FullName} ({exception.GetType().Name}: {exception.Message})");
        }

        private static void Warn(string message)
        {
            (_warn ??= Console.Error.WriteLine)("[RitsuLib.HarmonyPatchAllTypeLoadGuard] " + message);
        }
    }
}
