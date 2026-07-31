using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Splits large user payloads into CRC-protected chunk frames and retains them for selective retransmission.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将大型用户载荷拆分为带 CRC 校验的分块帧，并保留这些帧以供选择性重传。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Frames are fully constructed and registered for retransmission before the first transport attempt. If the
    ///         first attempt fails, its outbound state is removed. A later failed attempt stops the remaining sends while
    ///         retaining the state for frames that may already have reached the peer.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         会在首次传输尝试前完整构造帧并注册重传状态。首次尝试失败时会移除其出站状态；之后的尝试失败时，
    ///         会停止发送剩余帧，但保留已可能到达对等方的帧的重传状态。
    ///     </para>
    /// </remarks>
    public static class RitsuLibSidecarChunkStream
    {
        private static long _streamIdMonotonic;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allocates a process-local, monotonically increasing stream ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         分配进程内单调递增的流 ID。
        ///     </para>
        /// </summary>
        public static ulong AllocateStreamId()
        {
            return (ulong)Interlocked.Increment(ref _streamIdMonotonic);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to send <paramref name="full" /> to the host as a chunked stream.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将 <paramref name="full" /> 作为分块流发送到主机。
        ///     </para>
        /// </summary>
        public static void TrySendToHost(
            RunManager? runManager,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes,
            IProgress<RitsuLibSidecarChunkStreamSendProgress>? progress = null)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.Client,
                null,
                userOpcode,
                full,
                semantics,
                maxSegment,
                progress);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to send a chunked stream from the host to one peer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从主机向一个对等端发送分块流。
        ///     </para>
        /// </summary>
        public static void TrySendToPeer(
            RunManager? runManager,
            ulong peerNetId,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes,
            IProgress<RitsuLibSidecarChunkStreamSendProgress>? progress = null)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.HostToPeer,
                peerNetId,
                userOpcode,
                full,
                semantics,
                maxSegment,
                progress);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to broadcast a chunked stream from the host to every eligible peer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从主机向所有符合条件的对等端广播分块流。
        ///     </para>
        /// </summary>
        public static void TrySendBroadcast(
            RunManager? runManager,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync,
            int maxSegment = RitsuLibSidecarChunkBinary.DefaultMaxSegmentDataBytes,
            IProgress<RitsuLibSidecarChunkStreamSendProgress>? progress = null)
        {
            SendImpl(
                runManager,
                RitsuLibSidecarChunkSendKind.HostBroadcast,
                null,
                userOpcode,
                full,
                semantics,
                maxSegment,
                progress);
        }

        private static void SendImpl(
            RunManager? runManager,
            RitsuLibSidecarChunkSendKind kind,
            ulong? peerNetId,
            ulong userOpcode,
            ReadOnlyMemory<byte> full,
            RitsuLibSidecarDeliverySemantics semantics,
            int maxSegment,
            IProgress<RitsuLibSidecarChunkStreamSendProgress>? progress)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            ArgumentOutOfRangeException.ThrowIfLessThan(maxSegment, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(maxSegment, ushort.MaxValue);

            var totalU = (uint)full.Length;
            if (totalU == 0)
            {
                RitsuLibSidecarRepeatedWarningLog.Warn(
                    $"chunk-send-empty:peer={peerNetId?.ToString() ?? "broadcast"}:op={userOpcode}",
                    "[Sidecar] Chunked send empty; ignored.");
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

            var logicalSent = 0L;
            for (var i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];
                var sent = kind switch
                {
                    RitsuLibSidecarChunkSendKind.Client => RitsuLibSidecarHighLevelSend.TrySendAsClient(
                        runManager,
                        RitsuLibSidecarControlOpcodes.ChunkedFrame,
                        frame,
                        semantics),
                    RitsuLibSidecarChunkSendKind.HostBroadcast => RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                        runManager,
                        RitsuLibSidecarControlOpcodes.ChunkedFrame,
                        frame,
                        semantics),
                    _ => RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                        runManager,
                        peerNetId!.Value,
                        RitsuLibSidecarControlOpcodes.ChunkedFrame,
                        frame,
                        semantics),
                };

                if (!sent)
                {
                    if (i == 0)
                        RitsuLibSidecarChunkOutboundRegistry.TryRemove(stream);

                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        $"chunk-send-failed:peer={peerNetId?.ToString() ?? "broadcast"}:op={userOpcode}",
                        "[Sidecar] Chunked send failed; remaining frames were not sent.");
                    return;
                }

                logicalSent += frame.Length - RitsuLibSidecarChunkBinary.FixedHeaderSize;
                progress?.Report(new(i + 1, count, logicalSent, totalU));
            }
        }
    }
}
