using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.CardPiles.Nodes;
using NVec2 = System.Numerics.Vector2;

namespace STS2RitsuLib.TopBar
{
    /// <summary>
    ///     Single source of truth for where mod top-bar buttons land relative to the vanilla
    ///     <c>%Deck</c> button. Both pile-backed
    ///     <see cref="STS2RitsuLib.CardPiles.ModCardPileUiStyle.TopBarDeck" /> buttons and
    ///     action-backed <see cref="ModTopBarButtonRegistry" /> buttons funnel through this helper so
    ///     the two systems cannot disagree about slot ordering / direction — addressing the user
    ///     feedback that having <i>two</i> independent layout algorithms was a "meaningless split".
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The actual layout container for the right-side cluster is
    ///         <c>RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff</c> — and crucially it is
    ///         an <see cref="HBoxContainer" /> (<c>separation=0</c>, <c>alignment=end</c>) that
    ///         auto-lays out its children left-to-right in registration order. The canonical vanilla
    ///         child sequence is <c>[SaveIndicator][Padding][TimerContainer][Map][DeckContainer][PauseButton]</c>
    ///         — so to drop a mod button "just to the left of the deck" we need two things:
    ///         <list type="number">
    ///             <item>the button must be parented under this HBoxContainer, and</item>
    ///             <item>its child index must be immediately before <c>DeckContainer</c>'s index.</item>
    ///         </list>
    ///         Because the container is auto-laid-out, we <b>do not</b> set
    ///         <see cref="Control.Position" /> ourselves — doing so would fight the HBoxContainer and
    ///         produce the "button appears nowhere visible" bug from the previous iteration.
    ///     </para>
    ///     <para>
    ///         Slot ordering: mod buttons are inserted in registration order, each one claiming the
    ///         slot <i>immediately before</i> the deck container / deck button. The newest
    ///         registration ends up closest to the deck; the earliest registration ends up furthest
    ///         left. This keeps every mod button contiguous with the vanilla deck icon and avoids
    ///         splitting them into "pile row" / "action row".
    ///     </para>
    /// </remarks>
    public static class ModTopBarLayout
    {
        /// <summary>
        ///     Returns the real right-side layout container (<c>RightAlignedStuff</c>, an
        ///     <see cref="HBoxContainer" />) — the parent of either <c>%Deck</c> directly or the
        ///     <c>DeckContainer</c> that wraps it. Returns null when the top bar hasn't resolved
        ///     <see cref="NTopBar.Deck" /> yet.
        /// </summary>
        public static Control? GetRightAlignedContainer(NTopBar topBar)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            var deck = topBar.Deck;
            if (deck == null)
                return null;
            // The deck may sit inside a `DeckContainer` MarginContainer; walk up until we hit
            // the HBoxContainer so the lookup is stable across scene refactors that add / remove the
            // wrapper.
            var cursor = deck.GetParent();
            while (cursor is { } node)
            {
                if (node is HBoxContainer hbox)
                    return hbox;
                cursor = node.GetParent();
            }

            return deck.GetParent() as Control;
        }

        /// <summary>
        ///     Returns the direct child of the right-aligned container that ultimately contains
        ///     <c>%Deck</c> — often the <c>DeckContainer</c> <see cref="MarginContainer" />
        ///     that wraps the deck button. We insert mod buttons immediately <i>before</i> this node
        ///     so they land "just to the left of the deck", matching the existing vanilla placement
        ///     of Map / DeckContainer / PauseButton.
        /// </summary>
        public static Node? GetDeckSlotAnchor(NTopBar topBar)
        {
            var container = GetRightAlignedContainer(topBar);
            var deck = topBar.Deck;
            if (container == null || deck == null)
                return null;
            Node cursor = deck;
            while (cursor.GetParent() is { } parent && parent != container)
                cursor = parent;
            return cursor.GetParent() == container ? cursor : null;
        }

        /// <summary>
        ///     Attaches <paramref name="button" /> to the right-aligned container (re-parenting when
        ///     necessary) and orders it so it sits immediately to the <b>left</b> of the deck-slot
        ///     anchor. The button is sized to match one vanilla top-bar slot
        ///     (<c>80×80</c> minimum) and left at <see cref="Vector2.Zero" /> <see cref="Control.Position" />
        ///     — the enclosing <see cref="HBoxContainer" /> drives the actual screen position.
        ///     Returns true on success, false when the top bar isn't ready yet (caller should retry).
        /// </summary>
        public static bool Place(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            var anchor = GetDeckSlotAnchor(topBar);
            if (container == null || anchor == null)
                return false;

            return PlaceBeforeAnchor(container, anchor, button, offset);
        }

        /// <summary>
        ///     Attaches <paramref name="button" /> to the right-aligned container and places it immediately
        ///     <b>after</b> the deck slot anchor (typically between deck and pause).
        /// </summary>
        /// <returns>False when the top bar anchor nodes are not ready yet.</returns>
        public static bool PlaceAfterDeck(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            var anchor = GetDeckSlotAnchor(topBar);
            if (container == null || anchor == null)
                return false;

            return PlaceAfterAnchor(container, anchor, button, offset);
        }

        /// <summary>
        ///     Attaches <paramref name="button" /> to the right-aligned container and places it immediately
        ///     <b>before</b> the modifiers slot when available; falls back to the default deck-left placement.
        /// </summary>
        /// <returns>False only when all candidate anchors are unavailable.</returns>
        public static bool PlaceBeforeModifiers(NTopBar topBar, NModCardPileButton button, Vector2 offset = default)
        {
            ArgumentNullException.ThrowIfNull(topBar);
            ArgumentNullException.ThrowIfNull(button);

            var container = GetRightAlignedContainer(topBar);
            if (container == null)
                return false;

            var modifiers = topBar.GetNodeOrNull<Control>("%Modifiers");
            if (modifiers == null)
                return Place(topBar, button, offset);

            Node anchor = modifiers;
            while (anchor.GetParent() is { } parent && parent != container)
                anchor = parent;
            return anchor.GetParent() != container
                ? Place(topBar, button, offset)
                : PlaceBeforeAnchor(container, anchor, button, offset);
        }

        private static bool PlaceBeforeAnchor(Control container, Node anchor, NModCardPileButton button, Vector2 offset)
        {
            AttachToContainer(container, button);

            var anchorIndex = anchor.GetIndex();
            var currentIndex = button.GetIndex();
            var targetIndex = currentIndex < anchorIndex ? anchorIndex - 1 : anchorIndex;
            if (currentIndex != targetIndex)
                container.MoveChild(button, targetIndex);

            button.ApplyVisualOffset(offset);
            return true;
        }

        private static bool PlaceAfterAnchor(Control container, Node anchor, NModCardPileButton button, Vector2 offset)
        {
            AttachToContainer(container, button);

            var anchorIndex = anchor.GetIndex();
            var currentIndex = button.GetIndex();
            var targetIndex = currentIndex < anchorIndex ? anchorIndex : anchorIndex + 1;
            if (currentIndex != targetIndex)
                container.MoveChild(button, targetIndex);

            button.ApplyVisualOffset(offset);
            return true;
        }

        private static void AttachToContainer(Control container, NModCardPileButton button)
        {
            if (button.GetParent() != container)
            {
                button.GetParent()?.RemoveChild(button);
                container.AddChildSafely(button);
            }

            button.Position = Vector2.Zero;
            button.Scale = Vector2.One;
        }

        /// <summary>
        ///     System.Numerics-flavoured overload for callers (e.g. <see cref="ModTopBarButtonSpec" />)
        ///     that carry offsets as <see cref="NVec2" />.
        /// </summary>
        public static bool Place(NTopBar topBar, NModCardPileButton button, NVec2 offset)
        {
            return Place(topBar, button, new Vector2(offset.X, offset.Y));
        }
    }
}
