using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    internal static class CharacterAssetOverridePatchHelper
    {
        internal static bool TryUseOverride(
            CharacterModel instance,
            ref string __result,
            Func<IModCharacterAssetOverrides, string?> selector,
            string memberName,
            bool requireExistingResource = true)
        {
            var overrideValue = ResolveOverride(instance, selector, memberName);
            if (string.IsNullOrWhiteSpace(overrideValue))
                return true;

            if (requireExistingResource && !IsCompatibleResource(instance, memberName, overrideValue)) return true;

            __result = overrideValue;
            return false;
        }

        internal static bool TryResolveOverridePath(
            CharacterModel instance,
            Func<IModCharacterAssetOverrides, string?> selector,
            string memberName,
            out string path)
        {
            path = ResolveOverride(instance, selector, memberName) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(path);
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

        private static bool IsCompatibleResource(CharacterModel instance, string memberName, string path)
        {
            // ReSharper disable once InvertIf
            if (!GodotResourcePath.ResourceExists(path))
            {
                AssetPathDiagnostics.WarnModCharacterAssetOverrideMissing(instance, memberName, path);
                return false;
            }

            return memberName switch
            {
                nameof(IModCharacterAssetOverrides.CustomVisualsPath) =>
                    IsLoadableAsAny(instance, memberName, path, nameof(PackedScene), typeof(PackedScene),
                        typeof(Texture2D)),
                nameof(IModCharacterAssetOverrides.CustomIconPath) =>
                    IsLoadableAsAny(instance, memberName, path, nameof(PackedScene), typeof(PackedScene),
                        typeof(Texture2D)),
                nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath) =>
                    IsLoadable<PackedScene>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath) =>
                    IsLoadableAsAny(instance, memberName, path, nameof(PackedScene), typeof(PackedScene),
                        typeof(Texture2D)),
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath) =>
                    IsLoadableAsAny(instance, memberName, path, nameof(PackedScene), typeof(PackedScene),
                        typeof(Texture2D)),
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectBgPath) =>
                    IsLoadableAsAny(instance, memberName, path, nameof(PackedScene), typeof(PackedScene),
                        typeof(Texture2D)),
                nameof(IModCharacterAssetOverrides.CustomTrailPath) =>
                    IsLoadable<PackedScene>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomIconTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomIconOutlineTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectIconPath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectLockedIconPath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomMapMarkerPath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomArmPointingTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomArmRockTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomArmPaperTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomArmScissorsTexturePath) =>
                    IsLoadable<Texture2D>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectTransitionPath) =>
                    IsLoadable<Material>(instance, memberName, path),
                nameof(IModCharacterAssetOverrides.CustomCombatSpineSkeletonDataPath) =>
                    IsLoadable<Resource>(instance, memberName, path),
                _ => true,
            };
        }

        private static bool IsLoadable<T>(CharacterModel instance, string memberName, string path)
            where T : class
        {
            if (GodotResourcePath.TryLoad<T>(path, out _))
                return true;

            ContentAssetOverridePatchHelper.LogLoadFailure(instance, memberName, path, typeof(T).Name);
            return false;
        }

        private static bool IsLoadableAsAny(CharacterModel instance, string memberName, string path,
            string expectedTypeLabel, params Type[] allowedTypes)
        {
            if ((from candidate in GodotResourcePath.EnumerateCandidatePaths(path)
                    where ResourceLoader.Exists(candidate)
                    select ResourceLoader.Load(candidate))
                .Any(resource => allowedTypes.Any(type => type.IsInstanceOfType(resource)))) return true;

            ContentAssetOverridePatchHelper.LogLoadFailure(instance, memberName, path,
                $"{expectedTypeLabel} or {nameof(Texture2D)}");
            return false;
        }

        private static bool TryResolveRegisteredProfile(CharacterModel instance, out CharacterAssetProfile profile)
        {
            return ModContentRegistry.TryGetEffectiveCharacterAssetReplacement(instance.Id.Entry, out profile);
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconOutlineTexturePath" /> so <see cref="IModCharacterAssetOverrides" />
    ///     can supply a custom outline texture path.
    ///     patch <see cref="CharacterModel.IconOutlineTexturePath" />，使 <see cref="IModCharacterAssetOverrides" />
    ///     可以提供自定义描边纹理路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterIconOutlineTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_outline_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconOutlineTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.VisualsPath" />，用于自定义 mod 角色场景路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterVisualsPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_visuals_path";
        public static string Description => "Allow mod characters to override CharacterModel.VisualsPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.VisualsPath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.EnergyCounterPath" />，用于 mod 角色 UI 资产。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterEnergyCounterPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_energy_counter_path";
        public static string Description => "Allow mod characters to override CharacterModel.EnergyCounterPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.EnergyCounterPath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomEnergyCounterPath,
                nameof(IModCharacterAssetOverrides.CustomEnergyCounterPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.MerchantAnimPath" /> for merchant-room animations.
    ///     patch <see cref="CharacterModel.MerchantAnimPath" />，用于商人房间动画。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterMerchantAnimPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_merchant_anim_path";
        public static string Description => "Allow mod characters to override CharacterModel.MerchantAnimPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.MerchantAnimPath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomMerchantAnimPath,
                nameof(IModCharacterAssetOverrides.CustomMerchantAnimPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.RestSiteAnimPath" /> for rest-site animations.
    ///     patch <see cref="CharacterModel.RestSiteAnimPath" />，用于营火动画。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterRestSiteAnimPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_rest_site_anim_path";
        public static string Description => "Allow mod characters to override CharacterModel.RestSiteAnimPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.RestSiteAnimPath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomRestSiteAnimPath,
                nameof(IModCharacterAssetOverrides.CustomRestSiteAnimPath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconTexturePath" /> for mod character UI icon textures.
    ///     patch <see cref="CharacterModel.IconTexturePath" />，用于 mod 角色 UI 图标纹理。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterIconTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.IconTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomIconTexturePath,
                nameof(IModCharacterAssetOverrides.CustomIconTexturePath));
        }
    }

    /// <summary>
    ///     Patches <see cref="CharacterModel.IconPath" /> for compact mod character icons.
    ///     patch <see cref="CharacterModel.IconPath" />，用于紧凑 mod 角色图标。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterIconPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_icon_path";
        public static string Description => "Allow mod characters to override CharacterModel.IconPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "IconPath", MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch 角色选择背景路径，使 mod 可以替换 <c>CharacterSelectBg</c>。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterSelectBgPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_select_bg_path";
        public static string Description => "Allow mod characters to override CharacterModel.CharacterSelectBg";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectBg), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomCharacterSelectBgPath,
                nameof(IModCharacterAssetOverrides.CustomCharacterSelectBgPath));
        }
    }

    /// <summary>
    ///     Patches non-public <see cref="CharacterModel.CharacterSelectIcon" /> path getter.
    ///     patch 非 public 的 <see cref="CharacterModel.CharacterSelectIcon" /> 路径 getter。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterSelectIconPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_select_icon_path";
        public static string Description => "Allow character-select icon path override for vanilla and mod characters";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "CharacterSelectIconPath", null, true, MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch 非 public 的 <see cref="CharacterModel.CharacterSelectLockedIcon" /> 路径 getter。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterSelectLockedIconPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_select_locked_icon_path";

        public static string Description =>
            "Allow character-select locked icon path override for vanilla and mod characters";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "CharacterSelectLockedIconPath", null, true, MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch 非 public 的 <see cref="CharacterModel.MapMarker" /> 路径 getter。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterMapMarkerPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_map_marker_path";
        public static string Description => "Allow character map-marker path override for vanilla and mod characters";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), "MapMarkerPath", null, true, MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.CharacterSelectTransitionPath" />，用于自定义选择界面转场。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterSelectTransitionPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_transition_path";

        public static string Description =>
            "Allow mod characters to override CharacterModel.CharacterSelectTransitionPath";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectTransitionPath), MethodType.Getter),
            ];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.TrailPath" />，用于卡牌 trail VFX 场景。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterTrailPathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_trail_path";
        public static string Description => "Allow mod characters to override CharacterModel.TrailPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.TrailPath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.AttackSfx" />；不要求 FMOD 路径作为 Godot 资源存在。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterAttackSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_attack_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.AttackSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.CastSfx" />，用于自定义施放音频。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterCastSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_cast_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.CastSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch <see cref="CharacterModel.DeathSfx" />，用于自定义死亡音频。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterDeathSfxPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_death_sfx";
        public static string Description => "Allow mod characters to override CharacterModel.DeathSfx";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.DeathSfx), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
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
    ///     patch 多人模式指向姿势的手臂纹理路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterArmPointingTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_pointing_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmPointingTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPointingTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPointingTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “rock” arm texture path.
    ///     patch 多人模式 RPS “rock” 手臂纹理路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterArmRockTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_rock_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmRockTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmRockTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmRockTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “paper” arm texture path.
    ///     patch 多人模式 RPS “paper” 手臂纹理路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterArmPaperTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_paper_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmPaperTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmPaperTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmPaperTexturePath));
        }
    }

    /// <summary>
    ///     Patches multiplayer RPS “scissors” arm texture path.
    ///     patch 多人模式 RPS “scissors” 手臂纹理路径。
    /// </summary>
    [HarmonyAfter(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.Last)]
    internal class CharacterArmScissorsTexturePathPatch : IPatchMethod
    {
        public static string PatchId => "character_asset_override_arm_scissors_texture_path";
        public static string Description => "Allow mod characters to override CharacterModel.ArmScissorsTexturePath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexturePath), MethodType.Getter)];
        }

        public static bool Prefix(CharacterModel __instance, ref string __result)
        {
            return CharacterAssetOverridePatchHelper.TryUseOverride(__instance, ref __result,
                o => o.CustomArmScissorsTexturePath,
                nameof(IModCharacterAssetOverrides.CustomArmScissorsTexturePath));
        }
    }
}
