namespace STS2RitsuLib.Networking.Sidecar
{
    internal enum RitsuLibSidecarBulkFrameType : byte
    {
        Offer = 1,
        Accept = 2,
        Data = 3,
        Acknowledge = 4,
        Complete = 5,
        Completed = 6,
        Abort = 7,
    }

    internal enum RitsuLibSidecarBulkAbortReason : byte
    {
        Rejected = 1,
        Canceled = 2,
        TimedOut = 3,
        Disconnected = 4,
        EndpointDisposed = 5,
        SourceFailed = 6,
        DestinationFailed = 7,
        IntegrityFailed = 8,
        ResourceLimit = 9,
        ProtocolError = 10,
    }

    internal readonly record struct RitsuLibSidecarBulkFrame(
        RitsuLibSidecarBulkFrameType Type,
        ulong TransferId,
        long TotalLength,
        int ChunkBytes,
        int WindowBytes,
        long Offset,
        RitsuLibSidecarBulkAbortReason AbortReason,
        RitsuLibSidecarBulkStreamMetadata? Metadata,
        ReadOnlyMemory<byte> Payload,
        byte[]? Sha256);
}
