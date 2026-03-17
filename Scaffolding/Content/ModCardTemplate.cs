using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class ModCardTemplate(
        int baseCost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        bool showInCardLibrary = true,
        bool autoAdd = true) : CardModel(baseCost, type, rarity, target, showInCardLibrary), IModCardAssetOverrides
    {
        protected bool AutoAddRequested { get; } = autoAdd;
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        public virtual CardAssetProfile AssetProfile => CardAssetProfile.Empty;
        public virtual string? CustomPortraitPath => AssetProfile.PortraitPath;
        public virtual string? CustomBetaPortraitPath => AssetProfile.BetaPortraitPath;
        public virtual string? CustomFramePath => AssetProfile.FramePath;
        public virtual string? CustomPortraitBorderPath => AssetProfile.PortraitBorderPath;
        public virtual string? CustomEnergyIconPath => AssetProfile.EnergyIconPath;
        public virtual string? CustomFrameMaterialPath => AssetProfile.FrameMaterialPath;
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
        public virtual string? CustomBannerTexturePath => AssetProfile.BannerTexturePath;
        public virtual string? CustomBannerMaterialPath => AssetProfile.BannerMaterialPath;
    }
}
