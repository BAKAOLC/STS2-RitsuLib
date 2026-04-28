using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Binary layout for <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> and <c>HandshakeAck</c> payloads.</summary>
    public static class RitsuLibSidecarHandshakeBinary
    {
        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> body: version, max version, features (all
        ///     big-endian where multi-byte).
        /// </summary>
        public const int HandshakePayloadSize = 2 + 2 + 4;

        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> body: selected version, ok byte,
        ///     ack-sender features.
        /// </summary>
        public const int AckPayloadSize = 2 + 1 + 4;

        /// <summary>Serializes a hello payload; <paramref name="d" /> must be at least <see cref="HandshakePayloadSize" />.</summary>
        /// <param name="d">Output buffer for the handshake body.</param>
        /// <param name="wireFormatVersion">Wire format version this sender is using.</param>
        /// <param name="supportedWireFormatVersionMax">Maximum wire version this sender can parse from peers.</param>
        /// <param name="features">Capability bits this sender supports.</param>
        public static void WriteHandshake(Span<byte> d, ushort wireFormatVersion, ushort supportedWireFormatVersionMax,
            RitsuLibSidecarPeerFeatures features)
        {
            if (d.Length < HandshakePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            BinaryPrimitives.WriteUInt16BigEndian(d[..2], wireFormatVersion);
            BinaryPrimitives.WriteUInt16BigEndian(d.Slice(2, 2), supportedWireFormatVersionMax);
            BinaryPrimitives.WriteUInt32BigEndian(d.Slice(4, 4), (uint)features);
        }

        /// <summary>Deserializes a hello body from a full <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> message payload.</summary>
        /// <param name="d">Buffer containing at least <see cref="HandshakePayloadSize" /> bytes.</param>
        /// <param name="wireFormatVersion">Peer wire format version.</param>
        /// <param name="supportedWireFormatVersionMax">Maximum wire version the peer can parse.</param>
        /// <param name="features">Capability bits advertised by the peer.</param>
        public static void ReadHandshake(ReadOnlySpan<byte> d, out ushort wireFormatVersion,
            out ushort supportedWireFormatVersionMax, out RitsuLibSidecarPeerFeatures features)
        {
            if (d.Length < HandshakePayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            wireFormatVersion = BinaryPrimitives.ReadUInt16BigEndian(d);
            supportedWireFormatVersionMax = BinaryPrimitives.ReadUInt16BigEndian(d[2..]);
            features = (RitsuLibSidecarPeerFeatures)BinaryPrimitives.ReadUInt32BigEndian(d.Slice(4, 4));
        }

        /// <summary>Serializes an ack; <paramref name="d" /> must be at least <see cref="AckPayloadSize" />.</summary>
        /// <param name="d">Output buffer for the ack body.</param>
        /// <param name="selectedWireFormatVersion">Wire version chosen for the session after negotiation.</param>
        /// <param name="ok">Whether the negotiation succeeded.</param>
        /// <param name="ackSenderFeatures">Capabilities of the node that writes this buffer (the responder).</param>
        public static void WriteAck(
            Span<byte> d,
            ushort selectedWireFormatVersion,
            bool ok,
            RitsuLibSidecarPeerFeatures ackSenderFeatures)
        {
            if (d.Length < AckPayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            BinaryPrimitives.WriteUInt16BigEndian(d, selectedWireFormatVersion);
            d[2] = ok ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt32BigEndian(d.Slice(3, 4), (uint)ackSenderFeatures);
        }

        /// <summary>Deserializes an ack body from a <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> message payload.</summary>
        /// <param name="d">Buffer containing at least <see cref="AckPayloadSize" /> bytes.</param>
        /// <param name="selectedWireFormatVersion">Wire version chosen by the responder.</param>
        /// <param name="ok">Whether the responder accepted the handshake.</param>
        /// <param name="ackSenderFeatures">Capabilities of the node that sent the ack.</param>
        public static void ReadAck(
            ReadOnlySpan<byte> d,
            out ushort selectedWireFormatVersion,
            out bool ok,
            out RitsuLibSidecarPeerFeatures ackSenderFeatures)
        {
            if (d.Length < AckPayloadSize)
                throw new ArgumentException("Buffer too small", nameof(d));

            selectedWireFormatVersion = BinaryPrimitives.ReadUInt16BigEndian(d);
            ok = d[2] != 0;
            ackSenderFeatures = (RitsuLibSidecarPeerFeatures)BinaryPrimitives.ReadUInt32BigEndian(d.Slice(3, 4));
        }
    }
}
