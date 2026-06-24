using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Patches <see cref="RestSiteOption.Icon" /> to load a custom texture when the option implements
    ///     <see cref="IModRestSiteOptionAssetOverrides" />.
    ///     修补 <see cref="RestSiteOption.Icon" />，当选项实现
    ///     <see cref="IModRestSiteOptionAssetOverrides" /> 时加载自定义纹理。
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
    ///     Patches <see cref="RestSiteOption.Title" /> to return a custom <see cref="LocString" /> when the option
    ///     implements <see cref="IModRestSiteOptionCustomTitle" />.
    ///     implements <c>IModRestSiteOptionCustomTitle</c>.
    ///     修补 <see cref="RestSiteOption.Title" />，当选项
    ///     实现 <see cref="IModRestSiteOptionCustomTitle" /> 时返回自定义 <see cref="LocString" />。
    ///     实现 <c>IModRestSiteOptionCustomTitle</c>。
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
