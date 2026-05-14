using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     JSON serializer helper used by typed sidecar descriptors.
    ///     类型化 sidecar descriptor 使用的 JSON serializer 辅助方法。
    /// </summary>
    public sealed class RitsuLibSidecarJsonSerializer<T>
    {
        /// <summary>
        ///     Serializes a message into UTF-8 JSON bytes.
        ///     将消息序列化为 UTF-8 JSON 字节。
        /// </summary>
        public byte[] Serialize(T message)
        {
            return JsonSerializer.SerializeToUtf8Bytes(message);
        }

        /// <summary>
        ///     Deserializes a message from UTF-8 JSON bytes.
        ///     从 UTF-8 JSON 字节反序列化消息。
        /// </summary>
        public T Deserialize(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<T>(payload)
                   ?? throw new InvalidOperationException($"Failed to deserialize typed sidecar payload: {typeof(T)}");
        }
    }

    /// <summary>
    ///     Typed sidecar descriptor containing module key, message key, serializer delegates, and delivery semantics.
    ///     包含 module key、message key、serializer 委托和投递语义的类型化 sidecar 描述符。
    /// </summary>
    public sealed record RitsuLibSidecarMessageDescriptor<T>(
        string ModuleId,
        string MessageKey,
        Func<T, byte[]> Serialize,
        Func<ReadOnlySpan<byte>, T> Deserialize,
        RitsuLibSidecarDeliverySemantics Delivery = RitsuLibSidecarDeliverySemantics.StableSync,
        bool Required = false);

    /// <summary>
    ///     Dispatch context for one typed message delivery.
    ///     单次类型化消息投递的分发上下文。
    /// </summary>
    public readonly record struct RitsuLibSidecarTypedDispatchContext<T>(
        T Message,
        ulong SenderNetId,
        NetTransferMode TransferMode,
        int Channel,
        bool IsHostIngest);

    /// <summary>
    ///     Event payload emitted after typed message dispatch.
    ///     类型化消息分发后发出的事件载荷。
    /// </summary>
    public readonly record struct SidecarTypedMessageReceivedEvent(
        ulong Opcode,
        string ModuleId,
        string MessageKey,
        ulong SenderNetId);

    /// <summary>
    ///     Typed sidecar registry for descriptor registration, collision checks, subscriptions, and convenience sends.
    ///     用于 descriptor 注册、冲突检查、订阅和便捷发送的类型化 sidecar 注册表。
    /// </summary>
    public static class RitsuLibSidecarTypedMessageRegistry
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RegistrationBase> Registrations = [];

        /// <summary>
        ///     Raised after any typed message is successfully deserialized and dispatched.
        ///     任意类型化消息成功反序列化并分发后引发。
        /// </summary>
        public static event Action<SidecarTypedMessageReceivedEvent>? TypedMessageReceived;

        /// <summary>
        ///     Registers a descriptor and returns its stable opcode. Re-registering the same descriptor returns the same
        ///     opcode.
        ///     注册 descriptor 并返回其稳定 opcode。重复注册同一 descriptor 会返回相同的
        ///     opcode。
        /// </summary>
        public static ulong Register<T>(RitsuLibSidecarMessageDescriptor<T> descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.ModuleId);
            ArgumentException.ThrowIfNullOrEmpty(descriptor.MessageKey);
            ArgumentNullException.ThrowIfNull(descriptor.Serialize);
            ArgumentNullException.ThrowIfNull(descriptor.Deserialize);

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
        ///     Subscribes one handler to a typed descriptor. Disposing the return value unsubscribes it.
        ///     为类型化 descriptor 订阅一个处理器。释放返回值会取消订阅。
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
        ///     Sends a typed message from client to host using a direct net service reference.
        ///     使用直接 net service 引用从客户端向主机发送类型化消息。
        /// </summary>
        public static bool SendToHost<T>(INetGameService? netService, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var payload = descriptor.Serialize(message);
            return RitsuLibSidecarHighLevelSend.TrySendAsClient(netService, opcode, payload, descriptor.Delivery);
        }

        /// <summary>
        ///     Sends a typed message from client to host using <see cref="RunManager" />.
        ///     使用 <see cref="RunManager" /> 从客户端向主机发送类型化消息。
        /// </summary>
        public static bool SendToHost<T>(RunManager? runManager, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var payload = descriptor.Serialize(message);
            return RitsuLibSidecarHighLevelSend.TrySendAsClient(runManager, opcode, payload, descriptor.Delivery);
        }

        /// <summary>
        ///     Sends a typed message from host to one peer.
        ///     从主机向一个对等端发送类型化消息。
        /// </summary>
        public static bool SendToPeer<T>(INetGameService? netService, ulong peerNetId,
            RitsuLibSidecarMessageDescriptor<T> descriptor, T message)
        {
            var opcode = Register(descriptor);
            var payload = descriptor.Serialize(message);
            return RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(netService, peerNetId, opcode, payload,
                descriptor.Delivery);
        }

        /// <summary>
        ///     Broadcasts a typed message to sidecar-reachable peers using a direct net service reference.
        ///     使用直接 net service 引用向 sidecar 可达的对等端广播类型化消息。
        /// </summary>
        public static bool Broadcast<T>(INetGameService? netService, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var payload = descriptor.Serialize(message);
            return RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(netService, opcode, payload,
                descriptor.Delivery);
        }

        /// <summary>
        ///     Broadcasts a typed message to sidecar-reachable peers using <see cref="RunManager" />.
        ///     使用 <see cref="RunManager" /> 向 sidecar 可达的对等端广播类型化消息。
        /// </summary>
        public static bool Broadcast<T>(RunManager? runManager, RitsuLibSidecarMessageDescriptor<T> descriptor,
            T message)
        {
            var opcode = Register(descriptor);
            var payload = descriptor.Serialize(message);
            return RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(runManager, opcode, payload,
                descriptor.Delivery);
        }

        private static void HandleDispatch<T>(ulong opcode, Registration<T> registration,
            in RitsuLibSidecarDispatchContext context)
        {
            T message;
            try
            {
                message = registration.Deserialize(context.Payload.Span);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Sidecar] Typed message deserialize failed opcode={opcode}: {ex.Message}");
                return;
            }

            Action<RitsuLibSidecarTypedDispatchContext<T>>[] handlers;
            lock (Gate)
            {
                handlers = [..registration.Handlers];
            }

            var typedContext = new RitsuLibSidecarTypedDispatchContext<T>(
                message,
                context.SenderNetId,
                context.TransferMode,
                context.Channel,
                context.IsHostIngest);
            foreach (var handler in handlers)
                handler(typedContext);

            TypedMessageReceived?.Invoke(
                new(opcode, registration.ModuleId, registration.MessageKey, context.SenderNetId));
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
