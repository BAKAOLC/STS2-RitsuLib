using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal static class CharacterAssetOverridePatchHelper
    {
        internal static bool TryUseOverride(
            CharacterModel instance,
            // ReSharper disable once InconsistentNaming
            ref string __result,
            Func<IModCharacterAssetOverrides, string?> selector,
            string memberName,
            bool requireExistingResource = true)
        {
            if (instance is not IModCharacterAssetOverrides overrides)
                return true;

            var overrideValue = selector(overrides);
            if (string.IsNullOrWhiteSpace(overrideValue))
                return true;

            if (requireExistingResource && !ResourceLoader.Exists(overrideValue))
            {
                AssetPathDiagnostics.WarnModCharacterAssetOverrideMissing(instance, memberName, overrideValue);
                return true;
            }

            __result = overrideValue;
            return false;
        }
    }

    public class CharacterIconOutlineTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_outline_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconOutlineTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_IconOutlineTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomIconOutlineTexturePath,
                nameof(IModCharacterAssetOverrides.CustomIconOutlineTexturePath));
        }
    }

    public class CharacterVisualsPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_visuals_path";
        public static string Description => "Allow mod characters to override CharacterModel.VisualsPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_VisualsPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomVisualsPath,
                nameof(IModCharacterAssetOverrides.CustomVisualsPath));
        }
    }

    public class CharacterEnergyCounterPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_energy_counter_path";
        public static string Description => "Allow mod characters to override CharacterModel.EnergyCounterPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_EnergyCounterPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomEnergyCounterPath,
                nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath));
        }
    }

    public class CharacterMerchantAnimPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_merchant_anim_path";
        public static string Description => "Allow mod characters to override CharacterModel.MerchantAnimPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_MerchantAnimPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomMerchantAnimPath,
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath));
        }
    }

    public class CharacterRestSiteAnimPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_rest_site_anim_path";
        public static string Description => "Allow mod characters to override CharacterModel.RestSiteAnimPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_RestSiteAnimPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomRestSiteAnimPath,
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath));
        }
    }

    public class CharacterIconTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_IconTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomIconTexturePath,
                nameof(IModCharacterAssetOverrides.CustomIconTexturePath));
        }
    }

    public class CharacterIconPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_IconPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomIconPath,
                nameof(IModCharacterAssetOverrides.CustomIconPath));
        }
    }

    public class CharacterSelectBgPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_select_bg_path";
        public static string Description => "Allow mod characters to override CharacterModel.CharacterSelectBg";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_CharacterSelectBg")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomCharacterSelectBgPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectBgPath));
        }
    }

    public class CharacterSelectTransitionPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_transition_path";

        public static string Description =>
            "Allow mod characters to override CharacterModel.CharacterSelectTransitionPath";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_CharacterSelectTransitionPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomCharacterSelectTransitionPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectTransitionPath));
        }
    }

    public class CharacterTrailPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_trail_path";
        public static string Description => "Allow mod characters to override CharacterModel.TrailPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_TrailPath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomTrailPath,
                nameof(IModCharacterAssetOverrides.CustomTrailPath));
        }
    }

    public class CharacterAttackSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_attack_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.AttackSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_AttackSfx")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomAttackSfx,
                nameof(IModCharacterAssetOverrides.CustomAttackSfx),
                false);
        }
    }

    public class CharacterCastSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_cast_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.CastSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_CastSfx")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomCastSfx,
                nameof(IModCharacterAssetOverrides.CustomCastSfx),
                false);
        }
    }

    public class CharacterDeathSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_death_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.DeathSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_DeathSfx")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomDeathSfx,
                nameof(IModCharacterAssetOverrides.CustomDeathSfx),
                false);
        }
    }

    public class CharacterArmPointingTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_pointing_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmPointingTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_ArmPointingTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPointingTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPointingTexturePath));
        }
    }

    public class CharacterArmRockTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_rock_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmRockTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_ArmRockTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmRockTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmRockTexturePath));
        }
    }

    public class CharacterArmPaperTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_paper_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmPaperTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_ArmPaperTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPaperTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPaperTexturePath));
        }
    }

    public class CharacterArmScissorsTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_scissors_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmScissorsTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "get_ArmScissorsTexturePath")];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmScissorsTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmScissorsTexturePath));
        }
    }
}
