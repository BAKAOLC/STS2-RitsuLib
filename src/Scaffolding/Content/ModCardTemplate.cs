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
    }
}
