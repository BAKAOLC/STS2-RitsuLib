using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Optional icon path overrides for <see cref="ModifierModel" />.
    ///     <see cref="ModifierModel" /> 的可选图标路径覆盖。
    /// </summary>
    public interface IModModifierAssetOverrides
    {
        /// <summary>
        ///     Path bundle; default is empty.
        ///     路径包；默认为空。
        /// </summary>
        ModifierAssetProfile AssetProfile => ModifierAssetProfile.Empty;

        /// <summary>
        ///     Icon path override for custom run and daily-run UI.
        ///     自定义 run 与每日挑战 UI 的图标路径覆盖。
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     Patches <see cref="ModifierModel" /> icon path for <see cref="IModModifierAssetOverrides" />.
    ///     为 <see cref="IModModifierAssetOverrides" /> 修补 <see cref="ModifierModel" /> 图标路径。
    /// </summary>
    public sealed class ModifierIconPathPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "content_asset_override_modifier_icon_path";

        /// <inheritdoc />
        public static string Description => "Allow mod modifiers to override IconPath";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModifierModel), "IconPath", MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Supplies <see cref="IModModifierAssetOverrides.CustomIconPath" /> when the resource exists.
        ///     当资源存在时提供 <see cref="IModModifierAssetOverrides.CustomIconPath" />。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(ModifierModel __instance, ref string __result)
            //Resharper restore InconsistentNaming
        {
            if (!ContentAssetOverridePatchHelper.TryUseExternalPathOverride(
                    __instance,
                    ref __result,
                    () => ExternalAssetOverrideRegistry.TryGetModifierIconPath(__instance, out var path)
                        ? path
                        : null,
                    "ExternalAssetOverrideRegistry.ModifierIconPath"))
                return false;

            return ContentAssetOverridePatchHelper.TryUseStringOverride<IModModifierAssetOverrides>(
                __instance,
                ref __result,
                static o => o.CustomIconPath,
                nameof(IModModifierAssetOverrides.CustomIconPath));
        }
    }
}
