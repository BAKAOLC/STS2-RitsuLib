using System.Buffers;
using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Optional 8-byte big-endian correlation id in the header extension immediately after the 1-byte delivery tag
    ///     from <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> (layout: delivery, correlation × 8, optional
    ///     tail).
    /// </summary>
    public static class RitsuLibSidecarRequestCorrelation
    {
        /// <summary>Size of the correlation id in the extension (after the delivery byte).</summary>
        public const int BigEndianU64Bytes = 8;

        /// <summary>
        ///     Minimum full <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> length to read a
        ///     correlation.
        /// </summary>
        public const int MinHeaderExtensionBytesWithCorrelation = 1 + BigEndianU64Bytes;

        private static long _nextCorrelation;

        /// <summary>Allocates a monotonically increasing correlation value for request/reply matching.</summary>
        public static ulong AllocateCorrelationId()
        {
            return (ulong)Interlocked.Increment(ref _nextCorrelation);
        }

        /// <summary>Writes <paramref name="correlationId" /> big-endian into the first 8 bytes of <paramref name="destination" />.</summary>
        public static void WriteCorrelationBigEndian(Span<byte> destination, ulong correlationId)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, correlationId);
        }

        /// <summary>
        ///     Builds <c>additionalHeaderExtension</c> for <see cref="RitsuLibSidecarHighLevelSend.TrySendAsClient" />:
        ///     correlation (8 BE) then <paramref name="tailAfterCorrelation" />.
        /// </summary>
        public static byte[] PackAdditional(ulong correlationId, ReadOnlySpan<byte> tailAfterCorrelation = default)
        {
            var buf = new byte[BigEndianU64Bytes + tailAfterCorrelation.Length];
            WriteCorrelationBigEndian(buf.AsSpan(0, BigEndianU64Bytes), correlationId);
            tailAfterCorrelation.CopyTo(buf.AsSpan(BigEndianU64Bytes));
            return buf;
        }

        /// <summary>Appends correlation and tail to <paramref name="writer" />.</summary>
        public static void PackAdditionalTo(ulong correlationId, ReadOnlySpan<byte> tailAfterCorrelation,
            IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(BigEndianU64Bytes + tailAfterCorrelation.Length);
            WriteCorrelationBigEndian(span, correlationId);
            tailAfterCorrelation.CopyTo(span[BigEndianU64Bytes..]);
            writer.Advance(BigEndianU64Bytes + tailAfterCorrelation.Length);
        }

        /// <summary>Reads the correlation from a full header extension (delivery byte first).</summary>
        public static bool TryReadCorrelation(ReadOnlyMemory<byte> fullHeaderExtension, out ulong correlationId)
        {
            correlationId = 0;
            if (fullHeaderExtension.Length < MinHeaderExtensionBytesWithCorrelation)
                return false;

            correlationId = BinaryPrimitives.ReadUInt64BigEndian(fullHeaderExtension.Span.Slice(1, BigEndianU64Bytes));
            return true;
        }

        /// <summary>True when a correlation is present and equals <paramref name="expected" />.</summary>
        public static bool HeaderExtensionCorrelationEquals(ReadOnlyMemory<byte> fullHeaderExtension, ulong expected)
        {
            return TryReadCorrelation(fullHeaderExtension, out var c) && c == expected;
        }
    }
}
