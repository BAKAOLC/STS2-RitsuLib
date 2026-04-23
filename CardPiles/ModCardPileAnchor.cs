using Godot;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Location hint for a mod card pile's UI node or fly-in target. Explicit anchors take precedence over
    ///     style defaults; when no anchor is provided, ritsulib auto-stacks same-style piles in registration
    ///     order ("explicit anchor + auto-stack fallback").
    /// </summary>
    public enum ModCardPileAnchorKind
    {
        /// <summary>
        ///     Let the style's default slot decide; multiple entries auto-stack along the style axis.
        /// </summary>
        StyleDefault = 0,

        /// <summary>
        ///     Near the bottom-left draw-pile button (auto-stacks leftwards on overflow).
        /// </summary>
        BottomLeftPrimary = 1,

        /// <summary>
        ///     Near the bottom-left discard button (auto-stacks rightwards on overflow).
        /// </summary>
        BottomLeftSecondary = 2,

        /// <summary>
        ///     Near the bottom-right exhaust button (auto-stacks leftwards on overflow).
        /// </summary>
        BottomRightPrimary = 3,

        /// <summary>
        ///     Reserved for a future second bottom-right slot; stacks left of the primary.
        /// </summary>
        BottomRightSecondary = 4,

        /// <summary>
        ///     Slot in the top bar immediately after the vanilla deck button.
        /// </summary>
        TopBarAfterDeck = 5,

        /// <summary>
        ///     Slot in the top bar before the right-most modifier cluster.
        /// </summary>
        TopBarBeforeModifiers = 6,

        /// <summary>
        ///     Centered above the vanilla hand (used by <see cref="ModCardPileUiStyle.ExtraHand" />).
        /// </summary>
        ExtraHandAbove = 7,

        /// <summary>
        ///     Centered below the vanilla hand (used by <see cref="ModCardPileUiStyle.ExtraHand" />).
        /// </summary>
        ExtraHandBelow = 8,

        /// <summary>
        ///     User-specified absolute coordinate (resolved via <see cref="ModCardPileAnchor.CustomPosition" />).
        /// </summary>
        Custom = 9,
    }

    /// <summary>
    ///     UI anchoring descriptor paired with <see cref="ModCardPileUiStyle" />. Combines a discrete slot kind
    ///     with an optional pixel offset (and an absolute position for <see cref="ModCardPileAnchorKind.Custom" />).
    /// </summary>
    /// <param name="Kind">Discrete slot the pile wants to attach to.</param>
    /// <param name="Offset">Additional pixel offset applied on top of the resolved slot position.</param>
    /// <param name="CustomPosition">
    ///     Absolute viewport coordinate used only when <see cref="Kind" /> is
    ///     <see cref="ModCardPileAnchorKind.Custom" />; otherwise ignored.
    /// </param>
    public readonly record struct ModCardPileAnchor(
        ModCardPileAnchorKind Kind,
        Vector2 Offset = default,
        Vector2 CustomPosition = default)
    {
        /// <summary>
        ///     Convenience anchor that falls back to the style's default slot.
        /// </summary>
        public static ModCardPileAnchor Default { get; } = new(ModCardPileAnchorKind.StyleDefault);

        /// <summary>
        ///     Builds a <see cref="ModCardPileAnchorKind.Custom" /> anchor at <paramref name="position" />.
        /// </summary>
        public static ModCardPileAnchor AtPosition(Vector2 position)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, position);
        }
    }
}
