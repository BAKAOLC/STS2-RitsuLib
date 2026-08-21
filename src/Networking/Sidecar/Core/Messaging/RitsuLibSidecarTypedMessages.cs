using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">JSON serializer for typed Sidecar message descriptors.</para>
    ///     <para xml:lang="zh-CN">类型化 Sidecar 消息描述符使用的 JSON 序列化器。</para>
    /// </summary>
    public sealed class RitsuLibSidecarJsonSerializer<T>
    {
        /// <summary>
        ///     <para xml:lang="en">Serializes a message into UTF-8 JSON bytes.</para>
        ///     <para xml:lang="zh-CN">将消息序列化为 UTF-8 JSON 字节。</para>
        /// </summary>
        public byte[] Serialize(T message)
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }

        /// <summary>
        ///     <para xml:lang="en">Deserializes a message from UTF-8 JSON bytes.</para>
        ///     <para xml:lang="zh-CN">从 UTF-8 JSON 字节反序列化消息。</para>
        /// </summary>
        public T Deserialize(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<T>(payload)
                   ?? throw new InvalidOperationException($"Failed to deserialize typed sidecar payload: {typeof(T)}");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Typed Sidecar descriptor containing module and message keys, serialization delegates, and delivery semantics.
    ///     </para>
    ///     <para xml:lang="zh-CN">包含模组键、消息键、序列化委托及投递语义的类型化 Sidecar 描述符。</para>
    /// </summary>
    public sealed record RitsuLibSidecarMessageDescriptor<T>(
        string ModuleId,
        string MessageKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        RitsuLibSidecarDeliverySemantics Delivery = RitsuLibSidecarDeliverySemantics.StableSync,
        bool Required = false);

    /// <summary>
    ///     <para xml:lang="en">Dispatch context for one typed message delivery.</para>
    ///     <para xml:lang="zh-CN">单次类型化消息投递的分发上下文。</para>
    /// </summary>
    public readonly record struct RitsuLibSidecarTypedDispatchContext<T>(
        T Message,
        ulong SenderNetId,
        NetTransferMode TransferMode,
        int Channel,
        bool IsHostIngest);

    /// <summary>
    ///     <para xml:lang="en">Event payload emitted after typed message dispatch.</para>
    ///     <para xml:lang="zh-CN">类型化消息分发后发出的事件载荷。</para>
    /// </summary>
    public readonly record struct SidecarTypedMessageReceivedEvent(
        ulong Opcode,
        string ModuleId,
        string MessageKey,
        ulong SenderNetId);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registry for typed Sidecar descriptors, collision checks, subscriptions, and convenience send methods.
    ///     </para>
    ///     <para xml:lang="zh-CN">用于类型化 Sidecar 描述符注册、冲突检查、订阅及便捷发送的注册表。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Registered descriptors receive a bounded routed-endpoint compatibility bridge when local capacity permits.
    ///         Convenience sends prefer the routed path for each compatible, route-confirmed recipient and use the legacy
    ///         opcode path only for recipients or payloads that cannot use it. Split broadcasts never send both paths to
    ///         the same recipient.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         本地容量允许时，已注册描述符会获得有界的路由端点兼容桥。便捷发送会针对每个兼容且已确认路由的
    ///         接收方优先使用新路径，仅在接收方或载荷不适用时回退旧操作码路径；拆分广播绝不会向同一接收方
    ///         同时发送两条路径。
    ///     </para>
    /// </remarks>
    public static class RitsuLibSidecarTypedMessageRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];
        private static int _migrationEndpointCount;

        /// <summary>
        ///     <para xml:lang="en">Raised after any typed message is successfully deserialized and dispatched.</para>
        ///     <para xml:lang="zh-CN">任意类型化消息成功反序列化并分发后引发。</para>
        /// </summary>
        public static event Action<SidecarTypedMessageReceivedEvent>? TypedMessageReceived;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a descriptor and returns its stable opcode. Re-registering the same module ID, message key,
        ///         and payload type returns the existing opcode without replacing its serialization or delivery settings.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册描述符并返回其稳定操作码。使用相同模组 ID、消息键和载荷类型重复注册时，
        ///         返回已有操作码，且不会替换其序列化或投递设置。
        ///     </para>
        /// </summary>
        public static ulong Register<T>(RitsuLibSidecarMessageDescriptor<T> descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.ModuleId);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.MessageKey);
            ArgumentNullException.ThrowIfNull(descriptor.Serialize);
            ArgumentNullException.ThrowIfNull(descriptor.Deserialize);
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();

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
                        $"Sidecar typed message opcode conflict: {descriptor.ModuleId}/{descriptor.MessageKey} -> {opcode}");
                }

                var reg = new Registration<T>(
                    descriptor.ModuleId,
                    descriptor.MessageKey,
                    descriptor.Serialize,
                    descriptor.Deserialize,
                    descriptor.Delivery);
                TryAttachMigrationEndpoint(opcode, reg);
                Registrations[opcode] = reg;
                RitsuLibSidecarBus.RegisterHandler(opcode, ctx => HandleDispatch(opcode, reg, in ctx));
            }

            if (descriptor.Required)
                RitsuLibSidecarRequiredCapabilities.RegisterRequiredCapability(
                    $"{descriptor.ModuleId}:{descriptor.MessageKey}",
                    RitsuLibSidecarSessionManager.CanSendToPeer);
            return opcode;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Subscribes one handler to a typed descriptor. Disposing the returned value unsubscribes it.
        ///     </para>
        ///     <para xml:lang="zh-CN">为类型化描述符订阅一个处理器。释放返回值即可取消订阅。</para>
        /// </summary>
        public static IDisposable Subscribe<T>(
            RitsuLibSidecarMessageDescriptor<T> descriptor,
            Action<RitsuLibSidecarTypedDispatchContext<T>> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            var opcode = Register(descriptor);
            lock (Gate)
            {
                if (Registrations[opcode] is not Registration<T> reg)
                    throw new InvalidOperationException("Typed descriptor registered with incompatible payload type");

                reg.Handlers.Add(handler);
            }

            return new Subscription(() =>
            {
                lock (Gate)
                {
                    if (Registrations.TryGetValue(opcode, out var regBase) && regBase is Registration<T> typed)
                        typed.Handlers.Remove(handler);
                }
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Sends a typed message from client to host using a direct net service reference.</para>
        ///     <para xml:lang="zh-CN">使用直接网络服务引用从客户端向主机发送类型化消息。</para>
        /// </summary>
        public static bool SendToHost<T>(INetGameService? netService, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var registration = GetRegistration<T>(opcode);
            var payload = SerializePayload(registration, message);
            if (CanUseRoutedPath(netService) && registration.EndpointHandle is { } endpoint)
            {
                var result = endpoint.SendToHost(payload);
                if (result.IsAccepted)
                    return true;
                if (!ShouldFallbackToLegacy(result.Status))
                    return false;
            }

            return RitsuLibSidecarHighLevelSend.TrySendAsClient(netService, opcode, payload, registration.Delivery);
        }

        /// <summary>
        ///     <para xml:lang="en">Sends a typed message from client to host using <see cref="RunManager" />.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="RunManager" /> 从客户端向主机发送类型化消息。</para>
        /// </summary>
        public static bool SendToHost<T>(RunManager? runManager, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            return SendToHost(runManager?.NetService, descriptor, message);
        }

        /// <summary>
        ///     <para xml:lang="en">Sends a typed message from host to one peer.</para>
        ///     <para xml:lang="zh-CN">从主机向一个对等端发送类型化消息。</para>
        /// </summary>
        public static bool SendToPeer<T>(INetGameService? netService, ulong peerNetId,
            RitsuLibSidecarMessageDescriptor<T> descriptor, T message)
        {
            var opcode = Register(descriptor);
            var registration = GetRegistration<T>(opcode);
            var payload = SerializePayload(registration, message);
            if (CanUseRoutedPath(netService) && registration.EndpointHandle is { } endpoint)
            {
                var result = endpoint.SendToPeer(peerNetId, payload);
                if (result.IsAccepted)
                    return true;
                if (!ShouldFallbackToLegacy(result.Status))
                    return false;
            }

            return RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(netService, peerNetId, opcode, payload,
                registration.Delivery);
        }

        /// <summary>
        ///     <para xml:lang="en">Broadcasts a typed message to Sidecar-reachable peers using a direct net-service reference.</para>
        ///     <para xml:lang="zh-CN">使用直接网络服务引用向 Sidecar 可达的对等端广播类型化消息。</para>
        /// </summary>
        public static bool Broadcast<T>(INetGameService? netService, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var registration = GetRegistration<T>(opcode);
            var payload = SerializePayload(registration, message);
            if (!CanUseRoutedPath(netService) ||
                netService is not NetHostGameService { IsConnected: true } host ||
                registration.EndpointHandle is not { } endpoint)
                return RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                    netService,
                    opcode,
                    payload,
                    registration.Delivery);

            var routedParticipants = endpoint.GetParticipantsSnapshot().ToHashSet();
            foreach (var peer in host.ConnectedPeers)
            {
                if (!peer.readyForBroadcasting ||
                    !RitsuLibSidecarSessionManager.CanSendToPeer(peer.peerId))
                    continue;

                if (routedParticipants.Contains(peer.peerId))
                {
                    var result = endpoint.SendToPeer(peer.peerId, payload);
                    if (result.IsAccepted || !ShouldFallbackToLegacy(result.Status))
                        continue;
                }

                RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    netService,
                    peer.peerId,
                    opcode,
                    payload,
                    registration.Delivery);
            }

            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Broadcasts a typed message to Sidecar-reachable peers using <see cref="RunManager" />.</para>
        ///     <para xml:lang="zh-CN">使用 <see cref="RunManager" /> 向 Sidecar 可达的对等端广播类型化消息。</para>
        /// </summary>
        public static bool Broadcast<T>(RunManager? runManager, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            return Broadcast(runManager?.NetService, descriptor, message);
        }

        private static void TryAttachMigrationEndpoint<T>(ulong opcode, Registration<T> registration)
        {
            if (_migrationEndpointCount >= RitsuLibSidecarEndpointPolicy.MaxLegacyTypedMigrationEndpoints)
                return;
            var deliveryProfile = ResolveDeliveryProfile(registration.Delivery);
            var maxPayloadBytes = deliveryProfile == RitsuLibSidecarDeliveryProfile.Control
                ? RitsuLibSidecarEndpointPolicy.MaxControlPayloadBytes
                : RitsuLibSidecarEndpointPolicy.MaxRealtimePayloadBytes;
            try
            {
                var endpoint = RitsuLibSidecarEndpoints.Register(
                    new(
                        "ritsulib.typed",
                        $"message/{opcode:x16}",
                        1,
                        1,
                        deliveryProfile,
                        RitsuLibSidecarEndpointTopology.HostAuthority,
                        maxPayloadBytes,
                        RitsuLibSidecarEndpointDispatchMode.ReceiveThread),
                    message => HandleEndpointDispatch(opcode, registration, message));
                registration.AttachEndpoint(endpoint);
                _migrationEndpointCount++;
            }
            catch (InvalidOperationException ex)
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"typed-endpoint-unavailable:opcode={opcode}:{ex.Message}",
                    $"[Sidecar] Routed typed-message endpoint unavailable opcode={opcode}; legacy path remains active: {ex.Message}");
            }
        }

        private static void HandleEndpointDispatch<T>(
            ulong opcode,
            Registration<T> registration,
            RitsuLibSidecarEndpointMessage endpointMessage)
        {
            var deliveryProfile = ResolveDeliveryProfile(registration.Delivery);
            if (!RitsuLibSidecarEndpointTransport.TryGetNetworkParameters(
                    deliveryProfile,
                    out var transferMode,
                    out var channel))
                return;
            HandlePayload(
                opcode,
                registration,
                endpointMessage.Payload.Span,
                endpointMessage.OriginalSenderNetId,
                transferMode,
                channel,
                RitsuLibSidecarSessionManager.CurrentNetService is NetHostGameService);
        }

        private static void HandleDispatch<T>(ulong opcode, Registration<T> registration,
            in RitsuLibSidecarDispatchContext context)
        {
            HandlePayload(
                opcode,
                registration,
                context.Payload.Span,
                context.SenderNetId,
                context.TransferMode,
                context.Channel,
                context.IsHostIngest);
        }

        private static void HandlePayload<T>(
            ulong opcode,
            Registration<T> registration,
            ReadOnlySpan<byte> payload,
            ulong senderNetId,
            NetTransferMode transferMode,
            int channel,
            bool isHostIngest)
        {
            T message;
            try
            {
                message = registration.Deserialize(payload);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"typed-deserialize:opcode={opcode}:sender={senderNetId}:{ex.GetType().FullName}:{ex.Message}",
                    $"[Sidecar] Typed message deserialize failed opcode={opcode}: {ex.Message}");
                return;
            }

            Action<RitsuLibSidecarTypedDispatchContext<T>>[] handlers;
            lock (Gate)
            {
                handlers = [.. registration.Handlers];
            }

            var typedContext = new RitsuLibSidecarTypedDispatchContext<T>(
                message,
                senderNetId,
                transferMode,
                channel,
                isHostIngest);
            foreach (var handler in handlers)
                handler(typedContext);

            TypedMessageReceived?.Invoke(
                new(opcode, registration.ModuleId, registration.MessageKey, senderNetId));
        }

        private static Registration<T> GetRegistration<T>(ulong opcode)
        {
            lock (Gate)
            {
                return Registrations.TryGetValue(opcode, out var registration) &&
                       registration is Registration<T> typed
                    ? typed
                    : throw new InvalidOperationException(
                        "Typed descriptor registered with incompatible payload type.");
            }
        }

        private static byte[] SerializePayload<T>(Registration<T> registration, T message)
        {
            return registration.Serialize(message)
                   ?? throw new InvalidOperationException("Typed message serializer returned null.");
        }

        private static bool CanUseRoutedPath(INetGameService? netService)
        {
            return ReferenceEquals(netService, RitsuLibSidecarSessionManager.CurrentNetService);
        }

        private static bool ShouldFallbackToLegacy(RitsuLibSidecarSendStatus status)
        {
            return status is RitsuLibSidecarSendStatus.RouteUnavailable
                or RitsuLibSidecarSendStatus.ProfileUnsupported
                or RitsuLibSidecarSendStatus.PayloadTooLarge
                or RitsuLibSidecarSendStatus.DestinationUnavailable;
        }

        private static RitsuLibSidecarDeliveryProfile ResolveDeliveryProfile(
            RitsuLibSidecarDeliverySemantics delivery)
        {
            return delivery == RitsuLibSidecarDeliverySemantics.BestEffort
                ? RitsuLibSidecarDeliveryProfile.RealtimeDatagram
                : RitsuLibSidecarDeliveryProfile.Control;
        }

        private abstract class RegistrationBase(string moduleId, string messageKey)
        {
            public string ModuleId { get; } = moduleId;
            public string MessageKey { get; } = messageKey;
        }

        private sealed class Registration<T>(
            string moduleId,
            string messageKey,
            Func<T, byte[]> serialize,
            Func<ReadOnlySpan<byte>, T> deserialize,
            RitsuLibSidecarDeliverySemantics delivery)
            : RegistrationBase(moduleId, messageKey)
        {
            public Func<T, byte[]> Serialize { get; } = serialize;
            public Func<ReadOnlySpan<byte>, T> Deserialize { get; } = deserialize;
            public RitsuLibSidecarDeliverySemantics Delivery { get; } = delivery;
            public List<Action<RitsuLibSidecarTypedDispatchContext<T>>> Handlers { get; } = [];
            public RitsuLibSidecarEndpointHandle? EndpointHandle { get; private set; }

            public void AttachEndpoint(RitsuLibSidecarEndpointHandle endpoint)
            {
                EndpointHandle = endpoint;
            }
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                dispose();
            }
        }
    }
}
