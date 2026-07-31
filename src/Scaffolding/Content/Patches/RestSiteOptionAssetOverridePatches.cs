using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Patches <see cref="RestSiteOption.Icon" /> to load a custom texture when the option implements
    ///         <see cref="IModRestSiteOptionAssetOverrides" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         修补 <see cref="RestSiteOption.Icon" />，使实现 <see cref="IModRestSiteOptionAssetOverrides" /> 的选项
    ///         能够加载自定义纹理。
    ///     </para>
    /// </summary>
    internal class RestSiteOptionIconPatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_rest_site_option_icon";
        public static string Description => "Allow mod rest site options to override icon texture";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RestSiteOption), "Icon", MethodType.Getter)];
        }

        public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
        {
            return ContentAssetOverridePatchHelper.TryUseTextureOverride<IModRestSiteOptionAssetOverrides>(
                __instance,
                ref __result,
                static o => o.CustomIconPath,
                nameof(IModRestSiteOptionAssetOverrides.CustomIconPath));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Patches <see cref="RestSiteOption.Title" /> so options implementing
    ///         <see cref="IModRestSiteOptionCustomTitle" /> can return a custom <see cref="LocString" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         修补 <see cref="RestSiteOption.Title" />，使实现 <see cref="IModRestSiteOptionCustomTitle" /> 的选项
    ///         能够返回自定义 <see cref="LocString" />。
    ///     </para>
    /// </summary>
    internal class RestSiteOptionTitlePatch : IPatchMethod
    {
        public static string PatchId => "content_asset_override_rest_site_option_title";
        public static string Description => "Allow mod rest site options to override title";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RestSiteOption), "Title", MethodType.Getter)];
        }

        public static bool Prefix(RestSiteOption __instance, ref LocString __result)
        {
            if (__instance is not IModRestSiteOptionCustomTitle { CustomTitle: { } customTitle })
                return true;

            __result = customTitle;
            return false;
        }
    }
}
