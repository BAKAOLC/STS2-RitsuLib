using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    internal static class RuntimeModDataStubPatcher
    {
        private const BindingFlags StaticMethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly Lock Gate = new();
        private static readonly Harmony Harmony = new($"{Const.ModId}.mod-data-interop");
        private static readonly HashSet<MethodBase> PatchedMethods = [];
        private static readonly Dictionary<MethodBase, StubRoute> Routes = [];

        private static readonly MethodInfo GetPrefixOpen =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(GetPrefix), StaticMethodFlags)!;

        private static readonly MethodInfo ModifyPrefixOpen =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(ModifyPrefix), StaticMethodFlags)!;

        private static readonly MethodInfo SavePrefixMethod =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(SavePrefix), StaticMethodFlags)!;

        public static int TryPatchEntry(
            Type providerType,
            string modId,
            string key,
            Type dataType,
            string? getMethodName,
            string? modifyMethodName,
            string? saveMethodName)
        {
            var suffix = ToPascalIdentifier(key);
            var patched = 0;

            if (TryResolveGetMethod(providerType, dataType, getMethodName ?? $"Get{suffix}", getMethodName != null,
                    out var getMethod))
                patched += PatchOnce(
                    getMethod,
                    GetPrefixOpen.MakeGenericMethod(dataType, getMethod.ReturnType),
                    new(modId, key, dataType));

            if (TryResolveModifyMethod(providerType, dataType, modifyMethodName ?? $"Modify{suffix}",
                    modifyMethodName != null, out var modifyMethod))
                patched += PatchOnce(
                    modifyMethod,
                    ModifyPrefixOpen.MakeGenericMethod(dataType),
                    new(modId, key, dataType));

            if (TryResolveSaveMethod(providerType, saveMethodName ?? $"Save{suffix}", saveMethodName != null,
                    out var saveMethod))
                patched += PatchOnce(saveMethod, SavePrefixMethod, new(modId, key, dataType));

            return patched;
        }

        private static int PatchOnce(MethodInfo original, MethodInfo prefix, StubRoute route)
        {
            lock (Gate)
            {
                if (!PatchedMethods.Add(original))
                    return 0;

                Routes[original] = route;
                try
                {
                    Harmony.Patch(original, new(prefix));
                    return 1;
                }
                catch (Exception ex)
                {
                    PatchedMethods.Remove(original);
                    Routes.Remove(original);
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeModDataInteropSource] Failed to patch ModData stub method {FormatMethod(original)}: {ex.Message}");
                    return 0;
                }
            }
        }

        private static bool TryResolveGetMethod(
            Type providerType,
            Type dataType,
            string methodName,
            bool warnIfInvalid,
            out MethodInfo method)
        {
            method = null!;
            var candidate = providerType.GetMethod(methodName, StaticMethodFlags, []);
            if (candidate == null)
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName,
                        $"expected static {methodName}() returning {dataType.FullName} or one of its base types, but the method was not found.");
                return false;
            }

            if (candidate.ReturnType == typeof(void) || !candidate.ReturnType.IsAssignableFrom(dataType))
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName,
                        $"expected static {methodName}() returning {dataType.FullName} or one of its base types.");
                return false;
            }

            method = candidate;
            return true;
        }

        private static bool TryResolveModifyMethod(
            Type providerType,
            Type dataType,
            string methodName,
            bool warnIfInvalid,
            out MethodInfo method)
        {
            method = null!;
            var actionType = typeof(Action<>).MakeGenericType(dataType);
            var candidate = providerType.GetMethod(methodName, StaticMethodFlags, [actionType]);
            if (candidate == null)
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName,
                        $"expected static void {methodName}(Action<{dataType.FullName}>), but the method was not found.");
                return false;
            }

            if (candidate.ReturnType != typeof(void))
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName,
                        $"expected static void {methodName}(Action<{dataType.FullName}>).");
                return false;
            }

            method = candidate;
            return true;
        }

        private static bool TryResolveSaveMethod(
            Type providerType,
            string methodName,
            bool warnIfInvalid,
            out MethodInfo method)
        {
            method = null!;
            var candidate = providerType.GetMethod(methodName, StaticMethodFlags, []);
            if (candidate == null)
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName,
                        $"expected static void {methodName}(), but the method was not found.");
                return false;
            }

            if (candidate.ReturnType != typeof(void))
            {
                if (warnIfInvalid)
                    WarnInvalid(providerType, methodName, $"expected static void {methodName}().");
                return false;
            }

            method = candidate;
            return true;
        }

        private static bool GetPrefix<TData, TResult>(MethodBase __originalMethod, ref TResult __result)
            where TData : class, new()
            where TResult : class
        {
            if (!TryGetRoute(__originalMethod, out var route))
                return true;

            __result = (TResult)(object)ModDataStore.For(route.ModId).Get<TData>(route.Key);
            return false;
        }

        private static bool ModifyPrefix<TData>(MethodBase __originalMethod, Action<TData> __0)
            where TData : class, new()
        {
            if (!TryGetRoute(__originalMethod, out var route))
                return true;

            ModDataStore.For(route.ModId).Modify(route.Key, __0);
            return false;
        }

        private static bool SavePrefix(MethodBase __originalMethod)
        {
            if (!TryGetRoute(__originalMethod, out var route))
                return true;

            ModDataStore.For(route.ModId).Save(route.Key);
            return false;
        }

        private static bool TryGetRoute(MethodBase originalMethod, out StubRoute route)
        {
            lock (Gate)
            {
                return Routes.TryGetValue(originalMethod, out route);
            }
        }

        private static string ToPascalIdentifier(string key)
        {
            var chars = new List<char>(key.Length);
            var upperNext = true;
            foreach (var ch in key)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    upperNext = true;
                    continue;
                }

                chars.Add(upperNext ? char.ToUpperInvariant(ch) : ch);
                upperNext = false;
            }

            return chars.Count == 0 ? "Data" : new(chars.ToArray());
        }

        private static void WarnInvalid(Type providerType, string methodName, string detail)
        {
            RitsuLibFramework.Logger.Warn(
                $"[RuntimeModDataInteropSource] Invalid ModData stub method on {providerType.FullName}: {detail}");
        }

        private static string FormatMethod(MethodBase method)
        {
            return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
        }

        private readonly record struct StubRoute(string ModId, string Key, Type DataType);
    }
}
