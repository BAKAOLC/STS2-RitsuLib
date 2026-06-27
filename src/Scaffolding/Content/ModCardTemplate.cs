using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
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
    ///     <see cref="ModCardHandOutlineRegistry" /> /
    ///     Mod 卡牌的基础 <see cref="CardModel" />：接入额外悬停提示（关键词），并通过 <see cref="IModCardAssetOverrides" />
    ///     提供可选资源覆盖。对于金色/红色手牌高亮（Evil Eye / Osty 风格），请重写 <c>ShouldGlowGoldInternal</c> / <c>ShouldGlowRedInternal</c>，或结合
    ///     <see cref="ModCardHandGlowRegistry" /> / <c>ModContentRegistry.RegisterCardHandGlow&lt;TCard&gt;()</c> 使用
    ///     <see cref="CardModelHandGlowExtensions" />。对于任意手牌高亮颜色，请使用 <see cref="ModCardHandOutlineRegistry" /> /
    ///     <see cref="ModCardHandOutlineRegistry" /> / <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;()</c>。
    /// </summary>
    public abstract class ModCardTemplate(
        int baseCost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool showInCardLibrary = true)
        : CardModel(baseCost, type, rarity, target, showInCardLibrary), IModCardAssetOverrides,
            IModCardPortraitMaterialOverride, IModCardFrameMaterialOverride, IModCardBannerMaterialOverride,
            IModCardPortraitBorderMaterialOverride, IModCardEnergyIconMaterialOverride,
            IModCardAncientBorderMaterialOverride, IModCardAncientTextBgMaterialOverride,
            IModCardAncientBannerMaterialOverride
    {
        /// <summary>
        ///     Legacy constructor overload; <paramref name="autoAdd" /> is ignored.
        ///     旧版构造函数重载；<paramref name="autoAdd" /> 会被忽略。
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
        ///     Legacy string keyword declarations seeded onto every instance of this card on first
        ///     <see cref="CardModel.Keywords" /> access. Prefer overriding <see cref="CardModel.CanonicalKeywords" />
        ///     and returning <see cref="CardKeyword" /> values directly, using
        ///     <c>ModKeywordRegistry.GetCardKeyword(id)</c> or <c>id.GetModCardKeyword()</c> for registered mod
        ///     keywords.
        ///     旧版字符串关键词声明，会在首次访问 <see cref="CardModel.Keywords" /> 时种入每个卡牌实例。请优先重写
        ///     <see cref="CardModel.CanonicalKeywords" /> 并直接返回 <see cref="CardKeyword" /> 值；注册过的 mod
        ///     关键词可使用 <c>ModKeywordRegistry.GetCardKeyword(id)</c> 或 <c>id.GetModCardKeyword()</c> 转换。
        /// </summary>
        [Obsolete(
            "Use CardModel.CanonicalKeywords with CardKeyword values instead. Registered mod keyword ids can be converted with ModKeywordRegistry.GetCardKeyword(id) or id.GetModCardKeyword().")]
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Legacy string card-tag declarations seeded onto every instance when <see cref="CardModel.Tags" />
        ///     is first materialized. Prefer overriding <see cref="CardModel.CanonicalTags" /> and returning
        ///     <see cref="CardTag" /> values directly, using <c>ModCardTagRegistry.GetCardTag(id)</c> or
        ///     <c>id.GetModCardTag()</c> for registered mod card tags.
        ///     旧版字符串卡牌标签声明，会在首次实体化 <see cref="CardModel.Tags" /> 时种入每个卡牌实例。请优先重写
        ///     <see cref="CardModel.CanonicalTags" /> 并直接返回 <see cref="CardTag" /> 值；注册过的 mod
        ///     卡牌标签可使用 <c>ModCardTagRegistry.GetCardTag(id)</c> 或 <c>id.GetModCardTag()</c> 转换。
        /// </summary>
        [Obsolete(
            "Use CardModel.CanonicalTags with CardTag values instead. Registered mod card tag ids can be converted with ModCardTagRegistry.GetCardTag(id) or id.GetModCardTag().")]
        protected virtual IEnumerable<string> RegisteredCardTagIds => [];

        /// <summary>
        ///     Extra hover tips appended after keyword-derived tips.
        ///     在关键词派生提示之后追加的额外悬浮提示。
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => AdditionalHoverTips.ToArray();

        /// <inheritdoc />
        public virtual Material? CustomAncientBannerMaterial => AssetProfile.AncientBannerMaterial;

        /// <inheritdoc />
        public virtual Material? CustomAncientBorderMaterial => AssetProfile.AncientBorderMaterial;

        /// <inheritdoc />
        public virtual Material? CustomAncientTextBgMaterial => AssetProfile.AncientTextBgMaterial;

        /// <inheritdoc />
        public virtual CardAssetProfile AssetProfile => CardAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomPortraitPath => AssetProfile.PortraitPath;

        /// <inheritdoc />
        public virtual string? CustomBetaPortraitPath => AssetProfile.BetaPortraitPath;

        /// <inheritdoc />
        public virtual string? CustomPortraitMaterialPath => AssetProfile.PortraitMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomFramePath => AssetProfile.FramePath;

        /// <inheritdoc />
        public virtual string? CustomPortraitBorderPath => AssetProfile.PortraitBorderPath;

        /// <inheritdoc />
        public virtual string? CustomEnergyIconPath => AssetProfile.EnergyIconPath;

        /// <inheritdoc />
        public virtual string? CustomAncientBorderPath => AssetProfile.AncientBorderPath;

        /// <inheritdoc />
        public virtual string? CustomAncientTextBgPath => AssetProfile.AncientTextBgPath;

        /// <inheritdoc />
        public virtual string? CustomAncientBannerPath => AssetProfile.AncientBannerPath;

        /// <inheritdoc />
        public virtual CardVisualStyle CustomVisualStyle => AssetProfile.VisualStyle;

        /// <inheritdoc />
        public virtual string? CustomFrameMaterialPath => AssetProfile.FrameMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomPortraitBorderMaterialPath => AssetProfile.PortraitBorderMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomEnergyIconMaterialPath => AssetProfile.EnergyIconMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomAncientBorderMaterialPath => AssetProfile.AncientBorderMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomAncientTextBgMaterialPath => AssetProfile.AncientTextBgMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomAncientBannerMaterialPath => AssetProfile.AncientBannerMaterialPath;

        /// <inheritdoc />
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;

        /// <inheritdoc />
        public virtual string? CustomBannerTexturePath => AssetProfile.BannerTexturePath;

        /// <inheritdoc />
        public virtual string? CustomBannerMaterialPath => AssetProfile.BannerMaterialPath;

        /// <inheritdoc />
        public virtual Material? CustomBannerMaterial => AssetProfile.BannerMaterial;

        /// <inheritdoc />
        public virtual Material? CustomEnergyIconMaterial => AssetProfile.EnergyIconMaterial;

        /// <inheritdoc />
        public virtual Material? CustomFrameMaterial => AssetProfile.FrameMaterial;

        /// <inheritdoc />
        public virtual Material? CustomPortraitBorderMaterial => AssetProfile.PortraitBorderMaterial;

        /// <inheritdoc />
        public virtual Material? CustomPortraitMaterial => AssetProfile.PortraitMaterial;

        /// <summary>
        ///     Internal accessor for the mod-keyword seeding patch.
        ///     供 mod 关键词种入补丁使用的内部访问器。
        /// </summary>
        internal IEnumerable<string> EnumerateRegisteredKeywordIds()
        {
#pragma warning disable CS0618
            return RegisteredKeywordIds;
#pragma warning restore CS0618
        }

        /// <summary>
        ///     Internal accessor for the mod card-tag seeding patch.
        ///     供 mod 卡牌标签种入补丁使用的内部访问器。
        /// </summary>
        internal IEnumerable<string> EnumerateRegisteredCardTagIds()
        {
#pragma warning disable CS0618
            return RegisteredCardTagIds;
#pragma warning restore CS0618
        }
    }
}
