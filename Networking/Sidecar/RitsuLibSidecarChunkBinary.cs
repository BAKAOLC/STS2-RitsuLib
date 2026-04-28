using System.Buffers.Binary;
using System.IO.Hashing;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Layout for <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> user payload.</summary>
    public static class RitsuLibSidecarChunkBinary
    {
        /// <summary>
        ///     Size in bytes of the fixed prefix (all big-endian): <c>userOpcode</c>, <c>streamId</c>, <c>index</c>, <c>count</c>,
        ///     <c>totalSize</c>, <c>segment length</c>, <c>segmentCrc32</c> (ISO 3309 over the segment only).
        /// </summary>
        public const int FixedHeaderSize = 8 + 8 + 4 + 4 + 4 + 2 + 4;

        /// <summary>Default max bytes per segment (excluding this header).</summary>
        public const int DefaultMaxSegmentDataBytes = 16 * 1024;

        /// <summary>Serializes one chunk frame. Returns total bytes written.</summary>
        /// <param name="destination">Buffer; must be at least <see cref="FixedHeaderSize" /> + <paramref name="segment" />.Length.</param>
        /// <param name="userOpcode">Reassembled payload is dispatched to this user opcode.</param>
        /// <param name="streamId">Groups all parts of one stream (per-sender unique id recommended).</param>
        /// <param name="index">Zero-based part index; parts may arrive out of order.</param>
        /// <param name="count">Total parts in this stream.</param>
        /// <param name="totalPayloadSize">Logical length of the full recombined payload.</param>
        /// <param name="segment">This chunk’s slice of the logical payload.</param>
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

            BinaryPrimitives.WriteUInt64BigEndian(destination[..8], userOpcode);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8, 8), streamId);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(16, 4), index);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(20, 4), count);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(24, 4), totalPayloadSize);
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(28, 2), (ushort)segment.Length);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(30, 4), crc);
            segment.CopyTo(destination[FixedHeaderSize..]);
            return FixedHeaderSize + segment.Length;
        }

        /// <summary>Parses one chunk frame from a full <see cref="RitsuLibSidecarControlOpcodes.ChunkedFrame" /> message body.</summary>
        /// <param name="source">Buffer containing the fixed header and segment bytes.</param>
        /// <param name="userOpcode">Target opcode after reassembly.</param>
        /// <param name="streamId">Stream key part with sender identity in the reassembler.</param>
        /// <param name="index">Part index.</param>
        /// <param name="count">Total parts.</param>
        /// <param name="totalPayloadSize">Logical full length; repeated on every part.</param>
        /// <param name="segmentCrc32">Expected CRC32 (ISO 3309) of <paramref name="segment" />.</param>
        /// <param name="segment">Payload bytes of this part only.</param>
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

            userOpcode = BinaryPrimitives.ReadUInt64BigEndian(source);
            streamId = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(8, 8));
            index = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(16, 4));
            count = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(20, 4));
            totalPayloadSize = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(24, 4));
            var len = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(28, 2));
            segmentCrc32 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(30, 4));
            if (source.Length < FixedHeaderSize + len)
                throw new ArgumentException("Truncated segment", nameof(source));

            segment = source.Slice(FixedHeaderSize, len);
        }
    }
}
