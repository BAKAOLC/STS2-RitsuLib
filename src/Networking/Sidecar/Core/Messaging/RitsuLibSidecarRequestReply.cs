using System.Buffers;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Request/await-reply helpers on top of <see cref="RitsuLibSidecarBus.WaitForNextAsync" /> for precise callback
    ///     control flow. Continuations after <c>await</c> often run on the thread pool; use
    ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(System.Threading.Tasks.Task{T})" />
    ///     when the follow-up must touch Godot nodes or scene-tree-only APIs.
    ///     基于 <see cref="RitsuLibSidecarBus.WaitForNextAsync" /> 的 request/await-reply 辅助方法，用于精确回调
    ///     控制流。<c>await</c> 之后的 continuation 通常在线程池上运行；当后续操作必须访问 Godot 节点或仅场景树 API 时，请使用
    ///     。
    /// </summary>
    public static class RitsuLibSidecarRequestReply
    {
        /// <summary>
        ///     Default timeout used by request/reply helpers.
        ///     请求/回复辅助方法使用的默认超时。
        /// </summary>
        public static readonly TimeSpan DefaultReplyTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     Client sends request to host and awaits one matching reply opcode.
        ///     客户端向主机发送请求，并等待一个匹配的回复 opcode。
        /// </summary>
        public static async Task<RitsuLibSidecarDispatchContext> SendRequestToHostAndWaitReplyAsync(
            RunManager? runManager,
            ulong requestOpcode,
            ReadOnlyMemory<byte> requestPayload,
            ulong replyOpcode,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                replyOpcode,
                effectiveTimeout,
                replyPredicate,
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    runManager,
                    requestOpcode,
                    requestPayload.Span,
                    RitsuLibSidecarDeliverySemantics.StableSync))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (client -> host)."));

            return await wait.ConfigureAwait(false);
        }

        /// <summary>
        ///     Client → host request/reply with an 8-byte correlation in the header extension; reply must use the same
        ///     correlation after the delivery byte (see <see cref="RitsuLibSidecarRequestCorrelation" />).
        ///     客户端 → 主机请求/回复，在 header 扩展中包含 8 字节 correlation；回复必须在投递字节后使用相同的
        ///     correlation（见 <see cref="RitsuLibSidecarRequestCorrelation" />）。
        /// </summary>
        public static async Task<RitsuLibSidecarDispatchContext> SendCorrelatedRequestToHostAndWaitReplyAsync(
            RunManager? runManager,
            ulong requestOpcode,
            ReadOnlyMemory<byte> requestPayload,
            ulong replyOpcode,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var correlationId = RitsuLibSidecarRequestCorrelation.AllocateCorrelationId();
            var extra = RitsuLibSidecarRequestCorrelation.PackAdditional(correlationId);
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                replyOpcode,
                effectiveTimeout,
                ctx =>
                    RitsuLibSidecarRequestCorrelation.HeaderExtensionCorrelationEquals(ctx.Envelope.HeaderExtension,
                        correlationId)
                    && (replyPredicate?.Invoke(ctx) ?? true),
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    runManager,
                    requestOpcode,
                    requestPayload.Span,
                    RitsuLibSidecarDeliverySemantics.StableSync,
                    additionalHeaderExtension: extra))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (client -> host)."));

            return await wait.ConfigureAwait(false);
        }

        /// <summary>
        ///     Typed client → host request/reply: encodes <paramref name="request" />, adds correlation, waits for
        ///     <paramref name="responseCodec" /> opcode, decodes the reply payload.
        ///     类型化客户端 → 主机请求/回复：编码 <paramref name="request" />，添加 correlation，等待
        ///     <paramref name="responseCodec" /> opcode，并解码回复载荷。
        /// </summary>
        public static async Task<TResponse> SendCorrelatedRequestToHostAsync<TRequest, TResponse>(
            RunManager? runManager,
            IRitsuLibSidecarMessageCodec<TRequest> requestCodec,
            IRitsuLibSidecarMessageCodec<TResponse> responseCodec,
            TRequest request,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
            where TRequest : notnull
            where TResponse : notnull
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var writer = new ArrayBufferWriter<byte>();
            requestCodec.Encode(writer, request);
            var correlationId = RitsuLibSidecarRequestCorrelation.AllocateCorrelationId();
            var extra = RitsuLibSidecarRequestCorrelation.PackAdditional(correlationId);
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                responseCodec.Opcode,
                effectiveTimeout,
                ctx =>
                    RitsuLibSidecarRequestCorrelation.HeaderExtensionCorrelationEquals(ctx.Envelope.HeaderExtension,
                        correlationId)
                    && (replyPredicate?.Invoke(ctx) ?? true),
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    runManager,
                    requestCodec.Opcode,
                    writer.WrittenSpan,
                    RitsuLibSidecarDeliverySemantics.StableSync,
                    additionalHeaderExtension: extra))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (client -> host)."));

            var ctx = await wait.ConfigureAwait(false);
            if (!responseCodec.TryDecode(ctx.Payload.Span, out var message) || message is null)
                throw new InvalidOperationException("Sidecar reply decode failed.");

            return message;
        }

        /// <summary>
        ///     Host sends request to one peer and awaits one matching reply opcode.
        ///     主机向一个对等端发送请求，并等待一个匹配的回复 opcode。
        /// </summary>
        public static async Task<RitsuLibSidecarDispatchContext> SendRequestToPeerAndWaitReplyAsync(
            RunManager? runManager,
            ulong peerNetId,
            ulong requestOpcode,
            ReadOnlyMemory<byte> requestPayload,
            ulong replyOpcode,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                replyOpcode,
                effectiveTimeout,
                ctx => ctx.SenderNetId == peerNetId && (replyPredicate?.Invoke(ctx) ?? true),
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    runManager,
                    peerNetId,
                    requestOpcode,
                    requestPayload.Span,
                    RitsuLibSidecarDeliverySemantics.StableSync))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (host -> peer)."));

            return await wait.ConfigureAwait(false);
        }

        /// <summary>
        ///     Host → peer request/reply with correlation in the header extension; reply must echo the same correlation.
        ///     主机 → 对等端请求/回复，在 header 扩展中包含 correlation；回复必须回显相同的 correlation。
        /// </summary>
        public static async Task<RitsuLibSidecarDispatchContext> SendCorrelatedRequestToPeerAndWaitReplyAsync(
            RunManager? runManager,
            ulong peerNetId,
            ulong requestOpcode,
            ReadOnlyMemory<byte> requestPayload,
            ulong replyOpcode,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var correlationId = RitsuLibSidecarRequestCorrelation.AllocateCorrelationId();
            var extra = RitsuLibSidecarRequestCorrelation.PackAdditional(correlationId);
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                replyOpcode,
                effectiveTimeout,
                ctx =>
                    ctx.SenderNetId == peerNetId
                    && RitsuLibSidecarRequestCorrelation.HeaderExtensionCorrelationEquals(ctx.Envelope.HeaderExtension,
                        correlationId)
                    && (replyPredicate?.Invoke(ctx) ?? true),
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    runManager,
                    peerNetId,
                    requestOpcode,
                    requestPayload.Span,
                    RitsuLibSidecarDeliverySemantics.StableSync,
                    additionalHeaderExtension: extra))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (host -> peer)."));

            return await wait.ConfigureAwait(false);
        }

        /// <summary>
        ///     Typed host → peer request/reply with correlation.
        ///     带 correlation 的类型化主机 → 对等端请求/回复。
        /// </summary>
        public static async Task<TResponse> SendCorrelatedRequestToPeerAsync<TRequest, TResponse>(
            RunManager? runManager,
            ulong peerNetId,
            IRitsuLibSidecarMessageCodec<TRequest> requestCodec,
            IRitsuLibSidecarMessageCodec<TResponse> responseCodec,
            TRequest request,
            TimeSpan timeout = default,
            Func<RitsuLibSidecarDispatchContext, bool>? replyPredicate = null,
            CancellationToken cancellationToken = default)
            where TRequest : notnull
            where TResponse : notnull
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var writer = new ArrayBufferWriter<byte>();
            requestCodec.Encode(writer, request);
            var correlationId = RitsuLibSidecarRequestCorrelation.AllocateCorrelationId();
            var extra = RitsuLibSidecarRequestCorrelation.PackAdditional(correlationId);
            var effectiveTimeout = timeout <= TimeSpan.Zero ? DefaultReplyTimeout : timeout;
            var wait = RitsuLibSidecarBus.WaitForNextAsync(
                responseCodec.Opcode,
                effectiveTimeout,
                ctx =>
                    ctx.SenderNetId == peerNetId
                    && RitsuLibSidecarRequestCorrelation.HeaderExtensionCorrelationEquals(ctx.Envelope.HeaderExtension,
                        correlationId)
                    && (replyPredicate?.Invoke(ctx) ?? true),
                true,
                cancellationToken);
            if (!RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    runManager,
                    peerNetId,
                    requestCodec.Opcode,
                    writer.WrittenSpan,
                    RitsuLibSidecarDeliverySemantics.StableSync,
                    additionalHeaderExtension: extra))
                _ = RitsuLibSidecarBus.TryFailWaitIfStillPending(
                    wait,
                    new InvalidOperationException("Sidecar request send failed (host -> peer)."));

            var ctx = await wait.ConfigureAwait(false);
            if (!responseCodec.TryDecode(ctx.Payload.Span, out var message) || message is null)
                throw new InvalidOperationException("Sidecar reply decode failed.");

            return message;
        }
    }
}
