using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Cards.FreePlay;
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
    ///     Combat UI change-response context for a secondary-resource attachment.
    ///     次级资源挂载节点的战斗 UI 变更响应上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCombatUiChangeContext<TParent, TNode>(
        TParent Parent,
        TNode Node,
        SecondaryResourceChangeContext Change,
        IReadOnlyList<SecondaryResourceDefinition> Definitions,
        IReadOnlyList<SecondaryResourceDefinition> VisibleDefinitions)
        where TParent : Node
        where TNode : Node
    {
        /// <summary>
        ///     Player whose secondary resource changed.
        ///     次级资源发生变化的玩家。
        /// </summary>
        public Player Player => Change.Player;

        /// <summary>
        ///     Resource definition whose amount changed.
        ///     数量发生变化的资源定义。
        /// </summary>
        public SecondaryResourceDefinition Definition => Change.Definition;

        /// <summary>
        ///     Previous amount.
        ///     变化前数量。
        /// </summary>
        public int OldAmount => Change.OldAmount;

        /// <summary>
        ///     New amount.
        ///     变化后数量。
        /// </summary>
        public int NewAmount => Change.NewAmount;

        /// <summary>
        ///     Signed delta from old to new amount.
        ///     从旧数量到新数量的带符号差值。
        /// </summary>
        public int Delta => Change.Delta;

        /// <summary>
        ///     Reason attached to the amount mutation.
        ///     附加在数量变更上的原因。
        /// </summary>
        public SecondaryResourceChangeReason Reason => Change.Reason;

        /// <summary>
        ///     Optional source model supplied by the mutating command.
        ///     变更命令提供的可选来源模型。
        /// </summary>
        public AbstractModel? Source => Change.Source;
    }

    /// <summary>
    ///     Handles secondary-resource amount changes for a combat UI attachment.
    ///     为战斗 UI 挂载节点处理次级资源数量变化。
    /// </summary>
    public delegate void SecondaryResourceCombatUiChangedHandler<TParent, TNode>(
        SecondaryResourceCombatUiChangeContext<TParent, TNode> context)
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
        PileType PileType,
        CardPreviewMode PreviewMode,
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

        private static readonly AttachedState<Node, List<Action<SecondaryResourceChangeContext>>> CombatChangeHandlers =
            new(() => []);

        private static readonly AttachedState<Node, List<Action>> CombatHiders = new(() => []);

        private static readonly AttachedState<Node, List<Action<CardModel, PileType, CardPreviewMode>>> CardUpdaters =
            new(() => []);

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

        internal static void UpdateCurrentCombatUi(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            if (!ModSecondaryResourceRegistry.HasAny ||
                !LocalContext.IsMe(player))
                return;

            var ui = NCombatRoom.Instance?.Ui;
            if (ui == null || !GodotObject.IsInstanceValid(ui))
                return;

            UpdateCombatUi(ui, player);
        }

        internal static void NotifyCurrentCombatUiChanged(SecondaryResourceChangeContext change)
        {
            if (!ModSecondaryResourceRegistry.HasAny ||
                !LocalContext.IsMe(change.Player))
                return;

            var ui = NCombatRoom.Instance?.Ui;
            if (ui == null || !GodotObject.IsInstanceValid(ui))
                return;

            NotifyCombatUiChanged(ui, change);
        }

        /// <summary>
        ///     Notifies all secondary-resource combat UI attachments for a parent node after an amount changes.
        ///     在数量变化后通知父节点上的所有次级资源战斗 UI 挂载项。
        /// </summary>
        public static void NotifyCombatUiChanged(Node parent, SecondaryResourceChangeContext change)
        {
            ArgumentNullException.ThrowIfNull(parent);
            if (!ModSecondaryResourceRegistry.HasAny ||
                !CombatChangeHandlers.TryGetValue(parent, out var handlers))
                return;

            foreach (var handler in handlers.ToArray())
                handler(change);
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
            UpdateCardUi(parent, card, PileType.None, CardPreviewMode.Normal);
        }

        /// <summary>
        ///     Updates all secondary-resource card UI attachments for a parent node.
        ///     更新父节点上的所有次级资源卡牌 UI 挂载项。
        /// </summary>
        public static void UpdateCardUi(
            Node parent,
            CardModel card,
            PileType pileType,
            CardPreviewMode previewMode)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(card);

            if (!ModSecondaryResourceRegistry.HasAny ||
                !CardUpdaters.TryGetValue(parent, out var updaters))
                return;

            foreach (var updater in updaters.ToArray())
                updater(card, pileType, previewMode);
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
            Action<SecondaryResourceCombatUiContext<TParent, TNode>> update,
            SecondaryResourceCombatUiChangedHandler<TParent, TNode>? changed = null)
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
                    SecondaryResourceVisibility.GetCombatUiDefinitions(player, true)));
            });

            if (changed == null)
                return;

            CombatChangeHandlers.GetOrCreate(parent).Add(change =>
            {
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                changed(new(
                    parent,
                    node,
                    change,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(change.Player, true)));
            });
        }

        internal static void RegisterCardUpdater<TParent, TNode>(
            TParent parent,
            TNode node,
            Action<SecondaryResourceCardUiContext<TParent, TNode>> update)
            where TParent : Node
            where TNode : Node
        {
            CardUpdaters.GetOrCreate(parent).Add((card, pileType, previewMode) =>
            {
                var plan = SecondaryResourcePaymentResolver.Plan(
                    card,
                    FreePlayBindingRegistry.IsCardFreeForUpcomingPlay(card));
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                update(new(
                    parent,
                    node,
                    card,
                    plan,
                    pileType,
                    previewMode,
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
        ///     Registers a NodeAttachment-backed combat UI node and update route on <see cref="NCombatUi" />.
        ///     在 <see cref="NCombatUi" /> 上注册一个基于 NodeAttachment 的战斗 UI 节点及其更新路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCombatUi<TNode>(
            string localId,
            Func<NCombatUi, TNode> factory,
            Action<SecondaryResourceCombatUiContext<NCombatUi, TNode>> update,
            NodeAttachmentOptions? options = null)
            where TNode : Node
        {
            return RegisterCombatUi<NCombatUi, TNode>(localId, factory, update, options);
        }

        /// <summary>
        ///     Registers a NodeAttachment-backed combat UI node with a change-response route on <see cref="NCombatUi" />.
        ///     在 <see cref="NCombatUi" /> 上注册一个基于 NodeAttachment 的战斗 UI 节点及其变更响应路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCombatUi<TNode>(
            string localId,
            Func<NCombatUi, TNode> factory,
            Action<SecondaryResourceCombatUiContext<NCombatUi, TNode>> update,
            SecondaryResourceCombatUiChangedHandler<NCombatUi, TNode> changed,
            NodeAttachmentOptions? options = null)
            where TNode : Node
        {
            return RegisterCombatUi<NCombatUi, TNode>(localId, factory, update, changed, options);
        }

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
        ///     Registers a NodeAttachment-backed combat UI node with update and change-response routes.
        ///     注册一个基于 NodeAttachment 的战斗 UI 节点及其更新和变更响应路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCombatUi<TParent, TNode>(
            string localId,
            Func<TParent, TNode> factory,
            Action<SecondaryResourceCombatUiContext<TParent, TNode>> update,
            SecondaryResourceCombatUiChangedHandler<TParent, TNode> changed,
            NodeAttachmentOptions? options = null)
            where TParent : Node
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(update);
            ArgumentNullException.ThrowIfNull(changed);

            return ModNodeAttachmentRegistry.For(_modId).RegisterReadyChild(
                localId,
                factory,
                (parent, node) =>
                {
                    SecondaryResourceUiRuntime.RegisterCombatUpdater(parent, node, update, changed);
                    SecondaryResourceUiRuntime.HideCombatUi(parent);
                },
                options);
        }

        /// <summary>
        ///     Registers a NodeAttachment-backed card UI node and update route on <see cref="NCard" />.
        ///     在 <see cref="NCard" /> 上注册一个基于 NodeAttachment 的卡牌 UI 节点及其更新路由。
        /// </summary>
        public NodeAttachmentDefinition RegisterCardUi<TNode>(
            string localId,
            Func<NCard, TNode> factory,
            Action<SecondaryResourceCardUiContext<NCard, TNode>> update,
            NodeAttachmentOptions? options = null)
            where TNode : Node
        {
            return RegisterCardUi<NCard, TNode>(localId, factory, update, WithDefaultCardUiOptions(options));
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

        private static NodeAttachmentOptions WithDefaultCardUiOptions(NodeAttachmentOptions? options)
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
                AttachParentSelector = source.AttachParentSelector ?? ResolveCardUiAttachParent,
                SetupTiming = source.SetupTiming,
                ChildIndex = source.ChildIndex,
                InsertBeforeName = source.InsertBeforeName,
                InsertAfterName = source.InsertAfterName,
                QueueFreeReplacedNode = source.QueueFreeReplacedNode,
            };
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

        private static Node ResolveCardUiAttachParent(Node parent)
        {
            return parent is NCard { Body: { } body } ? body : parent;
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
