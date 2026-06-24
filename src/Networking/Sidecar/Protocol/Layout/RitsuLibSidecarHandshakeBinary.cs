using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Binary layout for <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> and <c>HandshakeAck</c> payloads.
    ///     <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> 和 <c>HandshakeAck</c> 载荷的二进制布局。
    /// </summary>
    public static class RitsuLibSidecarHandshakeBinary
    {
        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> body: version, max version, features (all
        ///     big-endian where multi-byte).
        ///     <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> body 的长度：version、max version、features（多字节均为
        ///     big-endian）。
        /// </summary>
        public const int HandshakePayloadSize = RitsuLibSidecarHandshakeLayout.HandshakePayloadSize;

        /// <summary>
        ///     Length of a <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> body: selected version, ok byte,
        ///     ack-sender features.
        ///     <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> body 的长度：selected version、ok byte、
        ///     ack-sender features。
        /// </summary>
        public const int AckPayloadSize = RitsuLibSidecarHandshakeLayout.AckPayloadSize;

        /// <summary>
        ///     Serializes a hello payload; <paramref name="d" /> must be at least <see cref="HandshakePayloadSize" />.
        ///     序列化 hello 载荷；<paramref name="d" /> 至少必须为 <see cref="HandshakePayloadSize" />。
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
        ///     从完整的 <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> 消息载荷反序列化 hello body。
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
        ///     序列化 ack；<paramref name="d" /> 至少必须为 <see cref="AckPayloadSize" />。
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
        ///     从 <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> 消息载荷反序列化 ack body。
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
