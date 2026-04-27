using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
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
            var overrideValue = ResolveOverride(instance, selector, memberName);
            if (string.IsNullOrWhiteSpace(overrideValue))
                return true;

            if (requireExistingResource && !GodotResourcePath.ResourceExists(overrideValue))
            {
                AssetPathDiagnostics.WarnModCharacterAssetOverrideMissing(instance, memberName, overrideValue);
                return true;
            }

            __result = overrideValue;
            return false;
        }

        internal static string? ResolveCombatSpineSkeletonDataPath(CharacterModel instance)
        {
            if (TryResolveRegisteredProfile(instance, out var profile) &&
                !string.IsNullOrWhiteSpace(profile.Spine?.CombatSkeletonDataPath))
                return profile.Spine?.CombatSkeletonDataPath;

            if (instance is IModCharacterAssetOverrides overrides &&
                !string.IsNullOrWhiteSpace(overrides.CustomCombatSpineSkeletonDataPath))
                return overrides.CustomCombatSpineSkeletonDataPath;

            return null;
        }

        private static string? ResolveOverride(
            CharacterModel instance,
            Func<IModCharacterAssetOverrides, string?> selector,
            string memberName)
        {
            if (TryResolveRegisteredProfile(instance, out var profile))
            {
                var registered = memberName switch
                {
                    nameof(IModCharacterAssetOverrides.CustomVisualsPath) => profile.Scenes?.VisualsPath,
                    nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath) => profile.Scenes?.EnergyCounterPath,
                    nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath) => profile.Scenes?.MerchantAnimPath,
                    nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath) => profile.Scenes?.RestSiteAnimPath,
                    nameof(IModCharacterAssetOverrides.CustomIconTexturePath) => profile.Ui?.IconTexturePath,
                    nameof(IModCharacterAssetOverrides.CustomIconOutlineTexturePath) => profile.Ui
                        ?.IconOutlineTexturePath,
                    nameof(IModCharacterAssetOverrides.CustomIconPath) => profile.Ui?.IconPath,
                    nameof(IModCharacterAssetOverrides.CustomCharacterSelectBgPath) =>
                        profile.Ui?.CharacterSelectBgPath,
                    nameof(IModCharacterAssetOverrides.CustomCharacterSelectIconPath) =>
                        profile.Ui?.CharacterSelectIconPath,
                    nameof(IModCharacterAssetOverrides.CustomCharacterSelectLockedIconPath) =>
                        profile.Ui?.CharacterSelectLockedIconPath,
                    nameof(IModCharacterAssetOverrides.CustomCharacterSelectTransitionPath) =>
                        profile.Ui?.CharacterSelectTransitionPath,
                    nameof(IModCharacterAssetOverrides.CustomMapMarkerPath) => profile.Ui?.MapMarkerPath,
                    nameof(IModCharacterAssetOverrides.CustomTrailPath) => profile.Vfx?.TrailPath,
                    nameof(IModCharacterAssetOverrides.CustomAttackSfx) => profile.Audio?.AttackSfx,
                    nameof(IModCharacterAssetOverrides.CustomCastSfx) => profile.Audio?.CastSfx,
                    nameof(IModCharacterAssetOverrides.CustomDeathSfx) => profile.Audio?.DeathSfx,
                    nameof(IModCharacterAssetOverrides.CustomArmPointingTexturePath) =>
                        profile.Multiplayer?.ArmPointingTexturePath,
                    nameof(IModCharacterAssetOverrides.CustomArmRockTexturePath) =>
                        profile.Multiplayer?.ArmRockTexturePath,
                    nameof(IModCharacterAssetOverrides.CustomArmPaperTexturePath) =>
                        profile.Multiplayer?.ArmPaperTexturePath,
                    nameof(IModCharacterAssetOverrides.CustomArmScissorsTexturePath) =>
                        profile.Multiplayer?.ArmScissorsTexturePath,
                    _ => null,
                };
                if (!string.IsNullOrWhiteSpace(registered))
                    return registered;
            }

            if (instance is not IModCharacterAssetOverrides overrides) return null;
            var direct = selector(overrides);
            return !string.IsNullOrWhiteSpace(direct) ? direct : null;
        }

        private static bool TryResolveRegisteredProfile(CharacterModel instance, out CharacterAssetProfile profile)
        {
            return ModContentRegistry.TryGetEffectiveCharacterAssetReplacement(instance.Id.Entry, out profile);
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconOutlineTexturePath" /> so <see cref="IModCharacterAssetOverrides" />
    ///     can supply a custom outline texture path.
    /// </summary>
    public class CharacterIconOutlineTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_icon_outline_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.IconOutlineTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     When the instance implements <see cref="IModCharacterAssetOverrides" /> and a valid override path exists,
        ///     replaces the getter result; otherwise runs the original method.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.VisualsPath" /> for custom mod character scene paths.
    /// </summary>
    public class CharacterVisualsPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_visuals_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.VisualsPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.VisualsPath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomVisualsPath" /> when the resource exists.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.EnergyCounterPath" /> for mod character UI assets.
    /// </summary>
    public class CharacterEnergyCounterPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_energy_counter_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.EnergyCounterPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomEnergyCounterPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomEnergyCounterPath,
                nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.MerchantAnimPath" /> for merchant-room animations.
    /// </summary>
    public class CharacterMerchantAnimPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_merchant_anim_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.MerchantAnimPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomMerchantAnimPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomMerchantAnimPath,
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.RestSiteAnimPath" /> for rest-site animations.
    /// </summary>
    public class CharacterRestSiteAnimPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_rest_site_anim_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.RestSiteAnimPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomRestSiteAnimPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomRestSiteAnimPath,
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconTexturePath" /> for mod character UI icon textures.
    /// </summary>
    public class CharacterIconTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_icon_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.IconTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.IconTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomIconTexturePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomIconTexturePath,
                nameof(IModCharacterAssetOverrides.CustomIconTexturePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconPath" /> for compact mod character icons.
    /// </summary>
    public class CharacterIconPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_icon_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.IconPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "IconPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomIconPath" /> when the resource exists.
        /// </summary>
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

    /// <summary>
    ///     Patches character-select background path so mods can replace <c>CharacterSelectBg</c>.
    /// </summary>
    public class CharacterSelectBgPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_select_bg_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.CharacterSelectBg";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomCharacterSelectBgPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomCharacterSelectBgPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectBgPath));
        }
    }

    /// <summary>
    ///     Patches non-public <see cref="CharacterModel.CharacterSelectIcon" /> path getter.
    /// </summary>
    public class CharacterSelectIconPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_select_icon_path";

        /// <inheritdoc />
        public static string Description => "Allow character-select icon path override for vanilla and mod characters";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "CharacterSelectIconPath", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomCharacterSelectIconPath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomCharacterSelectIconPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectIconPath));
        }
    }

    /// <summary>
    ///     Patches non-public <see cref="CharacterModel.CharacterSelectLockedIcon" /> path getter.
    /// </summary>
    public class CharacterSelectLockedIconPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_select_locked_icon_path";

        /// <inheritdoc />
        public static string Description =>
            "Allow character-select locked icon path override for vanilla and mod characters";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "CharacterSelectLockedIconPath", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomCharacterSelectLockedIconPath" /> when valid.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomCharacterSelectLockedIconPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectLockedIconPath));
        }
    }

    /// <summary>
    ///     Patches non-public <see cref="CharacterModel.MapMarker" /> path getter.
    /// </summary>
    public class CharacterMapMarkerPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_map_marker_path";

        /// <inheritdoc />
        public static string Description => "Allow character map-marker path override for vanilla and mod characters";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "MapMarkerPath", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomMapMarkerPath" /> when valid.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(
                __instance,
                ref __result,
                o => o.CustomMapMarkerPath,
                nameof(IModCharacterAssetOverrides.CustomMapMarkerPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.CharacterSelectTransitionPath" /> for custom select-screen transitions.
    /// </summary>
    public class CharacterSelectTransitionPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_transition_path";

        /// <inheritdoc />
        public static string Description =>
            "Allow mod characters to override CharacterModel.CharacterSelectTransitionPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomCharacterSelectTransitionPath" /> when valid.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.TrailPath" /> for card-trail VFX scenes.
    /// </summary>
    public class CharacterTrailPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_trail_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.TrailPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomTrailPath" /> when the resource exists.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.AttackSfx" />; does not require the FMOD path to exist as a Godot resource.
    /// </summary>
    public class CharacterAttackSfxPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_attack_sfx";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.AttackSfx";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomAttackSfx" /> when non-empty.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.CastSfx" /> for custom cast audio.
    /// </summary>
    public class CharacterCastSfxPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_cast_sfx";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.CastSfx";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomCastSfx" /> when non-empty.
        /// </summary>
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

    /// <summary>
    ///     Patches <see cref="CharacterModel.DeathSfx" /> for custom death audio.
    /// </summary>
    public class CharacterDeathSfxPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_death_sfx";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.DeathSfx";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomDeathSfx" /> when non-empty.
        /// </summary>
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

    /// <summary>
    ///     Patches multiplayer arm texture path for the pointing pose.
    /// </summary>
    public class CharacterArmPointingTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_arm_pointing_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.ArmPointingTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomArmPointingTexturePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPointingTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPointingTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “rock” arm texture path.
    /// </summary>
    public class CharacterArmRockTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_arm_rock_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.ArmRockTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomArmRockTexturePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmRockTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmRockTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “paper” arm texture path.
    /// </summary>
    public class CharacterArmPaperTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_arm_paper_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.ArmPaperTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomArmPaperTexturePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPaperTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPaperTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “scissors” arm texture path.
    /// </summary>
    public class CharacterArmScissorsTexturePathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "character_asset_override_arm_scissors_texture_path";

        /// <inheritdoc />
        public static string Description => "Allow mod characters to override CharacterModel.ArmScissorsTexturePath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexturePath), MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModCharacterAssetOverrides.CustomArmScissorsTexturePath" /> when the resource exists.
        /// </summary>
        public static bool Prefix(CharacterModel __instance, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmScissorsTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmScissorsTexturePath));
        }
    }
}
