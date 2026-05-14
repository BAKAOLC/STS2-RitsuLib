using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="EnchantmentModel" /> for mods with <see cref="IModEnchantmentAssetOverrides" /> icon path.
    ///     Mod 附魔的基础 <see cref="EnchantmentModel" />，提供 <see cref="IModEnchantmentAssetOverrides" /> 图标路径。
    /// </summary>
    public abstract class ModEnchantmentTemplate : EnchantmentModel, IModEnchantmentAssetOverrides
    {
        /// <inheritdoc />
        public virtual EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;
    }
}
