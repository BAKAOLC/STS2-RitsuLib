using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="PowerModel" /> for mods: optional energy hover tip, keyword tips, and
    ///     <see cref="IModPowerAssetOverrides" /> icon paths.
    ///     Mod 能力的基础 <see cref="PowerModel" />：提供可选能量悬浮提示、关键词提示，以及
    ///     <see cref="IModPowerAssetOverrides" /> 图标路径。
    /// </summary>
    public abstract class ModPowerTemplate : PowerModel, IModPowerAssetOverrides
    {
        /// <summary>
        ///     Keyword ids surfaced on this power's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="PowerModel" /> has no <c>Keywords</c>/<c>CardKeyword</c>
        ///     storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to render a hover tip
        ///     via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour must be implemented
        ///     explicitly in the power's own logic.
        ///     要显示在此能力悬停提示上的关键词 id。<b>仅用于显示</b>：不同于 <see cref="ModCardTemplate.RegisteredKeywordIds" />，它<b>不会</b>参与任何游戏逻辑关键词集合（原版
        ///     <see cref="PowerModel" /> 没有 <c>Keywords</c>/<c>CardKeyword</c> 存储）- 每个 id 只会通过 <see cref="ModKeywordRegistry" />
        ///     查找，用来通过 <c>ToHoverTips()</c> 渲染悬停提示。请将它用于视觉说明；游戏行为必须在能力自身逻辑中显式实现。
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword-derived tips.
        ///     合并在关键词派生提示之后的额外悬浮提示。
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <summary>
        ///     When true, prepends an energy hover tip via <c>HoverTipFactory.ForEnergy</c>.
        ///     为 true 时，通过 <c>HoverTipFactory.ForEnergy</c> 在前面添加能量悬浮提示。
        /// </summary>
        protected virtual bool IncludeEnergyHoverTip => false;

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => BuildExtraHoverTips();

        /// <inheritdoc />
        public virtual PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <inheritdoc />
        public virtual string? CustomBigIconPath => AssetProfile.BigIconPath;

        private List<IHoverTip> BuildExtraHoverTips()
        {
            var tips = new List<IHoverTip>();

            if (IncludeEnergyHoverTip)
                tips.Add(HoverTipFactory.ForEnergy(this));

            tips.AddRange(AdditionalHoverTips);
            tips.AddRange(RegisteredKeywordIds.ToHoverTips());
            tips.AddRange(this.GetModKeywordHoverTips());
            return tips;
        }
    }
}
