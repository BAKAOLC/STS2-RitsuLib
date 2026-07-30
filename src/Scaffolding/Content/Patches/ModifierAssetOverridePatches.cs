using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Defines optional icon-path overrides for <see cref="ModifierModel" />.</para>
    ///     <para xml:lang="zh-CN">定义 <see cref="ModifierModel" /> 的可选图标路径覆盖。</para>
    /// </summary>
    public interface IModModifierAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the modifier asset profile.</para>
        ///     <para xml:lang="zh-CN">获取修饰符资源配置。</para>
        /// </summary>
        ModifierAssetProfile AssetProfile => ModifierAssetProfile.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets the icon-path override used by custom-run and daily-run UI.</para>
        ///     <para xml:lang="zh-CN">获取自定义模式和每日挑战界面使用的图标路径覆盖。</para>
        /// </summary>
        string? CustomIconPath => AssetProfile.IconPath;
    }

    /// <summary>
    ///     <para xml:lang="en">Applies external and interface icon-path overrides to <see cref="ModifierModel" />.</para>
    ///     <para xml:lang="zh-CN">将外部和接口图标路径覆盖应用到 <see cref="ModifierModel" />。</para>
    /// </summary>
    internal sealed class ModifierIconPathPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_modifier_icon_path";
        public static string Description => "Allow mod modifiers to override IconPath";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModifierModel), "IconPath", MethodType.Getter)];
        }

        /// <summary>
        ///     <para xml:lang="en">Applies the first available modifier icon-path override.</para>
        ///     <para xml:lang="zh-CN">应用首个可用的修饰符图标路径覆盖。</para>
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(ModifierModel __instance, ref string __result)
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
