using System.Buffers;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Request-and-reply helpers built on <see cref="RitsuLibSidecarBus.WaitForNextAsync" />.
    ///         Continuations after <c>await</c> often run on the thread pool; use
    ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(System.Threading.Tasks.Task{T})" />
    ///         when the follow-up must touch Godot nodes or scene-tree-only APIs.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         基于 <see cref="RitsuLibSidecarBus.WaitForNextAsync" /> 的请求及等待回复辅助方法。
    ///         <c>await</c> 之后的延续通常在线程池运行；后续操作必须访问 Godot 节点或仅限场景树的 API 时，
    ///         请使用
    ///         <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(System.Threading.Tasks.Task{T})" />。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarRequestReply
    {
        /// <summary>
        ///     <para xml:lang="en">Default timeout used by request/reply helpers.</para>
        ///     <para xml:lang="zh-CN">请求/回复辅助方法使用的默认超时。</para>
        /// </summary>
        public static readonly TimeSpan DefaultReplyTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     <para xml:lang="en">Client sends request to host and awaits one matching reply opcode.</para>
        ///     <para xml:lang="zh-CN">客户端向主机发送请求，并等待一个操作码匹配的回复。</para>
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
        ///     <para xml:lang="en">
        ///         Client → host request/reply with an 8-byte correlation in the header extension; reply must use the same
        ///         correlation after the delivery byte (see <see cref="RitsuLibSidecarRequestCorrelation" />).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         客户端向主机发送请求并等待回复，在标头扩展中包含 8 字节关联值；
        ///         回复必须在投递字节后使用相同的关联值（参见 <see cref="RitsuLibSidecarRequestCorrelation" />）。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Typed client → host request/reply: encodes <paramref name="request" />, adds correlation, waits for
        ///         <paramref name="responseCodec" /> opcode, decodes the reply payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         类型化的客户端到主机请求及回复：编码 <paramref name="request" />，添加关联值，
        ///         等待 <paramref name="responseCodec" /> 的操作码，并解码回复载荷。
        ///     </para>
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
        ///     <para xml:lang="en">Host sends request to one peer and awaits one matching reply opcode.</para>
        ///     <para xml:lang="zh-CN">主机向一个对等端发送请求，并等待一个操作码匹配的回复。</para>
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
        ///     <para xml:lang="en">Host → peer request/reply with correlation in the header extension; reply must echo the same correlation.</para>
        ///     <para xml:lang="zh-CN">主机向对等端发送请求并等待回复，标头扩展中包含关联值；回复必须回显相同的关联值。</para>
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
        ///     <para xml:lang="en">Typed host → peer request/reply with correlation.</para>
        ///     <para xml:lang="zh-CN">带关联值的类型化主机到对等端请求及回复。</para>
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
