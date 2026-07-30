using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="ActModel" /> for mods with chest Spine replacement, scene and map asset
    ///         overrides, and an optional combat-background layer directory containing <c>_bg_</c> and <c>_fg_</c>
    ///         scenes.
    ///     </para>
    ///     <para xml:lang="en">
    ///         To reuse an existing act's assets, return a profile created by
    ///         <see cref="ContentAssetProfiles.FromVanillaActId" /> from <see cref="AssetProfile" />, using the
    ///         base-game asset folder name rather than this model's ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="ActModel" />，支持替换宝箱 Spine 资源、场景和地图资源，以及指定包含
    ///         <c>_bg_</c> 与 <c>_fg_</c> 场景的可选战斗背景图层目录。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         若要复用现有章节的资源，请让 <see cref="AssetProfile" /> 返回由
    ///         <see cref="ContentAssetProfiles.FromVanillaActId" /> 创建的配置，并使用游戏本体的资源文件夹名称，
    ///         而不是此模型的 ID。
    ///     </para>
    /// </summary>
    public abstract class ModActTemplate : ActModel, IModActAssetOverrides, IModActRandomListPolicy
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

        /// <inheritdoc />
        public virtual bool AllowInRandomActList => false;
    }
}
