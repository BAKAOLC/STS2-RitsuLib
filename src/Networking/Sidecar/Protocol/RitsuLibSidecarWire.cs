using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides the fixed wire-format constants for RitsuLib multiplayer sidecar envelopes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 RitsuLib 多人 sidecar 信封所使用的固定线路格式常量。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarWire
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The recommended ENet or Steam channel for reliable sidecar traffic.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可靠 sidecar 流量使用的推荐 ENet 或 Steam 通道。
        ///     </para>
        /// </summary>
        public const int RecommendedReliableChannel = 48;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The recommended ENet or Steam channel for best-effort sidecar traffic.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尽力而为的 sidecar 流量使用的推荐 ENet 或 Steam 通道。
        ///     </para>
        /// </summary>
        public const int RecommendedUnreliableChannel = 49;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The recommended ENet channel for reliable bulk Sidecar streams. Backends without independent channels
        ///         may map it to their reliable transport while retaining RitsuLib's bulk scheduling limits.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可靠 Sidecar 批量流使用的推荐 ENet 通道。不支持独立通道的后端可以映射到其可靠传输，同时保留
        ///         RitsuLib 的批量调度限制。
        ///     </para>
        /// </summary>
        public const int RecommendedBulkChannel = 50;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The wire-format version written by the current library.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当前库写入的线路格式版本。
        ///     </para>
        /// </summary>
        public const ushort CurrentWireFormatVersion = 2;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The highest wire-format version accepted by this library.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当前库接受的最高线路格式版本。
        ///     </para>
        /// </summary>
        public const ushort SupportedWireFormatVersionMax = 2;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum logical payload size after optional decompression.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选解压后的最大逻辑载荷大小。
        ///     </para>
        /// </summary>
        public const uint MaxPayloadBytes = 4 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum header-extension length.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         标头扩展的最大长度。
        ///     </para>
        /// </summary>
        public const uint MaxHeaderExtensionBytes = 64 * RitsuLibSidecarBinaryLayout.KiB;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the length of <see cref="Magic" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="Magic" /> 的长度。
        ///     </para>
        /// </summary>
        public static int MagicLength => Magic.Length;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the minimum encoded envelope size, excluding extension and payload bytes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取不含扩展和载荷字节的最小信封编码大小。
        ///     </para>
        /// </summary>
        public static int MinEnvelopeSize => MagicLength +
                                             RitsuLibSidecarBinaryLayout.U16Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U64Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U32Size;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the <c>"STS2RitsuLib"u8</c> packet prefix.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <c>"STS2RitsuLib"u8</c> 数据包前缀。
        ///     </para>
        /// </summary>
        public static ReadOnlySpan<byte> Magic => "STS2RitsuLib"u8;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when <paramref name="packet" /> begins with <see cref="Magic" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <paramref name="packet" /> 以 <see cref="Magic" /> 开头时返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        public static bool MatchesMagic(ReadOnlySpan<byte> packet)
        {
            return packet.Length >= MagicLength && packet[..MagicLength].SequenceEqual(Magic);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to read the 64-bit opcode from a sufficiently long sidecar envelope prefix without validating
        ///         the complete envelope.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从长度足够的 sidecar 信封前缀读取 64 位操作码，但不验证完整信封。
        ///     </para>
        /// </summary>
        public static bool TryPeekOpcode(ReadOnlySpan<byte> packet, out ulong opcode)
        {
            opcode = 0;
            if (packet.Length < MagicLength +
                RitsuLibSidecarBinaryLayout.U16Size +
                RitsuLibSidecarBinaryLayout.U32Size +
                RitsuLibSidecarBinaryLayout.U64Size || !MatchesMagic(packet))
                return false;

            opcode = BinaryPrimitives.ReadUInt64BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.OpcodeOffset, RitsuLibSidecarEnvelopeLayout.OpcodeSize));
            return true;
        }
    }
}
