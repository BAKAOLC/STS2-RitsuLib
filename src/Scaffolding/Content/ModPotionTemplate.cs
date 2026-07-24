using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional image/outline paths for mod potions consumed by content asset patches.
    ///     供内容资源补丁读取的 mod 药水可选图片/轮廓路径。
    /// </summary>
    public interface IModPotionAssetOverrides
    {
        /// <summary>
        ///     Structured path bundle; <c>Custom*</c> properties typically mirror these fields.
        ///     结构化路径集合；<c>Custom*</c> 属性通常镜像这些字段。
        /// </summary>
        PotionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override path for <c>ImagePath</c> / bottle art.
        ///     <c>ImagePath</c> / 瓶身美术的覆盖路径。
        /// </summary>
        string? CustomImagePath { get; }

        /// <summary>
        ///     Override path for outline / silhouette art.
        ///     轮廓 / 剪影美术的覆盖路径。
        /// </summary>
        string? CustomOutlinePath { get; }
    }

    /// <summary>
    ///     Base <see cref="PotionModel" /> for mods: keyword hover tips and <see cref="IModPotionAssetOverrides" />.
    ///     Mod 药水的基础 <see cref="PotionModel" />：提供关键词悬浮提示和 <see cref="IModPotionAssetOverrides" />。
    /// </summary>
    public abstract class ModPotionTemplate : PotionModel, IModPotionAssetOverrides
    {
        /// <summary>
        ///     Keyword ids surfaced on this potion's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="PotionModel" /> has no <c>Keywords</c>/<c>CardKeyword</c>
        ///     storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to render a hover tip
        ///     via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour must be implemented
        ///     explicitly in the potion's own logic.
        ///     要显示在此药水悬停提示上的关键词 id。<b>仅用于显示</b>：不同于 <see cref="ModCardTemplate.RegisteredKeywordIds" />，它<b>不会</b>参与任何游戏逻辑关键词集合（原版
        ///     <see cref="PotionModel" /> 没有 <c>Keywords</c>/<c>CardKeyword</c> 存储）- 每个 id 只会通过 <see cref="ModKeywordRegistry" />
        ///     查找，用来通过 <c>ToHoverTips()</c> 渲染悬停提示。请将它用于视觉说明；游戏行为必须在药水自身逻辑中显式实现。
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips after keyword expansion.
        ///     关键词展开之后的额外悬浮提示。
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        public sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            .. AdditionalHoverTips,
            .. RegisteredKeywordIds.ToHoverTips(),
            .. this.GetModKeywordHoverTips(),
        ];

        /// <inheritdoc />
        public virtual PotionAssetProfile AssetProfile => PotionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomImagePath => AssetProfile.ImagePath;

        /// <inheritdoc />
        public virtual string? CustomOutlinePath => AssetProfile.OutlinePath;
    }
}
