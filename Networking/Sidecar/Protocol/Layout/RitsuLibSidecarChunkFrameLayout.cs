namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChunkFrameLayout
    {
        internal const int UserOpcodeOffset = 0;
        internal const int StreamIdOffset = UserOpcodeOffset + RitsuLibSidecarBinaryLayout.U64Size;
        internal const int SegmentIndexOffset = StreamIdOffset + RitsuLibSidecarBinaryLayout.U64Size;
        internal const int SegmentCountOffset = SegmentIndexOffset + RitsuLibSidecarBinaryLayout.U32Size;
        internal const int TotalPayloadSizeOffset = SegmentCountOffset + RitsuLibSidecarBinaryLayout.U32Size;
        internal const int SegmentLengthOffset = TotalPayloadSizeOffset + RitsuLibSidecarBinaryLayout.U32Size;
        internal const int SegmentCrc32Offset = SegmentLengthOffset + RitsuLibSidecarBinaryLayout.U16Size;

        internal const int FixedHeaderSize = SegmentCrc32Offset + RitsuLibSidecarBinaryLayout.U32Size;
    }
}
