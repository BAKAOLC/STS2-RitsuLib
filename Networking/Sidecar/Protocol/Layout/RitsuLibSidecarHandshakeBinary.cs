using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Binary layout for <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> and <c>HandshakeAck</c> payloads.
    /// </summary>
    public static class RitsuLibSidecarHandshakeBinary
    {
        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> body: version, max version, features (all
        ///     big-endian where multi-byte).
        /// </summary>
        public const int HandshakePayloadSize = RitsuLibSidecarHandshakeLayout.HandshakePayloadSize;

        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> body: selected version, ok byte,
        ///     ack-sender features.
        /// </summary>
        public const int AckPayloadSize = RitsuLibSidecarHandshakeLayout.AckPayloadSize;

        /// <summary>
        ///     Serializes a hello payload; <paramref name="d" /> must be at least <see cref="HandshakePayloadSize" />.
        /// </summary>
        public static void WriteHandshake(Span<byte> d, ushort wireFormatVersion, ushort supportedWireFormatVersionMax,
            RitsuLibSidecarPeerFeatures features)
        {
            if (d.Length < HandshakePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            BinaryPrimitives.WriteUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.WireFormatVersionOffset, RitsuLibSidecarBinaryLayout.U16Size),
                wireFormatVersion);
            BinaryPrimitives.WriteUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.SupportedWireFormatVersionMaxOffset,
                    RitsuLibSidecarBinaryLayout.U16Size),
                supportedWireFormatVersionMax);
            BinaryPrimitives.WriteUInt32BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.FeaturesOffset, RitsuLibSidecarBinaryLayout.U32Size),
                (uint)features);
        }

        /// <summary>
        ///     Deserializes a hello body from a full <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> message payload.
        /// </summary>
        public static void ReadHandshake(ReadOnlySpan<byte> d, out ushort wireFormatVersion,
            out ushort supportedWireFormatVersionMax, out RitsuLibSidecarPeerFeatures features)
        {
            if (d.Length < HandshakePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            wireFormatVersion = BinaryPrimitives.ReadUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.WireFormatVersionOffset, RitsuLibSidecarBinaryLayout.U16Size));
            supportedWireFormatVersionMax = BinaryPrimitives.ReadUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.SupportedWireFormatVersionMaxOffset,
                    RitsuLibSidecarBinaryLayout.U16Size));
            features = (RitsuLibSidecarPeerFeatures)BinaryPrimitives.ReadUInt32BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.FeaturesOffset, RitsuLibSidecarBinaryLayout.U32Size));
        }

        /// <summary>
        ///     Serializes an ack; <paramref name="d" /> must be at least <see cref="AckPayloadSize" />.
        /// </summary>
        public static void WriteAck(
            Span<byte> d,
            ushort selectedWireFormatVersion,
            bool ok,
            RitsuLibSidecarPeerFeatures ackSenderFeatures)
        {
            if (d.Length < AckPayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            BinaryPrimitives.WriteUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.AckSelectedWireFormatVersionOffset,
                    RitsuLibSidecarBinaryLayout.U16Size),
                selectedWireFormatVersion);
            d[RitsuLibSidecarHandshakeLayout.AckOkOffset] = ok ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt32BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.AckSenderFeaturesOffset, RitsuLibSidecarBinaryLayout.U32Size),
                (uint)ackSenderFeatures);
        }

        /// <summary>
        ///     Deserializes an ack body from a <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> message payload.
        /// </summary>
        public static void ReadAck(
            ReadOnlySpan<byte> d,
            out ushort selectedWireFormatVersion,
            out bool ok,
            out RitsuLibSidecarPeerFeatures ackSenderFeatures)
        {
            if (d.Length < AckPayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            selectedWireFormatVersion = BinaryPrimitives.ReadUInt16BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.AckSelectedWireFormatVersionOffset,
                    RitsuLibSidecarBinaryLayout.U16Size));
            ok = d[RitsuLibSidecarHandshakeLayout.AckOkOffset] != 0;
            ackSenderFeatures = (RitsuLibSidecarPeerFeatures)BinaryPrimitives.ReadUInt32BigEndian(
                d.Slice(RitsuLibSidecarHandshakeLayout.AckSenderFeaturesOffset, RitsuLibSidecarBinaryLayout.U32Size));
        }
    }
}
