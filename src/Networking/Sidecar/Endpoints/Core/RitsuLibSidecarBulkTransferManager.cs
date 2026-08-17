using System.Security.Cryptography;
using System.Threading.Channels;
using MegaCrit.Sts2.Core.Multiplayer;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarBulkTransferManager : IDisposable
    {
        private const int QueueRetryDelayMilliseconds = 10;

        private static long _acknowledgedOutboundBytes;
        private static long _committedInboundBytes;
        private static long _completedTransfers;
        private static long _nonCompletedTransfers;
        private static long _retransmittedFrames;
        private static long _transferIdSequence;

        private readonly Lock _gate = new();
        private readonly Func<RitsuLibSidecarBulkStreamOffer, RitsuLibSidecarBulkReceiveTarget?> _handler;
        private readonly Dictionary<InboundKey, long> _recentlyCompleted = [];
        private readonly Dictionary<InboundKey, InboundTransfer> _inbound = [];
        private readonly Dictionary<ulong, OutboundTransfer> _outbound = [];
        private readonly RitsuLibSidecarEndpointRegistration _registration;

        private int _disposed;

        internal static long AcknowledgedOutboundBytes => Interlocked.Read(ref _acknowledgedOutboundBytes);
        internal static long CommittedInboundBytes => Interlocked.Read(ref _committedInboundBytes);
        internal static long CompletedTransfers => Interlocked.Read(ref _completedTransfers);
        internal static long NonCompletedTransfers => Interlocked.Read(ref _nonCompletedTransfers);
        internal static long RetransmittedFrames => Interlocked.Read(ref _retransmittedFrames);

        internal RitsuLibSidecarBulkTransferManager(
            RitsuLibSidecarEndpointRegistration registration,
            Func<RitsuLibSidecarBulkStreamOffer, RitsuLibSidecarBulkReceiveTarget?> handler)
        {
            _registration = registration;
            _handler = handler;
        }

        private RitsuLibSidecarBulkStreamOptions Options => _registration.BulkOptions!;

        internal Task<RitsuLibSidecarBulkTransferResult> SendAsync(
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId,
            Stream source,
            long length,
            RitsuLibSidecarBulkStreamMetadata? metadata,
            IProgress<RitsuLibSidecarBulkStreamProgress>? progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!source.CanRead)
                throw new ArgumentException("Bulk stream source must be readable.", nameof(source));
            if (length < 0 || length > Options.MaxStreamBytes)
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    $"Length must be between zero and {Options.MaxStreamBytes} bytes.");
            if (Volatile.Read(ref _disposed) != 0 || _registration.IsDisposed)
                return Task.FromResult(new RitsuLibSidecarBulkTransferResult(
                    0,
                    0,
                    RitsuLibSidecarBulkTransferStatus.EndpointDisposed,
                    0));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(new RitsuLibSidecarBulkTransferResult(
                    0,
                    0,
                    RitsuLibSidecarBulkTransferStatus.Canceled,
                    0));

            var peerNetId = ResolveRemotePeer(destination, targetNetId);
            if (peerNetId == 0)
                return Task.FromResult(new RitsuLibSidecarBulkTransferResult(
                    0,
                    0,
                    RitsuLibSidecarBulkTransferStatus.ProtocolError,
                    0));
            var lease = RitsuLibSidecarBulkTransferCoordinator.TryAcquireOutbound();
            if (lease == null)
                return Task.FromResult(new RitsuLibSidecarBulkTransferResult(
                    0,
                    peerNetId,
                    RitsuLibSidecarBulkTransferStatus.ResourceLimit,
                    0));

            OutboundTransfer state;
            lock (_gate)
            {
                if (_disposed != 0 || _outbound.Count >= Options.MaxConcurrentOutboundStreams)
                {
                    lease.Dispose();
                    return Task.FromResult(new RitsuLibSidecarBulkTransferResult(
                        0,
                        peerNetId,
                        _disposed != 0
                            ? RitsuLibSidecarBulkTransferStatus.EndpointDisposed
                            : RitsuLibSidecarBulkTransferStatus.ResourceLimit,
                        0));
                }

                var transferId = AllocateTransferId();
                state = new(
                    transferId,
                    peerNetId,
                    destination,
                    targetNetId,
                    source,
                    length,
                    metadata ?? new(),
                    progress,
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
                    lease);
                _outbound.Add(transferId, state);
            }

            _ = Task.Run(() => RunOutboundAsync(state), CancellationToken.None);
            return state.Completion.Task;
        }

        internal void HandleMessage(RitsuLibSidecarEndpointMessage message)
        {
            if (Volatile.Read(ref _disposed) != 0 ||
                !RitsuLibSidecarBulkBinary.TryReadFrame(message.Payload, out var frame))
                return;

            switch (frame.Type)
            {
                case RitsuLibSidecarBulkFrameType.Offer:
                    HandleOffer(message, frame);
                    return;
                case RitsuLibSidecarBulkFrameType.Accept:
                    HandleAccept(message.OriginalSenderNetId, frame);
                    return;
                case RitsuLibSidecarBulkFrameType.Data:
                    HandleData(message.OriginalSenderNetId, frame);
                    return;
                case RitsuLibSidecarBulkFrameType.Acknowledge:
                    HandleAcknowledge(message.OriginalSenderNetId, frame);
                    return;
                case RitsuLibSidecarBulkFrameType.Complete:
                    HandleComplete(message.OriginalSenderNetId, frame);
                    return;
                case RitsuLibSidecarBulkFrameType.Completed:
                    HandleCompleted(message.OriginalSenderNetId, frame.TransferId);
                    return;
                case RitsuLibSidecarBulkFrameType.Abort:
                    HandleAbort(message.OriginalSenderNetId, frame);
                    return;
                default:
                    return;
            }
        }

        internal void Tick()
        {
            var now = Environment.TickCount64;
            InboundTransfer[] stale;
            lock (_gate)
            {
                stale =
                [
                    .. _inbound.Values.Where(state =>
                        now - Volatile.Read(ref state.LastActivityTick) > Options.IdleTimeout.TotalMilliseconds),
                ];
                foreach (var key in _recentlyCompleted
                             .Where(pair => pair.Value <= now)
                             .Select(static pair => pair.Key)
                             .ToArray())
                    _recentlyCompleted.Remove(key);
            }

            foreach (var state in stale)
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.TimedOut, true);
        }

        internal void RouteChanged()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;
            AbortAll(RitsuLibSidecarBulkTransferStatus.Disconnected);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            AbortAll(RitsuLibSidecarBulkTransferStatus.EndpointDisposed);
        }

        private async Task RunOutboundAsync(OutboundTransfer state)
        {
            try
            {
                var offer = RitsuLibSidecarBulkBinary.WriteOffer(
                    state.TransferId,
                    state.TotalLength,
                    Options.PreferredChunkBytes,
                    Options.ReceiveWindowBytes,
                    state.Metadata);
                var accept = await SendOfferAndWaitForAcceptAsync(state, offer).ConfigureAwait(false);
                if (accept is not { } accepted)
                    return;

                state.ChunkBytes = accepted.ChunkBytes;
                state.WindowBytes = accepted.WindowBytes;
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                while (state.SentOffset < state.TotalLength)
                {
                    if (!await WaitForWindowSpaceAsync(state).ConfigureAwait(false))
                    {
                        if (!IsTerminal(state))
                        {
                            TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
                            CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
                        }

                        return;
                    }

                    if (IsTerminal(state))
                        return;

                    int chunkLength;
                    lock (state.Gate)
                    {
                        var remaining = state.TotalLength - state.SentOffset;
                        var availableWindow = state.WindowBytes - (state.SentOffset - state.ConfirmedOffset);
                        chunkLength = (int)Math.Min(Math.Min(remaining, availableWindow), state.ChunkBytes);
                    }

                    if (chunkLength <= 0)
                        continue;
                    var data = GC.AllocateUninitializedArray<byte>(chunkLength);
                    if (!await ReadExactlyAsync(state.Source, data, state.Cancellation.Token).ConfigureAwait(false))
                    {
                        TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
                        CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
                        return;
                    }

                    hash.AppendData(data);
                    byte[] encoded;
                    lock (state.Gate)
                    {
                        encoded = RitsuLibSidecarBulkBinary.WriteData(state.TransferId, state.SentOffset, data);
                        state.Unacknowledged.Add(
                            state.SentOffset,
                            new(encoded, state.SentOffset + data.Length));
                        state.SentOffset += data.Length;
                    }

                    var sendStatus = await QueueFrameWithBackpressureAsync(state, encoded).ConfigureAwait(false);
                    if (sendStatus != RitsuLibSidecarSendStatus.Accepted)
                    {
                        CompleteOutbound(state, MapSendStatus(sendStatus));
                        return;
                    }
                }

                if (!await WaitForAcknowledgementAsync(state, state.TotalLength).ConfigureAwait(false))
                {
                    if (!IsTerminal(state))
                    {
                        TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
                        CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
                    }

                    return;
                }

                var digest = hash.GetHashAndReset();
                var complete = RitsuLibSidecarBulkBinary.WriteComplete(
                    state.TransferId,
                    state.TotalLength,
                    digest);
                for (var attempt = 0; attempt <= Options.MaxRetransmissions; attempt++)
                {
                    if (attempt > 0)
                        Interlocked.Increment(ref _retransmittedFrames);
                    var sendStatus = await QueueFrameWithBackpressureAsync(state, complete).ConfigureAwait(false);
                    if (sendStatus != RitsuLibSidecarSendStatus.Accepted)
                    {
                        CompleteOutbound(state, MapSendStatus(sendStatus));
                        return;
                    }

                    if (await WaitForSignalAsync(
                            state.CompletedSignal,
                            Options.AcknowledgementTimeout,
                            state.Cancellation.Token).ConfigureAwait(false))
                    {
                        CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.Completed);
                        return;
                    }
                }

                TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
                CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
            }
            catch (OperationCanceledException)
            {
                if (!IsTerminal(state))
                {
                    TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.Canceled);
                    CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.Canceled);
                }
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
                TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
                CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarBulk] Outbound transfer failed unexpectedly for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
                CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.SourceFailed);
            }
            finally
            {
                state.Cancellation.Dispose();
                state.AcknowledgementSignal.Dispose();
                state.CompletedSignal.Dispose();
            }
        }

        private async Task<RitsuLibSidecarBulkFrame?> SendOfferAndWaitForAcceptAsync(
            OutboundTransfer state,
            byte[] offer)
        {
            for (var attempt = 0; attempt <= Options.MaxRetransmissions; attempt++)
            {
                if (attempt > 0)
                    Interlocked.Increment(ref _retransmittedFrames);
                var sendStatus = await QueueFrameWithBackpressureAsync(state, offer).ConfigureAwait(false);
                if (sendStatus != RitsuLibSidecarSendStatus.Accepted)
                {
                    CompleteOutbound(state, MapSendStatus(sendStatus));
                    return null;
                }

                try
                {
                    return await state.Accepted.Task
                        .WaitAsync(Options.AcknowledgementTimeout, state.Cancellation.Token)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException) when (attempt < Options.MaxRetransmissions)
                {
                }
            }

            TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
            CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.TimedOut);
            return null;
        }

        private async Task<bool> WaitForWindowSpaceAsync(OutboundTransfer state)
        {
            while (true)
            {
                long sent;
                long confirmed;
                lock (state.Gate)
                {
                    sent = state.SentOffset;
                    confirmed = state.ConfirmedOffset;
                }

                if (sent - confirmed < state.WindowBytes)
                    return true;
                if (!await WaitForAcknowledgementAsync(state, confirmed + 1).ConfigureAwait(false))
                    return false;
            }
        }

        private async Task<bool> WaitForAcknowledgementAsync(OutboundTransfer state, long minimumOffset)
        {
            var retransmissions = 0;
            while (!IsTerminal(state))
            {
                lock (state.Gate)
                {
                    if (state.ConfirmedOffset >= minimumOffset)
                        return true;
                }

                if (await WaitForSignalAsync(
                        state.AcknowledgementSignal,
                        Options.AcknowledgementTimeout,
                        state.Cancellation.Token).ConfigureAwait(false))
                    continue;
                if (retransmissions >= Options.MaxRetransmissions)
                    return false;
                retransmissions++;

                byte[][] frames;
                lock (state.Gate)
                {
                    frames = [.. state.Unacknowledged.Values.Select(static chunk => chunk.Frame)];
                }

                foreach (var frame in frames)
                {
                    Interlocked.Increment(ref _retransmittedFrames);
                    var status = await QueueFrameWithBackpressureAsync(state, frame).ConfigureAwait(false);
                    if (status != RitsuLibSidecarSendStatus.Accepted)
                    {
                        CompleteOutbound(state, MapSendStatus(status));
                        return false;
                    }
                }
            }

            return false;
        }

        private async Task<RitsuLibSidecarSendStatus> QueueFrameWithBackpressureAsync(
            OutboundTransfer state,
            byte[] frame)
        {
            var started = Environment.TickCount64;
            while (!IsTerminal(state))
            {
                var result = RitsuLibSidecarEndpointProtocol.Send(
                    _registration,
                    state.Destination,
                    state.TargetNetId,
                    frame);
                if (result.Status == RitsuLibSidecarSendStatus.Accepted)
                {
                    Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
                    return result.Status;
                }

                if (result.Status is not (RitsuLibSidecarSendStatus.QueueFull or
                        RitsuLibSidecarSendStatus.RateLimited) ||
                    Environment.TickCount64 - started > Options.IdleTimeout.TotalMilliseconds)
                    return result.Status;
                await Task.Delay(QueueRetryDelayMilliseconds, state.Cancellation.Token).ConfigureAwait(false);
            }

            return RitsuLibSidecarSendStatus.EndpointDisposed;
        }

        private void HandleOffer(RitsuLibSidecarEndpointMessage message, RitsuLibSidecarBulkFrame frame)
        {
            var key = new InboundKey(message.OriginalSenderNetId, frame.TransferId);
            InboundTransfer? existing = null;
            var recentlyCompleted = false;
            lock (_gate)
            {
                if (_recentlyCompleted.TryGetValue(key, out var retainedUntil) &&
                    retainedUntil > Environment.TickCount64)
                    recentlyCompleted = true;
                else
                    _inbound.TryGetValue(key, out existing);
            }

            if (recentlyCompleted)
            {
                SendReplyBestEffort(
                    message.OriginalSenderNetId,
                    RitsuLibSidecarBulkBinary.WriteCompleted(frame.TransferId));
                return;
            }

            if (existing != null)
            {
                if (IsAccepted(existing))
                    SendAcceptBestEffort(existing);
                return;
            }

            if (frame.TotalLength > Options.MaxStreamBytes)
            {
                SendAbortBestEffort(
                    message.OriginalSenderNetId,
                    frame.TransferId,
                    RitsuLibSidecarBulkTransferStatus.ResourceLimit);
                return;
            }

            var route = _registration.GetRoute();
            var maxChunkBytes = route is { } current
                ? current.MaxPayloadBytes - RitsuLibSidecarBulkBinary.DataHeaderSize
                : 0;
            var negotiatedChunkBytes = Math.Min(
                Math.Min(frame.ChunkBytes, Options.PreferredChunkBytes),
                maxChunkBytes);
            if (negotiatedChunkBytes < RitsuLibSidecarEndpointPolicy.MinBulkChunkBytes)
            {
                SendAbortBestEffort(
                    message.OriginalSenderNetId,
                    frame.TransferId,
                    RitsuLibSidecarBulkTransferStatus.ProtocolError);
                return;
            }

            var negotiatedWindowBytes = Math.Min(frame.WindowBytes, Options.ReceiveWindowBytes);
            negotiatedWindowBytes = Math.Max(
                negotiatedChunkBytes,
                negotiatedWindowBytes / negotiatedChunkBytes * negotiatedChunkBytes);
            var lease = RitsuLibSidecarBulkTransferCoordinator.TryAcquireInbound();
            if (lease == null)
            {
                SendAbortBestEffort(
                    message.OriginalSenderNetId,
                    frame.TransferId,
                    RitsuLibSidecarBulkTransferStatus.ResourceLimit);
                return;
            }

            var state = new InboundTransfer(
                key,
                message.NegotiatedProtocolVersion,
                frame.TotalLength,
                frame.Metadata!,
                negotiatedChunkBytes,
                negotiatedWindowBytes,
                lease);
            InboundTransfer? racedExisting = null;
            RitsuLibSidecarBulkTransferStatus? rejectionStatus = null;
            lock (_gate)
            {
                if (_disposed != 0)
                    rejectionStatus = RitsuLibSidecarBulkTransferStatus.EndpointDisposed;
                else if (!_inbound.TryGetValue(key, out racedExisting))
                {
                    if (_inbound.Count >= Options.MaxConcurrentInboundStreams)
                        rejectionStatus = RitsuLibSidecarBulkTransferStatus.ResourceLimit;
                    else
                        _inbound.Add(key, state);
                }
            }

            if (racedExisting != null || rejectionStatus != null)
            {
                lease.Dispose();
                if (racedExisting != null && IsAccepted(racedExisting))
                    SendAcceptBestEffort(racedExisting);
                else if (rejectionStatus is { } status)
                    SendAbortBestEffort(message.OriginalSenderNetId, frame.TransferId, status);
                return;
            }

            if (!_registration.TryScheduleCallback(() => AcceptInboundOffer(state), 0))
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.ResourceLimit, true);
        }

        private void AcceptInboundOffer(InboundTransfer state)
        {
            if (!IsCurrentInbound(state) || IsTerminal(state))
                return;

            RitsuLibSidecarBulkReceiveTarget? target;
            try
            {
                target = _handler(new(
                    state.Key.SenderNetId,
                    state.Key.TransferId,
                    state.ProtocolVersion,
                    state.TotalLength,
                    state.Metadata));
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarBulk] Offer handler failed for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.Rejected, true);
                return;
            }

            if (target == null)
            {
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.Rejected, true);
                return;
            }

            if (!target.TryAttach())
            {
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError, true);
                return;
            }

            var writer = Channel.CreateBounded<InboundChunk>(new BoundedChannelOptions(
                state.WindowBytes / state.ChunkBytes + 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });
            var cancellationRegistration = target.CancellationToken.Register(() =>
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.Canceled, true));
            RitsuLibSidecarBulkTransferStatus? terminalStatus = null;
            lock (state.Gate)
            {
                if (state.Terminal != 0)
                {
                    terminalStatus = state.TerminalStatus;
                }
                else
                {
                    state.Target = target;
                    state.Writer = writer;
                    state.CancellationRegistration = cancellationRegistration;
                    state.WriterTask = Task.Run(() => RunInboundWriterAsync(state));
                    Volatile.Write(ref state.Accepted, 1);
                }
            }

            if (terminalStatus is { } status)
            {
                cancellationRegistration.Dispose();
                CompleteDetachedTarget(target, state, status);
                return;
            }

            SendAcceptBestEffort(state);
        }

        private void HandleAccept(ulong senderNetId, RitsuLibSidecarBulkFrame frame)
        {
            OutboundTransfer? state;
            lock (_gate)
            {
                _outbound.TryGetValue(frame.TransferId, out state);
            }

            if (state == null || state.PeerNetId != senderNetId || IsTerminal(state) ||
                frame.ChunkBytes > Options.PreferredChunkBytes ||
                frame.WindowBytes > Options.ReceiveWindowBytes)
                return;
            var route = _registration.GetRoute();
            if (route == null ||
                frame.ChunkBytes > route.Value.MaxPayloadBytes - RitsuLibSidecarBulkBinary.DataHeaderSize)
            {
                TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.ProtocolError);
                CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError);
                return;
            }

            Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
            state.Accepted.TrySetResult(frame);
        }

        private void HandleData(ulong senderNetId, RitsuLibSidecarBulkFrame frame)
        {
            var key = new InboundKey(senderNetId, frame.TransferId);
            InboundTransfer? state;
            lock (_gate)
            {
                _inbound.TryGetValue(key, out state);
            }

            if (state == null || !IsAccepted(state) || IsTerminal(state))
                return;

            var acknowledgeDuplicate = false;
            var abort = false;
            lock (state.Gate)
            {
                if (frame.Offset < state.EnqueuedOffset)
                {
                    acknowledgeDuplicate = true;
                }
                else if (frame.Offset != state.EnqueuedOffset ||
                         frame.Payload.Length > state.ChunkBytes ||
                         frame.Payload.Length != state.ChunkBytes &&
                         frame.Offset + frame.Payload.Length != state.TotalLength ||
                         frame.Offset + frame.Payload.Length > state.TotalLength ||
                         state.EnqueuedOffset - state.CommittedOffset + frame.Payload.Length > state.WindowBytes ||
                         !state.Writer!.Writer.TryWrite(new(frame.Offset, frame.Payload.ToArray())))
                {
                    abort = true;
                }
                else
                {
                    state.EnqueuedOffset += frame.Payload.Length;
                    Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
                }
            }

            if (abort)
            {
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError, true);
                return;
            }

            if (acknowledgeDuplicate)
                SendAcknowledgeBestEffort(state);
        }

        private async Task RunInboundWriterAsync(InboundTransfer state)
        {
            try
            {
                await foreach (var chunk in state.Writer!.Reader.ReadAllAsync(state.Cancellation.Token)
                                   .ConfigureAwait(false))
                {
                    long expectedOffset;
                    lock (state.Gate)
                    {
                        expectedOffset = state.CommittedOffset;
                    }

                    if (chunk.Offset != expectedOffset)
                    {
                        AbortInbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError, true);
                        return;
                    }

                    await state.Target!.Destination.WriteAsync(chunk.Payload, state.Cancellation.Token)
                        .ConfigureAwait(false);
                    state.Hash.AppendData(chunk.Payload);
                    long committed;
                    lock (state.Gate)
                    {
                        state.CommittedOffset += chunk.Payload.Length;
                        committed = state.CommittedOffset;
                        Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
                    }

                    Interlocked.Add(ref _committedInboundBytes, chunk.Payload.Length);

                    ReportProgress(
                        state.Target.Progress,
                        new(
                            state.Key.TransferId,
                            RitsuLibSidecarBulkTransferDirection.Receiving,
                            committed,
                            state.TotalLength));
                    var acknowledge = RitsuLibSidecarBulkBinary.WriteAcknowledge(
                        state.Key.TransferId,
                        committed);
                    var status = await QueueReplyWithBackpressureAsync(
                            state.Key.SenderNetId,
                            acknowledge,
                            state.Cancellation.Token)
                        .ConfigureAwait(false);
                    if (status != RitsuLibSidecarSendStatus.Accepted)
                    {
                        AbortInbound(state, MapSendStatus(status), false);
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.DestinationFailed, true);
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarBulk] Inbound writer failed unexpectedly for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.DestinationFailed, true);
            }
        }

        private void HandleAcknowledge(ulong senderNetId, RitsuLibSidecarBulkFrame frame)
        {
            OutboundTransfer? state;
            lock (_gate)
            {
                _outbound.TryGetValue(frame.TransferId, out state);
            }

            if (state == null || state.PeerNetId != senderNetId || IsTerminal(state))
                return;

            long confirmed;
            long newlyConfirmed = 0;
            var invalid = false;
            lock (state.Gate)
            {
                if (frame.Offset < state.ConfirmedOffset || frame.Offset > state.SentOffset ||
                    frame.Offset != state.ConfirmedOffset &&
                    state.Unacknowledged.Values.All(chunk => chunk.EndOffset != frame.Offset))
                {
                    invalid = true;
                    confirmed = state.ConfirmedOffset;
                }
                else
                {
                    if (frame.Offset == state.ConfirmedOffset)
                        return;
                    var previousConfirmed = state.ConfirmedOffset;
                    foreach (var offset in state.Unacknowledged
                                 .Where(pair => pair.Value.EndOffset <= frame.Offset)
                                 .Select(static pair => pair.Key)
                                 .ToArray())
                        state.Unacknowledged.Remove(offset);
                    state.ConfirmedOffset = frame.Offset;
                    confirmed = state.ConfirmedOffset;
                    newlyConfirmed = confirmed - previousConfirmed;
                    Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
                }
            }

            if (invalid)
            {
                TrySendAbort(state, RitsuLibSidecarBulkTransferStatus.ProtocolError);
                CompleteOutbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError);
                return;
            }

            Interlocked.Add(ref _acknowledgedOutboundBytes, newlyConfirmed);

            ReportProgress(
                state.Progress,
                new(
                    state.TransferId,
                    RitsuLibSidecarBulkTransferDirection.Sending,
                    confirmed,
                    state.TotalLength));
            state.AcknowledgementSignal.Release();
        }

        private void HandleComplete(ulong senderNetId, RitsuLibSidecarBulkFrame frame)
        {
            var key = new InboundKey(senderNetId, frame.TransferId);
            InboundTransfer? state;
            var recentlyCompleted = false;
            lock (_gate)
            {
                if (_recentlyCompleted.TryGetValue(key, out var retainedUntil) &&
                    retainedUntil > Environment.TickCount64)
                    recentlyCompleted = true;
                _inbound.TryGetValue(key, out state);
            }

            if (recentlyCompleted)
            {
                SendReplyBestEffort(senderNetId, RitsuLibSidecarBulkBinary.WriteCompleted(frame.TransferId));
                return;
            }

            if (state == null || !IsAccepted(state) || frame.TotalLength != state.TotalLength ||
                frame.Sha256 == null || IsTerminal(state))
                return;
            var invalid = false;
            var startCompletion = false;
            lock (state.Gate)
            {
                if (state.CommittedOffset != state.TotalLength || state.EnqueuedOffset != state.TotalLength)
                    invalid = true;
                else if (state.Terminal == 0)
                {
                    state.TerminalStatus = RitsuLibSidecarBulkTransferStatus.Completed;
                    Volatile.Write(ref state.Terminal, 1);
                    state.ExpectedSha256 = frame.Sha256;
                    state.Writer!.Writer.TryComplete();
                    startCompletion = true;
                }
            }

            if (invalid)
            {
                AbortInbound(state, RitsuLibSidecarBulkTransferStatus.ProtocolError, true);
                return;
            }

            if (startCompletion)
                _ = Task.Run(() => CompleteInboundAsync(state));
        }

        private async Task CompleteInboundAsync(InboundTransfer state)
        {
            var status = RitsuLibSidecarBulkTransferStatus.Completed;
            try
            {
                await state.WriterTask!.ConfigureAwait(false);
                var actualHash = state.Hash.GetHashAndReset();
                if (!CryptographicOperations.FixedTimeEquals(actualHash, state.ExpectedSha256!))
                {
                    status = RitsuLibSidecarBulkTransferStatus.IntegrityFailed;
                }
                else
                {
                    await state.Target!.Destination.FlushAsync().ConfigureAwait(false);
                    if (!state.Target.LeaveOpen)
                        await state.Target.Destination.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
                status = RitsuLibSidecarBulkTransferStatus.DestinationFailed;
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarBulk] Inbound completion failed unexpectedly for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                status = RitsuLibSidecarBulkTransferStatus.DestinationFailed;
            }

            if (status == RitsuLibSidecarBulkTransferStatus.Completed)
            {
                var completed = RitsuLibSidecarBulkBinary.WriteCompleted(state.Key.TransferId);
                var sendStatus = await QueueReplyWithBackpressureAsync(
                        state.Key.SenderNetId,
                        completed,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                if (sendStatus != RitsuLibSidecarSendStatus.Accepted)
                    status = MapSendStatus(sendStatus);
            }
            else
            {
                SendAbortBestEffort(state.Key.SenderNetId, state.Key.TransferId, status);
            }

            FinishInbound(state, status);
        }

        private void HandleCompleted(ulong senderNetId, ulong transferId)
        {
            OutboundTransfer? state;
            lock (_gate)
            {
                _outbound.TryGetValue(transferId, out state);
            }

            if (state == null || state.PeerNetId != senderNetId || IsTerminal(state))
                return;
            Volatile.Write(ref state.LastActivityTick, Environment.TickCount64);
            state.CompletedSignal.Release();
        }

        private void HandleAbort(ulong senderNetId, RitsuLibSidecarBulkFrame frame)
        {
            OutboundTransfer? outbound;
            lock (_gate)
            {
                _outbound.TryGetValue(frame.TransferId, out outbound);
            }

            if (outbound != null && outbound.PeerNetId == senderNetId)
            {
                CompleteOutbound(outbound, MapAbortReason(frame.AbortReason));
                return;
            }

            var key = new InboundKey(senderNetId, frame.TransferId);
            InboundTransfer? inbound;
            lock (_gate)
            {
                _inbound.TryGetValue(key, out inbound);
            }

            if (inbound != null)
                AbortInbound(inbound, MapAbortReason(frame.AbortReason), false);
        }

        private void SendAcceptBestEffort(InboundTransfer state)
        {
            SendReplyBestEffort(
                state.Key.SenderNetId,
                RitsuLibSidecarBulkBinary.WriteAccept(
                    state.Key.TransferId,
                    state.ChunkBytes,
                    state.WindowBytes));
        }

        private void SendAcknowledgeBestEffort(InboundTransfer state)
        {
            long committed;
            lock (state.Gate)
            {
                committed = state.CommittedOffset;
            }

            SendReplyBestEffort(
                state.Key.SenderNetId,
                RitsuLibSidecarBulkBinary.WriteAcknowledge(state.Key.TransferId, committed));
        }

        private void SendAbortBestEffort(
            ulong peerNetId,
            ulong transferId,
            RitsuLibSidecarBulkTransferStatus status)
        {
            SendReplyBestEffort(
                peerNetId,
                RitsuLibSidecarBulkBinary.WriteAbort(transferId, MapStatus(status)));
        }

        private void SendReplyBestEffort(ulong peerNetId, byte[] frame)
        {
            var (destination, targetNetId) = ResolveReplyDestination(peerNetId);
            RitsuLibSidecarEndpointProtocol.Send(_registration, destination, targetNetId, frame);
        }

        private async Task<RitsuLibSidecarSendStatus> QueueReplyWithBackpressureAsync(
            ulong peerNetId,
            byte[] frame,
            CancellationToken cancellationToken)
        {
            var (destination, targetNetId) = ResolveReplyDestination(peerNetId);
            var started = Environment.TickCount64;
            while (!cancellationToken.IsCancellationRequested && Volatile.Read(ref _disposed) == 0)
            {
                var result = RitsuLibSidecarEndpointProtocol.Send(
                    _registration,
                    destination,
                    targetNetId,
                    frame);
                if (result.Status is not (RitsuLibSidecarSendStatus.QueueFull or
                        RitsuLibSidecarSendStatus.RateLimited) ||
                    Environment.TickCount64 - started > Options.IdleTimeout.TotalMilliseconds)
                    return result.Status;
                await Task.Delay(QueueRetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }

            return _registration.IsDisposed
                ? RitsuLibSidecarSendStatus.EndpointDisposed
                : RitsuLibSidecarSendStatus.TransportUnavailable;
        }

        private void TrySendAbort(OutboundTransfer state, RitsuLibSidecarBulkTransferStatus status)
        {
            if (IsTerminal(state))
                return;
            var abort = RitsuLibSidecarBulkBinary.WriteAbort(state.TransferId, MapStatus(status));
            RitsuLibSidecarEndpointProtocol.Send(
                _registration,
                state.Destination,
                state.TargetNetId,
                abort);
        }

        private void AbortInbound(
            InboundTransfer state,
            RitsuLibSidecarBulkTransferStatus status,
            bool notifyRemote)
        {
            if (!TrySetInboundTerminal(state, status))
                return;
            if (notifyRemote)
                SendAbortBestEffort(state.Key.SenderNetId, state.Key.TransferId, status);
            state.Writer?.Writer.TryComplete();
            state.Cancellation.Cancel();
            _ = Task.Run(async () =>
            {
                if (state.WriterTask != null)
                {
                    try
                    {
                        await state.WriterTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                if (state.Target is { LeaveOpen: false } target)
                {
                    try
                    {
                        await target.Destination.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        RitsuLibFramework.Logger.ErrorNoTrace(
                            $"[SidecarBulk] Destination disposal failed for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                    }
                }

                FinishInbound(state, status);
            });
        }

        private void FinishInbound(InboundTransfer state, RitsuLibSidecarBulkTransferStatus status)
        {
            lock (_gate)
            {
                if (_inbound.TryGetValue(state.Key, out var current) && ReferenceEquals(current, state))
                    _inbound.Remove(state.Key);
                if (status == RitsuLibSidecarBulkTransferStatus.Completed)
                    _recentlyCompleted[state.Key] =
                        Environment.TickCount64 +
                        (long)RitsuLibSidecarEndpointPolicy.BulkCompletedTransferRetention.TotalMilliseconds;
            }

            state.CancellationRegistration.Dispose();
            state.Hash.Dispose();
            state.Cancellation.Dispose();
            state.Lease.Dispose();
            RecordTerminalStatus(status);
            state.Target?.Complete(new(
                state.Key.TransferId,
                state.Key.SenderNetId,
                status,
                Volatile.Read(ref state.CommittedOffset)));
        }

        private void CompleteDetachedTarget(
            RitsuLibSidecarBulkReceiveTarget target,
            InboundTransfer state,
            RitsuLibSidecarBulkTransferStatus status)
        {
            _ = Task.Run(async () =>
            {
                if (!target.LeaveOpen)
                {
                    try
                    {
                        await target.Destination.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        RitsuLibFramework.Logger.ErrorNoTrace(
                            $"[SidecarBulk] Detached destination disposal failed for {_registration.Descriptor.OwnerId}/{_registration.Descriptor.Name}: {exception}");
                    }
                }

                target.Complete(new(
                    state.Key.TransferId,
                    state.Key.SenderNetId,
                    status,
                    0));
            });
        }

        private void CompleteOutbound(OutboundTransfer state, RitsuLibSidecarBulkTransferStatus status)
        {
            if (Interlocked.CompareExchange(ref state.Terminal, 1, 0) != 0)
                return;
            lock (_gate)
            {
                if (_outbound.TryGetValue(state.TransferId, out var current) && ReferenceEquals(current, state))
                    _outbound.Remove(state.TransferId);
            }

            state.Cancellation.Cancel();
            state.Lease.Dispose();
            RecordTerminalStatus(status);
            state.Completion.TrySetResult(new(
                state.TransferId,
                state.PeerNetId,
                status,
                Volatile.Read(ref state.ConfirmedOffset)));
        }

        internal static void ResetStatistics()
        {
            Interlocked.Exchange(ref _acknowledgedOutboundBytes, 0);
            Interlocked.Exchange(ref _committedInboundBytes, 0);
            Interlocked.Exchange(ref _completedTransfers, 0);
            Interlocked.Exchange(ref _nonCompletedTransfers, 0);
            Interlocked.Exchange(ref _retransmittedFrames, 0);
        }

        private static void RecordTerminalStatus(RitsuLibSidecarBulkTransferStatus status)
        {
            ref var counter = ref status == RitsuLibSidecarBulkTransferStatus.Completed
                ? ref _completedTransfers
                : ref _nonCompletedTransfers;
            Interlocked.Increment(ref counter);
        }

        private void AbortAll(RitsuLibSidecarBulkTransferStatus status)
        {
            OutboundTransfer[] outbound;
            InboundTransfer[] inbound;
            lock (_gate)
            {
                outbound = [.. _outbound.Values];
                inbound = [.. _inbound.Values];
                _recentlyCompleted.Clear();
            }

            foreach (var state in outbound)
            {
                TrySendAbort(state, status);
                CompleteOutbound(state, status);
            }

            foreach (var state in inbound)
                AbortInbound(state, status, true);
        }

        private bool IsCurrentInbound(InboundTransfer state)
        {
            lock (_gate)
            {
                return _inbound.TryGetValue(state.Key, out var current) && ReferenceEquals(current, state);
            }
        }

        private ulong ResolveRemotePeer(RitsuLibSidecarEndpointDestination destination, ulong targetNetId)
        {
            return destination switch
            {
                RitsuLibSidecarEndpointDestination.Host
                    when RitsuLibSidecarSessionManager.CurrentNetService is NetClientGameService client =>
                    client.HostNetId,
                RitsuLibSidecarEndpointDestination.Peer => targetNetId,
                _ => 0,
            };
        }

        private static (RitsuLibSidecarEndpointDestination Destination, ulong TargetNetId) ResolveReplyDestination(
            ulong peerNetId)
        {
            return RitsuLibSidecarSessionManager.CurrentNetService is NetClientGameService client &&
                   peerNetId == client.HostNetId
                ? (RitsuLibSidecarEndpointDestination.Host, 0)
                : (RitsuLibSidecarEndpointDestination.Peer, peerNetId);
        }

        private static async Task<bool> ReadExactlyAsync(
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            var offset = 0;
            while (offset < destination.Length)
            {
                var read = await source.ReadAsync(destination[offset..], cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    return false;
                offset += read;
            }

            return true;
        }

        private static async Task<bool> WaitForSignalAsync(
            SemaphoreSlim signal,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return await signal.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }

        private static void ReportProgress(
            IProgress<RitsuLibSidecarBulkStreamProgress>? progress,
            RitsuLibSidecarBulkStreamProgress value)
        {
            if (progress == null)
                return;
            try
            {
                progress.Report(value);
            }
            catch (Exception exception)
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarBulk] Progress observer failed: {exception}");
            }
        }

        private static bool IsTerminal(OutboundTransfer state)
        {
            return Volatile.Read(ref state.Terminal) != 0;
        }

        private static bool IsTerminal(InboundTransfer state)
        {
            return Volatile.Read(ref state.Terminal) != 0;
        }

        private static bool IsAccepted(InboundTransfer state)
        {
            return Volatile.Read(ref state.Accepted) != 0;
        }

        private static bool TrySetInboundTerminal(
            InboundTransfer state,
            RitsuLibSidecarBulkTransferStatus status)
        {
            lock (state.Gate)
            {
                if (state.Terminal != 0)
                    return false;
                state.TerminalStatus = status;
                Volatile.Write(ref state.Terminal, 1);
                return true;
            }
        }

        private static ulong AllocateTransferId()
        {
            var value = unchecked((ulong)Interlocked.Increment(ref _transferIdSequence));
            return value == 0
                ? unchecked((ulong)Interlocked.Increment(ref _transferIdSequence))
                : value;
        }

        private static RitsuLibSidecarBulkTransferStatus MapSendStatus(RitsuLibSidecarSendStatus status)
        {
            return status switch
            {
                RitsuLibSidecarSendStatus.EndpointDisposed => RitsuLibSidecarBulkTransferStatus.EndpointDisposed,
                RitsuLibSidecarSendStatus.QueueFull or RitsuLibSidecarSendStatus.RateLimited =>
                    RitsuLibSidecarBulkTransferStatus.TimedOut,
                RitsuLibSidecarSendStatus.NoSession or
                    RitsuLibSidecarSendStatus.RouteUnavailable or
                    RitsuLibSidecarSendStatus.DestinationUnavailable or
                    RitsuLibSidecarSendStatus.ProfileUnsupported or
                    RitsuLibSidecarSendStatus.TransportUnavailable =>
                    RitsuLibSidecarBulkTransferStatus.Disconnected,
                _ => RitsuLibSidecarBulkTransferStatus.ProtocolError,
            };
        }

        private static RitsuLibSidecarBulkTransferStatus MapAbortReason(RitsuLibSidecarBulkAbortReason reason)
        {
            return reason switch
            {
                RitsuLibSidecarBulkAbortReason.Rejected => RitsuLibSidecarBulkTransferStatus.Rejected,
                RitsuLibSidecarBulkAbortReason.Canceled => RitsuLibSidecarBulkTransferStatus.Canceled,
                RitsuLibSidecarBulkAbortReason.TimedOut => RitsuLibSidecarBulkTransferStatus.TimedOut,
                RitsuLibSidecarBulkAbortReason.Disconnected => RitsuLibSidecarBulkTransferStatus.Disconnected,
                RitsuLibSidecarBulkAbortReason.EndpointDisposed =>
                    RitsuLibSidecarBulkTransferStatus.EndpointDisposed,
                RitsuLibSidecarBulkAbortReason.SourceFailed => RitsuLibSidecarBulkTransferStatus.SourceFailed,
                RitsuLibSidecarBulkAbortReason.DestinationFailed =>
                    RitsuLibSidecarBulkTransferStatus.DestinationFailed,
                RitsuLibSidecarBulkAbortReason.IntegrityFailed =>
                    RitsuLibSidecarBulkTransferStatus.IntegrityFailed,
                RitsuLibSidecarBulkAbortReason.ResourceLimit => RitsuLibSidecarBulkTransferStatus.ResourceLimit,
                _ => RitsuLibSidecarBulkTransferStatus.ProtocolError,
            };
        }

        private static RitsuLibSidecarBulkAbortReason MapStatus(RitsuLibSidecarBulkTransferStatus status)
        {
            return status switch
            {
                RitsuLibSidecarBulkTransferStatus.Rejected => RitsuLibSidecarBulkAbortReason.Rejected,
                RitsuLibSidecarBulkTransferStatus.Canceled => RitsuLibSidecarBulkAbortReason.Canceled,
                RitsuLibSidecarBulkTransferStatus.TimedOut => RitsuLibSidecarBulkAbortReason.TimedOut,
                RitsuLibSidecarBulkTransferStatus.Disconnected => RitsuLibSidecarBulkAbortReason.Disconnected,
                RitsuLibSidecarBulkTransferStatus.EndpointDisposed =>
                    RitsuLibSidecarBulkAbortReason.EndpointDisposed,
                RitsuLibSidecarBulkTransferStatus.SourceFailed => RitsuLibSidecarBulkAbortReason.SourceFailed,
                RitsuLibSidecarBulkTransferStatus.DestinationFailed =>
                    RitsuLibSidecarBulkAbortReason.DestinationFailed,
                RitsuLibSidecarBulkTransferStatus.IntegrityFailed =>
                    RitsuLibSidecarBulkAbortReason.IntegrityFailed,
                RitsuLibSidecarBulkTransferStatus.ResourceLimit => RitsuLibSidecarBulkAbortReason.ResourceLimit,
                _ => RitsuLibSidecarBulkAbortReason.ProtocolError,
            };
        }

        private readonly record struct InboundKey(ulong SenderNetId, ulong TransferId);

        private readonly record struct InboundChunk(long Offset, byte[] Payload);

        private readonly record struct OutboundChunk(byte[] Frame, long EndOffset);

        private sealed class OutboundTransfer
        {
            internal OutboundTransfer(
                ulong transferId,
                ulong peerNetId,
                RitsuLibSidecarEndpointDestination destination,
                ulong targetNetId,
                Stream source,
                long totalLength,
                RitsuLibSidecarBulkStreamMetadata metadata,
                IProgress<RitsuLibSidecarBulkStreamProgress>? progress,
                CancellationTokenSource cancellation,
                RitsuLibSidecarBulkTransferCoordinator.Lease lease)
            {
                TransferId = transferId;
                PeerNetId = peerNetId;
                Destination = destination;
                TargetNetId = targetNetId;
                Source = source;
                TotalLength = totalLength;
                Metadata = metadata;
                Progress = progress;
                Cancellation = cancellation;
                Lease = lease;
                LastActivityTick = Environment.TickCount64;
            }

            internal Lock Gate { get; } = new();
            internal SortedDictionary<long, OutboundChunk> Unacknowledged { get; } = [];

            internal TaskCompletionSource<RitsuLibSidecarBulkFrame> Accepted { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal TaskCompletionSource<RitsuLibSidecarBulkTransferResult> Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            internal SemaphoreSlim AcknowledgementSignal { get; } = new(0);
            internal SemaphoreSlim CompletedSignal { get; } = new(0);
            internal ulong TransferId { get; }
            internal ulong PeerNetId { get; }
            internal RitsuLibSidecarEndpointDestination Destination { get; }
            internal ulong TargetNetId { get; }
            internal Stream Source { get; }
            internal long TotalLength { get; }
            internal RitsuLibSidecarBulkStreamMetadata Metadata { get; }
            internal IProgress<RitsuLibSidecarBulkStreamProgress>? Progress { get; }
            internal CancellationTokenSource Cancellation { get; }
            internal RitsuLibSidecarBulkTransferCoordinator.Lease Lease { get; }
            internal int ChunkBytes { get; set; }
            internal int WindowBytes { get; set; }
            internal long SentOffset;
            internal long ConfirmedOffset;
            internal long LastActivityTick;
            internal int Terminal;
        }

        private sealed class InboundTransfer
        {
            internal InboundTransfer(
                InboundKey key,
                ushort protocolVersion,
                long totalLength,
                RitsuLibSidecarBulkStreamMetadata metadata,
                int chunkBytes,
                int windowBytes,
                RitsuLibSidecarBulkTransferCoordinator.Lease lease)
            {
                Key = key;
                ProtocolVersion = protocolVersion;
                TotalLength = totalLength;
                Metadata = metadata;
                ChunkBytes = chunkBytes;
                WindowBytes = windowBytes;
                Lease = lease;
                LastActivityTick = Environment.TickCount64;
            }

            internal Lock Gate { get; } = new();
            internal IncrementalHash Hash { get; } = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            internal CancellationTokenSource Cancellation { get; } = new();
            internal InboundKey Key { get; }
            internal ushort ProtocolVersion { get; }
            internal long TotalLength { get; }
            internal RitsuLibSidecarBulkStreamMetadata Metadata { get; }
            internal int ChunkBytes { get; }
            internal int WindowBytes { get; }
            internal RitsuLibSidecarBulkTransferCoordinator.Lease Lease { get; }
            internal RitsuLibSidecarBulkReceiveTarget? Target { get; set; }
            internal Channel<InboundChunk>? Writer { get; set; }
            internal Task? WriterTask { get; set; }
            internal byte[]? ExpectedSha256 { get; set; }
            internal CancellationTokenRegistration CancellationRegistration;
            internal RitsuLibSidecarBulkTransferStatus TerminalStatus;
            internal int Accepted;
            internal long EnqueuedOffset;
            internal long CommittedOffset;
            internal long LastActivityTick;
            internal int Terminal;
        }
    }
}
