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
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="CardModel" /> for mods with additional hover tips and optional card-asset
    ///         overrides. Gold and red hand glows can be supplied through <see cref="ModCardHandGlowRegistry" />;
    ///         arbitrary outline colors use <see cref="ModCardHandOutlineRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="CardModel" />，支持额外悬浮提示和可选的卡牌资源替换。金色与红色手牌
    ///         发光可以通过 <see cref="ModCardHandGlowRegistry" /> 提供；任意描边颜色则使用
    ///         <see cref="ModCardHandOutlineRegistry" />。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets legacy string keyword IDs added to each instance when <see cref="CardModel.Keywords" /> is
        ///         first accessed. New code should override <see cref="CardModel.CanonicalKeywords" /> and return
        ///         <see cref="CardKeyword" /> values, converting registered mod IDs through
        ///         <c>ModKeywordRegistry.GetCardKeyword(id)</c> or <c>id.GetModCardKeyword()</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取旧版字符串关键词 ID；首次访问 <see cref="CardModel.Keywords" /> 时会将其加入每个卡牌实例。
        ///         新代码应重写 <see cref="CardModel.CanonicalKeywords" /> 并返回 <see cref="CardKeyword" /> 值，
        ///         已注册的模组 ID 可通过 <c>ModKeywordRegistry.GetCardKeyword(id)</c> 或
        ///         <c>id.GetModCardKeyword()</c> 转换。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Use CardModel.CanonicalKeywords with CardKeyword values instead. Registered mod keyword ids can be converted with ModKeywordRegistry.GetCardKeyword(id) or id.GetModCardKeyword().")]
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets legacy string card-tag IDs added to each instance when <see cref="CardModel.Tags" /> is first
        ///         materialized. New code should override <see cref="CardModel.CanonicalTags" /> and return
        ///         <see cref="CardTag" /> values, converting registered mod IDs through
        ///         <c>ModCardTagRegistry.GetCardTag(id)</c> or <c>id.GetModCardTag()</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取旧版字符串卡牌标签 ID；首次创建 <see cref="CardModel.Tags" /> 时会将其加入每个卡牌实例。
        ///         新代码应重写 <see cref="CardModel.CanonicalTags" /> 并返回 <see cref="CardTag" /> 值，已注册的
        ///         模组 ID 可通过 <c>ModCardTagRegistry.GetCardTag(id)</c> 或 <c>id.GetModCardTag()</c> 转换。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Use CardModel.CanonicalTags with CardTag values instead. Registered mod card tag ids can be converted with ModCardTagRegistry.GetCardTag(id) or id.GetModCardTag().")]
        protected virtual IEnumerable<string> RegisteredCardTagIds => [];

        /// <summary>
        ///     <para xml:lang="en">Gets additional hover tips for this card.</para>
        ///     <para xml:lang="zh-CN">获取此卡牌的额外悬浮提示。</para>
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => [.. AdditionalHoverTips];

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
        ///     <para xml:lang="en">Exposes legacy keyword IDs to the seeding patch.</para>
        ///     <para xml:lang="zh-CN">向旧版关键词写入补丁提供关键词 ID。</para>
        /// </summary>
        internal IEnumerable<string> EnumerateRegisteredKeywordIds()
        {
#pragma warning disable CS0618
            return RegisteredKeywordIds;
#pragma warning restore CS0618
        }

        /// <summary>
        ///     <para xml:lang="en">Exposes legacy card-tag IDs to the seeding patch.</para>
        ///     <para xml:lang="zh-CN">向旧版卡牌标签写入补丁提供标签 ID。</para>
        /// </summary>
        internal IEnumerable<string> EnumerateRegisteredCardTagIds()
        {
#pragma warning disable CS0618
            return RegisteredCardTagIds;
#pragma warning restore CS0618
        }
    }
}
