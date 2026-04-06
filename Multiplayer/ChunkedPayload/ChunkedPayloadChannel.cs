using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Multiplayer.ChunkedPayload
{
    /// <summary>
    ///     Multiplexed reliable chunked binary channel on top of <see cref="INetGameService" />.
    /// </summary>
    public interface IChunkedPayloadChannel : IDisposable
    {
        /// <summary>
        ///     Logical sub-channel id (multiplexing).
        /// </summary>
        ushort StreamId { get; }

        /// <inheritdoc cref="ChunkedPayloadChannel.Send" />
        ChunkedPayloadSendResult Send(ReadOnlySpan<byte> payload, ulong? targetPeerId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Fired when a payload is fully received and CRC-checked.
        /// </summary>
        event EventHandler<ChunkedPayloadReceivedEventArgs>? Received;

        /// <summary>
        ///     Fired when a transfer is aborted (timeout, CRC, limits, etc.).
        /// </summary>
        event EventHandler<ChunkedPayloadFailedEventArgs>? Failed;
    }

    /// <summary>
    ///     Registers <see cref="RitsuLibChunkedNetFragmentMessage" />, reassembles fragments with timeout and CRC checks,
    ///     and exposes a simple send API. Thread-safe; handlers may run on the game’s networking thread.
    /// </summary>
    /// <inheritdoc cref="IChunkedPayloadChannel" />
    public sealed class ChunkedPayloadChannel : IChunkedPayloadChannel
    {
        private readonly MessageHandlerDelegate<RitsuLibChunkedNetFragmentMessage> _handler;
        private readonly INetGameService _net;
        private readonly ChunkedTransferOptions _options;
        private readonly Lock _sessionLock = new();

        private readonly Dictionary<TransferKey, ReassemblySession> _sessions = new();

        private bool _disposed;

        private long _reassemblyBytesReserved;

        /// <summary>
        ///     Subscribes to <see cref="RitsuLibChunkedNetFragmentMessage" /> immediately; call <see cref="Dispose" /> to
        ///     unregister.
        /// </summary>
        public ChunkedPayloadChannel(INetGameService netService, ushort streamId,
            ChunkedTransferOptions? options = null)
        {
            _net = netService ?? throw new ArgumentNullException(nameof(netService));
            StreamId = streamId;
            _options = options ?? new ChunkedTransferOptions();
            ValidateOptions(_options);
            _handler = OnFragmentReceived;
            _net.RegisterMessageHandler(_handler);
        }

        /// <inheritdoc />
        public ushort StreamId { get; }

        /// <inheritdoc />
        public event EventHandler<ChunkedPayloadReceivedEventArgs>? Received;

        /// <inheritdoc />
        public event EventHandler<ChunkedPayloadFailedEventArgs>? Failed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _net.UnregisterMessageHandler(_handler);
            lock (_sessionLock)
            {
                _sessions.Clear();
                _reassemblyBytesReserved = 0;
            }
        }

        /// <summary>
        ///     Sends a payload split across multiple <see cref="RitsuLibChunkedNetFragmentMessage" /> packets.
        /// </summary>
        /// <param name="payload">Data to send; CRC covers the full span.</param>
        /// <param name="targetPeerId">Host only: send to one client. Omit to broadcast to ready clients.</param>
        /// <param name="cancellationToken">Checked between fragments.</param>
        public ChunkedPayloadSendResult Send(ReadOnlySpan<byte> payload, ulong? targetPeerId = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!_net.IsConnected)
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.NotConnected);

            if (_net.Type == NetGameType.Client && targetPeerId.HasValue)
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.InvalidSendTarget,
                    "Clients must send without targetPeerId (traffic goes to the host).");

            var maxFrag = _options.MaxFragmentPayloadBytes;
            var maxTotal = _options.MaxTotalPayloadBytes;
            if (payload.Length > maxTotal)
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.PayloadTooLarge,
                    $"Length {payload.Length} exceeds MaxTotalPayloadBytes ({maxTotal}).");

            if (maxFrag is < 256 or > 65535)
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.InvalidLayout,
                    "MaxFragmentPayloadBytes must be in [256, 65535].");

            var crc = RitsuLibChunkedNetFragmentMessage.ComputePayloadCrc32(payload);
            var transferId = NextTransferId();
            var chunkCount = ComputeChunkCount(payload.Length, maxFrag);
            if (chunkCount > ComputeChunkCount(maxTotal, maxFrag))
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.InvalidLayout,
                    "Chunk count overflow.");

            var delay = _options.InterFragmentDelay;
            var sent = 0;
            try
            {
                for (var i = 0; i < chunkCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var offset = i * maxFrag;
                    var len = Math.Min(maxFrag, payload.Length - offset);
                    var fragment = new byte[len];
                    if (len > 0)
                        payload.Slice(offset, len).CopyTo(fragment);

                    var message = new RitsuLibChunkedNetFragmentMessage
                    {
                        SchemaVersion = RitsuLibChunkedNetFragmentMessage.SupportedSchemaVersion,
                        StreamId = StreamId,
                        DeclaredMaxFragmentBytes = (ushort)maxFrag,
                        TransferId = transferId,
                        TotalPayloadLength = (uint)payload.Length,
                        ChunkIndex = (uint)i,
                        ChunkCount = (uint)chunkCount,
                        PayloadCrc32 = crc,
                        Fragment = fragment,
                    };

                    if (_net.Type == NetGameType.Host)
                    {
                        if (targetPeerId.HasValue)
                            _net.SendMessage(message, targetPeerId.Value);
                        else
                            _net.SendMessage(message);
                    }
                    else
                    {
                        _net.SendMessage(message);
                    }

                    sent++;
                    if (i < chunkCount - 1 && delay > TimeSpan.Zero)
                        DelayWithCancellation(delay, cancellationToken);
                }

                return ChunkedPayloadSendResult.Success(sent);
            }
            catch (OperationCanceledException)
            {
                return ChunkedPayloadSendResult.Fail(ChunkedTransferFailureReason.Cancelled, null, sent);
            }
        }

        private static void DelayWithCancellation(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay <= TimeSpan.Zero)
                return;
            cancellationToken.ThrowIfCancellationRequested();
            // Single wait avoids 1 ms spin loops on the networking thread (Copilot PR review).
            if (cancellationToken.WaitHandle.WaitOne(delay))
                cancellationToken.ThrowIfCancellationRequested();
        }

        private void OnFragmentReceived(RitsuLibChunkedNetFragmentMessage msg, ulong senderId)
        {
            if (_disposed || msg.StreamId != StreamId)
                return;

            CleanupExpiredSessions();

            if (msg.SchemaVersion != RitsuLibChunkedNetFragmentMessage.SupportedSchemaVersion)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.UnsupportedOrCorrupt,
                    $"Schema {msg.SchemaVersion} is not supported.");
                return;
            }

            var maxFrag = msg.DeclaredMaxFragmentBytes;
            // ReSharper disable once PatternIsRedundant
            if (maxFrag is < 256 or > 65535)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.UnsupportedOrCorrupt,
                    "DeclaredMaxFragmentBytes out of range.");
                return;
            }

            if (msg.TotalPayloadLength > (uint)_options.MaxTotalPayloadBytes)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.PayloadTooLarge,
                    "Header TotalPayloadLength exceeds local MaxTotalPayloadBytes.");
                return;
            }

            var totalLen = (int)msg.TotalPayloadLength;
            var chunkCount = (int)msg.ChunkCount;
            if (chunkCount <= 0 || msg.ChunkIndex >= msg.ChunkCount)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.InvalidLayout,
                    "Invalid chunk index or count.");
                return;
            }

            var expectedChunks = ComputeChunkCount(totalLen, maxFrag);
            if (expectedChunks != chunkCount)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.InvalidLayout,
                    "ChunkCount does not match TotalPayloadLength and fragment size.");
                return;
            }

            var expectedLen = ExpectedFragmentLength((int)msg.ChunkIndex, chunkCount, totalLen, maxFrag);
            if (msg.Fragment.Length != expectedLen)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.InvalidLayout,
                    $"Fragment length {msg.Fragment.Length} != expected {expectedLen}.");
                return;
            }

            if (msg.Fragment.Length > _options.MaxFragmentPayloadBytes)
            {
                RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.InvalidLayout,
                    "Fragment exceeds local MaxFragmentPayloadBytes.");
                return;
            }

            List<Action>? afterLock = null;

            lock (_sessionLock)
            {
                var key = new TransferKey(senderId, msg.TransferId);
                if (!_sessions.TryGetValue(key, out var session))
                {
                    if (_sessions.Count >= _options.MaxConcurrentIncomingTransfers)
                    {
                        Defer(() => RaiseFailed(senderId, msg.TransferId,
                            ChunkedTransferFailureReason.TooManyConcurrentTransfers, null));
                        goto flush;
                    }

                    var need = (long)totalLen;
                    if (_reassemblyBytesReserved + need > _options.MaxReassemblyBytesInFlight)
                    {
                        Defer(() => RaiseFailed(senderId, msg.TransferId,
                            ChunkedTransferFailureReason.ReassemblyMemoryCap, null));
                        goto flush;
                    }

                    session = new()
                    {
                        SenderNetId = senderId,
                        StreamId = StreamId,
                        TransferId = msg.TransferId,
                        TotalLength = totalLen,
                        ChunkCount = chunkCount,
                        MaxFragment = maxFrag,
                        ExpectedCrc = msg.PayloadCrc32,
                        Buffer = new byte[totalLen],
                        ReceivedMarks = new bool[chunkCount],
                        DeadlineUtc = DateTime.UtcNow + _options.TransferTimeout,
                        ReservedBytes = need,
                    };
                    _sessions[key] = session;
                    _reassemblyBytesReserved += need;
                }
                else
                {
                    if (session.TotalLength != totalLen || session.ChunkCount != chunkCount ||
                        session.MaxFragment != maxFrag || session.ExpectedCrc != msg.PayloadCrc32)
                    {
                        RemoveSessionUnlocked(key, session);
                        Defer(() => RaiseFailed(senderId, msg.TransferId,
                            ChunkedTransferFailureReason.InvalidLayout,
                            "Conflicting continuation for the same TransferId."));
                        goto flush;
                    }
                }

                if (DateTime.UtcNow > session.DeadlineUtc)
                {
                    RemoveSessionUnlocked(key, session);
                    Defer(() => RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.TimedOut, null));
                    goto flush;
                }

                var idx = (int)msg.ChunkIndex;
                if (session.ReceivedMarks[idx])
                    goto flush;

                var start = idx * session.MaxFragment;
                if (msg.Fragment.Length > 0)
                    Array.Copy(msg.Fragment, 0, session.Buffer, start, msg.Fragment.Length);
                session.ReceivedMarks[idx] = true;
                session.ReceivedCount++;

                if (session.ReceivedCount < session.ChunkCount)
                    goto flush;

                _sessions.Remove(key);
                _reassemblyBytesReserved -= session.ReservedBytes;

                var crc = RitsuLibChunkedNetFragmentMessage.ComputePayloadCrc32(session.Buffer);
                if (crc != session.ExpectedCrc)
                {
                    Defer(() => RaiseFailed(senderId, msg.TransferId, ChunkedTransferFailureReason.CrcMismatch, null));
                    goto flush;
                }

                var payload = session.Buffer;
                var sid = senderId;
                var tid = msg.TransferId;
                Defer(() => DispatchCompleted(sid, tid, payload));
            }

            flush:
            if (afterLock == null)
                return;
            foreach (var a in afterLock)
                a();
            return;

            void Defer(Action a)
            {
                (afterLock ??= []).Add(a);
            }
        }

        private void DispatchCompleted(ulong senderId, ulong transferId, byte[] payload)
        {
            if (_options.CompletionDispatcher != null)
                _options.CompletionDispatcher(Invoke);
            else
                Invoke();
            return;

            void Invoke()
            {
                Received?.Invoke(this, new(senderId, StreamId, transferId, payload));
            }
        }

        private void RaiseFailed(ulong? senderId, ulong? transferId, ChunkedTransferFailureReason reason,
            string? detail)
        {
            var args = new ChunkedPayloadFailedEventArgs(senderId, StreamId, transferId, reason, detail);
            _options.TransferFailed?.Invoke(this, args);
            Failed?.Invoke(this, args);
        }

        private void RemoveSessionUnlocked(TransferKey key, ReassemblySession session)
        {
            if (_sessions.Remove(key))
                _reassemblyBytesReserved -= session.ReservedBytes;
        }

        private void CleanupExpiredSessions()
        {
            var now = DateTime.UtcNow;
            List<(ulong SenderNetId, ulong TransferId)>? timedOut = null;
            lock (_sessionLock)
            {
                foreach (var kv in new List<KeyValuePair<TransferKey, ReassemblySession>>(_sessions)
                             .Where(kv => now > kv.Value.DeadlineUtc))
                {
                    RemoveSessionUnlocked(kv.Key, kv.Value);
                    timedOut ??= [];
                    timedOut.Add((kv.Value.SenderNetId, kv.Value.TransferId));
                }
            }

            if (timedOut == null)
                return;
            foreach (var (sid, tid) in timedOut)
                RaiseFailed(sid, tid, ChunkedTransferFailureReason.TimedOut, null);
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(ChunkedPayloadChannel));
        }

        private static void ValidateOptions(ChunkedTransferOptions o)
        {
            ThrowIfMaxFragmentPayloadBytesInvalid(o.MaxFragmentPayloadBytes);
            ThrowIfMaxTotalPayloadBytesInvalid(o.MaxTotalPayloadBytes);
            ThrowIfMaxConcurrentIncomingTransfersInvalid(o.MaxConcurrentIncomingTransfers);
            ThrowIfMaxReassemblyBytesInFlightInvalid(o.MaxReassemblyBytesInFlight);
            ThrowIfTransferTimeoutInvalid(o.TransferTimeout);
        }

        private static void ThrowIfMaxFragmentPayloadBytesInvalid(int maxFragmentPayloadBytes)
        {
            if (maxFragmentPayloadBytes is < 256 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(maxFragmentPayloadBytes), maxFragmentPayloadBytes,
                    $"{nameof(ChunkedTransferOptions.MaxFragmentPayloadBytes)} must be within [256, 65535].");
        }

        private static void ThrowIfMaxTotalPayloadBytesInvalid(int maxTotalPayloadBytes)
        {
            if (maxTotalPayloadBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(maxTotalPayloadBytes), maxTotalPayloadBytes,
                    $"{nameof(ChunkedTransferOptions.MaxTotalPayloadBytes)} cannot be negative.");
        }

        private static void ThrowIfMaxConcurrentIncomingTransfersInvalid(int maxConcurrentIncomingTransfers)
        {
            if (maxConcurrentIncomingTransfers <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentIncomingTransfers),
                    maxConcurrentIncomingTransfers,
                    $"{nameof(ChunkedTransferOptions.MaxConcurrentIncomingTransfers)} must be positive.");
        }

        private static void ThrowIfMaxReassemblyBytesInFlightInvalid(long maxReassemblyBytesInFlight)
        {
            if (maxReassemblyBytesInFlight <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxReassemblyBytesInFlight),
                    maxReassemblyBytesInFlight,
                    $"{nameof(ChunkedTransferOptions.MaxReassemblyBytesInFlight)} must be positive.");
        }

        private static void ThrowIfTransferTimeoutInvalid(TimeSpan transferTimeout)
        {
            if (transferTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(transferTimeout), transferTimeout,
                    $"{nameof(ChunkedTransferOptions.TransferTimeout)} must be greater than zero.");
        }

        private static int ComputeChunkCount(int totalLen, int maxFrag)
        {
            if (totalLen == 0)
                return 1;
            return (totalLen + maxFrag - 1) / maxFrag;
        }

        internal static int ExpectedFragmentLength(int chunkIndex, int chunkCount, int totalLen, int maxFrag)
        {
            var offset = chunkIndex * maxFrag;
            return Math.Min(maxFrag, totalLen - offset);
        }

        private static ulong NextTransferId()
        {
            return unchecked((ulong)Random.Shared.NextInt64());
        }

        private readonly record struct TransferKey(ulong SenderNetId, ulong TransferId);

        private sealed class ReassemblySession
        {
            public required byte[] Buffer;

            public required int ChunkCount;

            public required DateTime DeadlineUtc;

            public required uint ExpectedCrc;

            public required int MaxFragment;

            public int ReceivedCount;

            public required bool[] ReceivedMarks;

            public required long ReservedBytes;
            public required ulong SenderNetId;

            public required ushort StreamId;

            public required int TotalLength;

            public required ulong TransferId;
        }
    }
}
