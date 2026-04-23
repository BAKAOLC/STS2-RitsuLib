using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Resolves fly-in target positions for mod card piles. Called from the
    ///     <c>PileTypeExtensions.GetTargetPosition</c> prefix patch so vanilla's switch never sees mod-minted
    ///     values.
    /// </summary>
    internal static class ModCardPileLayout
    {
        /// <summary>
        ///     Computes the screen-space target position cards should animate to when moved into
        ///     <paramref name="definition" />. Falls back to a centered screen coordinate if the expected UI
        ///     host node is not yet available (e.g. before combat starts or between scene transitions).
        /// </summary>
        /// <param name="definition">Pile definition describing style / anchor.</param>
        /// <param name="node">The flying card's node, used to offset the target by the card's half-size.</param>
        public static Vector2 GetTargetPosition(ModCardPileDefinition definition, NCard? node)
        {
            var fallback = FallbackPosition();

            if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
                return definition.Anchor.CustomPosition + definition.Anchor.Offset;

            var button = ModCardPileButtonRegistry.TryGetButton(definition);
            if (button != null && button.IsInsideTree())
                return button.GlobalPosition + button.Size * 0.5f + definition.Anchor.Offset;

            var extraHand = ModCardPileButtonRegistry.TryGetExtraHand(definition);
            if (extraHand != null && extraHand.IsInsideTree())
                return extraHand.GlobalPosition + extraHand.Size * 0.5f + definition.Anchor.Offset;

            if (definition.Style == ModCardPileUiStyle.TopBarDeck)
            {
                var deck = NRun.Instance?.GlobalUi?.TopBar?.Deck;
                if (deck != null)
                    return deck.GlobalPosition + deck.Size * 0.5f + new Vector2(-120f, 0f) + definition.Anchor.Offset;
            }

            if (!CombatManager.Instance.IsInProgress || NCombatRoom.Instance?.Ui == null)
                return fallback + definition.Anchor.Offset;

            var ui = NCombatRoom.Instance.Ui;
            return definition.Style switch
            {
                ModCardPileUiStyle.BottomLeft =>
                    ui.DrawPile.GlobalPosition + ui.DrawPile.Size * 0.5f + new Vector2(0f, -140f) +
                    definition.Anchor.Offset,
                ModCardPileUiStyle.BottomRight =>
                    ui.ExhaustPile.GlobalPosition + ui.ExhaustPile.Size * 0.5f + new Vector2(-140f, 0f) +
                    definition.Anchor.Offset,
                ModCardPileUiStyle.ExtraHand =>
                    new Vector2(fallback.X - (node?.Size.X ?? 0f) * 0.5f, fallback.Y - 260f) + definition.Anchor.Offset,
                _ => fallback + definition.Anchor.Offset,
            };
        }

        private static Vector2 FallbackPosition()
        {
            var game = NGame.Instance;
            if (game == null)
                return Vector2.Zero;

            var size = game.GetViewportRect().Size;
            return new(size.X * 0.5f, size.Y * 0.5f);
        }
    }
}
