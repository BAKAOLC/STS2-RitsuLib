using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a base <see cref="EnchantmentModel" /> for mods with an
    ///         <see cref="IModEnchantmentAssetOverrides" /> icon replacement.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为模组提供基础 <see cref="EnchantmentModel" />，支持
    ///         <see cref="IModEnchantmentAssetOverrides" /> 图标替换。
    ///     </para>
    /// </summary>
    public abstract class ModEnchantmentTemplate : EnchantmentModel, IModEnchantmentAssetOverrides
    {
        /// <inheritdoc />
        public virtual EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;
    }
}
