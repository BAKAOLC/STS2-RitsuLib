using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="ModifierModel" /> for mods with an
    ///         <see cref="IModModifierAssetOverrides" /> icon replacement.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="ModifierModel" />，支持
    ///         <see cref="IModModifierAssetOverrides" /> 图标替换。
    ///     </para>
    /// </summary>
    public abstract class ModModifierTemplate : ModifierModel, IModModifierAssetOverrides
    {
        /// <inheritdoc />
        public virtual ModifierAssetProfile AssetProfile => ModifierAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;
    }
}
