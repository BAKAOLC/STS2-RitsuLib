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
    ///     <para xml:lang="en">Creates and attaches UI nodes for registered mod card piles.</para>
    ///     <para xml:lang="zh-CN">为已注册的模组卡牌牌堆创建并挂载界面节点。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Custom positions use the coordinate space of the relevant host: the combat-piles container,
    ///         top bar, or combat UI. Their authored pivot is converted to the node's top-left position by
    ///         <see cref="ModCardPileCustomMountGeometry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         自定义位置使用相应宿主的坐标空间：战斗牌堆容器、顶部栏或战斗界面。其设计基准点由
    ///         <see cref="ModCardPileCustomMountGeometry" /> 换算为节点左上角位置。
    ///     </para>
    /// </remarks>
    internal static class ModCardPileInjector
    {
        /// <summary>
        ///     <para xml:lang="en">Mounts all bottom-row mod pile buttons in the combat-piles container.</para>
        ///     <para xml:lang="zh-CN">将所有底部区域的模组牌堆按钮挂载到战斗牌堆容器。</para>
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
        ///     <para xml:lang="en">
        ///         Mounts all <see cref="ModCardPileUiStyle.TopBarDeck" /> pile buttons on the top bar.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将所有 <see cref="ModCardPileUiStyle.TopBarDeck" /> 牌堆按钮挂载到顶部栏。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Mounts all <see cref="ModCardPileUiStyle.ExtraHand" /> containers on the combat UI.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将所有 <see cref="ModCardPileUiStyle.ExtraHand" /> 容器挂载到战斗界面。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Binds mounted pile controls to <paramref name="player" /> and their runtime piles.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将已挂载的牌堆控件绑定到 <paramref name="player" /> 及其运行时牌堆。
        ///     </para>
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
