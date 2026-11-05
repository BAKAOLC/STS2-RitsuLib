using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Installs Harmony prefixes that route compatible provider stub methods through RitsuLib mod-data
    ///         storage.
    ///     </para>
    ///     <para xml:lang="zh-CN">安装 Harmony 前置补丁，将兼容提供程序的存根方法路由到 RitsuLib 模组数据存储。</para>
    /// </summary>
    internal static class RuntimeModDataStubPatcher
    {
        private const BindingFlags StaticMethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly Lock Gate = new();
        private static readonly Harmony Harmony = new($"{Const.ModId}.mod-data-interop");
        private static readonly HashSet<MethodBase> PatchedMethods = [];
        private static readonly Dictionary<MethodBase, StubRoute> Routes = [];
        private static readonly ConcurrentDictionary<MethodBase, MethodInfo> PrefixImplementations = new();

        private static readonly MethodInfo PrefixFactoryMethod =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(CreatePrefix), StaticMethodFlags)!;

        private static readonly MethodInfo GetPrefixOpen =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(GetPrefix), StaticMethodFlags)!;

        private static readonly MethodInfo ModifyPrefixOpen =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(ModifyPrefix), StaticMethodFlags)!;

        private static readonly MethodInfo SavePrefixMethod =
            typeof(RuntimeModDataStubPatcher).GetMethod(nameof(SavePrefix), StaticMethodFlags)!;

        public static bool TryPatchEntry(
            Type providerType,
            string modId,
            string key,
            Type dataType,
            string? getMethodName,
            string? modifyMethodName,
            string? saveMethodName,
            Action registerStorage,
            out int patched)
        {
            var suffix = ToPascalIdentifier(key);
            patched = 0;
            var plans = new List<StubPatchPlan>();

            if (TryResolveGetMethod(providerType, dataType, getMethodName ?? $"Get{suffix}", getMethodName != null,
                    out var getMethod))
                plans.Add(new(getMethod, GetPrefixOpen.MakeGenericMethod(dataType, getMethod.ReturnType)));
            else if (getMethodName != null)
                return false;

            if (TryResolveModifyMethod(providerType, dataType, modifyMethodName ?? $"Modify{suffix}",
                    modifyMethodName != null, out var modifyMethod))
                plans.Add(new(modifyMethod, ModifyPrefixOpen.MakeGenericMethod(dataType)));
            else if (modifyMethodName != null)
                return false;

            if (TryResolveSaveMethod(providerType, saveMethodName ?? $"Save{suffix}", saveMethodName != null,
                    out var saveMethod))
                plans.Add(new(saveMethod, SavePrefixMethod));
            else if (saveMethodName != null)
                return false;

            var route = new StubRoute(modId, key, dataType);
            lock (Gate)
            {
                foreach (var plan in plans)
                    if (Routes.TryGetValue(plan.Original, out var existingRoute) && existingRoute != route)
                        throw new InvalidOperationException(
                            $"ModData stub method '{FormatMethod(plan.Original)}' already routes to another entry.");

                var installed = new List<StubPatchPlan>();
                var committed = false;
                try
                {
                    foreach (var plan in plans)
                        PatchInliningGuard.Prepare(plan.Original);

                    foreach (var plan in plans)
                    {
                        if (PatchedMethods.Contains(plan.Original))
                            continue;

                        PrefixImplementations[plan.Original] = plan.Prefix;
                        try
                        {
                            Harmony.Patch(plan.Original, new(PrefixFactoryMethod));
                        }
                        catch
                        {
                            PrefixImplementations.TryRemove(plan.Original, out _);
                            throw;
                        }

                        installed.Add(plan);
                    }

                    registerStorage();
                    foreach (var plan in installed)
                    {
                        Routes.Add(plan.Original, route);
                        PatchedMethods.Add(plan.Original);
                    }

                    committed = true;
                    patched = installed.Count;
                    return true;
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[RuntimeModDataInteropSource] Failed to route ModData entry '{modId}'::{key} on {providerType.FullName}: {ex.Message}");
                    return false;
                }
                finally
                {
                    if (!committed)
                        for (var index = installed.Count - 1; index >= 0; index--)
                        {
                            var plan = installed[index];
                            Harmony.Unpatch(plan.Original, PrefixFactoryMethod);
                            PrefixImplementations.TryRemove(plan.Original, out _);
                            Routes.Remove(plan.Original);
                            PatchedMethods.Remove(plan.Original);
                        }
                }
            }
        }

        private static MethodInfo CreatePrefix(MethodBase original)
        {
            return PrefixImplementations.TryGetValue(original, out var prefix)
                ? prefix
                : throw new InvalidOperationException(
                    $"No ModData prefix was prepared for '{FormatMethod(original)}'.");
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

            return chars.Count == 0 ? "Data" : new([.. chars]);
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

        private readonly record struct StubPatchPlan(MethodInfo Original, MethodInfo Prefix);
    }
}
