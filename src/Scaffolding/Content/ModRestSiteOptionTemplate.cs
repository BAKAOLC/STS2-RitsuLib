using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides an optional icon replacement for a mod rest-site option.</para>
    ///     <para xml:lang="zh-CN">提供模组休息处选项的可选图标替换。</para>
    /// </summary>
    public interface IModRestSiteOptionAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the structured rest-site option asset profile.</para>
        ///     <para xml:lang="zh-CN">获取结构化休息处选项资源配置。</para>
        /// </summary>
        RestSiteOptionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the rest-site option icon replacement path.</para>
        ///     <para xml:lang="zh-CN">获取休息处选项图标替换路径。</para>
        /// </summary>
        string? CustomIconPath { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Marks a rest-site option whose non-virtual <see cref="RestSiteOption.Title" /> getter should return
    ///         <see cref="CustomTitle" /> when one is supplied.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         标记在提供 <see cref="CustomTitle" /> 时，应由非虚
    ///         <see cref="RestSiteOption.Title" /> 属性返回该标题的休息处选项。
    ///     </para>
    /// </summary>
    public interface IModRestSiteOptionCustomTitle
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the optional title returned in place of <see cref="RestSiteOption.Title" />.</para>
        ///     <para xml:lang="zh-CN">获取用于替代 <see cref="RestSiteOption.Title" /> 的可选标题。</para>
        /// </summary>
        LocString? CustomTitle { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="RestSiteOption" /> for mods with optional icon and title replacements.
    ///         Add the option from an <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions" />
    ///         override.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="RestSiteOption" />，支持可选的图标和标题替换。请从
    ///         <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions" /> 重写中添加此选项。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         <see cref="RestSiteOption.IsEnabled" /> defaults to <c>true</c>. Set it in the subclass constructor when
    ///         the option should be conditionally grayed out, following the same pattern as vanilla
    ///         <c>SmithRestSiteOption</c> / <c>CookRestSiteOption</c>.
    ///     </para>
    ///     <para xml:lang="en">
    ///         Because <see cref="RestSiteOption.Title" /> and <see cref="RestSiteOption.Icon" /> are non-virtual,
    ///         RitsuLib patches their getters at runtime to respect <see cref="IModRestSiteOptionCustomTitle" /> and
    ///         <see cref="IModRestSiteOptionAssetOverrides" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="RestSiteOption.IsEnabled" /> 默认为 <c>true</c>。当选项需要按条件置灰时，请在子类构造函数中设置它，模式与原版
    ///         <c>SmithRestSiteOption</c> / <c>CookRestSiteOption</c> 相同。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         由于 <see cref="RestSiteOption.Title" /> 和 <see cref="RestSiteOption.Icon" /> 非 virtual，RitsuLib 会在运行时补丁它们的
    ///         属性，因此 RitsuLib 会在运行时补丁处理其 getter，以支持
    ///         <see cref="IModRestSiteOptionCustomTitle" /> 和 <see cref="IModRestSiteOptionAssetOverrides" />。
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
        ///     <para xml:lang="en">
        ///         Gets the optional title returned instead of the base
        ///         <c>LocString("rest_site_ui", "OPTION_{OptionId}.name")</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用于替代游戏本体 <c>LocString("rest_site_ui", "OPTION_{OptionId}.name")</c> 的可选标题。
        ///     </para>
        /// </summary>
        public virtual LocString? CustomTitle => null;
    }
}
