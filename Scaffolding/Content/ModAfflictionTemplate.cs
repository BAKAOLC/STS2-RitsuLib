using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="AfflictionModel" /> for mods: keyword hover tips and <see cref="IModAfflictionAssetOverrides" />
    ///     overlay path.
    /// </summary>
    public abstract class ModAfflictionTemplate : AfflictionModel, IModAfflictionAssetOverrides
    {
        /// <summary>
        ///     Keyword ids surfaced on this affliction's hover tips. <b>Display-only</b>: unlike
        ///     <see cref="ModCardTemplate.RegisteredKeywordIds" />, this does <b>not</b> participate in any
        ///     gameplay keyword set (vanilla <see cref="AfflictionModel" /> has no <c>Keywords</c>/
        ///     <c>CardKeyword</c> storage) — each id is looked up in <see cref="ModKeywordRegistry" /> purely to
        ///     render a hover tip via <c>ToHoverTips()</c>. Use it for visual documentation; gameplay behaviour
        ///     must be implemented explicitly in the affliction's own logic.
        /// </summary>
        protected virtual IEnumerable<string> RegisteredKeywordIds => [];

        /// <summary>
        ///     Additional hover tips merged after keyword expansion.
        /// </summary>
        protected virtual IEnumerable<IHoverTip> AdditionalHoverTips => [];

        /// <inheritdoc />
        protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
            AdditionalHoverTips
                .Concat(RegisteredKeywordIds.ToHoverTips())
                .Concat(this.GetModKeywordHoverTips())
                .ToArray();

        /// <inheritdoc />
        public virtual AfflictionAssetProfile AssetProfile => AfflictionAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomOverlayScenePath => AssetProfile.OverlayScenePath;
    }
}
