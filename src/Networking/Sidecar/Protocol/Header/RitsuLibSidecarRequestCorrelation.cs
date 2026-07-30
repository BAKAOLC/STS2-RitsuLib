using System.Buffers;
using System.Buffers.Binary;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Encodes an optional 8-byte big-endian correlation ID immediately after the delivery tag in a header
    ///         extension. Optional trailing bytes follow the correlation ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在标头扩展的投递标签之后编解码可选的 8 字节大端序关联 ID；其他可选字节位于关联 ID 之后。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarRequestCorrelation
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The size of the correlation ID in the extension.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         扩展中关联 ID 的大小。
        ///     </para>
        /// </summary>
        public const int BigEndianU64Bytes = RitsuLibSidecarBinaryLayout.U64Size;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The minimum complete header-extension length required to read a correlation ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         读取关联 ID 所需的完整标头扩展最小长度。
        ///     </para>
        /// </summary>
        public const int MinHeaderExtensionBytesWithCorrelation =
            RitsuLibSidecarBinaryLayout.ByteSize + BigEndianU64Bytes;

        private static long _nextCorrelation;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Allocates a process-local, monotonically increasing correlation ID for request/reply matching.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为请求与回复的匹配分配一个进程内单调递增的关联 ID。
        ///     </para>
        /// </summary>
        public static ulong AllocateCorrelationId()
        {
            return (ulong)Interlocked.Increment(ref _nextCorrelation);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes <paramref name="correlationId" /> in big-endian order to the first 8 bytes of
        ///         <paramref name="destination" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <paramref name="correlationId" /> 以大端序写入 <paramref name="destination" /> 的前 8 字节。
        ///     </para>
        /// </summary>
        public static void WriteCorrelationBigEndian(Span<byte> destination, ulong correlationId)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, correlationId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds additional header-extension bytes for <see cref="RitsuLibSidecarHighLevelSend" />: an
        ///         8-byte big-endian correlation ID followed by <paramref name="tailAfterCorrelation" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see cref="RitsuLibSidecarHighLevelSend" /> 构建附加标头扩展：先写入 8 字节大端序关联 ID，
        ///         再写入 <paramref name="tailAfterCorrelation" />。
        ///     </para>
        /// </summary>
        public static byte[] PackAdditional(ulong correlationId, ReadOnlySpan<byte> tailAfterCorrelation = default)
        {
            var buf = new byte[BigEndianU64Bytes + tailAfterCorrelation.Length];
            WriteCorrelationBigEndian(buf.AsSpan(0, BigEndianU64Bytes), correlationId);
            tailAfterCorrelation.CopyTo(buf.AsSpan(BigEndianU64Bytes));
            return buf;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Appends a big-endian correlation ID and trailing bytes to <paramref name="writer" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向 <paramref name="writer" /> 追加大端序关联 ID 和尾随字节。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Reads the correlation ID from a complete header extension whose first byte is the delivery tag.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从以投递标签为首字节的完整标头扩展中读取关联 ID。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when the header extension contains
        ///         <paramref name="expected" /> as its correlation ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当标头扩展包含关联 ID <paramref name="expected" /> 时返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        public static bool HeaderExtensionCorrelationEquals(ReadOnlyMemory<byte> fullHeaderExtension, ulong expected)
        {
            return TryReadCorrelation(fullHeaderExtension, out var c) && c == expected;
        }
    }
}
