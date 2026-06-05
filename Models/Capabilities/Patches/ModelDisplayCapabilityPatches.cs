using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Bridges generic model display capabilities into non-card model families.
    ///     将通用模型展示能力桥接到非卡牌模型族。
    /// </summary>
    internal static class ModelDisplayCapabilityPatches
    {
        private static void AppendHoverTips<TModel>(TModel model, ref IEnumerable<IHoverTip> result)
            where TModel : AbstractModel
        {
            var tips = ModelCapabilityHost.GetHoverTips(model).ToArray();
            if (tips.Length == 0)
                return;

            result = result.Concat(tips).Distinct().ToArray();
        }

        private static void AppendAssetPaths<TModel>(
            TModel model,
            ModelAssetPathContext context,
            ref IEnumerable<string> result)
            where TModel : AbstractModel
        {
            var paths = ModelCapabilityHost.GetAssetPaths(model, context)
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .ToArray();

            if (paths.Length == 0)
                return;

            result = result.Concat(paths).Distinct(StringComparer.Ordinal).ToArray();
        }

        private static void AddDynamicVars<TModel>(TModel model, LocString? locString)
            where TModel : AbstractModel
        {
            if (locString != null)
                ModelCapabilityHost.AddDynamicVarsTo(model, locString);
        }

        internal sealed class RelicDynamicDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_relic_dynamic_description";

            public static string Description => "Add model-capability dynamic vars to relic dynamic descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RelicModel), "DynamicDescription", MethodType.Getter)];
            }

            public static void Postfix(RelicModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class RelicDynamicEventDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_relic_dynamic_event_description";

            public static string Description => "Add model-capability dynamic vars to relic dynamic event descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RelicModel), "DynamicEventDescription", MethodType.Getter)];
            }

            public static void Postfix(RelicModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class PotionDynamicDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_potion_dynamic_description";

            public static string Description => "Add model-capability dynamic vars to potion dynamic descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PotionModel), "DynamicDescription", MethodType.Getter)];
            }

            public static void Postfix(PotionModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class EnchantmentDynamicDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_enchantment_dynamic_description";

            public static string Description => "Add model-capability dynamic vars to enchantment dynamic descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EnchantmentModel), "DynamicDescription", MethodType.Getter)];
            }

            public static void Postfix(EnchantmentModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class EnchantmentDynamicExtraCardTextPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_enchantment_dynamic_extra_card_text";

            public static string Description => "Add model-capability dynamic vars to enchantment extra card text";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EnchantmentModel), "DynamicExtraCardText", MethodType.Getter)];
            }

            public static void Postfix(EnchantmentModel __instance, ref LocString? __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class AfflictionDynamicDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_affliction_dynamic_description";

            public static string Description => "Add model-capability dynamic vars to affliction dynamic descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(AfflictionModel), "DynamicDescription", MethodType.Getter)];
            }

            public static void Postfix(AfflictionModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class AfflictionDynamicExtraCardTextPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_affliction_dynamic_extra_card_text";

            public static string Description => "Add model-capability dynamic vars to affliction extra card text";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(AfflictionModel), "DynamicExtraCardText", MethodType.Getter)];
            }

            public static void Postfix(AfflictionModel __instance, ref LocString? __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class PowerDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_power_description";

            public static string Description => "Add model-capability dynamic vars to power descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PowerModel), "Description", MethodType.Getter)];
            }

            public static void Postfix(PowerModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class PowerSmartDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_power_smart_description";

            public static string Description => "Add model-capability dynamic vars to power smart descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PowerModel), "SmartDescription", MethodType.Getter)];
            }

            public static void Postfix(PowerModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class PowerRemoteDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_power_remote_description";

            public static string Description => "Add model-capability dynamic vars to power remote descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PowerModel), "RemoteDescription", MethodType.Getter)];
            }

            public static void Postfix(PowerModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class OrbDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_orb_description";

            public static string Description => "Add model-capability dynamic vars to orb descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbModel), "Description", MethodType.Getter)];
            }

            public static void Postfix(OrbModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class OrbSmartDescriptionPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_orb_smart_description";

            public static string Description => "Add model-capability dynamic vars to orb smart descriptions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbModel), "SmartDescription", MethodType.Getter)];
            }

            public static void Postfix(OrbModel __instance, ref LocString __result)
            {
                AddDynamicVars(__instance, __result);
            }
        }

        internal sealed class RelicHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_relic_hover_tips";

            public static string Description => "Append model-capability hover tips to relics";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RelicModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class RelicHoverTipsExcludingRelicPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_relic_hover_tips_excluding_relic";

            public static string Description => "Append model-capability hover tips to relic secondary tips";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(RelicModel), "HoverTipsExcludingRelic", MethodType.Getter)];
            }

            public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class PowerHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_power_hover_tips";

            public static string Description => "Append model-capability hover tips to powers";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PowerModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class OrbHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_orb_hover_tips";

            public static string Description => "Append model-capability hover tips to orbs";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(OrbModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                ModelCapabilityHost.ApplyOrbHoverTipDescriptionFragments(__instance, ref __result);
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class PotionHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_potion_hover_tips";

            public static string Description => "Append model-capability hover tips to potions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(PotionModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class AfflictionHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_affliction_hover_tips";

            public static string Description => "Append model-capability hover tips to afflictions";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(AfflictionModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(AfflictionModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class EnchantmentHoverTipsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_enchantment_hover_tips";

            public static string Description => "Append model-capability hover tips to enchantments";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EnchantmentModel), "HoverTips", MethodType.Getter)];
            }

            public static void Postfix(EnchantmentModel __instance, ref IEnumerable<IHoverTip> __result)
            {
                AppendHoverTips(__instance, ref __result);
            }
        }

        internal sealed class CardRunAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_card_run_asset_paths";

            public static string Description => "Append model-capability run asset paths to cards";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CardModel), "RunAssetPaths", MethodType.Getter)];
            }

            public static void Postfix(CardModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Run), ref __result);
            }
        }

        internal sealed class CharacterAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_character_asset_paths";

            public static string Description => "Append model-capability asset paths to characters";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CharacterModel), "AssetPaths", MethodType.Getter)];
            }

            public static void Postfix(CharacterModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.General), ref __result);
            }
        }

        internal sealed class CharacterSelectAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_character_select_asset_paths";

            public static string Description => "Append model-capability character-select asset paths to characters";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(CharacterModel), "AssetPathsCharacterSelect", MethodType.Getter)];
            }

            public static void Postfix(CharacterModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.CharacterSelect), ref __result);
            }
        }

        internal sealed class OrbAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_orb_asset_paths";

            public static string Description => "Append model-capability asset paths to orbs";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbModel), "AssetPaths", MethodType.Getter)];
            }

            public static void Postfix(OrbModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Combat), ref __result);
            }
        }

        internal sealed class MonsterAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_monster_asset_paths";

            public static string Description => "Append model-capability asset paths to monsters";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(MonsterModel), "AssetPaths", MethodType.Getter)];
            }

            public static void Postfix(MonsterModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Combat), ref __result);
            }
        }

        internal sealed class ActAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_act_asset_paths";

            public static string Description => "Append model-capability asset paths to acts";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(ActModel), "AssetPaths", MethodType.Getter)];
            }

            public static void Postfix(ActModel __instance, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Map), ref __result);
            }
        }

        internal sealed class EncounterAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_encounter_asset_paths";

            public static string Description => "Append model-capability asset paths to encounters";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EncounterModel), nameof(EncounterModel.GetAssetPaths), [typeof(IRunState)])];
            }

            public static void Postfix(
                EncounterModel __instance,
                IRunState runState,
                ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Combat, runState), ref __result);
            }
        }

        internal sealed class EventAssetPathsPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_capability_event_asset_paths";

            public static string Description => "Append model-capability asset paths to events";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(EventModel), nameof(EventModel.GetAssetPaths), [typeof(IRunState)])];
            }

            public static void Postfix(EventModel __instance, IRunState runState, ref IEnumerable<string> __result)
            {
                AppendAssetPaths(__instance, new(__instance, ModelAssetPathScope.Run, runState), ref __result);
            }
        }
    }
}
