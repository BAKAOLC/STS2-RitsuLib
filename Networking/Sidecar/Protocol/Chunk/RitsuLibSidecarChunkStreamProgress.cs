namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Progress while sending a chunked sidecar stream (one segment per step).
    ///     发送分块 sidecar 流时的进度（每步一个 segment）。
    /// </summary>
    public readonly record struct RitsuLibSidecarChunkStreamSendProgress(
        int SegmentIndexZeroBased,
        int TotalSegments,
        long BytesSentIncludingCurrentSegment,
        long TotalLogicalBytes);

    /// <summary>
    ///     Progress while reassembling a chunked stream on the receive path.
    ///     在接收路径重组分块流时的进度。
    /// </summary>
    public readonly record struct RitsuLibSidecarChunkReceiveProgress(
        ulong SenderNetId,
        ulong StreamId,
        ulong UserOpcode,
        int ReceivedSegmentCount,
        int TotalSegments,
        int AccumulatedLogicalBytes,
        int TotalLogicalBytes,
        bool ReassemblyCompleted);
}
