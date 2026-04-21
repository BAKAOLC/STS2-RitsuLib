using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional icon path for mod rest site options consumed by asset override patches on
    ///     <see cref="RestSiteOption" />.
    /// </summary>
    public interface IModRestSiteOptionAssetOverrides
    {
        /// <summary>
        ///     Structured path bundle; <c>Custom*</c> properties typically mirror these fields.
        /// </summary>
        RestSiteOptionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override path for the rest site option icon texture.
        /// </summary>
        string? CustomIconPath { get; }
    }

    /// <summary>
    ///     Marker interface for rest site options whose <see cref="RestSiteOption.Title" /> should be replaced by
    ///     <see cref="CustomTitle" /> (patched at runtime because the base property is non-virtual).
    /// </summary>
    public interface IModRestSiteOptionCustomTitle
    {
        /// <summary>
        ///     When non-null, replaces the vanilla <see cref="RestSiteOption.Title" /> returned to callers (button label,
        ///     description panel, etc.).
        /// </summary>
        LocString? CustomTitle { get; }
    }

    /// <summary>
    ///     Base <see cref="RestSiteOption" /> for mods: custom icon via <see cref="IModRestSiteOptionAssetOverrides" />,
    ///     custom title via <see cref="IModRestSiteOptionCustomTitle" />, and overrideable description. Register the
    ///     option by adding it inside an <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions" />
    ///     override on a relic, card, or modifier.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="RestSiteOption.IsEnabled" /> defaults to <c>true</c>. Set it in the subclass constructor when
    ///         the option should be conditionally grayed out, following the same pattern as vanilla
    ///         <c>SmithRestSiteOption</c> / <c>CookRestSiteOption</c>.
    ///     </para>
    ///     <para>
    ///         Because <see cref="RestSiteOption.Title" /> and <see cref="RestSiteOption.Icon" /> are non-virtual,
    ///         RitsuLib patches their getters at runtime to respect <see cref="IModRestSiteOptionCustomTitle" /> and
    ///         <see cref="IModRestSiteOptionAssetOverrides" />.
    ///     </para>
    /// </remarks>
    public abstract class ModRestSiteOptionTemplate(Player owner)
        : RestSiteOption(owner), IModRestSiteOptionAssetOverrides, IModRestSiteOptionCustomTitle
    {
        /// <inheritdoc />
        public override IEnumerable<string> AssetPaths
        {
            get
            {
                var iconPath = CustomIconPath;
                return iconPath is not null ? [iconPath] : base.AssetPaths;
            }
        }

        /// <inheritdoc />
        public virtual RestSiteOptionAssetProfile AssetProfile => RestSiteOptionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <summary>
        ///     When non-null, replaces the vanilla <see cref="RestSiteOption.Title" /> (which derives from
        ///     <c>LocString("rest_site_ui", "OPTION_{OptionId}.name")</c>). Override to supply a mod-specific
        ///     <see cref="LocString" /> from a custom localization table.
        /// </summary>
        public virtual LocString? CustomTitle => null;
    }
}
