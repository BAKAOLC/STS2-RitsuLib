using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Interop.Patches
{
    internal static class AndroidBaseLibPropertyCompatHelper
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo?> StringPropertyCache = new();
        private static readonly ConcurrentDictionary<string, byte> LoggedOverrides = new();

        internal static bool TryOverrideString(
            object model,
            string compatibilityMemberName,
            ref string result,
            params string[] propertyNames)
        {
            var modelType = model.GetType();

            foreach (var propertyName in propertyNames)
            {
                var property = GetStringProperty(modelType, propertyName);
                if (property?.GetValue(model) is not string value || string.IsNullOrWhiteSpace(value))
                    continue;

                result = value;
                LogOverrideOnce(modelType, compatibilityMemberName, propertyName, value);
                return true;
            }

            return false;
        }

        private static PropertyInfo? GetStringProperty(Type type, string propertyName)
        {
            return StringPropertyCache.GetOrAdd(
                $"{type.AssemblyQualifiedName}|{propertyName}",
                _ =>
                {
                    const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                    var property = type.GetProperty(propertyName, instanceFlags);
                    return property?.PropertyType == typeof(string) ? property : null;
                });
        }

        private static void LogOverrideOnce(Type modelType, string memberName, string propertyName, string value)
        {
            var logKey = $"{modelType.FullName}:{memberName}:{value}";
            if (!LoggedOverrides.TryAdd(logKey, 0))
                return;

            RitsuLibFramework.Logger.Info(
                $"[AndroidCompat] Routed '{memberName}' for '{modelType.FullName}' through reflective compatibility via '{propertyName}': {value}");
        }
    }

    internal class AndroidBaseLibCharacterStringPropertyCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_character_string_property_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Reflect BaseLib-style character asset and audio getters on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CharacterModel), "get_VisualsPath"),
                new(typeof(CharacterModel), "get_EnergyCounterPath"),
                new(typeof(CharacterModel), "get_MerchantAnimPath"),
                new(typeof(CharacterModel), "get_RestSiteAnimPath"),
                new(typeof(CharacterModel), "get_IconTexturePath"),
                new(typeof(CharacterModel), "get_IconPath"),
                new(typeof(CharacterModel), "get_CharacterSelectBg"),
                new(typeof(CharacterModel), "get_CharacterSelectIconPath"),
                new(typeof(CharacterModel), "get_CharacterSelectLockedIconPath"),
                new(typeof(CharacterModel), "get_CharacterSelectTransitionPath"),
                new(typeof(CharacterModel), "get_MapMarkerPath"),
                new(typeof(CharacterModel), "get_TrailPath"),
                new(typeof(CharacterModel), "get_ArmPointingTexturePath"),
                new(typeof(CharacterModel), "get_ArmRockTexturePath"),
                new(typeof(CharacterModel), "get_ArmPaperTexturePath"),
                new(typeof(CharacterModel), "get_ArmScissorsTexturePath"),
                new(typeof(CharacterModel), "get_AttackSfx"),
                new(typeof(CharacterModel), "get_CastSfx"),
                new(typeof(CharacterModel), "get_DeathSfx"),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MethodBase __originalMethod, CharacterModel __instance, ref string __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            var applied = __originalMethod.Name switch
            {
                "get_VisualsPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.VisualsPath),
                    ref __result,
                    "CustomVisualsPath",
                    "CustomVisualPath"),
                "get_EnergyCounterPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.EnergyCounterPath),
                    ref __result,
                    "CustomEnergyCounterPath"),
                "get_MerchantAnimPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.MerchantAnimPath),
                    ref __result,
                    "CustomMerchantAnimPath"),
                "get_RestSiteAnimPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.RestSiteAnimPath),
                    ref __result,
                    "CustomRestSiteAnimPath"),
                "get_IconTexturePath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "IconTexturePath",
                    ref __result,
                    "CustomIconTexturePath"),
                "get_IconPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "IconPath",
                    ref __result,
                    "CustomIconPath"),
                "get_CharacterSelectBg" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.CharacterSelectBg),
                    ref __result,
                    "CustomCharacterSelectBgPath",
                    "CustomCharacterSelectBg"),
                "get_CharacterSelectIconPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "CharacterSelectIconPath",
                    ref __result,
                    "CustomCharacterSelectIconPath"),
                "get_CharacterSelectLockedIconPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "CharacterSelectLockedIconPath",
                    ref __result,
                    "CustomCharacterSelectLockedIconPath"),
                "get_CharacterSelectTransitionPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.CharacterSelectTransitionPath),
                    ref __result,
                    "CustomCharacterSelectTransitionPath"),
                "get_MapMarkerPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "MapMarkerPath",
                    ref __result,
                    "CustomMapMarkerPath"),
                "get_TrailPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.TrailPath),
                    ref __result,
                    "CustomTrailPath"),
                "get_ArmPointingTexturePath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "ArmPointingTexturePath",
                    ref __result,
                    "CustomArmPointingTexturePath"),
                "get_ArmRockTexturePath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "ArmRockTexturePath",
                    ref __result,
                    "CustomArmRockTexturePath"),
                "get_ArmPaperTexturePath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "ArmPaperTexturePath",
                    ref __result,
                    "CustomArmPaperTexturePath"),
                "get_ArmScissorsTexturePath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "ArmScissorsTexturePath",
                    ref __result,
                    "CustomArmScissorsTexturePath"),
                "get_AttackSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.AttackSfx),
                    ref __result,
                    "CustomAttackSfx"),
                "get_CastSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.CastSfx),
                    ref __result,
                    "CustomCastSfx"),
                "get_DeathSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(CharacterModel.DeathSfx),
                    ref __result,
                    "CustomDeathSfx"),
                _ => false,
            };

            return !applied;
        }
    }

    internal class AndroidBaseLibMonsterStringPropertyCompatibilityPatch : IPatchMethod
    {
        public static string PatchId => "android_baselib_monster_string_property_compat";

        public static bool IsCritical => false;

        public static string Description =>
            "Reflect BaseLib-style monster asset and audio getters on Android";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(MonsterModel), "get_VisualsPath"),
                new(typeof(MonsterModel), "get_AttackSfx"),
                new(typeof(MonsterModel), "get_CastSfx"),
                new(typeof(MonsterModel), "get_DeathSfx"),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(MethodBase __originalMethod, MonsterModel __instance, ref string __result)
        {
            if (!OperatingSystem.IsAndroid())
                return true;

            var applied = __originalMethod.Name switch
            {
                "get_VisualsPath" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "VisualsPath",
                    ref __result,
                    "CustomVisualsPath",
                    "CustomVisualPath"),
                "get_AttackSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "AttackSfx",
                    ref __result,
                    "CustomAttackSfx"),
                "get_CastSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    "CastSfx",
                    ref __result,
                    "CustomCastSfx"),
                "get_DeathSfx" => AndroidBaseLibPropertyCompatHelper.TryOverrideString(
                    __instance,
                    nameof(MonsterModel.DeathSfx),
                    ref __result,
                    "CustomDeathSfx"),
                _ => false,
            };

            return !applied;
        }
    }
}
