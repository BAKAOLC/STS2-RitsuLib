using System.Buffers;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Request/await-reply helpers on top of <see cref="RitsuLibSidecarBus.WaitForNextAsync" /> for precise callback
    ///     control flow. Continuations after <c>await</c> often run on the thread pool; use
    ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.ContinueOnGodotMainLoopAsync{T}(System.Threading.Tasks.Task{T})" />
    ///     when the follow-up must touch Godot nodes or scene-tree-only APIs.
    /// </summary>
    public static class RitsuLibSidecarRequestReply
    {
        /// <summary>Default timeout used by request/reply helpers.</summary>
        public static readonly TimeSpan DefaultReplyTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Client sends request to host and awaits one matching reply opcode.</summary>
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

        /// <summary>Host sends request to one peer and awaits one matching reply opcode.</summary>
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

        /// <summary>Typed host → peer request/reply with correlation.</summary>
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
