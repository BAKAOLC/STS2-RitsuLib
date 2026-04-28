using System.Buffers.Binary;
using System.IO.Hashing;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Layout for <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> user payload.
    /// </summary>
    public static class RitsuLibSidecarChunkBinary
    {
        /// <summary>
        ///     Size in bytes of the fixed prefix (all big-endian): <c>userOpcode</c>, <c>streamId</c>, <c>index</c>,
        ///     <c>count</c>, <c>totalSize</c>, <c>segment length</c>, <c>segmentCrc32</c>.
        /// </summary>
        public const int FixedHeaderSize = RitsuLibSidecarChunkFrameLayout.FixedHeaderSize;

        /// <summary>
        ///     Default max bytes per segment (excluding the fixed header).
        /// </summary>
        public const int DefaultMaxSegmentDataBytes = 16 * RitsuLibSidecarBinaryLayout.KiB;

        /// <summary>
        ///     Serializes one chunk frame and returns total bytes written.
        /// </summary>
        public static int WriteFrame(
            Span<byte> destination,
            ulong userOpcode,
            ulong streamId,
            uint index,
            uint count,
            uint totalPayloadSize,
            ReadOnlySpan<byte> segment)
        {
            if (destination.Length < FixedHeaderSize + segment.Length)
                throw new ArgumentException("Buffer too small", nameof(destination));

            var crc = Crc32.HashToUInt32(segment);

            BinaryPrimitives.WriteUInt64BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.UserOpcodeOffset,
                    RitsuLibSidecarBinaryLayout.U64Size),
                userOpcode);
            BinaryPrimitives.WriteUInt64BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.StreamIdOffset, RitsuLibSidecarBinaryLayout.U64Size),
                streamId);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.SegmentIndexOffset,
                    RitsuLibSidecarBinaryLayout.U32Size),
                index);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.SegmentCountOffset,
                    RitsuLibSidecarBinaryLayout.U32Size),
                count);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.TotalPayloadSizeOffset,
                    RitsuLibSidecarBinaryLayout.U32Size),
                totalPayloadSize);
            BinaryPrimitives.WriteUInt16BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.SegmentLengthOffset,
                    RitsuLibSidecarBinaryLayout.U16Size),
                (ushort)segment.Length);
            BinaryPrimitives.WriteUInt32BigEndian(
                destination.Slice(RitsuLibSidecarChunkFrameLayout.SegmentCrc32Offset,
                    RitsuLibSidecarBinaryLayout.U32Size),
                crc);
            segment.CopyTo(destination[FixedHeaderSize..]);
            return FixedHeaderSize + segment.Length;
        }

        /// <summary>
        ///     Parses one chunk frame from a full <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> message body.
        /// </summary>
        public static void ReadFrame(
            ReadOnlySpan<byte> source,
            out ulong userOpcode,
            out ulong streamId,
            out uint index,
            out uint count,
            out uint totalPayloadSize,
            out uint segmentCrc32,
            out ReadOnlySpan<byte> segment)
        {
            if (source.Length < FixedHeaderSize)
                throw new ArgumentException("Buffer too small", nameof(source));

            userOpcode = BinaryPrimitives.ReadUInt64BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.UserOpcodeOffset, RitsuLibSidecarBinaryLayout.U64Size));
            streamId = BinaryPrimitives.ReadUInt64BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.StreamIdOffset, RitsuLibSidecarBinaryLayout.U64Size));
            index = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.SegmentIndexOffset, RitsuLibSidecarBinaryLayout.U32Size));
            count = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.SegmentCountOffset, RitsuLibSidecarBinaryLayout.U32Size));
            totalPayloadSize = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.TotalPayloadSizeOffset,
                    RitsuLibSidecarBinaryLayout.U32Size));
            var len = BinaryPrimitives.ReadUInt16BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.SegmentLengthOffset, RitsuLibSidecarBinaryLayout.U16Size));
            segmentCrc32 = BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(RitsuLibSidecarChunkFrameLayout.SegmentCrc32Offset, RitsuLibSidecarBinaryLayout.U32Size));
            if (source.Length < FixedHeaderSize + len)
                throw new ArgumentException("Truncated segment", nameof(source));

            segment = source.Slice(FixedHeaderSize, len);
        }
    }
}
