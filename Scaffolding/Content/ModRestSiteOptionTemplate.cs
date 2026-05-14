using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional icon path for mod rest site options consumed by asset override patches on
    ///     <see cref="RestSiteOption" />.
    ///     Mod 休息点选项的可选图标路径，供 <see cref="RestSiteOption" /> 上的资源覆盖补丁读取。
    /// </summary>
    public interface IModRestSiteOptionAssetOverrides
    {
        /// <summary>
        ///     Structured path bundle; <c>Custom*</c> properties typically mirror these fields.
        ///     结构化路径集合；<c>Custom*</c> 属性通常镜像这些字段。
        /// </summary>
        RestSiteOptionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override path for the rest site option icon texture.
        ///     休息点选项图标贴图的覆盖路径。
        /// </summary>
        string? CustomIconPath { get; }
    }

    /// <summary>
    ///     Marker interface for rest site options whose <see cref="RestSiteOption.Title" /> should be replaced by
    ///     <see cref="CustomTitle" /> (patched at runtime because the base property is non-virtual).
    ///     标记接口：用于声明某个休息点选项的 <see cref="RestSiteOption.Title" /> 应由 <see cref="CustomTitle" /> 替换
    ///     （由于基类属性非 virtual，因此会在运行时打补丁）。
    /// </summary>
    public interface IModRestSiteOptionCustomTitle
    {
        /// <summary>
        ///     When non-null, replaces the vanilla <see cref="RestSiteOption.Title" /> returned to callers (button label,
        ///     description panel, etc.).
        ///     非 null 时，替换返回给调用方的原版 <see cref="RestSiteOption.Title" />（按钮标签、描述面板等）。
        /// </summary>
        LocString? CustomTitle { get; }
    }

    /// <summary>
    ///     Base <see cref="RestSiteOption" /> for mods: custom icon via <see cref="IModRestSiteOptionAssetOverrides" />,
    ///     custom title via <see cref="IModRestSiteOptionCustomTitle" />, and overrideable description. Register the
    ///     option by adding it inside an <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions" />
    ///     override on a relic, card, or modifier.
    ///     Mod 休息点选项的基础 <see cref="RestSiteOption" />：通过 <see cref="IModRestSiteOptionAssetOverrides" />
    ///     提供自定义图标，通过 <see cref="IModRestSiteOptionCustomTitle" /> 提供自定义标题，并允许重写描述。
    ///     请在遗物、卡牌或 modifier 的 <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions" />
    ///     重写中添加该选项来完成注册。
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
    ///         <see cref="IModRestSiteOptionAssetOverrides" />。
    ///     </para>
    ///     <para>
    ///         <see cref="RestSiteOption.IsEnabled" /> 默认为 <c>true</c>。当选项需要按条件置灰时，请在子类构造函数中设置它，模式与原版
    ///         <c>SmithRestSiteOption</c> / <c>CookRestSiteOption</c> 相同。
    ///     </para>
    ///     <para>
    ///         由于 <see cref="RestSiteOption.Title" /> 和 <see cref="RestSiteOption.Icon" /> 非 virtual，RitsuLib 会在运行时补丁它们的
    ///         getter，以支持 <see cref="IModRestSiteOptionCustomTitle" /> 和 <see cref="IModRestSiteOptionAssetOverrides" />。
    ///         <see cref="IModRestSiteOptionAssetOverrides" />。
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
        ///     <see cref="LocString" />。
        ///     非 null 时，替换原版 <see cref="RestSiteOption.Title" />（它来自 <c>LocString("rest_site_ui", "OPTION_{OptionId}.name")</c>
        ///     ）。重写它可从自定义本地化表提供 mod 专属 <see cref="LocString" />。
        ///     <see cref="LocString" />。
        /// </summary>
        public virtual LocString? CustomTitle => null;
    }
}
