using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="ModifierModel" /> for mods with <see cref="IModModifierAssetOverrides" /> icon path.
    ///     Mod 运行修饰符的基础 <see cref="ModifierModel" />，提供 <see cref="IModModifierAssetOverrides" /> 图标路径。
    /// </summary>
    public abstract class ModModifierTemplate : ModifierModel, IModModifierAssetOverrides
    {
        /// <inheritdoc />
        public virtual ModifierAssetProfile AssetProfile => ModifierAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;
    }
}
