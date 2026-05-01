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
        ///     Near the bottom-left draw pile button (auto-stacks rightwards toward the discard row).
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
        ///     User-specified mount position; pairing of <see cref="ModCardPileAnchor.CustomPosition" /> /
        ///     <see cref="ModCardPileAnchor.CustomAuthoringPivot" /> is described under <see cref="ModCardPileAnchor" />.
        /// </summary>
        Custom = 9,
    }

    /// <summary>
    ///     UI anchoring descriptor paired with <see cref="ModCardPileUiStyle" />. Combines a discrete slot kind
    ///     with an optional pixel offset (and an authoring point for <see cref="ModCardPileAnchorKind.Custom" />).
    ///     Preserved construction shapes: primary (<see cref="Kind" />, <see cref="Offset" />, optional custom
    ///     fields); two-argument (<c>kind</c>, <c>offset</c>); three-argument custom (<c>kind</c>,
    ///     <c>offset</c>, <c>customPosition</c>); pivot as either <see cref="Vector2" /> or separate floats.
    /// </summary>
    /// <param name="Kind">Discrete slot the pile wants to attach to.</param>
    /// <param name="Offset">
    ///     Additional pixel offset in the mount parent's local space, applied together with resolving
    ///     <paramref name="CustomPosition" /> for <see cref="ModCardPileAnchorKind.Custom" />.
    /// </param>
    /// <param name="CustomPosition">
    ///     Point in the mount parent's local space lying on nominal pile chrome after pivot resolution when
    ///     <paramref name="Kind" /> is <see cref="ModCardPileAnchorKind.Custom" />; ignored otherwise.
    /// </param>
    /// <param name="CustomAuthoringPivot">
    ///     For <see cref="ModCardPileAnchorKind.Custom" />: component-wise fractions (typically 0..1) mapping
    ///     <paramref name="CustomPosition" /> to a landmark on nominal chrome —
    ///     <c>(0,0)</c> top-left, <c>(0.5,0.5)</c> center, <c>(1,1)</c> bottom-right. Injected upper-left corner is
    ///     <c>CustomPosition + Offset − nominalChromeSize * CustomAuthoringPivot</c>; ignored unless
    ///     <paramref name="Kind" /> is <see cref="ModCardPileAnchorKind.Custom" />.
    /// </param>
    public readonly record struct ModCardPileAnchor(
        ModCardPileAnchorKind Kind,
        Vector2 Offset = default,
        Vector2 CustomPosition = default,
        Vector2 CustomAuthoringPivot = default)
    {
        /// <summary>
        ///     Historical two-argument anchor shape preserved for call sites (
        ///     <c>
        ///         <see cref="Offset" />
        ///     </c>
        ///     plus
        ///     default custom fields).
        /// </summary>
        public ModCardPileAnchor(ModCardPileAnchorKind kind, Vector2 offset)
            : this(kind, offset, default, default)
        {
        }

        /// <summary>
        ///     Three-argument custom anchor shape preserving <c>kind + offset + customPosition</c> with pivot
        ///     defaulting to <see cref="PivotUpperLeft" />.
        /// </summary>
        public ModCardPileAnchor(ModCardPileAnchorKind kind, Vector2 offset, Vector2 customPosition)
            : this(kind, offset, customPosition, default)
        {
        }

        /// <summary>
        ///     Custom anchor with authoring pivot expressed as scalar fractions (typically 0..1 per axis).
        /// </summary>
        public ModCardPileAnchor(
            ModCardPileAnchorKind kind,
            Vector2 offset,
            Vector2 customPosition,
            float customAuthoringPivotX,
            float customAuthoringPivotY)
            : this(kind, offset, customPosition, new(customAuthoringPivotX, customAuthoringPivotY))
        {
        }

        /// <summary>
        ///     Pivot that places <see cref="CustomPosition" /> on nominal chrome upper-left (<c>(0,0)</c>,
        ///     default <see cref="ModCardPileAnchorKind.Custom" /> behaviour).
        /// </summary>
        public static Vector2 PivotUpperLeft => Vector2.Zero;

        /// <summary>
        ///     Pivot that places <see cref="CustomPosition" /> on nominal chrome geometric center.
        /// </summary>
        public static Vector2 PivotCenter => Vector2.One * 0.5f;

        /// <summary>
        ///     Convenience anchor that falls back to the style's default slot.
        /// </summary>
        public static ModCardPileAnchor Default { get; } = new(ModCardPileAnchorKind.StyleDefault);

        /// <summary>
        ///     Builds a <see cref="ModCardPileAnchorKind.Custom" /> anchor at authored chrome upper-left
        ///     <paramref name="upperLeftPosition" /> (<see cref="PivotUpperLeft" /> semantics).
        /// </summary>
        public static ModCardPileAnchor AtPosition(Vector2 upperLeftPosition)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, upperLeftPosition);
        }

        /// <summary>
        ///     Builds a <see cref="ModCardPileAnchorKind.Custom" /> anchor at <paramref name="authoringPoint" />
        ///     interpreted as landmark <paramref name="chromePivotFraction" /> on nominal chrome (<c>X,Y</c> typically
        ///     between 0 and 1 inclusive).
        /// </summary>
        public static ModCardPileAnchor AtPivot(Vector2 authoringPoint, Vector2 chromePivotFraction)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, authoringPoint, chromePivotFraction);
        }

        /// <summary>
        ///     Builds a <see cref="ModCardPileAnchorKind.Custom" /> anchor placing nominal chrome geometric center at
        ///     <paramref name="centerPosition" /> (<see cref="PivotCenter" /> semantics).
        /// </summary>
        public static ModCardPileAnchor AtCenter(Vector2 centerPosition)
        {
            return new(ModCardPileAnchorKind.Custom, Vector2.Zero, centerPosition, PivotCenter);
        }
    }
}
