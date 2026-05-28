using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal enum RitsuLibSidecarSyncMessageRoute : byte
    {
        Direct = 0,
        ClientToHostAndBroadcast = 1,
    }

    /// <summary>
    ///     Describes a reliable sidecar message for vanilla channel-0 delivery and vanilla-aligned buffering gates.
    ///     描述一个可靠的 sidecar 消息；它使用原版 channel 0 投递，并对齐原版缓冲门控。
    /// </summary>
    /// <param name="ModuleId">Stable owner id for opcode derivation.</param>
    /// <param name="MessageKey">Stable message key for opcode derivation.</param>
    /// <param name="Serialize">Serializes the typed payload.</param>
    /// <param name="Deserialize">Deserializes the typed payload.</param>
    /// <param name="Handle">Runs when buffering and optional location gating have released the message.</param>
    /// <param name="LocationTargeted">When true, the message carries the current run location and waits for it.</param>
    public sealed record RitsuLibSidecarSyncMessageDescriptor<T>(
        string ModuleId,
        string MessageKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        Func<RitsuLibSidecarSyncMessageContext<T>, Task> Handle,
        bool LocationTargeted = false);

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
                    descriptor.LocationTargeted);
            }

            return opcode;
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
            if (netService is not NetHostGameService host || !CanSendToPeer(peerNetId))
                return false;

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(host.NetId, descriptor, opcode, RitsuLibSidecarSyncMessageRoute.Direct, payload);
            return RitsuLibSidecarSync.TrySendToPeer(host, peerNetId,
                RitsuLibSidecarSync.MessageOpcode, packet);
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
                return DispatchLocal(descriptor, message, netService.NetId, netService, false);

            if (netService is not NetHostGameService host || !CanSendToAllReadyPeers(host, null))
                return false;

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(host.NetId, descriptor, opcode, RitsuLibSidecarSyncMessageRoute.Direct, payload);
            return RitsuLibSidecarSync.TryBroadcastToReadyPeers(host,
                RitsuLibSidecarSync.MessageOpcode,
                packet,
                null) && DispatchLocal(descriptor, message, host.NetId, host, true);
        }

        /// <inheritdoc cref="Broadcast{T}(INetGameService?, RitsuLibSidecarSyncMessageDescriptor{T}, T)" />
        public static bool Broadcast<T>(
            RunManager? runManager,
            RitsuLibSidecarSyncMessageDescriptor<T> descriptor,
            T message)
        {
            return Broadcast(runManager?.NetService, descriptor, message);
        }

        internal static void RegisterBuiltInHandler()
        {
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarSync.MessageOpcode, HandleIncoming);
        }

        internal static void HandleBuffered(in RitsuLibSidecarDispatchContext context)
        {
            HandleIncoming(context);
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
                        netService.Type == NetGameType.Host);
                case NetGameType.Host:
                    return route == RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast
                        ? Broadcast(netService, descriptor, message)
                        : DispatchLocal(descriptor, message, netService.NetId, netService, true);
            }

            if (netService is not NetClientGameService client || !CanSendToPeer(client.HostNetId))
                return false;

            var payload = descriptor.Serialize(message);
            var packet = BuildPacket(client.NetId, descriptor, opcode, route, payload);
            return RitsuLibSidecarSync.TrySendToHost(client,
                RitsuLibSidecarSync.MessageOpcode,
                packet);
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
                packet.Route == RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast &&
                RunManager.Instance?.NetService is NetHostGameService host)
            {
                if (!CanSendToAllReadyPeers(host, context.SenderNetId))
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SidecarSync] Relay skipped for sync message opcode {packet.DescriptorOpcode}: at least one ready peer does not support sidecar.");
                    return;
                }

                _ = RitsuLibSidecarSync.TryBroadcastToReadyPeers(
                    host,
                    RitsuLibSidecarSync.MessageOpcode,
                    context.Envelope.Payload.Span,
                    context.SenderNetId);
            }

            registration.Dispatch(packet, context);
        }

        private static bool CanSendToPeer(ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId);
        }

        internal static bool CanSendToAllReadyPeers(NetHostGameService host, ulong? excludePeerId)
        {
            return host.ConnectedPeers.Where(peer => peer.readyForBroadcasting && peer.peerId != excludePeerId)
                .All(peer => CanSendToPeer(peer.peerId));
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
                RitsuLibFramework.Logger.Error($"[SidecarSync] Sync message handler failed: {ex}");
            }
        }

        private abstract class RegistrationBase(string moduleId, string messageKey, bool locationTargeted)
        {
            public string ModuleId { get; } = moduleId;
            public string MessageKey { get; } = messageKey;
            public bool LocationTargeted { get; } = locationTargeted;

            public abstract void Dispatch(RitsuLibSidecarSyncMessagePacket packet,
                RitsuLibSidecarDispatchContext rawContext);
        }

        private sealed class Registration<T>(
            string moduleId,
            string messageKey,
            Func<ReadOnlySpan<byte>, T> deserialize,
            Func<RitsuLibSidecarSyncMessageContext<T>, Task> handle,
            bool locationTargeted)
            : RegistrationBase(moduleId, messageKey, locationTargeted)
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
                _ = InvokeHandlerAsync(handle, context);
            }
        }
    }
}
