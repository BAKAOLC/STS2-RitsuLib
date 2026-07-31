namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes progress after an attempted segment send in a chunked sidecar stream.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述 sidecar 分块流尝试发送一个分段后的进度。
    ///     </para>
    /// </summary>
    public readonly record struct RitsuLibSidecarChunkStreamSendProgress(
        int SegmentIndexZeroBased,
        int TotalSegments,
        long BytesSentIncludingCurrentSegment,
        long TotalLogicalBytes);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes the receive-side progress of a chunked stream being reassembled.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述接收端重组 sidecar 分块流的进度。
    ///     </para>
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
