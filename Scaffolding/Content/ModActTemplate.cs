using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="ActModel" /> for mods: chest Spine path override and <see cref="IModActAssetOverrides" />
    ///     scene/map paths.
    /// </summary>
    public abstract class ModActTemplate : ActModel, IModActAssetOverrides
    {
        /// <inheritdoc />
        public override string ChestSpineResourcePath =>
            CustomChestSpineResourcePath ?? base.ChestSpineResourcePath;

        /// <inheritdoc />
        public virtual ActAssetProfile AssetProfile => ActAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomBackgroundScenePath => AssetProfile.BackgroundScenePath;

        /// <inheritdoc />
        public virtual string? CustomRestSiteBackgroundPath => AssetProfile.RestSiteBackgroundPath;

        /// <inheritdoc />
        public virtual string? CustomMapTopBgPath => AssetProfile.MapTopBgPath;

        /// <inheritdoc />
        public virtual string? CustomMapMidBgPath => AssetProfile.MapMidBgPath;

        /// <inheritdoc />
        public virtual string? CustomMapBotBgPath => AssetProfile.MapBotBgPath;

        /// <inheritdoc />
        public virtual string? CustomChestSpineResourcePath => AssetProfile.ChestSpineResourcePath;
    }
}
