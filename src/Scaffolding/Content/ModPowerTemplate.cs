using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="PowerModel" /> for mods with optional energy and keyword hover tips plus
    ///         icon replacements.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="PowerModel" />，支持可选的能量与关键词悬浮提示以及图标替换。
    ///     </para>
    /// </summary>
    public abstract class ModPowerTemplate : PowerModel, IModPowerAssetOverrides
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets display-only keyword IDs resolved through <see cref="ModKeywordRegistry" /> for hover tips.
        ///         They do not add gameplay keyword behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取仅用于显示的关键词 ID；这些 ID 通过 <see cref="ModKeywordRegistry" /> 解析为悬浮提示，
        ///         不会添加任何关键词游戏行为。
        ///     </para>
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     <para xml:lang="en">Gets additional hover tips.</para>
        ///     <para xml:lang="zh-CN">获取额外悬浮提示。</para>
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <summary>
        ///     <para xml:lang="en">Gets whether an energy hover tip is prepended.</para>
        ///     <para xml:lang="zh-CN">获取是否在最前方添加能量悬浮提示。</para>
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
