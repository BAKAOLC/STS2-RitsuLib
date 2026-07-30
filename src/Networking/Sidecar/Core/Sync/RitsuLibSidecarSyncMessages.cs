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
    ///     <para xml:lang="en">Specifies whether a Sidecar sync message tolerates unavailable peers or send failures.</para>
    ///     <para xml:lang="zh-CN">指定 Sidecar 同步消息是否容忍不可用的对等方或发送失败。</para>
    /// </summary>
    public enum RitsuLibSidecarSyncFailurePolicy : byte
    {
        /// <summary>
        ///     <para xml:lang="en">For game-flow messages, every targeted Sidecar-capable peer must be reachable before local handling runs.</para>
        ///     <para xml:lang="zh-CN">用于游戏流程消息；本地处理前，每个目标且支持 Sidecar 的对等方都必须可达。</para>
        /// </summary>
        Required = 0,

        /// <summary>
        ///     <para xml:lang="en">For non-gameplay messages only; unavailable peers are skipped and failures do not block local handling.</para>
        ///     <para xml:lang="zh-CN">仅用于非游戏流程消息；不可用的对等方会被跳过，失败不会阻止本地处理。</para>
        /// </summary>
        BestEffort = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">Specifies the host broadcast target set for a Sidecar sync message.</para>
    ///     <para xml:lang="zh-CN">指定 Sidecar 同步消息的主机广播目标集合。</para>
    /// </summary>
    public enum RitsuLibSidecarSyncBroadcastScope : byte
    {
        /// <summary>
        ///     <para xml:lang="en">Matches the host broadcast behavior of vanilla <see cref="INetGameService.SendMessage{T}(T)" />.</para>
        ///     <para xml:lang="zh-CN">与原版 <see cref="INetGameService.SendMessage{T}(T)" /> 的主机广播行为一致。</para>
        /// </summary>
        ReadyPeers = 0,

        /// <summary>
        ///     <para xml:lang="en">Sends to every connected peer, for lobby or session flows before vanilla marks peers ready.</para>
        ///     <para xml:lang="zh-CN">向每个已连接对等方发送，用于原版将对等方标记为就绪前的大厅或会话流程。</para>
        /// </summary>
        AllConnectedPeers = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a Sidecar message with vanilla-like routing and delivery policy without registering an
    ///         <see cref="INetMessage" /> subtype in the game's generated message-ID table.
    ///     </para>
    ///     <para xml:lang="zh-CN">描述具备原版式路由和传递策略的 Sidecar 消息，而不在游戏生成的消息 ID 表中注册 <see cref="INetMessage" /> 子类型。</para>
    /// </summary>
    /// <param name="ModuleId">
    ///     <para xml:lang="en">Stable owner ID used to derive the opcode.</para>
    ///     <para xml:lang="zh-CN">用于派生操作码的稳定所有者 ID。</para>
    /// </param>
    /// <param name="MessageKey">
    ///     <para xml:lang="en">Stable message key used to derive the opcode.</para>
    ///     <para xml:lang="zh-CN">用于派生操作码的稳定消息键。</para>
    /// </param>
    /// <param name="Serialize">
    ///     <para xml:lang="en">Serializes the typed payload.</para>
    ///     <para xml:lang="zh-CN">序列化强类型载荷。</para>
    /// </param>
    /// <param name="Deserialize">
    ///     <para xml:lang="en">Deserializes the typed payload.</para>
    ///     <para xml:lang="zh-CN">反序列化强类型载荷。</para>
    /// </param>
    /// <param name="Handle">
    ///     <para xml:lang="en">Runs after buffering and optional location gating release the message.</para>
    ///     <para xml:lang="zh-CN">在缓冲和可选位置门控释放消息后运行。</para>
    /// </param>
    /// <param name="LocationTargeted">
    ///     <para xml:lang="en">Whether the message carries and waits for the current run location.</para>
    ///     <para xml:lang="zh-CN">消息是否携带并等待当前局内位置。</para>
    /// </param>
    /// <param name="ShouldBuffer">
    ///     <para xml:lang="en">Whether the message waits behind vanilla <see cref="NetMessageBus" /> buffering.</para>
    ///     <para xml:lang="zh-CN">消息是否等待原版 <see cref="NetMessageBus" /> 缓冲。</para>
    /// </param>
    /// <param name="Mode">
    ///     <para xml:lang="en">Vanilla transport mode used to send the message.</para>
    ///     <para xml:lang="zh-CN">发送消息所用的原版传输模式。</para>
    /// </param>
    /// <param name="Channel">
    ///     <para xml:lang="en">Optional explicit channel; <see langword="null" /> uses <see cref="NetTransferModeExtensions.ToChannelId" />.</para>
    ///     <para xml:lang="zh-CN">可选显式通道；<see langword="null" /> 时使用 <see cref="NetTransferModeExtensions.ToChannelId" />。</para>
    /// </param>
    /// <param name="FailurePolicy">
    ///     <para xml:lang="en">Whether every targeted recipient is required for game-flow safety.</para>
    ///     <para xml:lang="zh-CN">游戏流程安全是否要求每个目标接收方可用。</para>
    /// </param>
    /// <param name="BroadcastScope">
    ///     <para xml:lang="en">Host peers receiving host-originated or host-relayed broadcasts.</para>
    ///     <para xml:lang="zh-CN">接收主机发起或主机转发广播的主机对等方。</para>
    /// </param>
    /// <param name="DispatchLocalOnBroadcast">
    ///     <para xml:lang="en">Whether host or single-player broadcasts also start the local handler.</para>
    ///     <para xml:lang="zh-CN">主机或单人游戏广播是否也启动本地处理器。</para>
    /// </param>
    /// <param name="LogLevel">
    ///     <para xml:lang="en">Vanilla-style network receive log level.</para>
    ///     <para xml:lang="zh-CN">原版式网络接收日志级别。</para>
    /// </param>
    /// <param name="ShouldBroadcast">
    ///     <para xml:lang="en">Whether client-originated sends request host relay.</para>
    ///     <para xml:lang="zh-CN">客户端发起的发送是否请求主机转发。</para>
    /// </param>
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
        ///     <para xml:lang="en">Preserves the original constructor ABI for mods compiled before transport and failure policies were added.</para>
        ///     <para xml:lang="zh-CN">为传输和失败策略加入前编译的模组保留原始构造函数 ABI。</para>
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
    ///     <para xml:lang="en">Provides runtime context to a Sidecar sync-message handler.</para>
    ///     <para xml:lang="zh-CN">向 Sidecar 同步消息处理器提供运行时上下文。</para>
    /// </summary>
    /// <param name="Message">
    ///     <para xml:lang="en">Typed message payload.</para>
    ///     <para xml:lang="zh-CN">强类型消息载荷。</para>
    /// </param>
    /// <param name="SenderNetId">
    ///     <para xml:lang="en">Original vanilla sender ID, preserved through host relay.</para>
    ///     <para xml:lang="zh-CN">通过主机转发保留的原始原版发送方 ID。</para>
    /// </param>
    /// <param name="NetService">
    ///     <para xml:lang="en">Current network service when available.</para>
    ///     <para xml:lang="zh-CN">可用时的当前网络服务。</para>
    /// </param>
    /// <param name="IsHostIngest">
    ///     <para xml:lang="en">Whether this peer received the packet as host.</para>
    ///     <para xml:lang="zh-CN">此对等方是否以主机身份接收该数据包。</para>
    /// </param>
    /// <param name="Location">
    ///     <para xml:lang="en">Run location carried by a location-targeted descriptor.</para>
    ///     <para xml:lang="zh-CN">由位置目标描述符携带的局内位置。</para>
    /// </param>
    public readonly record struct RitsuLibSidecarSyncMessageContext<T>(
        T Message,
        ulong SenderNetId,
        INetGameService? NetService,
        bool IsHostIngest,
        RunLocation? Location);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Sends Sidecar messages with vanilla-style routing, buffering, and optional run-location gating. Local
    ///         handlers start asynchronously; their failures are logged and do not change a prior successful send result.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使用原版式路由、缓冲和可选局内位置门控发送 Sidecar 消息。本地处理器异步启动；其失败会被记录，不会改变先前成功的发送结果。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarSyncMessages
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];
        private static readonly Logger NetworkLogger = new("RitsuLibSidecarSync", LogType.Network);

        /// <summary>
        ///     <para xml:lang="en">Registers a sync-message descriptor and returns its stable Sidecar opcode.</para>
        ///     <para xml:lang="zh-CN">注册同步消息描述符并返回其稳定 Sidecar 操作码。</para>
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
        ///     <para xml:lang="en">Sends a sync message using <see cref="INetGameService.SendMessage{T}(T)" />-style routing semantics.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="INetGameService.SendMessage{T}(T)" /> 式路由语义发送同步消息。</para>
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
        ///     <para xml:lang="en">Sends a sync message from client to host, or starts local handling for host and single-player services.</para>
        ///     <para xml:lang="zh-CN">从客户端向主机发送同步消息，或在主机和单人游戏服务中启动本地处理。</para>
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
        ///     <para xml:lang="en">Sends a sync message to the host and requests relay to the descriptor's broadcast scope.</para>
        ///     <para xml:lang="zh-CN">向主机发送同步消息，并请求转发到描述符的广播范围。</para>
        /// </summary>
        public static bool SendToHostAndBroadcast<T>(
            INetGameService? netService,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            if (!descriptor.ShouldBroadcast)
                return false;

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
        ///     <para xml:lang="en">Sends a sync message from host to one peer.</para>
        ///     <para xml:lang="zh-CN">从主机向单个对等方发送同步消息。</para>
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
        ///     <para xml:lang="en">
        ///         Broadcasts a sync message from the host to the descriptor's Sidecar-capable target peers and, when
        ///         configured, starts its local handler after the remote broadcast succeeds.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从主机向描述符的支持 Sidecar 的目标对等方广播同步消息，并在配置要求时于远程广播成功后启动本地处理器。
        ///     </para>
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

                shouldRelay = registration.ShouldBroadcast;
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
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"sync-malformed-packet:sender={context.SenderNetId}:ch={context.Channel}",
                    "[SidecarSync] Rejected malformed sync message packet.");
                return;
            }

            RegistrationBase? registration;
            lock (Gate)
            {
                Registrations.TryGetValue(packet.DescriptorOpcode, out registration);
            }

            if (registration == null)
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"sync-missing-descriptor:opcode={packet.DescriptorOpcode}:sender={context.SenderNetId}",
                    $"[SidecarSync] No sync message descriptor registered for opcode {packet.DescriptorOpcode}.");
                return;
            }

            if (registration.LocationTargeted && !packet.LocationTargeted)
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"sync-missing-location:opcode={packet.DescriptorOpcode}:sender={context.SenderNetId}",
                    $"[SidecarSync] Sync message opcode {packet.DescriptorOpcode} missing required location.");
                return;
            }

            if (RitsuLibSidecarSync.TryDeferForLocation(packet.LocationTargeted, packet.Location, context))
                return;

            if (context.IsHostIngest &&
                registration.ShouldBroadcast &&
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
                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        $"sync-deserialize:{ModuleId}/{MessageKey}:{ex.GetType().FullName}:{ex.Message}",
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
