using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="CardModel" /> for mods: hooks extra hover tips (keywords) and optional asset overrides via
    ///     <see cref="IModCardAssetOverrides" />. For gold/red hand highlights (Evil Eye / Osty-style), override
    ///     <c>ShouldGlowGoldInternal</c> / <c>ShouldGlowRedInternal</c> or use <see cref="ModCardHandGlowRegistry" /> /
    ///     <c>ModContentRegistry.RegisterCardHandGlow&lt;TCard&gt;()</c> with <see cref="CardModelHandGlowExtensions" />.
    ///     For arbitrary hand-highlight colors use <see cref="ModCardHandOutlineRegistry" /> /
    ///     <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;()</c>.
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
        ///     Mod keyword ids seeded onto every instance of this card on first <see cref="CardModel.Keywords" />
        ///     access. Intentionally kept as a separate channel from vanilla
        ///     <see cref="CardModel.CanonicalKeywords" /> so derived mods can still override
        ///     <c>CanonicalKeywords</c> for vanilla keywords without accidentally dropping their mod keyword
        ///     declarations. Each id is resolved to its pre-minted <see cref="CardKeyword" /> via
        ///     <see cref="ModKeywordRegistry" /> and unioned into <c>CardModel.Keywords</c> right after the vanilla
        ///     canonical seed runs, so runtime additions/removals and <c>DeepCloneFields</c> preserve them.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Internal accessor for the mod-keyword seeding patch.
        /// </summary>
        internal IEnumerable<string> EnumerateRegisteredKeywordIds() => RegisteredKeywordIds;

        /// <summary>
        ///     Extra hover tips appended after keyword-derived tips.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => AdditionalHoverTips.ToArray();

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
