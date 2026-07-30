using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="AfflictionModel" /> for mods with keyword hover tips and an
    ///         <see cref="IModAfflictionAssetOverrides" /> overlay-scene replacement.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="AfflictionModel" />，支持关键词悬浮提示和
    ///         <see cref="IModAfflictionAssetOverrides" /> 覆盖层场景替换。
    ///     </para>
    /// </summary>
    public abstract class ModAfflictionTemplate : AfflictionModel, IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the keyword IDs shown as hover tips for this affliction. These IDs are display-only:
        ///         <see cref="AfflictionModel" /> has no gameplay keyword collection, so each ID is resolved through
        ///         <see cref="ModKeywordRegistry" /> only to create a hover tip. Any gameplay behavior must be
        ///         implemented by the affliction itself.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取显示为此侵蚀悬浮提示的关键词 ID。这些 ID 仅用于显示：<see cref="AfflictionModel" />
        ///         没有参与游戏逻辑的关键词集合，因此每个 ID 只会通过 <see cref="ModKeywordRegistry" /> 解析为
        ///         悬浮提示。任何游戏行为都必须由侵蚀自身实现。
        ///     </para>
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     <para xml:lang="en">Gets additional hover tips included with the keyword-derived tips.</para>
        ///     <para xml:lang="zh-CN">获取与关键词生成的提示一同显示的额外悬浮提示。</para>
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            .. AdditionalHoverTips,
            .. RegisteredKeywordIds.ToHoverTips(),
            .. this.GetModKeywordHoverTips(),
        ];

        /// <inheritdoc />
        public virtual AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }
}
