using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Parses and builds sidecar envelopes: fixed magic, wire version, flags, 64-bit opcode, optional header
    ///     extension, then payload.
    ///     extension, then payload.
    ///     解析并构建 sidecar envelope：固定 magic、线版本、标志、64 位 opcode、可选 header
    ///     扩展，然后是载荷。
    ///     扩展，然后是载荷。
    /// </summary>
    public static class RitsuLibSidecarEnvelope
    {
        /// <summary>
        ///     Result of parsing an on-wire envelope.
        ///     解析线上 envelope 的结果。
        /// </summary>
        public enum ParseOutcome
        {
            /// <summary>
            ///     Parse succeeded.
            ///     解析成功。
            /// </summary>
            Ok,

            /// <summary>
            ///     Packet shorter than the minimum header.
            ///     数据包短于最小 header。
            /// </summary>
            TooSmall,

            /// <summary>
            ///     Magic mismatch.
            ///     Magic 不匹配。
            /// </summary>
            BadMagic,

            /// <summary>
            ///     Wire format version is zero or greater than <see cref="RitsuLibSidecarWire.SupportedWireFormatVersionMax" />.
            ///     线格式版本为零或大于 <see cref="RitsuLibSidecarWire.SupportedWireFormatVersionMax" />。
            /// </summary>
            WireVersionUnsupported,

            /// <summary>
            ///     Declared payload length invalid, gzip corrupt, or decompressed size over cap.
            ///     声明的载荷长度无效、gzip 损坏，或解压后的大小超过上限。
            /// </summary>
            PayloadLengthInvalid,

            /// <summary>
            ///     Header extension length over cap.
            ///     Header 扩展长度超过上限。
            /// </summary>
            ExtensionLengthInvalid,

            /// <summary>
            ///     Total packet length does not match header fields.
            ///     数据包总长度与 header 字段不匹配。
            /// </summary>
            TotalLengthMismatch,
        }

        private const uint KnownWireFlagsMask = (uint)RitsuLibSidecarWireFlags.PayloadGzip;

        /// <summary>
        ///     Parses an envelope from a byte array backing store.
        ///     从字节数组后备存储解析 envelope。
        /// </summary>
        /// <param name="packet">
        ///     Full on-wire bytes; slice views reference this array.
        ///     完整线上字节；slice 视图引用此数组。
        /// </param>
        /// <param name="parsed">
        ///     Populated when the return value is <see cref="ParseOutcome.Ok" />.
        ///     返回值为 <see cref="ParseOutcome.Ok" /> 时填充。
        /// </param>
        public static ParseOutcome TryParse(byte[] packet, out ParsedEnvelope parsed)
        {
            return TryParse(packet.AsSpan(), packet, out parsed);
        }

        /// <summary>
        ///     Parses an envelope; <paramref name="backing" /> must be the same array as <paramref name="packet" /> spans.
        ///     解析 envelope；<paramref name="backing" /> 必须与 <paramref name="packet" /> span 使用同一数组。
        /// </summary>
        /// <param name="packet">
        ///     Full on-wire bytes as a span over <paramref name="backing" />.
        ///     作为 <paramref name="backing" /> 上 span 的完整线上字节。
        /// </param>
        /// <param name="backing">
        ///     Array used to construct <see cref="ReadOnlyMemory{T}" /> for extension and payload.
        ///     用于为扩展和载荷构造 <see cref="ReadOnlyMemory{T}" /> 的数组。
        /// </param>
        /// <param name="parsed">
        ///     Populated when the return value is <see cref="ParseOutcome.Ok" />.
        ///     返回值为 <see cref="ParseOutcome.Ok" /> 时填充。
        /// </param>
        public static ParseOutcome TryParse(ReadOnlySpan<byte> packet, byte[] backing, out ParsedEnvelope parsed)
        {
            parsed = default;
            if (packet.Length < RitsuLibSidecarWire.MinEnvelopeSize)
                return ParseOutcome.TooSmall;

            if (!RitsuLibSidecarWire.MatchesMagic(packet))
                return ParseOutcome.BadMagic;

            var wireVersion = BinaryPrimitives.ReadUInt16BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.WireVersionOffset,
                    RitsuLibSidecarEnvelopeLayout.WireVersionSize));
            var flagsRaw = BinaryPrimitives.ReadUInt32BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.FlagsOffset, RitsuLibSidecarEnvelopeLayout.FlagsSize));
            var opcode = BinaryPrimitives.ReadUInt64BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.OpcodeOffset, RitsuLibSidecarEnvelopeLayout.OpcodeSize));
            var payloadLen = BinaryPrimitives.ReadUInt32BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.PayloadLengthOffset,
                    RitsuLibSidecarEnvelopeLayout.PayloadLengthSize));
            var extLen = BinaryPrimitives.ReadUInt32BigEndian(
                packet.Slice(RitsuLibSidecarEnvelopeLayout.ExtensionLengthOffset,
                    RitsuLibSidecarEnvelopeLayout.ExtensionLengthSize));

            if (wireVersion is 0 or > RitsuLibSidecarWire.SupportedWireFormatVersionMax)
                return ParseOutcome.WireVersionUnsupported;

            if (payloadLen > RitsuLibSidecarWire.MaxPayloadBytes)
                return ParseOutcome.PayloadLengthInvalid;

            if (extLen > RitsuLibSidecarWire.MaxHeaderExtensionBytes)
                return ParseOutcome.ExtensionLengthInvalid;

            var flags = (RitsuLibSidecarWireFlags)(flagsRaw & KnownWireFlagsMask);
            var total = RitsuLibSidecarEnvelopeLayout.FixedHeaderSize + extLen + payloadLen;
            if (total != packet.Length)
                return ParseOutcome.TotalLengthMismatch;

            var extMem = extLen == 0
                ? ReadOnlyMemory<byte>.Empty
                : new(backing, RitsuLibSidecarEnvelopeLayout.FixedHeaderSize, (int)extLen);

            var payloadOffset = RitsuLibSidecarEnvelopeLayout.FixedHeaderSize + (int)extLen;
            var rawPayload = new ReadOnlyMemory<byte>(backing, payloadOffset, (int)payloadLen);

            ReadOnlyMemory<byte> logicalPayload;
            if ((flags & RitsuLibSidecarWireFlags.PayloadGzip) != 0)
            {
                if (!RitsuLibSidecarCompression.TryGunzip(rawPayload.Span, out var decompressed))
                    return ParseOutcome.PayloadLengthInvalid;

                logicalPayload = decompressed;
            }
            else
            {
                logicalPayload = rawPayload;
            }

            parsed = new(wireVersion, flags, opcode, extMem, logicalPayload);
            return ParseOutcome.Ok;
        }

        /// <summary>
        ///     Builds a complete on-wire envelope. <paramref name="headerExtension" /> is copied after the fixed
        ///     header for forward-compatible optional fields.
        ///     构建完整线上 envelope。<paramref name="headerExtension" /> 会复制到固定
        ///     header 之后，用于向前兼容的可选字段。
        /// </summary>
        /// <param name="wireFormatVersion">
        ///     Wire format version field; must be within the supported range.
        ///     线格式版本字段；必须位于支持范围内。
        /// </param>
        /// <param name="flags">
        ///     Wire flags; gzip may be set when <paramref name="gzipLogicalPayload" /> is <c>true</c>.
        ///     线标志；当 <paramref name="gzipLogicalPayload" /> 为 <c>true</c> 时可设置 gzip。
        /// </param>
        /// <param name="opcode">
        ///     64-bit sidecar opcode.
        ///     64 位 sidecar opcode。
        /// </param>
        /// <param name="headerExtension">
        ///     Optional bytes after the fixed header, before the payload.
        ///     固定 header 之后、载荷之前的可选字节。
        /// </param>
        /// <param name="payloadLogical">
        ///     Uncompressed logical payload; may be compressed when
        ///     <paramref name="gzipLogicalPayload" /> is <c>true</c>.
        ///     未压缩的逻辑载荷；当
        ///     <paramref name="gzipLogicalPayload" /> 为 <c>true</c> 时可能被压缩。
        /// </param>
        /// <param name="gzipLogicalPayload">
        ///     When <c>true</c>, compresses the payload and ORs in
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     为 <c>true</c> 时，压缩载荷并按 OR 写入
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。
        /// </param>
        public static byte[] Build(
            ushort wireFormatVersion,
            RitsuLibSidecarWireFlags flags,
            ulong opcode,
            ReadOnlySpan<byte> headerExtension,
            ReadOnlySpan<byte> payloadLogical,
            bool gzipLogicalPayload)
        {
            if (wireFormatVersion is 0 or > RitsuLibSidecarWire.SupportedWireFormatVersionMax)
                throw new ArgumentOutOfRangeException(nameof(wireFormatVersion));

            if (headerExtension.Length > RitsuLibSidecarWire.MaxHeaderExtensionBytes)
                throw new ArgumentOutOfRangeException(nameof(headerExtension));

            var wirePayload = payloadLogical;
            if (gzipLogicalPayload)
            {
                var compressed = RitsuLibSidecarCompression.GzipCompress(payloadLogical);
                wirePayload = compressed;
                flags |= RitsuLibSidecarWireFlags.PayloadGzip;
            }

            if (wirePayload.Length > RitsuLibSidecarWire.MaxPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(payloadLogical));

            var total = RitsuLibSidecarWire.MinEnvelopeSize + headerExtension.Length + wirePayload.Length;
            var buffer = new byte[total];
            var span = buffer.AsSpan();
            RitsuLibSidecarWire.Magic.CopyTo(span);
            BinaryPrimitives.WriteUInt16BigEndian(
                span.Slice(RitsuLibSidecarEnvelopeLayout.WireVersionOffset,
                    RitsuLibSidecarEnvelopeLayout.WireVersionSize),
                wireFormatVersion);
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(RitsuLibSidecarEnvelopeLayout.FlagsOffset, RitsuLibSidecarEnvelopeLayout.FlagsSize),
                (uint)flags);
            BinaryPrimitives.WriteUInt64BigEndian(
                span.Slice(RitsuLibSidecarEnvelopeLayout.OpcodeOffset, RitsuLibSidecarEnvelopeLayout.OpcodeSize),
                opcode);
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(RitsuLibSidecarEnvelopeLayout.PayloadLengthOffset,
                    RitsuLibSidecarEnvelopeLayout.PayloadLengthSize),
                (uint)wirePayload.Length);
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(RitsuLibSidecarEnvelopeLayout.ExtensionLengthOffset,
                    RitsuLibSidecarEnvelopeLayout.ExtensionLengthSize),
                (uint)headerExtension.Length);

            var extensionOffset = RitsuLibSidecarEnvelopeLayout.FixedHeaderSize;
            headerExtension.CopyTo(span.Slice(extensionOffset, headerExtension.Length));
            var payloadWriteOffset = extensionOffset + headerExtension.Length;
            wirePayload.CopyTo(span[payloadWriteOffset..]);
            return buffer;
        }

        /// <summary>
        ///     Decoded header fields and logical payload.
        ///     已解码的 header 字段和逻辑载荷。
        /// </summary>
        public readonly struct ParsedEnvelope
        {
            /// <summary>
            ///     Creates a parsed envelope value.
            ///     创建 parsed envelope 值。
            /// </summary>
            /// <param name="wireFormatVersion">
            ///     Wire version from the packet.
            ///     数据包中的线版本。
            /// </param>
            /// <param name="flags">
            ///     Decoded wire flags.
            ///     已解码的线标志。
            /// </param>
            /// <param name="opcode">
            ///     64-bit opcode from the packet.
            ///     数据包中的 64 位 opcode。
            /// </param>
            /// <param name="headerExtension">
            ///     Optional extension segment.
            ///     可选扩展段。
            /// </param>
            /// <param name="payload">
            ///     Logical payload (decompressed if gzip was set).
            ///     逻辑载荷（如果设置了 gzip，则为解压后）。
            /// </param>
            public ParsedEnvelope(
                ushort wireFormatVersion,
                RitsuLibSidecarWireFlags flags,
                ulong opcode,
                ReadOnlyMemory<byte> headerExtension,
                ReadOnlyMemory<byte> payload)
            {
                WireFormatVersion = wireFormatVersion;
                Flags = flags;
                Opcode = opcode;
                HeaderExtension = headerExtension;
                Payload = payload;
            }

            /// <summary>
            ///     Wire format version from the packet.
            ///     数据包中的线格式版本。
            /// </summary>
            public ushort WireFormatVersion { get; }

            /// <summary>
            ///     Decoded flags (unknown bits cleared).
            ///     已解码标志（未知位已清除）。
            /// </summary>
            public RitsuLibSidecarWireFlags Flags { get; }

            /// <summary>
            ///     64-bit opcode (from <see cref="RitsuLibSidecarOpcodes.For" /> or a framework constant).
            ///     64 位 opcode（来自 <see cref="RitsuLibSidecarOpcodes.For" /> 或框架常量）。
            /// </summary>
            public ulong Opcode { get; }

            /// <summary>
            ///     Opaque header extension; v1 senders typically use length 0.
            ///     不透明 header 扩展；v1 发送方通常使用长度 0。
            /// </summary>
            public ReadOnlyMemory<byte> HeaderExtension { get; }

            /// <summary>
            ///     Logical payload (after optional gzip decompression).
            ///     逻辑载荷（可选 gzip 解压之后）。
            /// </summary>
            public ReadOnlyMemory<byte> Payload { get; }
        }
    }
}
