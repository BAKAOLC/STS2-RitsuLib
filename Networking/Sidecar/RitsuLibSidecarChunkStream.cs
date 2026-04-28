using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Splits a large user payload into <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> messages with
    ///     per-segment CRC32; loss recovery uses selective gap reports and re-sends only missing parts (see
    ///     <see cref="RitsuLibSidecarControlOpcodes.ChunkStreamSelectiveNack" />).
    /// </summary>
    public static class RitsuLibSidecarChunkStream
    {
        private static long _streamIdMonotonic;

        /// <summary>Generates a new monotonically increasing stream id (per process) for chunked sends.</summary>
        public static ulong AllocateStreamId()
        {
            return (ulong)Interlocked.Increment(ref _streamIdMonotonic);
        }

        /// <summary>
        ///     Sends <paramref name="full" /> in multiple <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> envelopes
        ///     to the host.
        /// </summary>
        public static void TrySendToHost(
            RunManager? runManager,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.Client,
                null,
                userOpcode,
                full,
                semantics,
                maxSegment);
        }

        /// <summary>Sends a chunked stream from host to a single client.</summary>
        public static void TrySendToPeer(
            RunManager? runManager,
            ulong peerNetId,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.HostToPeer,
                peerNetId,
                userOpcode,
                full,
                semantics,
                maxSegment);
        }

        /// <summary>Host: sends a chunked stream to every ready-to-broadcast peer.</summary>
        public static void TrySendBroadcast(
            RunManager? runManager,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.HostBroadcast,
                null,
                userOpcode,
                full,
                semantics,
                maxSegment);
        }

        private static void SendImpl(
            RunManager? runManager,
            RitsuLibSidecarChunkSendKind kind,
            ulong? peerNetId,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics,
            int maxSegment)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            ArgumentOutOfRangeException.ThrowIfLessThan(maxSegment, 1);

            var totalU = (uint)full.Length;
            if (totalU == 0)
            {
                RitsuLibFramework.Logger.Warn("[Sidecar] Chunked send empty; ignored.");
                return;
            }

            var stream = AllocateStreamId();
            var span = full.Span;
            var count = (int)((totalU + (uint)maxSegment - 1) / (uint)maxSegment);
            var frames = new byte[count][];
            for (var i = 0; i < count; i++)
            {
                var off = i * maxSegment;
                var len = Math.Min(maxSegment, (int)totalU - off);
                var seg = span.Slice(off, len);
                var frame = new byte[RitsuLibSidecarChunkBinary.FixedHeaderSize + len];
                RitsuLibSidecarChunkBinary.WriteFrame(
                    frame.AsSpan(),
                    userOpcode,
                    stream,
                    (uint)i,
                    (uint)count,
                    totalU,
                    seg);
                frames[i] = frame;

                switch (kind)
                {
                    case RitsuLibSidecarChunkSendKind.Client:
                        RitsuLibSidecarHighLevelSend.TrySendAsClient(
                            runManager,
                            RitsuLibSidecarControlOpcodes.ChunkedFrame,
                            frame,
                            semantics);
                        break;
                    case RitsuLibSidecarChunkSendKind.HostBroadcast:
                        RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                            runManager,
                            RitsuLibSidecarControlOpcodes.ChunkedFrame,
                            frame,
                            semantics);
                        break;
                    default:
                        RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                            runManager,
                            peerNetId!.Value,
                            RitsuLibSidecarControlOpcodes.ChunkedFrame,
                            frame,
                            semantics);
                        break;
                }
            }

            RitsuLibSidecarChunkOutboundRegistry.Register(
                new()
                {
                    StreamId = stream,
                    UserOpcode = userOpcode,
                    Count = count,
                    Frames = frames,
                    Kind = kind,
                    Semantics = semantics,
                    UnicastClientNetId = kind == RitsuLibSidecarChunkSendKind.HostToPeer ? peerNetId : null,
                });
        }
    }
}
