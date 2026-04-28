using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Wire-level constants for the RitsuLib multiplayer sidecar envelope. Values are fixed; nothing is derived
    ///     from reflection or sorted type lists.
    /// </summary>
    public static class RitsuLibSidecarWire
    {
        /// <summary>
        ///     ENet / Steam channel for reliable sidecar traffic. Placed high to reduce overlap with other mods that
        ///     pick low spare channels; vanilla 0.104.0 uses 0 and 1 only.
        /// </summary>
        public const int RecommendedReliableChannel = 48;

        /// <summary>
        ///     ENet channel for best-effort sidecar traffic.
        /// </summary>
        public const int RecommendedUnreliableChannel = 49;

        /// <summary>
        ///     Wire format version written by <see cref="RitsuLibSidecar.CreateEnvelope" />.
        /// </summary>
        public const ushort CurrentWireFormatVersion = 1;

        /// <summary>
        ///     Highest wire format this library accepts.
        /// </summary>
        public const ushort SupportedWireFormatVersionMax = 1;

        /// <summary>
        ///     Maximum logical payload size (after gzip decompress).
        /// </summary>
        public const uint MaxPayloadBytes = 4 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     Maximum header extension segment length (generous margin for future header TLVs).
        /// </summary>
        public const uint MaxHeaderExtensionBytes = 64 * RitsuLibSidecarBinaryLayout.KiB;

        /// <summary>
        ///     Length of <see cref="Magic" />.
        /// </summary>
        public static int MagicLength => Magic.Length;

        /// <summary>
        ///     Minimum on-wire size: magic + wire version + flags + opcode + payload length + extension length (no
        ///     extension bytes, no payload).
        /// </summary>
        public static int MinEnvelopeSize => MagicLength +
                                             RitsuLibSidecarBinaryLayout.U16Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U64Size +
                                             RitsuLibSidecarBinaryLayout.U32Size +
                                             RitsuLibSidecarBinaryLayout.U32Size;

        /// <summary>
        ///     Packet prefix; <c>"STS2RitsuLib"u8</c>.
        /// </summary>
        public static ReadOnlySpan<byte> Magic => "STS2RitsuLib"u8;

        /// <summary>
        ///     Returns true when <paramref name="packet" /> begins with <see cref="Magic" />.
        /// </summary>
        public static bool MatchesMagic(ReadOnlySpan<byte> packet)
        {
            return packet.Length >= MagicLength && packet[..MagicLength].SequenceEqual(Magic);
        }

        /// <summary>
        ///     Reads the 64-bit opcode from a sidecar envelope prefix when <see cref="MatchesMagic" /> holds and the
        ///     span is long enough; does not validate the full envelope.
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
