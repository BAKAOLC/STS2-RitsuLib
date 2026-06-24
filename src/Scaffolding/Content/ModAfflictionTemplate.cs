using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="AfflictionModel" /> for mods: keyword hover tips and <see cref="IModAfflictionAssetOverrides" />
    ///     overlay path.
    ///     Mod 苦痛的基础 <see cref="AfflictionModel" />：提供关键词悬浮提示和
    ///     <see cref="IModAfflictionAssetOverrides" /> 覆盖层路径。
    /// </summary>
    public abstract class ModAfflictionTemplate : AfflictionModel, IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     Keyword ids surfaced on this affliction's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="AfflictionModel" /> has no <c>Keywords</c>/
        ///     <c>CardKeyword</c> storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to
        ///     render a hover tip via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour
        ///     must be implemented explicitly in the affliction's own logic.
        ///     要显示在此苦痛悬停提示上的关键词 id。<b>仅用于显示</b>：不同于 <see cref="ModCardTemplate.RegisteredKeywordIds" />，它<b>不会</b>参与任何游戏逻辑关键词集合（原版
        ///     <see cref="AfflictionModel" /> 没有 <c>Keywords</c>/<c>CardKeyword</c> 存储）- 每个 id 只会通过
        ///     <see cref="ModKeywordRegistry" /> 查找，用来通过 <c>ToHoverTips()</c> 渲染悬停提示。请将它用于视觉说明；游戏行为必须在苦痛自身逻辑中显式实现。
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword expansion.
        ///     合并在关键词展开之后的额外悬浮提示。
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        /// <inheritdoc />
        public virtual AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }
}
