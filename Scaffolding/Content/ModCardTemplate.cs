using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="CardModel" /> for mods: hooks extra hover tips (keywords) and optional asset overrides via
    ///     <see cref="IModCardAssetOverrides" />.
    /// </summary>
    public abstract class ModCardTemplate(
        int baseCost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool showInCardLibrary = true)
        : CardModel(baseCost, type, rarity, target, showInCardLibrary), IModCardAssetOverrides
    {
        /// <summary>
        ///     Legacy constructor overload; <paramref name="autoAdd" /> is ignored.
        /// </summary>
        [Obsolete("The autoAdd parameter is no longer used and will be removed in a future version.")]
        protected ModCardTemplate(
            int baseCost,
            CardType type,
            CardRarity rarity,
            TargetType target,
            bool showInCardLibrary,
            bool autoAdd) : this(baseCost, type, rarity, target, showInCardLibrary)
        {
        }

        /// <summary>
        ///     Registered card-keyword ids merged into hover tips together with mod-keyword resolution.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Extra hover tips appended after keyword-derived tips.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        /// <inheritdoc />
        public virtual CardAssetProfile AssetProfile => CardAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomPortraitPath => AssetProfile.PortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBetaPortraitPath => AssetProfile.BetaPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomFramePath => AssetProfile.FramePath;

        /// <inheritdoc />
        public virtual string? CustomPortraitBorderPath => AssetProfile.PortraitBorderPath;

        /// <inheritdoc />
        public virtual string? CustomEnergyIconPath => AssetProfile.EnergyIconPath;

        /// <inheritdoc />
        public virtual string? CustomFrameMaterialPath => AssetProfile.FrameMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;

        /// <inheritdoc />
        public virtual string? CustomBannerTexturePath => AssetProfile.BannerTexturePath;

        /// <inheritdoc />
        public virtual string? CustomBannerMaterialPath => AssetProfile.BannerMaterialPath;
    }
}
