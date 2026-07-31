using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Encodes and decodes <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> and
    ///         <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> payloads.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         编解码 <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> 和
    ///         <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> 载荷。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarHandshakeBinary
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The handshake payload length, including the requested version, maximum supported version, and
        ///         feature flags. Multibyte values use big-endian order.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         握手载荷的长度，其中包含请求版本、支持的最高版本和功能标志；多字节值采用大端序。
        ///     </para>
        /// </summary>
        public const int HandshakePayloadSize = RitsuLibSidecarHandshakeLayout.HandshakePayloadSize;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The acknowledgement payload length, including the selected version, acceptance byte, and sender
        ///         feature flags.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         握手确认载荷的长度，其中包含选定版本、接受状态字节和发送方功能标志。
        ///     </para>
        /// </summary>
        public const int AckPayloadSize = RitsuLibSidecarHandshakeLayout.AckPayloadSize;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes a handshake payload to <paramref name="d" />, which must be at least
        ///         <see cref="HandshakePayloadSize" /> bytes long.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将握手载荷写入 <paramref name="d" />；其长度不得小于 <see cref="HandshakePayloadSize" /> 字节。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Reads a handshake from a complete <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从完整的 <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> 载荷中读取握手信息。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Writes a handshake acknowledgement to <paramref name="d" />, which must be at least
        ///         <see cref="AckPayloadSize" /> bytes long.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将握手确认写入 <paramref name="d" />；其长度不得小于 <see cref="AckPayloadSize" /> 字节。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Reads an acknowledgement from a complete
        ///         <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> payload.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从完整的 <see cref="RitsuLibSidecarControlOpcodes.HandshakeAck" /> 载荷中读取握手确认。
        ///     </para>
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
