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
    ///     <para xml:lang="en">Provides update data for a secondary-resource node attached to combat UI.</para>
    ///     <para xml:lang="zh-CN">提供附加到战斗界面的次级资源节点所需的更新数据。</para>
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
    ///     <para xml:lang="en">
    ///         Provides resource-change data for a secondary-resource node attached to combat UI.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供附加到战斗界面的次级资源节点所需的资源变化数据。</para>
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
        ///     <para xml:lang="en">Gets the player whose secondary resource changed.</para>
        ///     <para xml:lang="zh-CN">获取次级资源发生变化的玩家。</para>
        /// </summary>
        public Player Player => Change.Player;

        /// <summary>
        ///     <para xml:lang="en">Gets the resource definition whose amount changed.</para>
        ///     <para xml:lang="zh-CN">获取数量发生变化的资源定义。</para>
        /// </summary>
        public SecondaryResourceDefinition Definition => Change.Definition;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount before the change.</para>
        ///     <para xml:lang="zh-CN">获取变化前的数量。</para>
        /// </summary>
        public int OldAmount => Change.OldAmount;

        /// <summary>
        ///     <para xml:lang="en">Gets the amount after the change.</para>
        ///     <para xml:lang="zh-CN">获取变化后的数量。</para>
        /// </summary>
        public int NewAmount => Change.NewAmount;

        /// <summary>
        ///     <para xml:lang="en">Gets the signed difference from the old amount to the new amount.</para>
        ///     <para xml:lang="zh-CN">获取从旧数量到新数量的带符号差值。</para>
        /// </summary>
        public int Delta => Change.Delta;

        /// <summary>
        ///     <para xml:lang="en">Gets the reason assigned to the amount change.</para>
        ///     <para xml:lang="zh-CN">获取为本次数量变化指定的原因。</para>
        /// </summary>
        public SecondaryResourceChangeReason Reason => Change.Reason;

        /// <summary>
        ///     <para xml:lang="en">Gets the model that caused the change, if one was supplied.</para>
        ///     <para xml:lang="zh-CN">获取引发本次变化的模型（如有）。</para>
        /// </summary>
        public AbstractModel? Source => Change.Source;
    }

    /// <summary>
    ///     <para xml:lang="en">Handles an amount change for a secondary-resource node attached to combat UI.</para>
    ///     <para xml:lang="zh-CN">处理附加到战斗界面的次级资源节点收到的数量变化。</para>
    /// </summary>
    public delegate void SecondaryResourceCombatUiChangedHandler<TParent, TNode>(
        SecondaryResourceCombatUiChangeContext<TParent, TNode> context)
        where TParent : Node
        where TNode : Node;

    /// <summary>
    ///     <para xml:lang="en">Provides update data for a secondary-resource node attached to card UI.</para>
    ///     <para xml:lang="zh-CN">提供附加到卡牌界面的次级资源节点所需的更新数据。</para>
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
    ///     <para xml:lang="en">
    ///         Provides update data for a secondary-resource node attached to a multiplayer player-state display.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供附加到多人玩家状态栏的次级资源节点所需的更新数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceMultiplayerPlayerStateUiContext<TNode>(
        NMultiplayerPlayerState Parent,
        TNode Node,
        Player Player,
        IReadOnlyList<SecondaryResourceDefinition> Definitions,
        IReadOnlyList<SecondaryResourceDefinition> VisibleDefinitions)
        where TNode : Node;

    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes runtime updates to attached secondary-resource UI nodes and isolates failures in their
    ///         display-only callbacks.
    ///     </para>
    ///     <para xml:lang="zh-CN">将运行时更新分发到已挂载的次级资源界面节点，并隔离其纯显示回调中的错误。</para>
    /// </summary>
    public static class SecondaryResourceUiRuntime
    {
        private static readonly Lock CallbackFailureSync = new();
        private static readonly HashSet<Delegate> LoggedCallbackFailures = [];

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
        ///     <para xml:lang="en">Updates every secondary-resource combat UI attachment under a parent node.</para>
        ///     <para xml:lang="zh-CN">更新一个父节点下的所有次级资源战斗界面挂载项。</para>
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
        ///     <para xml:lang="en">
        ///         Notifies every secondary-resource combat UI attachment under a parent after an amount changes.
        ///     </para>
        ///     <para xml:lang="zh-CN">数量变化后通知一个父节点下的所有次级资源战斗界面挂载项。</para>
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
        ///     <para xml:lang="en">Hides every secondary-resource combat UI attachment under a parent node.</para>
        ///     <para xml:lang="zh-CN">隐藏一个父节点下的所有次级资源战斗界面挂载项。</para>
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
        ///     <para xml:lang="en">
        ///         Updates every secondary-resource card UI attachment under a parent using the default visual context.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认视觉上下文更新一个父节点下的所有次级资源卡牌界面挂载项。</para>
        /// </summary>
        public static void UpdateCardUi(Node parent, CardModel card)
        {
            UpdateCardUi(parent, card, PileType.None, CardPreviewMode.Normal);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Updates every secondary-resource card UI attachment under a parent using the supplied visual
        ///         context.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用提供的视觉上下文更新一个父节点下的所有次级资源卡牌界面挂载项。</para>
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
        ///     <para xml:lang="en">
        ///         Updates every secondary-resource UI attachment on one multiplayer player-state display.
        ///     </para>
        ///     <para xml:lang="zh-CN">更新一个多人玩家状态栏上的所有次级资源界面挂载项。</para>
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
        ///     <para xml:lang="en">
        ///         Marks whether a multiplayer player-state display is currently showing combat resources.
        ///     </para>
        ///     <para xml:lang="zh-CN">标记多人玩家状态栏当前是否应显示战斗资源。</para>
        /// </summary>
        public static void SetMultiplayerPlayerStateCombatActive(NMultiplayerPlayerState parent, bool active)
        {
            ArgumentNullException.ThrowIfNull(parent);
            MultiplayerPlayerStateCombatActive.Set(parent, active);
            if (!active)
                HideMultiplayerPlayerStateUi(parent);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Hides every secondary-resource UI attachment on one multiplayer player-state display.
        ///     </para>
        ///     <para xml:lang="zh-CN">隐藏一个多人玩家状态栏上的所有次级资源界面挂载项。</para>
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
                if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(node))
                    return;

                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                var context = new SecondaryResourceCombatUiContext<TParent, TNode>(
                    parent,
                    node,
                    player,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(player, true));
                InvokeCallback(update, "combat UI update", () => update(context));
            });

            if (changed == null)
                return;

            CombatChangeHandlers.GetOrCreate(parent).Add(change =>
            {
                if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(node))
                    return;

                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                var context = new SecondaryResourceCombatUiChangeContext<TParent, TNode>(
                    parent,
                    node,
                    change,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(change.Player, true));
                InvokeCallback(changed, "combat UI change", () => changed(context));
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
                if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(node))
                    return;

                var plan = SecondaryResourcePaymentResolver.Plan(
                    card,
                    SecondaryResourcePaymentFreeMode.FromCardCostScope(
                        FreePlayBindingRegistry.ResolveCardCostScopeForUpcomingPlay(card)));
                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                var context = new SecondaryResourceCardUiContext<TParent, TNode>(
                    parent,
                    node,
                    card,
                    plan,
                    pileType,
                    previewMode,
                    definitions,
                    SecondaryResourceVisibility.GetCardUiDefinitions(card, plan));
                InvokeCallback(update, "card UI update", () => update(context));
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
                if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(node))
                    return;

                var definitions = ModSecondaryResourceRegistry.GetDefinitionsSnapshot();
                var context = new SecondaryResourceMultiplayerPlayerStateUiContext<TNode>(
                    parent,
                    node,
                    parent.Player,
                    definitions,
                    SecondaryResourceVisibility.GetCombatUiDefinitions(parent.Player));
                InvokeCallback(update, "multiplayer player-state UI update", () => update(context));
            });
        }

        private static void HideNode(Node node)
        {
            if (GodotObject.IsInstanceValid(node) && node is CanvasItem canvasItem)
                canvasItem.Visible = false;
        }

        private static void InvokeCallback(Delegate callback, string surface, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                lock (CallbackFailureSync)
                {
                    if (!LoggedCallbackFailures.Add(callback))
                        return;
                }

                RitsuLibFramework.Logger.Warn(
                    $"[SecondaryResource] Registered {surface} callback failed: {ex}");
            }
        }
    }

    // ReSharper disable once ClassCannotBeInstantiated
    public sealed partial class ModSecondaryResourceRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a combat UI child and its update route on <see cref="NCombatUi" /> through node
        ///         attachments.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过节点挂载机制，在 <see cref="NCombatUi" /> 上注册战斗界面子节点及其更新路径。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a combat UI child with update and amount-change routes on <see cref="NCombatUi" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">在 <see cref="NCombatUi" /> 上注册带更新及数量变化处理路径的战斗界面子节点。</para>
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
        ///     <para xml:lang="en">Registers a combat UI child and its update route through node attachments.</para>
        ///     <para xml:lang="zh-CN">通过节点挂载机制注册战斗界面子节点及其更新路径。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a combat UI child with update and amount-change routes through node attachments.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过节点挂载机制注册带更新及数量变化处理路径的战斗界面子节点。</para>
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
        ///     <para xml:lang="en">
        ///         Registers a card UI child and its update route on <see cref="NCard" /> through node attachments.
        ///     </para>
        ///     <para xml:lang="zh-CN">通过节点挂载机制，在 <see cref="NCard" /> 上注册卡牌界面子节点及其更新路径。</para>
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
        ///     <para xml:lang="en">Registers a card UI child and its update route through node attachments.</para>
        ///     <para xml:lang="zh-CN">通过节点挂载机制注册卡牌界面子节点及其更新路径。</para>
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
        ///     <para xml:lang="en">Registers a secondary-resource UI child for each multiplayer player-state display.</para>
        ///     <para xml:lang="zh-CN">为每个多人玩家状态栏注册次级资源界面子节点。</para>
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
