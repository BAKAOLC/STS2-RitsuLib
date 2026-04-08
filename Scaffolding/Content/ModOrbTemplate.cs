using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="OrbModel" /> for mods: keyword hover tips, dimmed UI color default, and
    ///     <see cref="IModOrbAssetOverrides" /> paths and optional <see cref="TryCreateOrbSprite" />.
    /// </summary>
    public abstract class ModOrbTemplate : OrbModel, IModOrbAssetOverrides, IModOrbSpriteFactory
    {
        /// <summary>
        ///     Keyword ids merged into this orb’s hover tips.
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
        public override Color DarkenedColor => Colors.DarkSlateGray;

        /// <inheritdoc />
        public virtual OrbAssetProfile AssetProfile => OrbAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomIconPath => AssetProfile.IconPath;

        /// <inheritdoc />
        public virtual string? CustomVisualsScenePath => AssetProfile.VisualsScenePath;

        Node2D? IModOrbSpriteFactory.TryCreateOrbSprite()
        {
            return TryCreateOrbSprite();
        }

        /// <summary>
        ///     Non-null node replaces the scene from <see cref="CustomVisualsScenePath" />; provide Spine and animations
        ///     compatible with <c>CreateSprite</c> callers if required.
        /// </summary>
        protected virtual Node2D? TryCreateOrbSprite()
        {
            return null;
        }
    }
}
