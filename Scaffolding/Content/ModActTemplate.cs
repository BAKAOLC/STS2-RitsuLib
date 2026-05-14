using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="ActModel" /> for mods: chest Spine path override, <see cref="IModActAssetOverrides" /> scene/map
    ///     paths, and optional custom combat background layers directory (<c>_bg_</c> / <c>_fg_</c> scenes). To reuse a
    ///     vanilla act’s shipped art, set <see cref="AssetProfile" /> from
    ///     <see cref="ContentAssetProfiles.FromVanillaActId" /> (vanilla folder name, not this act’s model id).
    ///     Mod 章节的基础 <see cref="ActModel" />：提供宝箱 Spine 路径覆盖、<see cref="IModActAssetOverrides" />
    ///     场景/地图路径，以及可选的自定义战斗背景图层目录（<c>_bg_</c> / <c>_fg_</c> 场景）。
    ///     若要复用原版章节随游戏发布的美术，请从 <see cref="AssetProfile" /> 设置
    ///     <see cref="ContentAssetProfiles.FromVanillaActId" />（使用原版文件夹名，而不是此章节的模型 id）。
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
