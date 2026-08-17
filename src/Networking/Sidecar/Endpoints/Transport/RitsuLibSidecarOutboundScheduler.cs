using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarOutboundScheduler
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, PeerQueue> Queues = [];

        private static readonly RitsuLibSidecarDeliveryProfile[] ProfileOrder =
        [
            RitsuLibSidecarDeliveryProfile.Control,
            RitsuLibSidecarDeliveryProfile.RealtimeDatagram,
            RitsuLibSidecarDeliveryProfile.BulkStream,
        ];

        private static readonly RitsuLibSidecarDeliveryProfile[] ProfileSchedule =
        [
            RitsuLibSidecarDeliveryProfile.Control,
            RitsuLibSidecarDeliveryProfile.Control,
            RitsuLibSidecarDeliveryProfile.Control,
            RitsuLibSidecarDeliveryProfile.RealtimeDatagram,
            RitsuLibSidecarDeliveryProfile.Control,
            RitsuLibSidecarDeliveryProfile.RealtimeDatagram,
            RitsuLibSidecarDeliveryProfile.BulkStream,
        ];

        private static int _bulkQueuedBytes;
        private static int _bulkQueuedMessages;
        private static int _queuedMessages;
        private static int _queuedBytes;
        private static int _roundRobinOffset;
        private static int _profileScheduleIndex;
        private static long _disposedFrames;
        private static long _expiredFrames;
        private static long _queueRejectedFrames;
        private static long _realtimeEvictedFrames;
        private static long _staleSessionFrames;
        private static long _transportFailedFrames;

        internal static RitsuLibSidecarSendStatus TryEnqueue(
            INetGameService? netService,
            long sessionEpoch,
            ulong peerNetId,
            byte[] envelope,
            RitsuLibSidecarDeliveryProfile deliveryProfile,
            TimeSpan lifetime,
            RitsuLibSidecarEndpointRegistration? owner)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (netService == null || netService.Type == NetGameType.Singleplayer ||
                sessionEpoch != RitsuLibSidecarSessionManager.Epoch)
                return RitsuLibSidecarSendStatus.NoSession;
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId))
                return RitsuLibSidecarSendStatus.TransportUnavailable;
            if (!RitsuLibSidecarEndpointTransport.SupportsProfile(netService, deliveryProfile))
                return RitsuLibSidecarSendStatus.ProfileUnsupported;

            var expiresAt = deliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram
                ? Environment.TickCount64 + (long)lifetime.TotalMilliseconds
                : long.MaxValue;
            lock (Gate)
            {
                if (!Queues.TryGetValue(peerNetId, out var peerQueue))
                {
                    peerQueue = new();
                    Queues.Add(peerNetId, peerQueue);
                }

                if (!MakeRoomForFrame(peerQueue, envelope.Length, deliveryProfile))
                {
                    Interlocked.Increment(ref _queueRejectedFrames);
                    return RitsuLibSidecarSendStatus.QueueFull;
                }

                var frame = new OutboundFrame(
                    sessionEpoch,
                    peerNetId,
                    envelope,
                    deliveryProfile,
                    expiresAt,
                    owner);
                QueueFor(peerQueue, deliveryProfile).Enqueue(frame);
                peerQueue.MessageCount++;
                peerQueue.ByteCount += envelope.Length;
                _queuedMessages++;
                _queuedBytes += envelope.Length;
                if (deliveryProfile == RitsuLibSidecarDeliveryProfile.BulkStream)
                {
                    peerQueue.BulkByteCount += envelope.Length;
                    _bulkQueuedMessages++;
                    _bulkQueuedBytes += envelope.Length;
                }

                return RitsuLibSidecarSendStatus.Accepted;
            }
        }

        internal static void Tick(INetGameService? netService)
        {
            if (netService == null || netService.Type == NetGameType.Singleplayer)
            {
                Clear();
                return;
            }

            var epoch = RitsuLibSidecarSessionManager.Epoch;
            var packetsRemaining = RitsuLibSidecarEndpointPolicy.MaxOutboundPacketsPerTick;
            var bytesRemaining = RitsuLibSidecarEndpointPolicy.MaxOutboundBytesPerTick;
            while (packetsRemaining > 0 && bytesRemaining > 0)
            {
                OutboundFrame? next;
                lock (Gate)
                {
                    next = TryDequeueNext(epoch, Environment.TickCount64, bytesRemaining);
                }

                if (next is not { } frame)
                    break;

                packetsRemaining--;
                bytesRemaining -= frame.Envelope.Length;
                if (frame.Owner is { IsDisposed: true })
                {
                    Interlocked.Increment(ref _disposedFrames);
                }
                else if (!RitsuLibSidecarEndpointTransport.TrySend(
                             netService,
                             frame.PeerNetId,
                             frame.Envelope,
                             frame.DeliveryProfile))
                {
                    Interlocked.Increment(ref _transportFailedFrames);
                }
            }
        }

        internal static void RemovePeer(ulong peerNetId)
        {
            lock (Gate)
            {
                if (!Queues.Remove(peerNetId, out var peerQueue))
                    return;
                _queuedMessages -= peerQueue.MessageCount;
                _queuedBytes -= peerQueue.ByteCount;
                NormalizeCounts();
            }
        }

        internal static void RemoveEndpoint(RitsuLibSidecarEndpointRegistration registration)
        {
            lock (Gate)
            {
                foreach (var peerQueue in Queues.Values)
                {
                    RemoveOwnedFrames(peerQueue, peerQueue.Control, registration);
                    RemoveOwnedFrames(peerQueue, peerQueue.Realtime, registration);
                    RemoveOwnedFrames(peerQueue, peerQueue.Bulk, registration);
                }

                foreach (var peerId in Queues
                             .Where(static pair => pair.Value.MessageCount == 0)
                             .Select(static pair => pair.Key)
                             .ToArray())
                    Queues.Remove(peerId);
            }
        }

        internal static void Clear()
        {
            lock (Gate)
            {
                Queues.Clear();
                _queuedMessages = 0;
                _queuedBytes = 0;
                _bulkQueuedMessages = 0;
                _bulkQueuedBytes = 0;
                _roundRobinOffset = 0;
                _profileScheduleIndex = 0;
            }
        }

        internal static (int Messages, int Bytes) GetQueueCounts()
        {
            lock (Gate)
            {
                return (_queuedMessages, _queuedBytes);
            }
        }

        internal static long DisposedFrames => Interlocked.Read(ref _disposedFrames);
        internal static long ExpiredFrames => Interlocked.Read(ref _expiredFrames);
        internal static long QueueRejectedFrames => Interlocked.Read(ref _queueRejectedFrames);
        internal static long RealtimeEvictedFrames => Interlocked.Read(ref _realtimeEvictedFrames);
        internal static long StaleSessionFrames => Interlocked.Read(ref _staleSessionFrames);
        internal static long TransportFailedFrames => Interlocked.Read(ref _transportFailedFrames);

        internal static void ResetStatistics()
        {
            Interlocked.Exchange(ref _disposedFrames, 0);
            Interlocked.Exchange(ref _expiredFrames, 0);
            Interlocked.Exchange(ref _queueRejectedFrames, 0);
            Interlocked.Exchange(ref _realtimeEvictedFrames, 0);
            Interlocked.Exchange(ref _staleSessionFrames, 0);
            Interlocked.Exchange(ref _transportFailedFrames, 0);
        }

        private static bool MakeRoomForFrame(
            PeerQueue peerQueue,
            int envelopeBytes,
            RitsuLibSidecarDeliveryProfile deliveryProfile)
        {
            if (envelopeBytes > RitsuLibSidecarEndpointPolicy.MaxOutboundQueuedBytesPerPeer)
                return false;
            if (deliveryProfile == RitsuLibSidecarDeliveryProfile.BulkStream &&
                (peerQueue.Bulk.Count >= RitsuLibSidecarEndpointPolicy.MaxBulkQueuedMessagesPerPeer ||
                 _bulkQueuedMessages >= RitsuLibSidecarEndpointPolicy.MaxBulkQueuedMessagesGlobal ||
                 peerQueue.BulkByteCount + envelopeBytes >
                 RitsuLibSidecarEndpointPolicy.MaxBulkQueuedBytesPerPeer ||
                 _bulkQueuedBytes + envelopeBytes > RitsuLibSidecarEndpointPolicy.MaxBulkQueuedBytesGlobal))
                return false;

            while (WouldExceedQueueLimits(peerQueue, envelopeBytes) &&
                   deliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram &&
                   peerQueue.Realtime.Count > 0)
            {
                Interlocked.Increment(ref _realtimeEvictedFrames);
                RemoveDequeued(peerQueue, peerQueue.Realtime.Dequeue());
            }

            return !WouldExceedQueueLimits(peerQueue, envelopeBytes);
        }

        private static bool WouldExceedQueueLimits(PeerQueue peerQueue, int envelopeBytes)
        {
            return peerQueue.MessageCount >= RitsuLibSidecarEndpointPolicy.MaxOutboundQueuedMessagesPerPeer ||
                   _queuedMessages >= RitsuLibSidecarEndpointPolicy.MaxOutboundQueuedMessagesGlobal ||
                   peerQueue.ByteCount + envelopeBytes >
                   RitsuLibSidecarEndpointPolicy.MaxOutboundQueuedBytesPerPeer ||
                   _queuedBytes + envelopeBytes > RitsuLibSidecarEndpointPolicy.MaxOutboundQueuedBytesGlobal;
        }

        private static Queue<OutboundFrame> QueueFor(
            PeerQueue peerQueue,
            RitsuLibSidecarDeliveryProfile deliveryProfile)
        {
            return deliveryProfile switch
            {
                RitsuLibSidecarDeliveryProfile.Control => peerQueue.Control,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram => peerQueue.Realtime,
                RitsuLibSidecarDeliveryProfile.BulkStream => peerQueue.Bulk,
                _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
            };
        }

        private static OutboundFrame? TryDequeueNext(long epoch, long now, int maximumBytes)
        {
            if (Queues.Count == 0)
                return null;

            var peerIds = Queues.Keys.ToArray();
            if (_roundRobinOffset >= peerIds.Length)
                _roundRobinOffset = 0;

            var preferredProfile = ProfileSchedule[_profileScheduleIndex % ProfileSchedule.Length];
            var preferredOrderIndex = Array.IndexOf(ProfileOrder, preferredProfile);
            for (var pass = 0; pass < ProfileOrder.Length; pass++)
            for (var examined = 0; examined < peerIds.Length; examined++)
            {
                var index = (_roundRobinOffset + examined) % peerIds.Length;
                var peerId = peerIds[index];
                if (!Queues.TryGetValue(peerId, out var peerQueue))
                    continue;

                var profile = ProfileOrder[(preferredOrderIndex + pass) % ProfileOrder.Length];
                var queue = QueueFor(peerQueue, profile);
                DropInvalidHead(peerQueue, queue, epoch, now);
                if (queue.Count == 0)
                {
                    RemoveEmptyPeer(peerId, peerQueue);
                    continue;
                }

                if (queue.Peek().Envelope.Length > maximumBytes)
                    continue;
                var frame = queue.Dequeue();
                RemoveDequeued(peerQueue, frame);
                RemoveEmptyPeer(peerId, peerQueue);
                _roundRobinOffset = peerIds.Length == 0 ? 0 : (index + 1) % peerIds.Length;
                _profileScheduleIndex = unchecked(_profileScheduleIndex + 1) & int.MaxValue;
                return frame;
            }

            return null;
        }

        private static void DropInvalidHead(
            PeerQueue peerQueue,
            Queue<OutboundFrame> queue,
            long epoch,
            long now)
        {
            while (queue.TryPeek(out var frame))
            {
                if (frame.SessionEpoch == epoch &&
                    frame.ExpiresAtTickCount64 > now &&
                    frame.Owner is not { IsDisposed: true })
                    return;
                if (frame.SessionEpoch != epoch)
                    Interlocked.Increment(ref _staleSessionFrames);
                else if (frame.ExpiresAtTickCount64 <= now)
                    Interlocked.Increment(ref _expiredFrames);
                else
                    Interlocked.Increment(ref _disposedFrames);
                RemoveDequeued(peerQueue, queue.Dequeue());
            }
        }

        private static void RemoveOwnedFrames(
            PeerQueue peerQueue,
            Queue<OutboundFrame> queue,
            RitsuLibSidecarEndpointRegistration registration)
        {
            if (queue.Count == 0)
                return;
            var retained = new Queue<OutboundFrame>(queue.Count);
            while (queue.TryDequeue(out var frame))
            {
                if (ReferenceEquals(frame.Owner, registration))
                {
                    Interlocked.Increment(ref _disposedFrames);
                    RemoveDequeued(peerQueue, frame);
                }
                else
                    retained.Enqueue(frame);
            }

            while (retained.TryDequeue(out var frame))
                queue.Enqueue(frame);
        }

        private static void RemoveDequeued(PeerQueue peerQueue, OutboundFrame frame)
        {
            peerQueue.MessageCount--;
            peerQueue.ByteCount -= frame.Envelope.Length;
            _queuedMessages--;
            _queuedBytes -= frame.Envelope.Length;
            if (frame.DeliveryProfile == RitsuLibSidecarDeliveryProfile.BulkStream)
            {
                peerQueue.BulkByteCount -= frame.Envelope.Length;
                _bulkQueuedMessages--;
                _bulkQueuedBytes -= frame.Envelope.Length;
            }

            NormalizeCounts();
        }

        private static void RemoveEmptyPeer(ulong peerId, PeerQueue peerQueue)
        {
            if (peerQueue.MessageCount == 0)
                Queues.Remove(peerId);
        }

        private static void NormalizeCounts()
        {
            if (_queuedMessages < 0)
                _queuedMessages = 0;
            if (_queuedBytes < 0)
                _queuedBytes = 0;
            if (_bulkQueuedMessages < 0)
                _bulkQueuedMessages = 0;
            if (_bulkQueuedBytes < 0)
                _bulkQueuedBytes = 0;
        }

        private sealed class PeerQueue
        {
            internal Queue<OutboundFrame> Control { get; } = [];
            internal Queue<OutboundFrame> Realtime { get; } = [];
            internal Queue<OutboundFrame> Bulk { get; } = [];
            internal int MessageCount { get; set; }
            internal int ByteCount { get; set; }
            internal int BulkByteCount { get; set; }
        }

        private readonly record struct OutboundFrame(
            long SessionEpoch,
            ulong PeerNetId,
            byte[] Envelope,
            RitsuLibSidecarDeliveryProfile DeliveryProfile,
            long ExpiresAtTickCount64,
            RitsuLibSidecarEndpointRegistration? Owner);
    }
}
