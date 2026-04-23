using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional image/outline paths for mod potions consumed by content asset patches.
    /// </summary>
    public interface IModPotionAssetOverrides
    {
        /// <summary>
        ///     Structured path bundle; <c>Custom*</c> properties typically mirror these fields.
        /// </summary>
        PotionAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Override path for <c>ImagePath</c> / bottle art.
        /// </summary>
        string? CustomImagePath { get; }

        /// <summary>
        ///     Override path for outline / silhouette art.
        /// </summary>
        string? CustomOutlinePath { get; }
    }

    /// <summary>
    ///     Base <see cref="PotionModel" /> for mods: keyword hover tips and <see cref="IModPotionAssetOverrides" />.
    /// </summary>
    public abstract class ModPotionTemplate : PotionModel, IModPotionAssetOverrides
    {
        /// <summary>
        ///     Keyword ids surfaced on this potion's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="PotionModel" /> has no <c>Keywords</c>/<c>CardKeyword</c>
        ///     storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to render a hover tip
        ///     via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour must be implemented
        ///     explicitly in the potion's own logic.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips after keyword expansion.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        public sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        /// <inheritdoc />
        public virtual PotionAssetProfile AssetProfile => PotionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomImagePath => AssetProfile.ImagePath;

        /// <inheritdoc />
        public virtual string? CustomOutlinePath => AssetProfile.OutlinePath;
    }
}
