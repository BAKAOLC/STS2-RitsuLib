using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    public abstract class ModActTemplate : ActModel, IModActAssetOverrides
    {
        public override string ChestSpineResourcePath =>
            CustomChestSpineResourcePath ?? base.ChestSpineResourcePath;

        public virtual ActAssetProfile AssetProfile => ActAssetProfile.Empty;

        public virtual string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;
        public virtual string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;
        public virtual string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;
        public virtual string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;
        public virtual string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;
        public virtual string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;
    }
}
