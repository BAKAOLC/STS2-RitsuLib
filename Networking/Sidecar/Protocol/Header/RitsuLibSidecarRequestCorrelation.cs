using System.Buffers;
using System.Buffers.Binary;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Optional 8-byte big-endian correlation id in the header extension immediately after the 1-byte delivery tag
    ///     from <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> (layout: delivery, correlation × 8, optional
    ///     tail).
    ///     header 扩展中紧跟 1 字节投递标签之后的可选 8 字节 big-endian correlation id
    ///     来自 <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />（布局：delivery、correlation × 8、可选
    ///     tail）。
    /// </summary>
    public static class RitsuLibSidecarRequestCorrelation
    {
        /// <summary>
        ///     Size of the correlation id in the extension (after the delivery byte).
        ///     扩展中 correlation id 的大小（位于投递字节之后）。
        /// </summary>
        public const int BigEndianU64Bytes = RitsuLibSidecarBinaryLayout.U64Size;

        /// <summary>
        ///     Minimum full <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> length to read a
        ///     correlation.
        ///     读取
        ///     correlation 所需的完整 <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> 最小长度。
        /// </summary>
        public const int MinHeaderExtensionBytesWithCorrelation =
            RitsuLibSidecarBinaryLayout.ByteSize + BigEndianU64Bytes;

        private static long _nextCorrelation;

        /// <summary>
        ///     Allocates a monotonically increasing correlation value for request/reply matching.
        ///     为请求/回复匹配分配一个单调递增的 correlation 值。
        /// </summary>
        public static ulong AllocateCorrelationId()
        {
            return (ulong)Interlocked.Increment(ref _nextCorrelation);
        }

        /// <summary>
        ///     Writes <paramref name="correlationId" /> big-endian into the first 8 bytes of <paramref name="destination" />.
        ///     将 <paramref name="correlationId" /> 以 big-endian 写入 <paramref name="destination" /> 的前 8 字节。
        /// </summary>
        public static void WriteCorrelationBigEndian(Span<byte> destination, ulong correlationId)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, correlationId);
        }

        /// <summary>
        ///     Builds <c>additionalHeaderExtension</c> for
        ///     <see
        ///         cref="RitsuLibSidecarHighLevelSend.TrySendAsClient(RunManager?,ulong,System.ReadOnlySpan{byte},RitsuLibSidecarDeliverySemantics,RitsuLibSidecarWireFlags,bool,System.ReadOnlySpan{byte})" />
        ///     />
        ///     :
        ///     correlation (8 BE) then <paramref name="tailAfterCorrelation" />.
        ///     构建 <c>additionalHeaderExtension</c>，用于
        ///     <see />
        ///     ：
        ///     correlation（8 BE），然后是 <paramref name="tailAfterCorrelation" />。
        /// </summary>
        public static byte[] PackAdditional(ulong correlationId, ReadOnlySpan<byte> tailAfterCorrelation = default)
        {
            var buf = new byte[BigEndianU64Bytes + tailAfterCorrelation.Length];
            WriteCorrelationBigEndian(buf.AsSpan(0, BigEndianU64Bytes), correlationId);
            tailAfterCorrelation.CopyTo(buf.AsSpan(BigEndianU64Bytes));
            return buf;
        }

        /// <summary>
        ///     Appends correlation and tail to <paramref name="writer" />.
        ///     将 correlation 和 tail 追加到 <paramref name="writer" />。
        /// </summary>
        public static void PackAdditionalTo(ulong correlationId, ReadOnlySpan<byte> tailAfterCorrelation,
            IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(BigEndianU64Bytes + tailAfterCorrelation.Length);
            WriteCorrelationBigEndian(span, correlationId);
            tailAfterCorrelation.CopyTo(span[BigEndianU64Bytes..]);
            writer.Advance(BigEndianU64Bytes + tailAfterCorrelation.Length);
        }

        /// <summary>
        ///     Reads the correlation from a full header extension (delivery byte first).
        ///     从完整 header 扩展（投递字节在前）读取 correlation。
        /// </summary>
        public static bool TryReadCorrelation(ReadOnlyMemory<byte> fullHeaderExtension, out ulong correlationId)
        {
            correlationId = 0;
            if (fullHeaderExtension.Length < MinHeaderExtensionBytesWithCorrelation)
                return false;

            correlationId = BinaryPrimitives.ReadUInt64BigEndian(
                fullHeaderExtension.Span.Slice(
                    RitsuLibSidecarEnvelopeLayout.CorrelationOffsetInExtension,
                    BigEndianU64Bytes));
            return true;
        }

        /// <summary>
        ///     True when a correlation is present and equals <paramref name="expected" />.
        ///     存在 correlation 且等于 <paramref name="expected" /> 时为 True。
        /// </summary>
        public static bool HeaderExtensionCorrelationEquals(ReadOnlyMemory<byte> fullHeaderExtension, ulong expected)
        {
            return TryReadCorrelation(fullHeaderExtension, out var c) && c == expected;
        }
    }
}
