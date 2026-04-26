using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.CardPiles
{
    /// <summary>
    ///     Creates and attaches UI nodes for registered mod card piles, using the explicit
    ///     <see cref="ModCardPileAnchor" /> when provided and falling back to auto-stacking same-style piles
    ///     along the anchor's axis. Called from lifecycle patches that fire after the corresponding vanilla
    ///     <c>_Ready</c> runs.
    /// </summary>
    internal static class ModCardPileInjector
    {
        /// <summary>
        ///     Mounts all <see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" />
        ///     buttons onto the combat piles container.
        /// </summary>
        public static void InjectCombatButtons(NCombatPilesContainer container)
        {
            var leftDefinitions = ModCardPileRegistry.GetDefinitionsByStyle(ModCardPileUiStyle.BottomLeft);
            var rightDefinitions = ModCardPileRegistry.GetDefinitionsByStyle(ModCardPileUiStyle.BottomRight);

            if (leftDefinitions.Length == 0 && rightDefinitions.Length == 0)
                return;

            MountBottomLeftButtons(container, leftDefinitions);
            MountBottomRightButtons(container, rightDefinitions);
            ModCardPileCombatLayout.Relayout(container);
        }

        /// <summary>
        ///     Mounts all <see cref="ModCardPileUiStyle.TopBarDeck" /> buttons onto the top bar to the
        ///     <b>left</b> of the vanilla deck button, using the shared
        ///     <see cref="ModTopBarLayout" /> helper so pile-mode and action-mode buttons share one
        ///     row.
        /// </summary>
        public static void InjectTopBarButtons(NTopBar topBar)
        {
            var definitions = ModCardPileRegistry.GetDefinitionsByStyle(ModCardPileUiStyle.TopBarDeck);
            if (definitions.Length == 0)
                return;

            foreach (var definition in definitions)
            {
                var button = NModTopBarPileButton.Create(definition);
                topBar.AddChildSafely(button);

                var anchor = definition.Anchor;
                switch (anchor.Kind)
                {
                    case ModCardPileAnchorKind.Custom:
                        button.Position = anchor.CustomPosition + anchor.Offset;
                        break;
                    case ModCardPileAnchorKind.TopBarAfterDeck:
                        ModTopBarLayout.PlaceAfterDeck(topBar, button, anchor.Offset);
                        break;
                    case ModCardPileAnchorKind.TopBarBeforeModifiers:
                        ModTopBarLayout.PlaceBeforeModifiers(topBar, button, anchor.Offset);
                        break;
                    default:
                        ModTopBarLayout.Place(topBar, button, anchor.Offset);
                        break;
                }
            }
        }

        /// <summary>
        ///     Mounts all <see cref="ModCardPileUiStyle.ExtraHand" /> containers onto the combat UI.
        /// </summary>
        public static void InjectExtraHandContainers(NCombatUi combatUi)
        {
            var definitions = ModCardPileRegistry.GetDefinitionsByStyle(ModCardPileUiStyle.ExtraHand);
            if (definitions.Length == 0)
                return;

            foreach (var definition in definitions)
            {
                var hand = NModExtraHand.Create(definition);
                hand.Position = ResolveExtraHandPosition(combatUi, definition);
                combatUi.AddChildSafely(hand);
            }
        }

        /// <summary>
        ///     Initializes already-mounted buttons with the local <paramref name="player" /> so they resolve
        ///     their backing <see cref="ModCardPile" /> and start tracking card additions / removals.
        /// </summary>
        public static void InitializeForPlayer(NCombatUi combatUi, Player player)
        {
            foreach (var child in combatUi.GetChildren().OfType<NModExtraHand>())
                child.Initialize(player);

            var pilesContainer = combatUi.GetChildren().OfType<NCombatPilesContainer>().FirstOrDefault();
            if (pilesContainer != null)
            {
                foreach (var child in pilesContainer.GetChildren().OfType<NModCardPileButton>())
                    child.Initialize(player);
                ModCardPileCombatLayout.Relayout(pilesContainer);
            }

            var topBar = NRun.Instance?.GlobalUi?.TopBar;
            if (topBar == null) return;
            // Pile-backed top-bar buttons are now siblings of %Deck inside `RightAlignedStuff`, not
            // direct children of NTopBar — mirror that when iterating for player binding.
            var rightAligned = ModTopBarLayout.GetRightAlignedContainer(topBar);
            if (rightAligned == null) return;
            {
                foreach (var child in rightAligned.GetChildren().OfType<NModCardPileButton>())
                    child.Initialize(player);
            }
        }

        private static void MountBottomLeftButtons(
            NCombatPilesContainer container,
            ModCardPileDefinition[] definitions)
        {
            foreach (var definition in definitions)
            {
                var button = NModCardPileButton.Create(definition);
                if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
                    button.Position = definition.Anchor.CustomPosition + definition.Anchor.Offset;
                container.AddChildSafely(button);
            }
        }

        private static void MountBottomRightButtons(
            NCombatPilesContainer container,
            ModCardPileDefinition[] definitions)
        {
            foreach (var definition in definitions)
            {
                var button = NModCardPileButton.Create(definition);
                if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
                    button.Position = definition.Anchor.CustomPosition + definition.Anchor.Offset;
                container.AddChildSafely(button);
            }
        }

        private static Vector2 ResolveExtraHandPosition(NCombatUi combatUi, ModCardPileDefinition definition)
        {
            if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
                return definition.Anchor.CustomPosition + definition.Anchor.Offset;

            var viewport = combatUi.GetViewportRect().Size;
            var above = definition.Anchor.Kind == ModCardPileAnchorKind.ExtraHandAbove;
            var yOffset = above ? -260f : -420f;
            return new Vector2(viewport.X * 0.5f - 300f, viewport.Y + yOffset) + definition.Anchor.Offset;
        }
    }
}
