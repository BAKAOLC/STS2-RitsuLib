using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="PowerModel" /> for mods: optional energy hover tip, keyword tips, and
    ///     <see cref="IModPowerAssetOverrides" /> icon paths.
    /// </summary>
    public abstract class ModPowerTemplate : PowerModel, IModPowerAssetOverrides
    {
        /// <summary>
        ///     Keyword ids merged into hover tips for this power.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword-derived tips.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <summary>
        ///     When true, prepends an energy hover tip via <c>HoverTipFactory.ForEnergy</c>.
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
