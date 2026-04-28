namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Progress while sending a chunked sidecar stream (one segment per step).</summary>
    public readonly record struct RitsuLibSidecarChunkStreamSendProgress(
        int SegmentIndexZeroBased,
        int TotalSegments,
        long BytesSentIncludingCurrentSegment,
        long TotalLogicalBytes);

    /// <summary>Progress while reassembling a chunked stream on the receive path.</summary>
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
