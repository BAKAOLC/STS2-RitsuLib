using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Describes a sidecar payload executed through a vanilla <see cref="GenericHookGameAction" /> ordering token.
    ///     描述一个通过原版 <see cref="GenericHookGameAction" /> 排序令牌执行的 sidecar 载荷。
    /// </summary>
    /// <param name="ModuleId">Stable owner id for opcode derivation.</param>
    /// <param name="ActionKey">Stable action key for opcode derivation.</param>
    /// <param name="Serialize">Serializes the typed payload.</param>
    /// <param name="Deserialize">Deserializes the typed payload.</param>
    /// <param name="Execute">Runs when the vanilla hook action reaches the front of the action queue.</param>
    /// <param name="ActionType">Vanilla hook action type; only combat action types are supported.</param>
    public sealed record RitsuLibSidecarSyncActionDescriptor<T>(
        string ModuleId,
        string ActionKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        Func<RitsuLibSidecarSyncActionContext<T>, Task> Execute,
        GameActionType ActionType = GameActionType.Combat);

    /// <summary>
    ///     Runtime context delivered to a sidecar sync action executor.
    ///     传递给 sidecar 同步动作执行器的运行时上下文。
    /// </summary>
    /// <param name="Message">Typed action payload.</param>
    /// <param name="OwnerNetId">Owner of the vanilla hook action.</param>
    /// <param name="Action">The vanilla hook action used as the ordering token.</param>
    /// <param name="PlayerChoiceContext">Choice context that pauses and resumes the same queued action.</param>
    public readonly record struct RitsuLibSidecarSyncActionContext<T>(
        T Message,
        ulong OwnerNetId,
        GenericHookGameAction Action,
        GameActionPlayerChoiceContext PlayerChoiceContext);

    /// <summary>
    ///     Queues sidecar payloads through vanilla hook actions without adding mod-owned <c>INetAction</c> types.
    ///     通过原版 hook action 将 sidecar 载荷排入队列，而不添加 mod 自有的 <c>INetAction</c> 类型。
    /// </summary>
    public static class RitsuLibSidecarSyncActions
    {
        private const uint NextHookIdDelta = 1;

        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];
        private static readonly Dictionary<ActionKey, PendingActionPayload> PendingPayloads = [];
        private static readonly HashSet<ActionKey> BoundActions = [];

        /// <summary>
        ///     Registers a sync action descriptor and returns its stable sidecar opcode.
        ///     注册同步动作描述符，并返回其稳定 sidecar opcode。
        /// </summary>
        public static ulong Register<T>(RitsuLibSidecarSyncActionDescriptor<T> descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.ModuleId);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.ActionKey);
            ArgumentNullException.ThrowIfNull(descriptor.Serialize);
            ArgumentNullException.ThrowIfNull(descriptor.Deserialize);
            ArgumentNullException.ThrowIfNull(descriptor.Execute);
            ValidateActionType(descriptor.ActionType);

            var opcode = RitsuLibSidecarOpcodes.For(descriptor.ModuleId, descriptor.ActionKey);
            lock (Gate)
            {
                if (Registrations.TryGetValue(opcode, out var existing))
                {
                    if (existing is Registration<T> typed &&
                        typed.ModuleId == descriptor.ModuleId &&
                        typed.ActionKeyName == descriptor.ActionKey &&
                        typed.ActionType == descriptor.ActionType)
                        return opcode;

                    throw new InvalidOperationException(
                        $"Sidecar sync action opcode conflict: {descriptor.ModuleId}/{descriptor.ActionKey} -> {opcode}");
                }

                Registrations[opcode] = new Registration<T>(
                    descriptor.ModuleId,
                    descriptor.ActionKey,
                    descriptor.Deserialize,
                    descriptor.Execute,
                    descriptor.ActionType);
            }

            return opcode;
        }

        /// <summary>
        ///     Requests a combat hook action carrying the sidecar payload.
        ///     请求一个携带 sidecar 载荷的 combat hook action。
        /// </summary>
        public static bool RequestCombatAction<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncActionDescriptor<T> descriptor,
            T message,
            ulong? ownerNetId = null)
        {
            if (descriptor.ActionType != GameActionType.Combat)
                throw new InvalidOperationException(
                    $"{nameof(RequestCombatAction)} requires a {GameActionType.Combat} descriptor.");
            return Request(runManager, descriptor, message, ownerNetId);
        }

        /// <summary>
        ///     Requests a combat-play-phase hook action carrying the sidecar payload.
        ///     请求一个携带 sidecar 载荷的 combat-play-phase hook action。
        /// </summary>
        public static bool RequestCombatPlayPhaseAction<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncActionDescriptor<T> descriptor,
            T message,
            ulong? ownerNetId = null)
        {
            if (descriptor.ActionType != GameActionType.CombatPlayPhaseOnly)
                throw new InvalidOperationException(
                    $"{nameof(RequestCombatPlayPhaseAction)} requires a {GameActionType.CombatPlayPhaseOnly} descriptor.");
            return Request(runManager, descriptor, message, ownerNetId);
        }

        /// <summary>
        ///     Requests a hook action carrying the sidecar payload.
        ///     请求一个携带 sidecar 载荷的 hook action。
        /// </summary>
        public static bool Request<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncActionDescriptor<T> descriptor,
            T message,
            ulong? ownerNetId = null)
        {
            var opcode = Register(descriptor);
            var rm = runManager ?? RunManager.Instance;
            var net = rm?.NetService;
            if (rm == null || net == null)
                return false;

            var owner = ownerNetId ?? net.NetId;
            switch (net)
            {
                case NetClientGameService client when owner != client.NetId ||
                                                      !RitsuLibSidecarSessionManager.CanSendToPeer(client.HostNetId):
                case NetHostGameService host when !RitsuLibSidecarSyncMessages.CanSendToAllReadyPeers(host, null):
                    return false;
            }

            var action = rm.ActionQueueSynchronizer.GenerateHookAction(owner, descriptor.ActionType);
            var payload = descriptor.Serialize(message);
            var location = rm.RunLocationTargetedBuffer.CurrentLocation;
            var packet = RitsuLibSidecarSync.WriteActionPacket(
                opcode,
                owner,
                action.HookId,
                (byte)descriptor.ActionType,
                location,
                payload);

            switch (net.Type)
            {
                case NetGameType.Singleplayer:
                    StorePending(new(opcode, owner, action.HookId, (byte)descriptor.ActionType, location, payload));
                    BindIfReady(action);
                    rm.ActionQueueSynchronizer.RequestEnqueueHookAction(action);
                    return true;
                case NetGameType.Host:
                    return HostEnqueue(rm, new(opcode, owner, action.HookId, (byte)descriptor.ActionType, location,
                        payload));
            }

            if (net is not NetClientGameService connectedClient)
                return false;

            return RitsuLibSidecarSync.TrySendToHost(
                connectedClient,
                RitsuLibSidecarSync.ActionRequestOpcode,
                packet);
        }

        internal static void RegisterBuiltInHandlers()
        {
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarSync.ActionRequestOpcode, HandleRequest);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarSync.ActionAnnouncementOpcode, HandleAnnouncement);
        }

        internal static void HandleBufferedRequest(in RitsuLibSidecarDispatchContext context)
        {
            HandleRequest(context);
        }

        internal static void TryBindEnqueuedHookAction(uint hookActionId, ulong ownerId, GameActionType gameActionType)
        {
            var rm = RunManager.Instance;
            var action = rm?.ActionQueueSynchronizer.GetHookActionForId(hookActionId, ownerId, gameActionType);
            if (action != null)
                BindIfReady(action);
        }

        private static void HandleRequest(RitsuLibSidecarDispatchContext context)
        {
            if (!context.IsHostIngest || RunManager.Instance?.NetService is not NetHostGameService)
                return;

            if (!RitsuLibSidecarSync.TryReadActionPacket(context.Payload.Span, out var packet))
            {
                RitsuLibFramework.Logger.Warn("[SidecarSync] Rejected malformed sync action request.");
                return;
            }

            if (packet.OwnerNetId != context.SenderNetId)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SidecarSync] Rejected sync action request with owner {packet.OwnerNetId} from sender {context.SenderNetId}.");
                return;
            }

            if (RitsuLibSidecarSync.TryDeferForLocation(true, packet.Location, context))
                return;

            _ = HostEnqueue(RunManager.Instance, packet);
        }

        private static bool HostEnqueue(RunManager runManager, RitsuLibSidecarSyncActionPacket packet)
        {
            if (!TryGetRegistration(packet.DescriptorOpcode, out var registration))
                return false;

            var actionType = (GameActionType)packet.GameActionType;
            ValidateActionType(actionType);
            if (actionType != registration.ActionType)
                return false;

            if (runManager.NetService is NetHostGameService host)
            {
                if (!RitsuLibSidecarSyncMessages.CanSendToAllReadyPeers(host, null))
                    return false;

                var announcement = RitsuLibSidecarSync.WriteActionPacket(
                    packet.DescriptorOpcode,
                    packet.OwnerNetId,
                    packet.HookActionId,
                    packet.GameActionType,
                    packet.Location,
                    packet.Payload);
                if (!RitsuLibSidecarSync.TryBroadcastToReadyPeers(
                        host,
                        RitsuLibSidecarSync.ActionAnnouncementOpcode,
                        announcement,
                        null))
                    return false;
            }

            StorePending(packet);
            FastForwardHookIdPast(runManager.ActionQueueSynchronizer, packet.HookActionId);
            var action = runManager.ActionQueueSynchronizer.GetHookActionForId(
                packet.HookActionId,
                packet.OwnerNetId,
                actionType);
            BindIfReady(action);
            runManager.ActionQueueSynchronizer.RequestEnqueueHookAction(action);
            return true;
        }

        private static void HandleAnnouncement(RitsuLibSidecarDispatchContext context)
        {
            if (!RitsuLibSidecarSync.TryReadActionPacket(context.Payload.Span, out var packet))
            {
                RitsuLibFramework.Logger.Warn("[SidecarSync] Rejected malformed sync action announcement.");
                return;
            }

            StorePending(packet);
            FastForwardHookIdPast(RunManager.Instance.ActionQueueSynchronizer, packet.HookActionId);
        }

        private static bool TryGetRegistration(ulong opcode, out RegistrationBase registration)
        {
            lock (Gate)
            {
                return Registrations.TryGetValue(opcode, out registration!);
            }
        }

        private static void StorePending(RitsuLibSidecarSyncActionPacket packet)
        {
            lock (Gate)
            {
                PendingPayloads[new(packet.OwnerNetId, packet.HookActionId)] = new(
                    packet.DescriptorOpcode,
                    packet.Payload,
                    (GameActionType)packet.GameActionType);
            }
        }

        private static void BindIfReady(GenericHookGameAction action)
        {
            PendingActionPayload pending;
            var key = new ActionKey(action.OwnerId, action.HookId);
            lock (Gate)
            {
                if (BoundActions.Contains(key) || !PendingPayloads.TryGetValue(key, out pending))
                    return;

                BoundActions.Add(key);
                PendingPayloads.Remove(key);
            }

            if (!TryGetRegistration(pending.DescriptorOpcode, out var registration))
            {
                BindNoOp(action,
                    $"descriptor opcode {pending.DescriptorOpcode} is not registered on this peer");
                return;
            }

            registration.Bind(action, pending.Payload);
        }

        private static void BindNoOp(GenericHookGameAction action, string reason)
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            var owner = state?.Players.FirstOrDefault(p => p.NetId == action.OwnerId);
            var netId = RunManager.Instance?.NetService?.NetId;
            if (owner == null || !netId.HasValue)
            {
                RitsuLibFramework.Logger.Error(
                    $"[SidecarSync] Unable to bind failed sync action {action.HookId}: {reason}; owner/net service missing.");
                return;
            }

            RitsuLibFramework.Logger.Error(
                $"[SidecarSync] Binding sync action {action.HookId} as no-op because {reason}.");
            var hookContext = new HookPlayerChoiceContext(owner, netId.Value, action.ActionType);
            _ = hookContext.AssignTaskAndWaitForPauseOrCompletion(Task.CompletedTask);
            action.SetChoiceContext(hookContext);
        }

        private static void ValidateActionType(GameActionType actionType)
        {
            if (actionType is not (GameActionType.Combat or GameActionType.CombatPlayPhaseOnly))
                throw new InvalidOperationException(
                    $"Sidecar sync actions only support {GameActionType.Combat} and {GameActionType.CombatPlayPhaseOnly}.");
        }

        private static void FastForwardHookIdPast(
            ActionQueueSynchronizer synchronizer,
            uint hookActionId)
        {
            if (hookActionId == uint.MaxValue)
                return;

            var nextHookId = hookActionId + NextHookIdDelta;
            if (synchronizer.NextHookId < nextHookId)
                synchronizer.FastForwardHookId(nextHookId);
        }

        private readonly record struct ActionKey(ulong OwnerNetId, uint HookActionId);

        private readonly record struct PendingActionPayload(
            ulong DescriptorOpcode,
            byte[] Payload,
            GameActionType ActionType);

        private abstract class RegistrationBase(
            string moduleId,
            string actionKeyName,
            GameActionType actionType)
        {
            public string ModuleId { get; } = moduleId;
            public string ActionKeyName { get; } = actionKeyName;
            public GameActionType ActionType { get; } = actionType;
            public abstract void Bind(GenericHookGameAction action, byte[] payload);
        }

        private sealed class Registration<T>(
            string moduleId,
            string actionKeyName,
            Func<ReadOnlySpan<byte>, T> deserialize,
            Func<RitsuLibSidecarSyncActionContext<T>, Task> execute,
            GameActionType actionType)
            : RegistrationBase(moduleId, actionKeyName, actionType)
        {
            public override void Bind(GenericHookGameAction action, byte[] payload)
            {
                T message;
                try
                {
                    message = deserialize(payload);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SidecarSync] Failed to deserialize sync action {ModuleId}/{ActionKeyName}: {ex.Message}");
                    BindNoOp(action, $"payload deserialize failed for {ModuleId}/{ActionKeyName}");
                    return;
                }

                var state = RunManager.Instance?.DebugOnlyGetState();
                var owner = state?.Players.FirstOrDefault(p => p.NetId == action.OwnerId);
                if (owner == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SidecarSync] Failed to bind sync action {ModuleId}/{ActionKeyName}: owner {action.OwnerId} not found.");
                    BindNoOp(action, $"owner {action.OwnerId} not found for {ModuleId}/{ActionKeyName}");
                    return;
                }

                var netId = RunManager.Instance?.NetService?.NetId;
                if (!netId.HasValue)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SidecarSync] Failed to bind sync action {ModuleId}/{ActionKeyName}: net service not available.");
                    BindNoOp(action, $"net service not available for {ModuleId}/{ActionKeyName}");
                    return;
                }

                var hookContext = new HookPlayerChoiceContext(owner, netId.Value,
                    action.ActionType);
                var choiceContext = new GameActionPlayerChoiceContext(action);
                var task = ExecuteWhenStarted(action, message, choiceContext);
                _ = hookContext.AssignTaskAndWaitForPauseOrCompletion(task);
                action.SetChoiceContext(hookContext);
            }

            private async Task ExecuteWhenStarted(
                GenericHookGameAction action,
                T message,
                GameActionPlayerChoiceContext choiceContext)
            {
                await action.ExecutionStartedTask;
                var context = new RitsuLibSidecarSyncActionContext<T>(
                    message,
                    action.OwnerId,
                    action,
                    choiceContext);
                await execute(context);
            }
        }
    }
}
