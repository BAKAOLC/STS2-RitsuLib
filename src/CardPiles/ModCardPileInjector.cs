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
    ///     为已注册的 mod 卡牌牌堆创建并附加 UI 节点：优先使用显式 <see cref="ModCardPileAnchor" />，
    ///     否则沿 anchor 轴自动堆叠同样式牌堆。由对应原版 <c>_Ready</c> 运行后的 lifecycle patch 调用。
    /// </summary>
    /// <remarks>
    ///     For <see cref="ModCardPileAnchorKind.Custom" />, <see cref="ModCardPileAnchor.CustomAuthoringPivot" />
    ///     (normalized chrome fractions — see <see cref="ModCardPileAnchor" />) maps
    ///     <see cref="ModCardPileAnchor.CustomPosition" /> to nominal chrome landmarks before injecting
    ///     <see cref="Godot.Control.Position" /> (upper-left corner). Coordinate space matches each mount parent:
    ///     <see cref="NCombatPilesContainer" /> for bottom-row piles,
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar" /> for arbitrary top-bar placements, combat UI
    ///     for <see cref="ModCardPileUiStyle.ExtraHand" /> — consistent with fly-in fallback resolution in
    ///     <see cref="ModCardPileLayout" />.
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar" />，<see cref="ModCardPileUiStyle.ExtraHand" />
    ///     对于 <see cref="ModCardPileAnchorKind.Custom" />，<see cref="ModCardPileAnchor.CustomAuthoringPivot" />
    ///     （normalized chrome fractions，见 <see cref="ModCardPileAnchor" />）会先将
    ///     <see cref="ModCardPileAnchor.CustomPosition" /> 映射到名义 chrome landmark，再注入
    ///     <see cref="Godot.Control.Position" />（左上角）。坐标空间与每个 mount parent 匹配：
    ///     底部 row 牌堆使用 <see cref="NCombatPilesContainer" />，任意 top-bar placement 使用
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar" />，<see cref="ModCardPileUiStyle.ExtraHand" /> 使用 combat
    ///     UI；这与
    ///     <see cref="ModCardPileLayout" /> 中的 fly-in fallback 解析一致。
    /// </remarks>
    internal static class ModCardPileInjector
    {
        /// <summary>
        ///     Mounts all <see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" />
        ///     buttons onto the combat piles container.
        ///     将所有 <see cref="ModCardPileUiStyle.BottomLeft" /> / <see cref="ModCardPileUiStyle.BottomRight" />
        ///     按钮挂载到战斗牌堆容器。
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
        ///     将所有 <see cref="ModCardPileUiStyle.TopBarDeck" /> 按钮挂载到顶部栏，并位于原版 deck 按钮
        ///     <b>左侧</b>；使用共享 <see cref="ModTopBarLayout" /> helper，使 pile-mode 与 action-mode 按钮共享同一行。
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
                        button.Position =
                            ModCardPileCustomMountGeometry.ControlTopLeftFromAuthoring(anchor,
                                ModCardPileUiStyle.TopBarDeck);
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
        ///     将所有 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器挂载到 combat UI。
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
        ///     使用本地 <paramref name="player" /> 初始化已经挂载的按钮，使它们解析 backing
        ///     <see cref="ModCardPile" /> 并开始追踪卡牌添加/移除。
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
                    button.Position = ModCardPileCustomMountGeometry.ControlTopLeftFromAuthoring(
                        definition.Anchor, definition.Style);

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
                    button.Position = ModCardPileCustomMountGeometry.ControlTopLeftFromAuthoring(
                        definition.Anchor, definition.Style);

                container.AddChildSafely(button);
            }
        }

        private static Vector2 ResolveExtraHandPosition(NCombatUi combatUi, ModCardPileDefinition definition)
        {
            if (definition.Anchor.Kind == ModCardPileAnchorKind.Custom)
                return ModCardPileCustomMountGeometry.ControlTopLeftFromAuthoring(definition.Anchor,
                    definition.Style);

            var viewport = combatUi.GetViewportRect().Size;
            var above = definition.Anchor.Kind == ModCardPileAnchorKind.ExtraHandAbove;
            var yOffset = above ? -260f : -420f;
            return new Vector2(viewport.X * 0.5f - 300f, viewport.Y + yOffset) + definition.Anchor.Offset;
        }
    }
}
