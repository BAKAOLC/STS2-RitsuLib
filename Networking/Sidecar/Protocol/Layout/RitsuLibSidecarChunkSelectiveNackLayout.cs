namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarChunkSelectiveNackLayout
    {
        internal const int StreamIdOffset = 0;
        internal const int StreamIdSize = RitsuLibSidecarBinaryLayout.U64Size;

        internal const int UserOpcodeOffset = StreamIdOffset + StreamIdSize;
        internal const int UserOpcodeSize = RitsuLibSidecarBinaryLayout.U64Size;

        internal const int CountOffset = UserOpcodeOffset + UserOpcodeSize;
        internal const int CountSize = RitsuLibSidecarBinaryLayout.U32Size;

        internal const int MissingRangeCountOffset = CountOffset + CountSize;
        internal const int MissingRangeCountSize = RitsuLibSidecarBinaryLayout.U16Size;

        internal const int HeaderSize = MissingRangeCountOffset + MissingRangeCountSize;

        internal const int RangeStartIndexOffsetWithinRange = 0;

        internal const int RangeLengthOffsetWithinRange =
            RangeStartIndexOffsetWithinRange + RitsuLibSidecarBinaryLayout.U32Size;

        internal const int RangeSize = RangeLengthOffsetWithinRange + RitsuLibSidecarBinaryLayout.U32Size;
    }
}
