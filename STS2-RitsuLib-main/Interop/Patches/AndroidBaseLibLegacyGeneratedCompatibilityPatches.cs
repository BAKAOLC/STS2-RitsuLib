using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    internal static class AndroidBaseLibLegacyGeneratedCompatHelper
    {
        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly ConcurrentDictionary<Type, LegacyEncounterCompatAccessors> EncounterAccessorCache = [];
        private static readonly ConcurrentDictionary<MethodBase, bool> HasLegacyCompatOwnerCache = [];
        private static readonly ConcurrentDictionary<string, byte> LoggedRoutes = [];
        private static readonly ConcurrentDictionary<Type, MethodInfo?> OnUpgradeMethodCache = [];

        internal static bool HasLegacyCompatOwner(MethodBase originalMethod)
        {
            return HasLegacyCompatOwnerCache.GetOrAdd(
                originalMethod,
                static method =>
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    if (patchInfo == null)
                        return false;

                    return patchInfo.Prefixes.Concat(patchInfo.Postfixes).Concat(patchInfo.Transpilers)
                        .Concat(patchInfo.Finalizers)
                        .Any(static patch =>
                            patch.owner.EndsWith(".BaseLibToRitsuCompat", StringComparison.Ordinal));
                });
        }

        internal static void InvokeCardUpgradeFallback(CardModel card)
        {
            var onUpgrade = OnUpgradeMethodCache.GetOrAdd(
                card.GetType(),
                static type => type.GetMethod("OnUpgrade", InstanceFlags, null, Type.EmptyTypes, null));

            onUpgrade?.Invoke(card, null);
            card.FinalizeUpgradeInternal();

            LogOnce(
                $"{card.GetType().FullName}:upgrade",
                $"[AndroidCompat] Routed CardModel.UpgradeInternal through reflection fallback for '{card.GetType().FullName}'.");
        }

        internal static BackgroundAssets BuildEncounterBackgroundFallback(EncounterModel encounter, ActModel parentAct,
            Rng rng)
        {
            var accessors = EncounterAccessorCache.GetOrAdd(
                encounter.GetType(),
                static type => new LegacyEncounterCompatAccessors
                {
                    PrepCustomBackground = type.GetMethod(
                        "PrepCustomBackground",
                        InstanceFlags,
                        null,
                        [typeof(ActModel), typeof(Rng)],
                        null),
                    GetPreparedBackgroundAssets = type.GetMethod(
                        "GetPreparedBackgroundAssets",
                        InstanceFlags,
                        null,
                        Type.EmptyTypes,
                        null),
                });

            accessors.PrepCustomBackground?.Invoke(encounter, [parentAct, rng]);

            if (accessors.GetPreparedBackgroundAssets?.Invoke(encounter, null) is BackgroundAssets prepared)
            {
                LogOnce(
                    $"{encounter.GetType().FullName}:encounter-bg-custom",
                    $"[AndroidCompat] Routed EncounterModel.GetBackgroundAssets through legacy custom background fallback for '{encounter.GetType().FullName}'.");
                return prepared;
            }

            LogOnce(
                $"{encounter.GetType().FullName}:encounter-bg-act",
                $"[AndroidCompat] Routed EncounterModel.GetBackgroundAssets to act background fallback for '{encounter.GetType().FullName}'.");
            return parentAct.GenerateBackgroundAssets(rng);
        }

        private static void LogOnce(string key, string message)
        {
            if (!LoggedRoutes.TryAdd(key, 0))
                return;

            RitsuLibFramework.Logger.Info(message);
        }

        private sealed class LegacyEncounterCompatAccessors
        {
            public MethodInfo? GetPreparedBackgroundAssets { get; init; }
            public MethodInfo? PrepCustomBackground { get; init; }
        }
    }

    internal class AndroidBaseLibCardUpgradeInternalCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_card_upgrade_internal_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Route CardModel.UpgradeInternal through reflection on Android when BaseLibToRitsu legacy patches are active";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.UpgradeInternal))];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MethodBase __originalMethod, CardModel __instance)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (!AndroidBaseLibLegacyGeneratedCompatHelper.HasLegacyCompatOwner(__originalMethod))
                return true;

            try
            {
                AndroidBaseLibLegacyGeneratedCompatHelper.InvokeCardUpgradeFallback(__instance);
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AndroidCompat] Card upgrade fallback failed for '{__instance.GetType().FullName}': {ex.GetBaseException().Message}");
                return true;
            }
        }
    }

    internal class AndroidBaseLibEncounterGetBackgroundAssetsCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_encounter_get_background_assets_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Route EncounterModel.GetBackgroundAssets through Android-safe BaseLibToRitsu fallback logic";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(EncounterModel), "GetBackgroundAssets", [typeof(ActModel), typeof(Rng)])];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MethodBase __originalMethod, EncounterModel __instance, ActModel parentAct, Rng rng,
            ref BackgroundAssets __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            if (!AndroidBaseLibLegacyGeneratedCompatHelper.HasLegacyCompatOwner(__originalMethod))
                return true;

            try
            {
                __result = AndroidBaseLibLegacyGeneratedCompatHelper.BuildEncounterBackgroundFallback(
                    __instance,
                    parentAct,
                    rng);
                return false;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[AndroidCompat] Encounter background fallback failed for '{__instance.GetType().FullName}': {ex.GetBaseException().Message}. Falling back to parent act background.");
                __result = parentAct.GenerateBackgroundAssets(rng);
                return false;
            }
        }
    }
}
