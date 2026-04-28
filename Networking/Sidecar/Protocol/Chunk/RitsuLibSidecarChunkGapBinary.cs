using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Control payloads for selective gap reports and reassembly completion (SACK-style flow).
    /// </summary>
    internal static class RitsuLibSidecarChunkGapBinary
    {
        public const int MaxMissingRangesPerMessage = 256;

        public const int ReassemblyDonePayloadSize = RitsuLibSidecarBinaryLayout.U64Size;

        public const int SelectiveNackHeaderSize = RitsuLibSidecarChunkSelectiveNackLayout.HeaderSize;

        public const int SelectiveNackRangeSize = RitsuLibSidecarChunkSelectiveNackLayout.RangeSize;

        public static int WriteSelectiveNack(
            Span<byte> destination,
            ulong streamId,
            ulong userOpcode,
            uint count,
            ReadOnlySpan<MissingRange> missingRangesSorted)
        {
            if (missingRangesSorted.Length > MaxMissingRangesPerMessage)
                throw new ArgumentOutOfRangeException(nameof(missingRangesSorted));

            var need = SelectiveNackHeaderSize + missingRangesSorted.Length * SelectiveNackRangeSize;
            if (destination.Length < need)
                throw new ArgumentException("Buffer too small", nameof(destination));

            BinaryPrimitives.WriteUInt64BigEndian(
                destination.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.StreamIdOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.StreamIdSize),
                streamId);
            BinaryPrimitives.WriteUInt64BigEndian(
                destination.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.UserOpcodeOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.UserOpcodeSize),
                userOpcode);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.CountOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.CountSize),
                count);
            BinaryPrimitives.WriteUInt16BigEndian(
                destination.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.MissingRangeCountOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.MissingRangeCountSize),
                (ushort)missingRangesSorted.Length);
            var o = SelectiveNackHeaderSize;
            foreach (var range in missingRangesSorted)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    destination.Slice(o + RitsuLibSidecarChunkSelectiveNackLayout.RangeStartIndexOffsetWithinRange,
                        RitsuLibSidecarBinaryLayout.U32Size),
                    range.StartIndex);
                BinaryPrimitives.WriteUInt32BigEndian(
                    destination.Slice(o + RitsuLibSidecarChunkSelectiveNackLayout.RangeLengthOffsetWithinRange,
                        RitsuLibSidecarBinaryLayout.U32Size),
                    range.Length);
                o += SelectiveNackRangeSize;
            }

            return o;
        }

        public static void ReadSelectiveNack(
            ReadOnlySpan<byte> source,
            out ulong streamId,
            out ulong userOpcode,
            out uint count,
            out MissingRange[] missingRanges)
        {
            if (source.Length < SelectiveNackHeaderSize)
                throw new ArgumentException("Buffer too small", nameof(source));

            streamId = BinaryPrimitives.ReadUInt64BigEndian(source);
            userOpcode = BinaryPrimitives.ReadUInt64BigEndian(
                source.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.UserOpcodeOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.UserOpcodeSize));
            count = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.CountOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.CountSize));
            var n = BinaryPrimitives.ReadUInt16BigEndian(
                source.Slice(
                    RitsuLibSidecarChunkSelectiveNackLayout.MissingRangeCountOffset,
                    RitsuLibSidecarChunkSelectiveNackLayout.MissingRangeCountSize));
            if (n > MaxMissingRangesPerMessage)
                throw new ArgumentException("Invalid missing count", nameof(source));

            if (source.Length < SelectiveNackHeaderSize + n * SelectiveNackRangeSize)
                throw new ArgumentException("Truncated range list", nameof(source));

            missingRanges = new MissingRange[n];
            var o = SelectiveNackHeaderSize;
            for (var i = 0; i < n; i++)
            {
                var start = BinaryPrimitives.ReadUInt32BigEndian(
                    source.Slice(o + RitsuLibSidecarChunkSelectiveNackLayout.RangeStartIndexOffsetWithinRange,
                        RitsuLibSidecarBinaryLayout.U32Size));
                var len = BinaryPrimitives.ReadUInt32BigEndian(
                    source.Slice(o + RitsuLibSidecarChunkSelectiveNackLayout.RangeLengthOffsetWithinRange,
                        RitsuLibSidecarBinaryLayout.U32Size));
                missingRanges[i] = new(start, len);
                o += SelectiveNackRangeSize;
            }
        }

        public static void WriteReassemblyDone(Span<byte> destination, ulong streamId)
        {
            if (destination.Length < ReassemblyDonePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(destination));

            BinaryPrimitives.WriteUInt64BigEndian(
                destination[..RitsuLibSidecarBinaryLayout.U64Size],
                streamId);
        }

        public static void ReadReassemblyDone(ReadOnlySpan<byte> source, out ulong streamId)
        {
            if (source.Length < ReassemblyDonePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(source));

            streamId = BinaryPrimitives.ReadUInt64BigEndian(source);
        }

        public readonly record struct MissingRange(uint StartIndex, uint Length);
    }
}
