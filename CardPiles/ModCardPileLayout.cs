using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
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
            var defaultPosition = GetDefaultTargetPosition(definition, node);
            var resolver = definition.FlightTargetPositionResolver;
            if (resolver == null)
                return defaultPosition;

            var context = new ModCardPileFlightTargetContext(definition, node, defaultPosition);
            return resolver(context) ?? defaultPosition;
        }

        public static Vector2 GetShuffleStartPosition(
            ModCardPileDefinition definition,
            CardPile startPile,
            CardPile targetPile)
        {
            var defaultPosition = GetDefaultTargetPosition(definition, null);
            var resolver = definition.FlightStartPositionResolver;
            if (resolver == null)
                return defaultPosition;

            var context = new ModCardPileFlightStartContext(definition, startPile, targetPile, defaultPosition);
            return resolver(context) ?? defaultPosition;
        }

        private static Vector2 GetDefaultTargetPosition(ModCardPileDefinition definition, NCard? node)
        {
            var fallback = FallbackPosition();

            var button = ModCardPileButtonRegistry.TryGetButton(definition);
            if (button != null && button.IsInsideTree())
                return ApplyCardNodeOffset(button.GlobalPosition + button.Size * 0.5f + definition.Anchor.Offset, node);

            var extraHand = ModCardPileButtonRegistry.TryGetExtraHand(definition);
            if (extraHand != null && extraHand.IsInsideTree())
                return ApplyCardNodeOffset(extraHand.GlobalPosition + extraHand.Size * 0.5f + definition.Anchor.Offset,
                    node);

            if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
            {
                var centerFallback = ResolveCustomAnchorFallbackCenter(definition);
                return ApplyCardNodeOffset(centerFallback, node);
            }

            if (definition.Style == ModCardPileUiStyle.TopBarDeck)
            {
                var deck = NRun.Instance?.GlobalUi?.TopBar?.Deck;
                if (deck != null)
                    return ApplyCardNodeOffset(
                        deck.GlobalPosition + deck.Size * 0.5f + new Vector2(-120f, 0f) + definition.Anchor.Offset,
                        node);
            }

            if (!CombatManager.Instance.IsInProgress || NCombatRoom.Instance?.Ui == null)
                return ApplyCardNodeOffset(fallback + definition.Anchor.Offset, node);

            var ui = NCombatRoom.Instance.Ui;
            return definition.Style switch
            {
                ModCardPileUiStyle.BottomLeft =>
                    ApplyCardNodeOffset(
                        ui.DrawPile.GlobalPosition + ui.DrawPile.Size * 0.5f + new Vector2(0f, -140f) +
                        definition.Anchor.Offset,
                        node),
                ModCardPileUiStyle.BottomRight =>
                    ApplyCardNodeOffset(
                        ui.ExhaustPile.GlobalPosition + ui.ExhaustPile.Size * 0.5f + new Vector2(-140f, 0f) +
                        definition.Anchor.Offset,
                        node),
                ModCardPileUiStyle.ExtraHand =>
                    ApplyCardNodeOffset(new Vector2(fallback.X, fallback.Y - 260f) + definition.Anchor.Offset, node),
                _ => ApplyCardNodeOffset(fallback + definition.Anchor.Offset, node),
            };
        }

        /// <summary>
        ///     Landing centre (global coords) aligned with <see cref="ModCardPileInjector" /> after resolving
        ///     authored <see cref="ModCardPileAnchorKind.Custom" /> points through <see cref="ModCardPileCustomMountGeometry" />.
        /// </summary>
        private static Vector2 ResolveCustomAnchorFallbackCenter(ModCardPileDefinition definition)
        {
            var style = definition.Style;
            var topLeftParentLocal =
                ModCardPileCustomMountGeometry.ControlTopLeftFromAuthoring(definition.Anchor, style);
            var centreParentLocal =
                ModCardPileCustomMountGeometry.NominalCentreFromTopLeft(topLeftParentLocal, style);

            switch (style)
            {
                case ModCardPileUiStyle.BottomLeft:
                case ModCardPileUiStyle.BottomRight:
                {
                    var ui = NCombatRoom.Instance?.Ui;
                    var container = ui?.GetChildren().OfType<NCombatPilesContainer>().FirstOrDefault();
                    if (container != null && container.IsInsideTree())
                        return ControlToGlobalScreenPoint(container, centreParentLocal);
                    break;
                }
                case ModCardPileUiStyle.TopBarDeck:
                {
                    if (NRun.Instance?.GlobalUi?.TopBar is Control topBar && topBar.IsInsideTree())
                        return ControlToGlobalScreenPoint(topBar, centreParentLocal);
                    break;
                }
                case ModCardPileUiStyle.ExtraHand:
                {
                    if (NCombatRoom.Instance?.Ui is Control combatUi && combatUi.IsInsideTree())
                        return ControlToGlobalScreenPoint(combatUi, centreParentLocal);
                    break;
                }
            }

            return centreParentLocal;
        }

        private static Vector2 ControlToGlobalScreenPoint(Control host, Vector2 localPoint)
        {
            return host.GetGlobalTransformWithCanvas() * localPoint;
        }

        private static Vector2 ApplyCardNodeOffset(Vector2 centerPosition, NCard? node)
        {
            if (node == null)
                return centerPosition;
            return centerPosition - node.Size * 0.5f;
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
