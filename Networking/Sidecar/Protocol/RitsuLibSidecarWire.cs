using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Wire-level constants for the RitsuLib multiplayer sidecar envelope. Values are fixed; nothing is derived
    ///     from reflection or sorted type lists.
    ///     RitsuLib 多人 sidecar envelope 的线级常量。值是固定的；没有任何内容派生
    ///     自反射或排序后的类型列表。
    /// </summary>
    public static class RitsuLibSidecarWire
    {
        /// <summary>
        ///     ENet / Steam channel for reliable sidecar traffic. Placed high to reduce overlap with other mods that
        ///     pick low spare channels; vanilla 0.104.0 uses 0 and 1 only.
        ///     可靠 sidecar 流量使用的 ENet / Steam channel。放在较高位置，以减少与
        ///     选择低位备用 channel 的其他 mod 重叠；原版 0.104.0 只使用 0 和 1。
        /// </summary>
        public const int RecommendedReliableChannel = 48;

        /// <summary>
        ///     ENet channel for best-effort sidecar traffic.
        ///     best-effort sidecar 流量使用的 ENet channel。
        /// </summary>
        public const int RecommendedUnreliableChannel = 49;

        /// <summary>
        ///     Wire format version written by <see cref="RitsuLibSidecar.CreateEnvelope" />.
        ///     <see cref="RitsuLibSidecar.CreateEnvelope" /> 写入的线格式版本。
        /// </summary>
        public const ushort CurrentWireFormatVersion = 1;

        /// <summary>
        ///     Highest wire format this library accepts.
        ///     此库接受的最高线格式版本。
        /// </summary>
        public const ushort SupportedWireFormatVersionMax = 1;

        /// <summary>
        ///     Maximum logical payload size (after gzip decompress).
        ///     最大逻辑载荷大小（gzip 解压之后）。
        /// </summary>
        public const uint MaxPayloadBytes = 4 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     Maximum header extension segment length (generous margin for future header TLVs).
        ///     最大 header 扩展段长度（为未来 header TLV 预留的宽裕余量）。
        /// </summary>
        public const uint MaxHeaderExtensionBytes = 64 * RitsuLibSidecarBinaryLayout.KiB;

        /// <summary>
        ///     Length of <see cref="Magic" />.
        ///     <see cref="Magic" /> 的长度。
        /// </summary>
        public static int MagicLength => Magic.Length;

        /// <summary>
        ///     Minimum on-wire size: magic + wire version + flags + opcode + payload length + extension length (no
        ///     extension bytes, no payload).
        ///     extension bytes, no payload).
        ///     最小线上大小：magic + wire version + flags + opcode + payload length + extension length（无
        ///     extension 字节，无 payload）。
        /// </summary>
        public static int MinEnvelopeSize => MagicLength +
                                             RitsuLibSidecarBinaryLayout.U16Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U64Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U32Size;

        /// <summary>
        ///     Packet prefix; <c>"STS2RitsuLib"u8</c>.
        ///     数据包前缀；<c>"STS2RitsuLib"u8</c>。
        /// </summary>
        public static ReadOnlySpan<byte> Magic => "STS2RitsuLib"u8;

        /// <summary>
        ///     Returns true when <paramref name="packet" /> begins with <see cref="Magic" />.
        ///     当 <paramref name="packet" /> 以 <see cref="Magic" /> 开头时返回 true。
        /// </summary>
        public static bool MatchesMagic(ReadOnlySpan<byte> packet)
        {
            return packet.Length >= MagicLength && packet[..MagicLength].SequenceEqual(Magic);
        }

        /// <summary>
        ///     Reads the 64-bit opcode from a sidecar envelope prefix when <see cref="MatchesMagic" /> holds and the
        ///     span is long enough; does not validate the full envelope.
        ///     当 <see cref="MatchesMagic" /> 成立且
        ///     span 足够长时，从 sidecar envelope 前缀读取 64 位 opcode；不验证完整 envelope。
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
