using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Control payloads for selective gap reports and reassembly completion (SACK-style flow).</summary>
    internal static class RitsuLibSidecarChunkGapBinary
    {
        /// <summary>Upper bound on missing ranges per <c>ChunkStreamSelectiveNack</c> message.</summary>
        public const int MaxMissingRangesPerMessage = 256;

        public const int ReassemblyDonePayloadSize = 8;

        /// <summary>Header before variable-length range list for selective NACK messages.</summary>
        public const int SelectiveNackHeaderSize = 8 + 8 + 4 + 2;

        public const int SelectiveNackRangeSize = 4 + 4;

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

            BinaryPrimitives.WriteUInt64BigEndian(destination[..8], streamId);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8, 8), userOpcode);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(16, 4), count);
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(20, 2), (ushort)missingRangesSorted.Length);
            var o = SelectiveNackHeaderSize;
            foreach (var range in missingRangesSorted)
            {
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(o, 4), range.StartIndex);
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(o + 4, 4), range.Length);
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
            userOpcode = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(8, 8));
            count = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(16, 4));
            var n = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(20, 2));
            if (n > MaxMissingRangesPerMessage)
                throw new ArgumentException("Invalid missing count", nameof(source));

            if (source.Length < SelectiveNackHeaderSize + n * SelectiveNackRangeSize)
                throw new ArgumentException("Truncated range list", nameof(source));

            missingRanges = new MissingRange[n];
            var o = SelectiveNackHeaderSize;
            for (var i = 0; i < n; i++)
            {
                var start = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(o, 4));
                var len = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(o + 4, 4));
                missingRanges[i] = new(start, len);
                o += SelectiveNackRangeSize;
            }
        }

        public static void WriteReassemblyDone(Span<byte> destination, ulong streamId)
        {
            if (destination.Length < ReassemblyDonePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(destination));

            BinaryPrimitives.WriteUInt64BigEndian(destination[..8], streamId);
        }

        public static void ReadReassemblyDone(ReadOnlySpan<byte> source, out ulong streamId)
        {
            if (source.Length < ReassemblyDonePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(source));

            streamId = BinaryPrimitives.ReadUInt64BigEndian(source);
        }

        /// <summary>One missing range: zero-based part index + length (both <c>u32</c>, big-endian on wire).</summary>
        public readonly record struct MissingRange(uint StartIndex, uint Length);
    }
}
