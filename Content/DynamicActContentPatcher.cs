using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Content
{
    internal static class DynamicActContentPatcher
    {
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
                var unlockedAncientsPostfix = DynamicPatchBuilder.FromMethod(
                    typeof(DynamicActContentPatcher),
                    nameof(GetUnlockedAncientsPostfix));

                foreach (var actType in actTypes)
                {
                    TryAddPropertyGetterPatch(builder, actType, nameof(ActModel.AllEvents), eventsPostfix, logger);
                    TryAddPropertyGetterPatch(builder, actType, nameof(ActModel.AllAncients), ancientsPostfix, logger);
                    TryAddMethodPatch(
                        builder,
                        actType,
                        nameof(ActModel.GenerateAllEncounters),
                        [],
                        encountersPostfix,
                        logger);
                    TryAddMethodPatch(
                        builder,
                        actType,
                        nameof(ActModel.GetUnlockedAncients),
                        [typeof(UnlockState)],
                        unlockedAncientsPostfix,
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
            Logger logger)
        {
            try
            {
                builder.AddPropertyGetter(
                    actType,
                    propertyName,
                    postfix: postfix,
                    description: $"Patch {actType.Name}.{propertyName} for dynamic mod content");
            }
            catch (Exception ex)
            {
                logger.Warn(
                    $"[Content] Could not queue getter '{actType.Name}.{propertyName}' for dynamic patching: {ex.Message}");
            }
        }

        private static void TryAddMethodPatch(
            DynamicPatchBuilder builder,
            Type actType,
            string methodName,
            Type[] parameterTypes,
            HarmonyMethod postfix,
            Logger logger)
        {
            try
            {
                builder.AddMethod(
                    actType,
                    methodName,
                    parameterTypes,
                    postfix: postfix,
                    description: $"Patch {actType.Name}.{methodName} for dynamic mod content");
            }
            catch (Exception ex)
            {
                logger.Warn(
                    $"[Content] Could not queue method '{actType.Name}.{methodName}' for dynamic patching: {ex.Message}");
            }
        }

        // ReSharper disable InconsistentNaming
        private static void AllEventsPostfix(ActModel __instance, ref IEnumerable<EventModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModContentRegistry.AppendActEvents(__instance, __result);
        }

        // ReSharper disable InconsistentNaming
        private static void AllAncientsPostfix(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModContentRegistry.AppendActAncients(__instance, __result);
        }

        // ReSharper disable InconsistentNaming
        private static void AllEncountersPostfix(ActModel __instance, ref IEnumerable<EncounterModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModContentRegistry.AppendGlobalEncounters(
                ModContentRegistry.AppendActEncounters(__instance, __result));
        }

        // ReSharper disable InconsistentNaming
        private static void GetUnlockedAncientsPostfix(
                ActModel __instance,
                UnlockState unlockState,
                ref IEnumerable<AncientEventModel> __result)
            // ReSharper restore InconsistentNaming
        {
            __result = ModUnlockRegistry.FilterUnlocked(
                ModContentRegistry.AppendActAncients(__instance, __result),
                unlockState);
        }
    }
}
