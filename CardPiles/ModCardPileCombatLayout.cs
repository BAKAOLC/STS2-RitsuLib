using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Re-packs mod combat bottom-row pile buttons when <see cref="NModCardPileButton" /> visibility
    ///     changes so <see cref="ModCardPileSpec.VisibleWhen" /> does not leave empty gaps. Manual
    ///     <see cref="ModCardPileAnchorKind.Custom" /> anchors are left untouched.
    /// </summary>
    internal static class ModCardPileCombatLayout
    {
        private const float BottomLeftStackDeltaX = 100f;
        private const float BottomRightStackDeltaX = -100f;

        public static void Relayout(NCombatPilesContainer? container)
        {
            if (container == null || !container.IsInsideTree())
                return;
            if (container.DrawPile is not { } draw
                || container.DiscardPile is not { } discard
                || container.ExhaustPile is not { } exhaust)
                return;

            var buttons = container.GetChildren().OfType<NModCardPileButton>().ToList();
            RelayoutLeft(draw, discard, buttons);
            RelayoutRight(exhaust, buttons);
        }

        private static void RelayoutLeft(Control draw, Control discard, IReadOnlyList<NModCardPileButton> buttons)
        {
            var row = buttons
                .Where(TakesLeftAutoSlot)
                .OrderBy(b => b.Definition!.Id, StringComparer.Ordinal)
                .ToList();

            var primaryIndex = 0;
            var secondaryIndex = 0;
            foreach (var button in row)
            {
                var def = button.Definition!;
                var anchor = def.Anchor;
                if (anchor.Kind == ModCardPileAnchorKind.BottomLeftSecondary)
                {
                    button.Position = discard.Position
                                      + new Vector2(BottomLeftStackDeltaX * (secondaryIndex + 1), 0f)
                                      + anchor.Offset;
                    secondaryIndex++;
                }
                else
                {
                    button.Position = draw.Position
                                      + new Vector2(BottomLeftStackDeltaX * (primaryIndex + 1), 0f)
                                      + anchor.Offset;
                    primaryIndex++;
                }
            }
        }

        private static void RelayoutRight(Control exhaust, IReadOnlyList<NModCardPileButton> buttons)
        {
            var row = buttons
                .Where(TakesRightAutoSlot)
                .OrderBy(b => b.Definition!.Id, StringComparer.Ordinal)
                .ToList();

            var primaryIndex = 0;
            var secondaryIndex = 0;
            foreach (var button in row)
            {
                var def = button.Definition!;
                var anchor = def.Anchor;
                if (anchor.Kind == ModCardPileAnchorKind.BottomRightSecondary)
                {
                    button.Position = exhaust.Position
                                      + new Vector2(
                                          BottomRightStackDeltaX * (primaryIndex + secondaryIndex + 2),
                                          0f)
                                      + anchor.Offset;
                    secondaryIndex++;
                }
                else
                {
                    button.Position = exhaust.Position
                                      + new Vector2(BottomRightStackDeltaX * (primaryIndex + 1), 0f)
                                      + anchor.Offset;
                    primaryIndex++;
                }
            }
        }

        private static bool TakesLeftAutoSlot(NModCardPileButton button)
        {
            if (button.Definition is not { } def)
                return false;
            if (def.Style != ModCardPileUiStyle.BottomLeft)
                return false;
            return def.Anchor.Kind != ModCardPileAnchorKind.Custom && button.Visible;
        }

        private static bool TakesRightAutoSlot(NModCardPileButton button)
        {
            if (button.Definition is not { } def)
                return false;
            if (def.Style != ModCardPileUiStyle.BottomRight)
                return false;
            return def.Anchor.Kind != ModCardPileAnchorKind.Custom && button.Visible;
        }
    }
}
