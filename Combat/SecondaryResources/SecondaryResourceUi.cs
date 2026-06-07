using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Combat UI update context for a secondary-resource attachment.
    ///     次级资源挂载节点的战斗 UI 更新上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCombatUiContext<TParent, TNode>(
        TParent Parent,
        TNode Node,
        Player? Player,
        IReadOnlyList<SecondaryResourceDefinition> Definitions,
        IReadOnlyList<SecondaryResourceDefinition> VisibleDefinitions)
        where TParent : Node
        where TNode : Node;

    /// <summary>
    ///     Card UI update context for a secondary-resource attachment.
    ///     次级资源挂载节点的卡牌 UI 更新上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCardUiContext<TParent, TNode>(
        TParent Parent,
        TNode Node,
        CardModel Card,
        SecondaryResourcePaymentPlan Plan,
        IReadOnlyList<SecondaryResourceDefinition> Definitions,
        IReadOnlyList<SecondaryResourceDefinition> VisibleDefinitions)
        where TParent : Node
        where TNode : Node;

    /// <summary>
    ///     Multiplayer player-state UI update context for a secondary-resource attachment.
    ///     次级资源挂载节点的多人玩家状态 UI 更新上下文。
    /// </summary>
    public readonly record struct SecondaryResourceMultiplayerPlayerStateUiContext<TNode>(
        NMultiplayerPlayerState Parent,
        TNode Node,
        Player Player,
        IReadOnlyList<SecondaryResourceDefinition> Definitions,
        IReadOnlyList<SecondaryResourceDefinition> VisibleDefinitions)
        where TNode : Node;

    /// <summary>
    ///     Runtime update routing for secondary-resource UI node attachments.
    ///     次级资源 UI 节点挂载项的运行时更新路由。
    /// </summary>
    public static class SecondaryResourceUiRuntime
    {
        private static readonly AttachedState<Node, List<Action<Player?>>> CombatUpdaters = new(() => []);
        private static readonly AttachedState<Node, List<Action>> CombatHiders = new(() => []);
        private static readonly AttachedState<Node, List<Action<CardModel>>> CardUpdaters = new(() => []);
        private static readonly AttachedState<Node, List<Action>> MultiplayerPlayerStateUpdaters = new(() => []);
        private static readonly AttachedState<Node, List<Action>> MultiplayerPlayerStateHiders = new(() => []);
        private static readonly AttachedState<NMultiplayerPlayerState, bool> MultiplayerPlayerStateCombatActive = new();

        /// <summary>
        ///     Updates all secondary-resource combat UI attachments for a parent node.
        ///     更新父节点上的所有次级资源战斗 UI 挂载项。
        /// </summary>
        public static void UpdateCombatUi(Node parent, Player? player)
        {
            ArgumentNullException.ThrowIfNull(parent);
            if (!ModSecondaryResourceRegistry.HasAny ||
                !CombatUpdaters.TryGetValue(parent, out var updaters))
                return;

            foreach (var updater in updaters.ToArray())
                updater(player);
        }

        /// <summary>
        ///     Hides all secondary-resource combat UI attachments for a parent node.
        ///     隐藏父节点上的所有次级资源战斗 UI 挂载项。
        /// </summary>
        public static void HideCombatUi(Node parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            if (!CombatHiders.TryGetValue(parent, out var hiders))
                return;

            foreach (var hider in hiders.ToArray())
                hider();
        }

        /// <summary>
        ///     Updates all secondary-resource card UI attachments for a parent node.
        ///     更新父节点上的所有次级资源卡牌 UI 挂载项。
        /// </summary>
        public static void UpdateCardUi(Node parent, CardModel card)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(card);

            if (!ModSecondaryResourceRegistry.HasAny ||
                !CardUpdaters.TryGetValue(parent, out var updaters))
                return;

            foreach (var updater in updaters.ToArray())
                updater(card);
        }

        /// <summary>
        ///     Updates all secondary-resource UI attachments for one multiplayer player-state row.
        ///     更新一个多人玩家状态行上的所有次级资源 UI 挂载项。
        /// </summary>
        public static void UpdateMultiplayerPlayerStateUi(NMultiplayerPlayerState parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            if (!ModSecondaryResourceRegistry.HasAny ||
                !MultiplayerPlayerStateUpdaters.TryGetValue(parent, out var updaters))
                return;

            if (!MultiplayerPlayerStateCombatActive.TryGetValue(parent, out var active) || !active)
            {
                HideMultiplayerPlayerStateUi(parent);
                return;
            }

            foreach (var updater in updaters.ToArray())
                updater();
        }

        /// <summary>
        ///     Marks a multiplayer player-state row as being inside or outside combat resource display.
        ///     标记多人玩家状态行是否处于战斗资源显示阶段。
        /// </summary>
        public static void SetMultiplayerPlayerStateCombatActive(NMultiplayerPlayerState parent, bool active)
        {
            ArgumentNullException.ThrowIfNull(parent);
            MultiplayerPlayerStateCombatActive.Set(parent, active);
            if (!active)
                HideMultiplayerPlayerStateUi(parent);
        }

        /// <summary>
        ///     Hides all secondary-resource UI attachments for one multiplayer player-state row.
        ///     隐藏一个多人玩家状态行上的所有次级资源 UI 挂载项。
        /// </summary>
        public static void HideMultiplayerPlayerStateUi(NMultiplayerPlayerState parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            if (!MultiplayerPlayerStateHiders.TryGetValue(parent, out var hiders))
                return;

            foreach (var hider in hiders.ToArray())
                hider();
        }

        internal static void RegisterCombatUpdater<TParent, TNode>(
            TParent parent,
            TNode node,
            Action<SecondaryResourceCombatUiContext<TParent, TNode>> update)
            where TParent : Node
            where TNode : Node
        {
            CombatHiders.GetOrCreate(parent).Add(() => HideNode(node));
            CombatUpdaters.GetOrCreate(parent).Add(player =>
            {
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                update(new(
                    parent,
                    node,
                    player,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(player)));
            });
        }

        internal static void RegisterCardUpdater<TParent, TNode>(
            TParent parent,
            TNode node,
            Action<SecondaryResourceCardUiContext<TParent, TNode>> update)
            where TParent : Node
            where TNode : Node
        {
            CardUpdaters.GetOrCreate(parent).Add(card =>
            {
                var plan = SecondaryResourcePaymentResolver.Plan(card);
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                update(new(
                    parent,
                    node,
                    card,
                    plan,
                    definitions,
                    SecondaryResourceVisibility.GetCardUiDefinitions(card, plan)));
            });
        }

        internal static void RegisterMultiplayerPlayerStateUpdater<TNode>(
            NMultiplayerPlayerState parent,
            TNode node,
            Action<SecondaryResourceMultiplayerPlayerStateUiContext<TNode>> update)
            where TNode : Node
        {
            MultiplayerPlayerStateHiders.GetOrCreate(parent).Add(() => HideNode(node));
            MultiplayerPlayerStateUpdaters.GetOrCreate(parent).Add(() =>
            {
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                update(new(
                    parent,
                    node,
                    parent.Player,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(parent.Player)));
            });
        }

        private static void HideNode(Node node)
        {
            if (node is CanvasItem canvasItem)
                canvasItem.Visible = false;
        }
    }

    public sealed partial class ModSecondaryResourceRegistry
    {
        /// <summary>
        ///     Registers a NodeAttachment-backed combat UI node and update route.
        ///     注册一个基于 NodeAttachment 的战斗 UI 节点及其更新路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCombatUi<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            Action<SecondaryResourceCombatUiContext<TParent, TNode>> update,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(update);

            return ModNodeAttachmentRegistry.For(_modId).RegisterReadyChild(
                localId,
                factory,
                (parent, node) =>
                {
                    SecondaryResourceUiRuntime.RegisterCombatUpdater(parent, node, update);
                    SecondaryResourceUiRuntime.HideCombatUi(parent);
                },
                options);
        }

        /// <summary>
        ///     Registers a NodeAttachment-backed card UI node and update route.
        ///     注册一个基于 NodeAttachment 的卡牌 UI 节点及其更新路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCardUi<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            Action<SecondaryResourceCardUiContext<TParent, TNode>> update,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(update);

            return ModNodeAttachmentRegistry.For(_modId).RegisterReadyChild(
                localId,
                factory,
                (parent, node) =>
                    SecondaryResourceUiRuntime.RegisterCardUpdater(parent, node, update),
                options);
        }

        /// <summary>
        ///     Registers a NodeAttachment-backed UI node for each multiplayer player-state row.
        ///     为每个多人玩家状态行注册一个基于 NodeAttachment 的 UI 节点。
        /// </summary>
        public NodeAttachmentDefinition RegisterMultiplayerPlayerStateUi<TNode>(
            string localId,
            Func<NMultiplayerPlayerState, TNode> factory,
            Action<SecondaryResourceMultiplayerPlayerStateUiContext<TNode>> update,
            NodeAttachmentOptions? options = null)
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(update);

            return ModNodeAttachmentRegistry.For(_modId).RegisterReadyChild(
                localId,
                factory,
                (parent, node) =>
                {
                    SecondaryResourceUiRuntime.RegisterMultiplayerPlayerStateUpdater(parent, node, update);
                    SecondaryResourceMultiplayerPlayerStateUiTicker.Ensure(parent);
                    SecondaryResourceUiRuntime.HideMultiplayerPlayerStateUi(parent);
                },
                WithDefaultMultiplayerPlayerStateOptions(options));
        }

        private static NodeAttachmentOptions WithDefaultMultiplayerPlayerStateOptions(NodeAttachmentOptions? options)
        {
            var source = options ?? NodeAttachmentOptions.Default;
            return new()
            {
                Name = source.Name,
                Order = source.Order,
                UniqueNameInOwner = source.UniqueNameInOwner,
                IncludeDerivedParentTypes = source.IncludeDerivedParentTypes,
                DuplicatePolicy = source.DuplicatePolicy,
                AddMode = source.AddMode,
                AttachParentSelector = source.AttachParentSelector ?? ResolveMultiplayerPlayerStateAttachParent,
                SetupTiming = NodeAttachmentSetupTiming.AfterAdd,
                ChildIndex = source.ChildIndex,
                InsertBeforeName = source.InsertBeforeName,
                InsertAfterName = source.InsertAfterName,
                QueueFreeReplacedNode = source.QueueFreeReplacedNode,
            };
        }

        private static Node ResolveMultiplayerPlayerStateAttachParent(Node parent)
        {
            return parent is NMultiplayerPlayerState playerState &&
                   playerState.GetNodeOrNull<HBoxContainer>("TopInfoContainer") is { } topInfoContainer
                ? topInfoContainer
                : parent;
        }
    }

    internal partial class SecondaryResourceMultiplayerPlayerStateUiTicker : Node
    {
        private const string NodeName = "RitsuLibSecondaryResourceMultiplayerPlayerStateUiTicker";
        private NMultiplayerPlayerState _parent = null!;

        public static void Ensure(NMultiplayerPlayerState parent)
        {
            if (parent.GetNodeOrNull<SecondaryResourceMultiplayerPlayerStateUiTicker>(NodeName) != null)
                return;

            parent.AddChild(new SecondaryResourceMultiplayerPlayerStateUiTicker
            {
                Name = NodeName,
                _parent = parent,
            });
        }

        public override void _Process(double delta)
        {
            if (IsInstanceValid(_parent))
                SecondaryResourceUiRuntime.UpdateMultiplayerPlayerStateUi(_parent);
        }
    }
}
