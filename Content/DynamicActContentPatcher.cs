using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Content
{
    internal static class DynamicActContentPatcher
    {
        private const BindingFlags DeclaredInstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Lock SyncRoot = new();
        private static bool _patched;

        internal static void EnsurePatched()
        {
            lock (SyncRoot)
            {
                if (_patched)
                    return;

                var logger = RitsuLibFramework.Logger;
                var actTypes = ReflectionHelper.GetSubtypes<ActModel>()
                    .Concat(ReflectionHelper.GetSubtypesInMods<ActModel>())
                    .Concat(ModContentRegistry.GetRegisteredActTypes())
                    .Where(type => type is { IsAbstract: false, IsInterface: false })
                    .Distinct()
                    .ToArray();

                var builder = new DynamicPatchBuilder("dynamic_act_content");
                var eventsPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicActContentPatcher), nameof(AllEventsPostfix));
                var ancientsPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicActContentPatcher), nameof(AllAncientsPostfix));
                var encountersPostfix =
                    DynamicPatchBuilder.FromMethod(typeof(DynamicActContentPatcher), nameof(AllEncountersPostfix));
                var bossDiscoveryOrderPostfix = DynamicPatchBuilder.FromMethod(
                    typeof(DynamicActContentPatcher),
                    nameof(BossDiscoveryOrderPostfix));
                var unlockedAncientsPostfix = DynamicPatchBuilder.FromMethod(
                    typeof(DynamicActContentPatcher),
                    nameof(GetUnlockedAncientsPostfix));

                var queuedMethods = new HashSet<MethodInfo>();
                foreach (var actType in actTypes)
                {
                    TryAddPropertyGetterPatch(
                        builder,
                        actType,
                        nameof(ActModel.AllEvents),
                        eventsPostfix,
                        queuedMethods,
                        logger);
                    TryAddPropertyGetterPatch(
                        builder,
                        actType,
                        nameof(ActModel.AllAncients),
                        ancientsPostfix,
                        queuedMethods,
                        logger);
                    TryAddPropertyGetterPatch(
                        builder,
                        actType,
                        nameof(ActModel.BossDiscoveryOrder),
                        bossDiscoveryOrderPostfix,
                        queuedMethods,
                        logger);
                    TryAddMethodPatch(
                        builder,
                        actType,
                        nameof(ActModel.GenerateAllEncounters),
                        [],
                        encountersPostfix,
                        queuedMethods,
                        logger);
                    TryAddMethodPatch(
                        builder,
                        actType,
                        nameof(ActModel.GetUnlockedAncients),
                        [typeof(UnlockState)],
                        unlockedAncientsPostfix,
                        queuedMethods,
                        logger);
                }

                if (!RitsuLibFramework
                        .GetFrameworkPatcher(RitsuLibFramework.FrameworkPatcherArea.ContentRegistry)
                        .ApplyDynamic(builder))
                    throw new InvalidOperationException("Failed to apply dynamic Act content patches.");

                _patched = true;
                logger.Info($"[Content] Dynamic act content patching initialized for {actTypes.Length} act type(s).");
            }
        }

        private static void TryAddPropertyGetterPatch(
            DynamicPatchBuilder builder,
            Type actType,
            string propertyName,
            HarmonyMethod postfix,
            HashSet<MethodInfo> queuedMethods,
            Logger logger)
        {
            var getter = FindDeclaredPropertyGetter(actType, propertyName);
            if (getter == null || !queuedMethods.Add(getter))
                return;

            try
            {
                builder.Add(
                    getter,
                    postfix: postfix,
                    description: $"Patch {getter.DeclaringType?.Name}.{propertyName} for dynamic mod content");
            }
            catch (Exception ex)
            {
                queuedMethods.Remove(getter);
                logger.Warn(
                    $"[Content] Could not queue getter '{getter.DeclaringType?.Name}.{propertyName}' for dynamic patching: {ex.Message}");
            }
        }

        private static void TryAddMethodPatch(
            DynamicPatchBuilder builder,
            Type actType,
            string methodName,
            Type[] parameterTypes,
            HarmonyMethod postfix,
            HashSet<MethodInfo> queuedMethods,
            Logger logger)
        {
            var method = FindDeclaredMethodImplementation(actType, methodName, parameterTypes);
            if (method == null || !queuedMethods.Add(method))
                return;

            try
            {
                builder.Add(
                    method,
                    postfix: postfix,
                    description: $"Patch {method.DeclaringType?.Name}.{methodName} for dynamic mod content");
            }
            catch (Exception ex)
            {
                queuedMethods.Remove(method);
                logger.Warn(
                    $"[Content] Could not queue method '{method.DeclaringType?.Name}.{methodName}' for dynamic patching: {ex.Message}");
            }
        }

        private static MethodInfo? FindDeclaredPropertyGetter(Type concreteActType, string propertyName)
        {
            for (var walk = concreteActType;
                 walk != null && typeof(ActModel).IsAssignableFrom(walk);
                 walk = walk.BaseType)
            {
                var property = walk.GetProperty(propertyName, DeclaredInstanceFlags);
                if (property?.GetMethod is { IsAbstract: false } getter)
                    return getter;
            }

            return null;
        }

        private static MethodInfo? FindDeclaredMethodImplementation(
            Type concreteActType,
            string methodName,
            Type[] parameterTypes)
        {
            for (var walk = concreteActType;
                 walk != null && typeof(ActModel).IsAssignableFrom(walk);
                 walk = walk.BaseType)
            {
                var method = walk.GetMethod(methodName, DeclaredInstanceFlags, null, parameterTypes, null);
                if (method is { IsAbstract: false })
                    return method;
            }

            return null;
        }

        private static void AllEventsPostfix(ActModel __instance, ref IEnumerable<EventModel> __result)
        {
            __result = ModContentRegistry.AppendActEvents(__instance, __result);
        }

        private static void AllAncientsPostfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
        {
            __result = ModContentRegistry.AppendActAncients(__instance, __result);
        }

        private static void AllEncountersPostfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
        {
            __result = ModEncounterActValidityFilter.FilterForAct(
                __instance,
                ModContentRegistry.AppendGlobalEncounters(
                    ModContentRegistry.AppendActEncounters(__instance, __result)));
        }

        private static void BossDiscoveryOrderPostfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
        {
            __result = ModEncounterActValidityFilter.FilterForAct(__instance, __result);
        }

        private static void GetUnlockedAncientsPostfix(
            ActModel __instance,
            object[] __args,
            ref IEnumerable<AncientEventModel> __result)
        {
            if (__args.Length == 0 || __args[0] is not UnlockState unlockState)
                return;

            __result = ModAncientActValidityFilter.FilterForAct(
                __instance,
                ModUnlockRegistry.FilterUnlocked(
                    ModContentRegistry.AppendActAncients(__instance, __result),
                    unlockState));
        }
    }
}
