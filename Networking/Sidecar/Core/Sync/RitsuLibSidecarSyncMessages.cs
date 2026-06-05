using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal enum RitsuLibSidecarSyncMessageRoute : byte
    {
        Direct = 0,
        ClientToHostAndBroadcast = 1,
    }

    /// <summary>
    ///     Whether a sidecar sync message may tolerate missing peers or send failures.
    ///     Sidecar 同步消息是否允许缺失 peer 或发送失败。
    /// </summary>
    public enum RitsuLibSidecarSyncFailurePolicy : byte
    {
        /// <summary>
        ///     Game-flow messages: every targeted sidecar-capable peer must be reachable before local handling runs.
        ///     游戏流程消息：本地处理前，所有目标 sidecar peer 都必须可达。
        /// </summary>
        Required = 0,

        /// <summary>
        ///     Non-gameplay messages only: missing peers are skipped and failures do not block local handling.
        ///     仅用于非游戏流程消息：缺失 peer 会被跳过，失败不会阻止本地处理。
        /// </summary>
        BestEffort = 1,
    }

    /// <summary>
    ///     Host broadcast target set for a sidecar sync message.
    ///     Sidecar 同步消息的主机广播目标集合。
    /// </summary>
    public enum RitsuLibSidecarSyncBroadcastScope : byte
    {
        /// <summary>
        ///     Matches vanilla <see cref="INetGameService.SendMessage{T}(T)" /> host broadcast behavior.
        ///     对齐原版 <see cref="INetGameService.SendMessage{T}(T)" /> 的主机广播行为。
        /// </summary>
        ReadyPeers = 0,

        /// <summary>
        ///     Sends to every connected peer; intended for lobby/session sidecar flows before vanilla ready state.
        ///     发送给所有已连接 peer；用于原版 ready 状态前的 lobby/session sidecar 流程。
        /// </summary>
        AllConnectedPeers = 1,
    }

    /// <summary>
    ///     Describes a sidecar message with vanilla-like message policy without registering an <see cref="INetMessage" />
    ///     subtype in the game's generated message id table.
    ///     描述一个不注册到游戏生成 message id 表、但具备原版式消息策略的 sidecar 消息。
    /// </summary>
    /// <param name="ModuleId">Stable owner id for opcode derivation.</param>
    /// <param name="MessageKey">Stable message key for opcode derivation.</param>
    /// <param name="Serialize">Serializes the typed payload.</param>
    /// <param name="Deserialize">Deserializes the typed payload.</param>
    /// <param name="Handle">Runs when buffering and optional location gating have released the message.</param>
    /// <param name="LocationTargeted">When true, the message carries the current run location and waits for it.</param>
    /// <param name="ShouldBuffer">When true, the message waits behind vanilla <see cref="NetMessageBus" /> buffering.</param>
    /// <param name="Mode">Vanilla transport mode used when sending this message.</param>
    /// <param name="Channel">Optional explicit channel; null uses <see cref="NetTransferModeExtensions.ToChannelId" />.</param>
    /// <param name="FailurePolicy">Whether all targeted recipients are required for game-flow safety.</param>
    /// <param name="BroadcastScope">Which host peers receive host-originated or host-relayed broadcasts.</param>
    /// <param name="DispatchLocalOnBroadcast">Whether host/singleplayer broadcasts also run the local handler.</param>
    /// <param name="LogLevel">Vanilla-style network receive log level.</param>
    /// <param name="ShouldBroadcast">Vanilla-style host relay flag for client-originated sends.</param>
    public sealed record RitsuLibSidecarSyncMessageDescriptor<T>(
        string ModuleId,
        string MessageKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        Func<RitsuLibSidecarSyncMessageContext<T>, Task> Handle,
        bool LocationTargeted = false,
        bool ShouldBuffer = true,
        NetTransferMode Mode = NetTransferMode.Reliable,
        int? Channel = null,
        RitsuLibSidecarSyncFailurePolicy FailurePolicy = RitsuLibSidecarSyncFailurePolicy.Required,
        RitsuLibSidecarSyncBroadcastScope BroadcastScope = RitsuLibSidecarSyncBroadcastScope.ReadyPeers,
        bool DispatchLocalOnBroadcast = true,
        LogLevel LogLevel = LogLevel.Debug,
        bool ShouldBroadcast = false)
    {
        /// <summary>
        ///     Preserves the original constructor ABI for mods compiled before transport/failure policy was added.
        ///     为 transport/failure policy 加入前编译的 mod 保留原构造函数 ABI。
        /// </summary>
        public RitsuLibSidecarSyncMessageDescriptor(
            string moduleId,
            string messageKey,
            Func<T, byte[]> serialize,
            Func<ReadOnlySpan<byte>, T> deserialize,
            Func<RitsuLibSidecarSyncMessageContext<T>, Task> handle,
            bool locationTargeted = false,
            bool shouldBuffer = true)
            : this(
                moduleId,
                messageKey,
                serialize,
                deserialize,
                handle,
                locationTargeted,
                shouldBuffer,
                NetTransferMode.Reliable)
        {
        }
    }

    /// <summary>
    ///     Runtime context delivered to a sidecar sync message handler.
    ///     传递给 sidecar 同步消息处理器的运行时上下文。
    /// </summary>
    /// <param name="Message">Typed message payload.</param>
    /// <param name="SenderNetId">Original vanilla sender id, preserved across host relay.</param>
    /// <param name="NetService">Current net service, when available.</param>
    /// <param name="IsHostIngest">True when this peer received the packet as host.</param>
    /// <param name="Location">Run location carried by a location-targeted descriptor.</param>
    public readonly record struct RitsuLibSidecarSyncMessageContext<T>(
        T Message,
        ulong SenderNetId,
        INetGameService? NetService,
        bool IsHostIngest,
        RunLocation? Location);

    /// <summary>
    ///     Sends sidecar messages using vanilla reliable channel ordering, net buffering, and optional run-location gating.
    ///     使用原版可靠 channel 顺序、网络缓冲和可选 run-location 门控发送 sidecar 消息。
    /// </summary>
    public static class RitsuLibSidecarSyncMessages
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];
        private static readonly Logger NetworkLogger = new("RitsuLibSidecarSync", LogType.Network);

        /// <summary>
        ///     Registers a sync message descriptor and returns its stable sidecar opcode.
        ///     注册同步消息描述符，并返回其稳定 sidecar opcode。
        /// </summary>
        public static ulong Register<T>(RitsuLibSidecarSyncMessageDescriptor<T> descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.ModuleId);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.MessageKey);
            ArgumentNullException.ThrowIfNull(descriptor.Serialize);
            ArgumentNullException.ThrowIfNull(descriptor.Deserialize);
            ArgumentNullException.ThrowIfNull(descriptor.Handle);
            ValidateDescriptorPolicy(descriptor);

            var opcode = RitsuLibSidecarOpcodes.For(descriptor.ModuleId, descriptor.MessageKey);
            lock (Gate)
            {
                if (Registrations.TryGetValue(opcode, out var existing))
                {
                    if (existing is Registration<T> typed &&
                        typed.ModuleId == descriptor.ModuleId &&
                        typed.MessageKey == descriptor.MessageKey)
                        return opcode;

                    throw new InvalidOperationException(
                        $"Sidecar sync message opcode conflict: {descriptor.ModuleId}/{descriptor.MessageKey} -> {opcode}");
                }

                Registrations[opcode] = new Registration<T>(
                    descriptor.ModuleId,
                    descriptor.MessageKey,
                    descriptor.Deserialize,
                    descriptor.Handle,
                    descriptor.LocationTargeted,
                    descriptor.ShouldBuffer,
                    descriptor.Mode,
                    ResolveChannel(descriptor),
                    descriptor.FailurePolicy,
                    descriptor.BroadcastScope,
                    descriptor.LogLevel,
                    descriptor.ShouldBroadcast);
            }

            return opcode;
        }

        /// <summary>
        ///     Sends a sync message with vanilla <see cref="INetGameService.SendMessage{T}(T)" /> routing semantics.
        ///     使用原版 <see cref="INetGameService.SendMessage{T}(T)" /> 路由语义发送同步消息。
        /// </summary>
        public static bool Send<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            if (netService is null)
                return false;

            return netService.Type switch
            {
                NetGameType.Singleplayer => DispatchLocal(descriptor, message, netService.NetId, netService, false),
                NetGameType.Host => BroadcastRemoteOnly(netService, descriptor, message),
                _ => SendToHostCore(
                    netService,
                    descriptor,
                    message,
                    descriptor.ShouldBroadcast
                        ? RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast
                        : RitsuLibSidecarSyncMessageRoute.Direct),
            };
        }

        /// <inheritdoc cref="Send{T}(INetGameService?, RitsuLibSidecarSyncMessageDescriptor{T}, T)" />
        public static bool Send<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return Send(runManager?.NetService, descriptor, message);
        }

        /// <summary>
        ///     Sends a sync message from client to host, or handles it locally for host/singleplayer services.
        ///     从客户端向主机发送同步消息；在主机或单人服务中则本地处理。
        /// </summary>
        public static bool SendToHost<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return SendToHostCore(netService, descriptor, message, RitsuLibSidecarSyncMessageRoute.Direct);
        }

        /// <inheritdoc cref="SendToHost{T}(INetGameService?, RitsuLibSidecarSyncMessageDescriptor{T}, T)" />
        public static bool SendToHost<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return SendToHost(runManager?.NetService, descriptor, message);
        }

        /// <summary>
        ///     Sends a sync message to the host and asks the host to relay it to ready peers.
        ///     向主机发送同步消息，并请求主机转发给已 ready 的 peer。
        /// </summary>
        public static bool SendToHostAndBroadcast<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return SendToHostCore(netService, descriptor, message,
                RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast);
        }

        /// <inheritdoc cref="SendToHostAndBroadcast{T}(INetGameService?, RitsuLibSidecarSyncMessageDescriptor{T}, T)" />
        public static bool SendToHostAndBroadcast<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return SendToHostAndBroadcast(runManager?.NetService, descriptor, message);
        }

        /// <summary>
        ///     Sends a sync message from host to one peer.
        ///     从主机向单个 peer 发送同步消息。
        /// </summary>
        public static bool SendToPeer<T>(
            INetGameService? netService,
            ulong peerNetId,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            if (netService is not NetHostGameService host)
                return false;
            if (!CanSendToPeer(peerNetId))
                return FailUnavailablePeer(peerNetId, descriptor);

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(host.NetId, descriptor, opcode, RitsuLibSidecarSyncMessageRoute.Direct, payload);
            return RitsuLibSidecarSync.TrySendToPeer(host, peerNetId,
                RitsuLibSidecarSync.MessageOpcode, packet, descriptor.Mode, ResolveChannel(descriptor));
        }

        /// <summary>
        ///     Broadcasts a sync message from host to all ready sidecar-capable peers and handles it locally.
        ///     从主机向所有已 ready 且支持 sidecar 的 peer 广播同步消息，并在本地处理。
        /// </summary>
        public static bool Broadcast<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            if (netService is null)
                return false;

            if (netService.Type == NetGameType.Singleplayer)
                return !descriptor.DispatchLocalOnBroadcast ||
                       DispatchLocal(descriptor, message, netService.NetId, netService, false);

            if (netService is not NetHostGameService host)
                return false;

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(host.NetId, descriptor, opcode, RitsuLibSidecarSyncMessageRoute.Direct, payload);
            var sent = RitsuLibSidecarSync.TryBroadcastToPeers(host,
                RitsuLibSidecarSync.MessageOpcode,
                packet,
                null,
                descriptor.BroadcastScope,
                descriptor.Mode,
                ResolveChannel(descriptor),
                descriptor.FailurePolicy);
            if (!sent)
                return false;

            var dispatched = !descriptor.DispatchLocalOnBroadcast ||
                             DispatchLocal(descriptor, message, host.NetId, host, true);
            return sent && dispatched;
        }

        /// <inheritdoc cref="Broadcast{T}(INetGameService?, RitsuLibSidecarSyncMessageDescriptor{T}, T)" />
        public static bool Broadcast<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return Broadcast(runManager?.NetService, descriptor, message);
        }

        private static bool BroadcastRemoteOnly<T>(
            INetGameService netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            if (netService is not NetHostGameService host)
                return false;

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(host.NetId, descriptor, opcode, RitsuLibSidecarSyncMessageRoute.Direct, payload);
            return RitsuLibSidecarSync.TryBroadcastToPeers(host,
                RitsuLibSidecarSync.MessageOpcode,
                packet,
                null,
                descriptor.BroadcastScope,
                descriptor.Mode,
                ResolveChannel(descriptor),
                descriptor.FailurePolicy);
        }

        internal static void RegisterBuiltInHandler()
        {
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarSync.MessageOpcode, HandleIncoming);
        }

        internal static void HandleBuffered(in RitsuLibSidecarDispatchContext context)
        {
            HandleIncoming(context);
        }

        internal static bool ShouldBufferIncoming(ReadOnlySpan<byte> payload)
        {
            if (!RitsuLibSidecarSync.TryReadMessagePacket(payload, out var packet))
                return true;

            lock (Gate)
            {
                return !Registrations.TryGetValue(packet.DescriptorOpcode, out var registration) ||
                       registration.ShouldBuffer;
            }
        }

        internal static bool TryGetRelayPolicy(
            ReadOnlySpan<byte> payload,
            out bool shouldRelay,
            out RitsuLibSidecarSyncBroadcastScope scope,
            out RitsuLibSidecarSyncFailurePolicy failurePolicy)
        {
            shouldRelay = false;
            scope = RitsuLibSidecarSyncBroadcastScope.ReadyPeers;
            failurePolicy = RitsuLibSidecarSyncFailurePolicy.Required;
            if (!RitsuLibSidecarSync.TryReadMessagePacket(payload, out var packet))
                return false;

            lock (Gate)
            {
                if (!Registrations.TryGetValue(packet.DescriptorOpcode, out var registration))
                    return false;

                shouldRelay = packet.Route == RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast ||
                              registration.ShouldBroadcast;
                scope = registration.BroadcastScope;
                failurePolicy = registration.FailurePolicy;
                return true;
            }
        }

        private static bool SendToHostCore<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message,
            RitsuLibSidecarSyncMessageRoute route)
        {
            var opcode = Register(descriptor);
            if (netService is null)
                return false;

            switch (netService.Type)
            {
                case NetGameType.Singleplayer:
                    return DispatchLocal(descriptor, message, netService.NetId, netService,
                        false);
                case NetGameType.Host:
                    return route == RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast
                        ? Broadcast(netService, descriptor, message)
                        : DispatchLocal(descriptor, message, netService.NetId, netService, true);
            }

            if (netService is not NetClientGameService client)
                return false;
            if (!CanSendToPeer(client.HostNetId))
                return FailUnavailablePeer(client.HostNetId, descriptor);

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(client.NetId, descriptor, opcode, route, payload);
            return RitsuLibSidecarSync.TrySendToHost(client,
                RitsuLibSidecarSync.MessageOpcode,
                packet,
                descriptor.Mode,
                ResolveChannel(descriptor));
        }

        private static byte[] BuildPacket<T>(
            ulong originalSenderNetId,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            ulong opcode,
            RitsuLibSidecarSyncMessageRoute route,
            ReadOnlySpan<byte> payload)
        {
            var location = descriptor.LocationTargeted
                ? RunManager.Instance?.RunLocationTargetedBuffer?.CurrentLocation ?? default
                : default;
            return RitsuLibSidecarSync.WriteMessagePacket(
                opcode,
                originalSenderNetId,
                route,
                descriptor.LocationTargeted,
                location,
                payload);
        }

        private static bool DispatchLocal<T>(
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message,
            ulong senderNetId,
            INetGameService? netService,
            bool isHostIngest)
        {
            var ctx = new RitsuLibSidecarSyncMessageContext<T>(
                message,
                senderNetId,
                netService,
                isHostIngest,
                descriptor.LocationTargeted
                    ? RunManager.Instance?.RunLocationTargetedBuffer?.CurrentLocation
                    : null);
            _ = InvokeHandlerAsync(descriptor.Handle, ctx);
            return true;
        }

        private static void HandleIncoming(RitsuLibSidecarDispatchContext context)
        {
            if (!RitsuLibSidecarSync.TryReadMessagePacket(context.Payload.Span, out var packet))
            {
                RitsuLibFramework.Logger.Warn("[SidecarSync] Rejected malformed sync message packet.");
                return;
            }

            RegistrationBase? registration;
            lock (Gate)
            {
                Registrations.TryGetValue(packet.DescriptorOpcode, out registration);
            }

            if (registration == null)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SidecarSync] No sync message descriptor registered for opcode {packet.DescriptorOpcode}.");
                return;
            }

            if (registration.LocationTargeted && !packet.LocationTargeted)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SidecarSync] Sync message opcode {packet.DescriptorOpcode} missing required location.");
                return;
            }

            if (RitsuLibSidecarSync.TryDeferForLocation(packet.LocationTargeted, packet.Location, context))
                return;

            if (context.IsHostIngest &&
                (packet.Route == RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast ||
                 registration.ShouldBroadcast) &&
                RunManager.Instance?.NetService is NetHostGameService host)
                if (!RitsuLibSidecarSync.TryBroadcastToPeers(
                        host,
                        RitsuLibSidecarSync.MessageOpcode,
                        context.Envelope.Payload.Span,
                        context.SenderNetId,
                        registration.BroadcastScope,
                        context.TransferMode,
                        context.Channel,
                        registration.FailurePolicy))
                {
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[SidecarSync] Required relay failed for sync message {registration.ModuleId}/{registration.MessageKey}; local handler suppressed.");
                    return;
                }

            registration.Dispatch(packet, context);
        }

        private static bool CanSendToPeer(ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId);
        }

        private static bool FailUnavailablePeer<T>(
            ulong peerNetId,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor)
        {
            if (descriptor.FailurePolicy == RitsuLibSidecarSyncFailurePolicy.Required)
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarSync] Required sync message {descriptor.ModuleId}/{descriptor.MessageKey} cannot reach peer {peerNetId}; send suppressed.");

            return false;
        }

        private static int ResolveChannel<T>(RitsuLibSidecarSyncMessageDescriptor<T> descriptor)
        {
            return descriptor.Channel ?? descriptor.Mode.ToChannelId();
        }

        private static void ValidateDescriptorPolicy<T>(RitsuLibSidecarSyncMessageDescriptor<T> descriptor)
        {
            if (!Enum.IsDefined(descriptor.LogLevel))
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.LogLevel, "Invalid log level.");
            if (!Enum.IsDefined(descriptor.Mode))
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Mode, "Invalid transfer mode.");
            if (descriptor.Channel is < 0)
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Channel,
                    "Channel cannot be negative.");
            if (!Enum.IsDefined(descriptor.FailurePolicy))
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.FailurePolicy,
                    "Invalid sync failure policy.");
            if (!Enum.IsDefined(descriptor.BroadcastScope))
                throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.BroadcastScope,
                    "Invalid sync broadcast scope.");
        }

        internal static bool CanSendToAllTargetPeers(
            NetHostGameService host,
            ulong? excludePeerId,
            RitsuLibSidecarSyncBroadcastScope scope)
        {
            return TargetPeers(host, excludePeerId, scope)
                .All(peer => CanSendToPeer(peer.peerId));
        }

        internal static IEnumerable<NetClientData> TargetPeers(
            NetHostGameService host,
            ulong? excludePeerId,
            RitsuLibSidecarSyncBroadcastScope scope)
        {
            return host.ConnectedPeers.Where(peer =>
                peer.peerId != excludePeerId &&
                (scope == RitsuLibSidecarSyncBroadcastScope.AllConnectedPeers || peer.readyForBroadcasting));
        }

        private static async Task InvokeHandlerAsync<T>(
            Func<RitsuLibSidecarSyncMessageContext<T>, Task> handler,
            RitsuLibSidecarSyncMessageContext<T> context)
        {
            try
            {
                await handler(context);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.ErrorNoTrace($"[SidecarSync] Sync message handler failed: {ex}");
            }
        }

        private abstract class RegistrationBase(
            string moduleId,
            string messageKey,
            bool locationTargeted,
            bool shouldBuffer,
            NetTransferMode mode,
            int channel,
            RitsuLibSidecarSyncFailurePolicy failurePolicy,
            RitsuLibSidecarSyncBroadcastScope broadcastScope,
            LogLevel logLevel,
            bool shouldBroadcast)
        {
            public string ModuleId { get; } = moduleId;
            public string MessageKey { get; } = messageKey;
            public bool LocationTargeted { get; } = locationTargeted;
            public bool ShouldBuffer { get; } = shouldBuffer;
            public NetTransferMode Mode { get; } = mode;
            public int Channel { get; } = channel;
            public RitsuLibSidecarSyncFailurePolicy FailurePolicy { get; } = failurePolicy;
            public RitsuLibSidecarSyncBroadcastScope BroadcastScope { get; } = broadcastScope;

            public LogLevel LogLevel { get; } = logLevel;
            public bool ShouldBroadcast { get; } = shouldBroadcast;

            public abstract void Dispatch(RitsuLibSidecarSyncMessagePacket packet,
                RitsuLibSidecarDispatchContext rawContext);
        }

        private sealed class Registration<T>(
            string moduleId,
            string messageKey,
            Func<ReadOnlySpan<byte>, T> deserialize,
            Func<RitsuLibSidecarSyncMessageContext<T>, Task> handle,
            bool locationTargeted,
            bool shouldBuffer,
            NetTransferMode mode,
            int channel,
            RitsuLibSidecarSyncFailurePolicy failurePolicy,
            RitsuLibSidecarSyncBroadcastScope broadcastScope,
            LogLevel logLevel,
            bool shouldBroadcast)
            : RegistrationBase(moduleId, messageKey, locationTargeted, shouldBuffer, mode, channel, failurePolicy,
                broadcastScope, logLevel, shouldBroadcast)
        {
            public override void Dispatch(RitsuLibSidecarSyncMessagePacket packet,
                RitsuLibSidecarDispatchContext rawContext)
            {
                T message;
                try
                {
                    message = deserialize(packet.Payload);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SidecarSync] Failed to deserialize sync message {ModuleId}/{MessageKey}: {ex.Message}");
                    return;
                }

                var context = new RitsuLibSidecarSyncMessageContext<T>(
                    message,
                    packet.OriginalSenderNetId,
                    RunManager.Instance?.NetService,
                    rawContext.IsHostIngest,
                    packet.LocationTargeted ? packet.Location : null);
                NetworkLogger.LogMessage(LogLevel,
                    $"Received sidecar sync message {ModuleId}/{MessageKey}, sending to 1 handlers", 0);
                _ = InvokeHandlerAsync(handle, context);
            }
        }
    }
}
