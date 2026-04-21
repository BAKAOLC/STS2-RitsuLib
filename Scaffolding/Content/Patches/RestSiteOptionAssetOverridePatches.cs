using Godot;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Patches <see cref="RestSiteOption.Icon" /> to load a custom texture when the option implements
    ///     <see cref="IModRestSiteOptionAssetOverrides" />.
    /// </summary>
    public class RestSiteOptionIconPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_rest_site_option_icon";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod rest site options to override icon texture";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RestSiteOption), "get_Icon")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Loads the texture from <see cref="IModRestSiteOptionAssetOverrides.CustomIconPath" /> when present.
        /// </summary>
        public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
            // ReSharper restore InconsistentNaming
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
    /// </summary>
    public class RestSiteOptionTitlePatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "content_asset_override_rest_site_option_title";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description => "Allow mod rest site options to override title";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RestSiteOption), "get_Title")];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Returns <see cref="IModRestSiteOptionCustomTitle.CustomTitle" /> when non-null.
        /// </summary>
        public static bool Prefix(RestSiteOption __instance, ref LocString __result)
            // ReSharper restore InconsistentNaming
        {
            if (__instance is not IModRestSiteOptionCustomTitle { CustomTitle: { } customTitle })
                return true;

            __result = customTitle;
            return false;
        }
    }
}
