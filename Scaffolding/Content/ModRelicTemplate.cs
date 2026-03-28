using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="RelicModel" /> for mods: optional energy hover tip, keyword tips, and
    ///     <see cref="IModRelicAssetOverrides" /> paths.
    /// </summary>
    public abstract class ModRelicTemplate : RelicModel, IModRelicAssetOverrides
    {
        /// <summary>
        ///     Card-style keyword ids to surface on this relic’s hover tips.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword-derived content.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <summary>
        ///     When true, prepends an energy summary hover tip via <c>HoverTipFactory.ForEnergy</c>.
        /// </summary>
        protected virtual bool IncludeEnergyHoverTip => false;

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => BuildExtraHoverTips();

        /// <inheritdoc />
        public virtual RelicAssetProfile AssetProfile => RelicAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <inheritdoc />
        public virtual string? CustomIconOutlinePath => AssetProfile.IconOutlinePath;

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
