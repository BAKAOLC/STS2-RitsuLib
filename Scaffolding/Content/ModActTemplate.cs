using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="ActModel" /> for mods: chest Spine path override, <see cref="IModActAssetOverrides" /> scene/map
    ///     paths, and optional custom combat background layers directory (<c>_bg_</c> / <c>_fg_</c> scenes). To reuse a
    ///     vanilla act’s shipped art, set <see cref="AssetProfile" /> from
    ///     <see cref="ContentAssetProfiles.FromVanillaActId" /> (vanilla folder name, not this act’s model id).
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

        /// <inheritdoc />
        public virtual string? CustomBackgroundLayersDirectoryPath => AssetProfile.BackgroundLayersDirectoryPath;
    }
}
