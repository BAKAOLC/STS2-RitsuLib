using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Parses and builds Sidecar envelopes containing magic, wire version, flags, opcode, an optional
    ///         header extension, and payload.
    ///     </para>
    ///     <para xml:lang="zh-CN">解析和构建包含魔数、线格式版本、标志、操作码、可选头扩展和载荷的 Sidecar 信封。</para>
    /// </summary>
    public static class RitsuLibSidecarEnvelope
    {
        /// <summary>
        ///     <para xml:lang="en">Specifies the outcome of parsing an on-wire envelope.</para>
        ///     <para xml:lang="zh-CN">指定解析线上信封的结果。</para>
        /// </summary>
        public enum ParseOutcome
        {
            /// <summary>
            ///     <para xml:lang="en">Parsing succeeded.</para>
            ///     <para xml:lang="zh-CN">解析成功。</para>
            /// </summary>
            Ok,

            /// <summary>
            ///     <para xml:lang="en">The packet is shorter than the minimum header.</para>
            ///     <para xml:lang="zh-CN">数据包短于最小头部。</para>
            /// </summary>
            TooSmall,

            /// <summary>
            ///     <para xml:lang="en">The magic value does not match.</para>
            ///     <para xml:lang="zh-CN">魔数不匹配。</para>
            /// </summary>
            BadMagic,

            /// <summary>
            ///     <para xml:lang="en">
            ///         The wire format version is zero or exceeds
            ///         <see cref="RitsuLibSidecarWire.SupportedWireFormatVersionMax" />.
            ///     </para>
            ///     <para xml:lang="zh-CN">线格式版本为零或超过 <see cref="RitsuLibSidecarWire.SupportedWireFormatVersionMax" />。</para>
            /// </summary>
            WireVersionUnsupported,

            /// <summary>
            ///     <para xml:lang="en">
            ///         The declared payload length is invalid, decompression fails, or decompressed data exceeds the
            ///         cap.
            ///     </para>
            ///     <para xml:lang="zh-CN">声明的载荷长度无效、解压失败，或解压数据超过上限。</para>
            /// </summary>
            PayloadLengthInvalid,

            /// <summary>
            ///     <para xml:lang="en">The header-extension length exceeds the cap.</para>
            ///     <para xml:lang="zh-CN">头扩展长度超过上限。</para>
            /// </summary>
            ExtensionLengthInvalid,

            /// <summary>
            ///     <para xml:lang="en">The total packet length does not match the header fields.</para>
            ///     <para xml:lang="zh-CN">数据包总长度与头部字段不匹配。</para>
            /// </summary>
            TotalLengthMismatch,

            /// <summary>
            ///     <para xml:lang="en">
            ///         The supplied span does not contain exactly the bytes in the array used for returned memory views.
            ///     </para>
            ///     <para xml:lang="zh-CN">提供的跨度与用于返回内存视图的数组内容不完全一致。</para>
            /// </summary>
            BackingMismatch,
        }

        private const RitsuLibSidecarWireFlags PayloadCompressionFlags =
            RitsuLibSidecarWireFlags.PayloadGzip |
            RitsuLibSidecarWireFlags.PayloadBrotli;

        private const uint KnownWireFlagsMask = (uint)PayloadCompressionFlags;

        /// <summary>
        ///     <para xml:lang="en">Parses an envelope from the byte array that backs the returned memory views.</para>
        ///     <para xml:lang="zh-CN">从返回内存视图所引用的底层字节数组解析信封。</para>
        /// </summary>
        /// <param name="packet">
        ///     <para xml:lang="en">Full on-wire bytes; returned memory views reference this array.</para>
        ///     <para xml:lang="zh-CN">完整线上字节；返回的内存视图引用此数组。</para>
        /// </param>
        /// <param name="parsed">
        ///     <para xml:lang="en">Populated when the return value is <see cref="ParseOutcome.Ok" />.</para>
        ///     <para xml:lang="zh-CN">返回值为 <see cref="ParseOutcome.Ok" /> 时填充。</para>
        /// </param>
        public static ParseOutcome TryParse(byte[] packet, out ParsedEnvelope parsed)
        {
            return TryParse(packet.AsSpan(), packet, out parsed);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Parses an envelope only when <paramref name="packet" /> exactly contains the bytes in
        ///         <paramref name="backing" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">仅当 <paramref name="packet" /> 精确包含 <paramref name="backing" /> 中的字节时解析信封。</para>
        /// </summary>
        /// <param name="packet">
        ///     <para xml:lang="en">Full on-wire bytes equal to the contents of <paramref name="backing" />.</para>
        ///     <para xml:lang="zh-CN">与 <paramref name="backing" /> 内容相等的完整线上字节。</para>
        /// </param>
        /// <param name="backing">
        ///     <para xml:lang="en">Array used to construct <see cref="ReadOnlyMemory{T}" /> views for the extension and payload.</para>
        ///     <para xml:lang="zh-CN">用于构造扩展和载荷 <see cref="ReadOnlyMemory{T}" /> 视图的数组。</para>
        /// </param>
        /// <param name="parsed">
        ///     <para xml:lang="en">Populated when the return value is <see cref="ParseOutcome.Ok" />.</para>
        ///     <para xml:lang="zh-CN">返回值为 <see cref="ParseOutcome.Ok" /> 时填充。</para>
        /// </param>
        public static ParseOutcome TryParse(ReadOnlySpan<byte> packet, byte[] backing, out ParsedEnvelope parsed)
        {
            parsed = default;
            if (backing == null || packet.Length != backing.Length || !packet.SequenceEqual(backing))
                return ParseOutcome.BackingMismatch;

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
            var compression = flags & PayloadCompressionFlags;
            switch (compression)
            {
                case RitsuLibSidecarWireFlags.None:
                    logicalPayload = rawPayload;
                    break;
                case RitsuLibSidecarWireFlags.PayloadGzip:
                {
                    if (!RitsuLibSidecarCompression.TryGunzip(rawPayload.Span, out var decompressed))
                        return ParseOutcome.PayloadLengthInvalid;

                    logicalPayload = decompressed;
                    break;
                }
                case RitsuLibSidecarWireFlags.PayloadBrotli:
                {
                    if (!RitsuLibSidecarCompression.TryBrotliDecompress(rawPayload.Span, out var decompressed))
                        return ParseOutcome.PayloadLengthInvalid;

                    logicalPayload = decompressed;
                    break;
                }
                default:
                    return ParseOutcome.PayloadLengthInvalid;
            }

            parsed = new(wireVersion, flags, opcode, extMem, logicalPayload);
            return ParseOutcome.Ok;
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a complete on-wire envelope, optionally gzip-compressing the logical payload.</para>
        ///     <para xml:lang="zh-CN">构建完整线上信封，并可选择 gzip 压缩逻辑载荷。</para>
        /// </summary>
        /// <param name="wireFormatVersion">
        ///     <para xml:lang="en">Wire format version, which must be within the supported range.</para>
        ///     <para xml:lang="zh-CN">必须位于支持范围内的线格式版本。</para>
        /// </param>
        /// <param name="flags">
        ///     <para xml:lang="en">Wire flags; the gzip flag is controlled by <paramref name="gzipLogicalPayload" />.</para>
        ///     <para xml:lang="zh-CN">线格式标志；gzip 标志由 <paramref name="gzipLogicalPayload" /> 控制。</para>
        /// </param>
        /// <param name="opcode">
        ///     <para xml:lang="en">64-bit Sidecar opcode.</para>
        ///     <para xml:lang="zh-CN">64 位 Sidecar 操作码。</para>
        /// </param>
        /// <param name="headerExtension">
        ///     <para xml:lang="en">Optional bytes after the fixed header and before the payload.</para>
        ///     <para xml:lang="zh-CN">固定头部之后、载荷之前的可选字节。</para>
        /// </param>
        /// <param name="payloadLogical">
        ///     <para xml:lang="en">Uncompressed logical payload.</para>
        ///     <para xml:lang="zh-CN">未压缩的逻辑载荷。</para>
        /// </param>
        /// <param name="gzipLogicalPayload">
        ///     <para xml:lang="en">
        ///         Whether to gzip-compress the payload and set
        ///         <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">是否 gzip 压缩载荷并设置 <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。</para>
        /// </param>
        public static byte[] Build(
            ushort wireFormatVersion,
            RitsuLibSidecarWireFlags flags,
            ulong opcode,
            ReadOnlySpan<byte> headerExtension,
            ReadOnlySpan<byte> payloadLogical,
            bool gzipLogicalPayload)
        {
            return Build(
                wireFormatVersion,
                flags,
                opcode,
                headerExtension,
                payloadLogical,
                gzipLogicalPayload
                    ? RitsuLibSidecarPayloadCompression.Gzip
                    : RitsuLibSidecarPayloadCompression.None);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a complete on-wire envelope using the requested payload compression mode.</para>
        ///     <para xml:lang="zh-CN">使用请求的载荷压缩模式构建完整线上信封。</para>
        /// </summary>
        /// <param name="wireFormatVersion">
        ///     <para xml:lang="en">Wire format version, which must be within the supported range.</para>
        ///     <para xml:lang="zh-CN">必须位于支持范围内的线格式版本。</para>
        /// </param>
        /// <param name="flags">
        ///     <para xml:lang="en">Wire flags with compression bits replaced by <paramref name="compression" />.</para>
        ///     <para xml:lang="zh-CN">其压缩位会由 <paramref name="compression" /> 替换的线格式标志。</para>
        /// </param>
        /// <param name="opcode">
        ///     <para xml:lang="en">64-bit Sidecar opcode.</para>
        ///     <para xml:lang="zh-CN">64 位 Sidecar 操作码。</para>
        /// </param>
        /// <param name="headerExtension">
        ///     <para xml:lang="en">Optional bytes after the fixed header and before the payload.</para>
        ///     <para xml:lang="zh-CN">固定头部之后、载荷之前的可选字节。</para>
        /// </param>
        /// <param name="payloadLogical">
        ///     <para xml:lang="en">Uncompressed logical payload.</para>
        ///     <para xml:lang="zh-CN">未压缩的逻辑载荷。</para>
        /// </param>
        /// <param name="compression">
        ///     <para xml:lang="en">Payload compression mode.</para>
        ///     <para xml:lang="zh-CN">载荷压缩模式。</para>
        /// </param>
        public static byte[] Build(
            ushort wireFormatVersion,
            RitsuLibSidecarWireFlags flags,
            ulong opcode,
            ReadOnlySpan<byte> headerExtension,
            ReadOnlySpan<byte> payloadLogical,
            RitsuLibSidecarPayloadCompression compression)
        {
            if (wireFormatVersion is 0 or > RitsuLibSidecarWire.SupportedWireFormatVersionMax)
                throw new ArgumentOutOfRangeException(nameof(wireFormatVersion));

            if (headerExtension.Length > RitsuLibSidecarWire.MaxHeaderExtensionBytes)
                throw new ArgumentOutOfRangeException(nameof(headerExtension));

            if (payloadLogical.Length > RitsuLibSidecarWire.MaxPayloadBytes)
                throw new ArgumentOutOfRangeException(nameof(payloadLogical));

            var wirePayload = payloadLogical;
            flags &= ~PayloadCompressionFlags;
            switch (compression)
            {
                case RitsuLibSidecarPayloadCompression.None:
                    break;
                case RitsuLibSidecarPayloadCompression.Gzip:
                    wirePayload = RitsuLibSidecarCompression.GzipCompress(payloadLogical);
                    flags |= RitsuLibSidecarWireFlags.PayloadGzip;
                    break;
                case RitsuLibSidecarPayloadCompression.Brotli:
                    wirePayload = RitsuLibSidecarCompression.BrotliCompress(payloadLogical);
                    flags |= RitsuLibSidecarWireFlags.PayloadBrotli;
                    break;
                case RitsuLibSidecarPayloadCompression.Auto:
                    if (RitsuLibSidecarCompression.TryBrotliAutoCompress(payloadLogical, out var compressed))
                    {
                        wirePayload = compressed;
                        flags |= RitsuLibSidecarWireFlags.PayloadBrotli;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(compression));
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
        ///     <para xml:lang="en">Contains decoded header fields and the logical payload.</para>
        ///     <para xml:lang="zh-CN">包含已解码的头部字段和逻辑载荷。</para>
        /// </summary>
        public readonly struct ParsedEnvelope
        {
            /// <summary>
            ///     <para xml:lang="en">Creates a parsed-envelope value.</para>
            ///     <para xml:lang="zh-CN">创建已解析的信封值。</para>
            /// </summary>
            /// <param name="wireFormatVersion">
            ///     <para xml:lang="en">Wire format version from the packet.</para>
            ///     <para xml:lang="zh-CN">数据包中的线格式版本。</para>
            /// </param>
            /// <param name="flags">
            ///     <para xml:lang="en">Decoded wire flags.</para>
            ///     <para xml:lang="zh-CN">已解码的线格式标志。</para>
            /// </param>
            /// <param name="opcode">
            ///     <para xml:lang="en">64-bit opcode from the packet.</para>
            ///     <para xml:lang="zh-CN">数据包中的 64 位操作码。</para>
            /// </param>
            /// <param name="headerExtension">
            ///     <para xml:lang="en">Optional header-extension segment.</para>
            ///     <para xml:lang="zh-CN">可选头扩展段。</para>
            /// </param>
            /// <param name="payload">
            ///     <para xml:lang="en">Logical payload, decompressed when required by the flags.</para>
            ///     <para xml:lang="zh-CN">逻辑载荷；必要时会按标志解压。</para>
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
            ///     <para xml:lang="en">Wire format version from the packet.</para>
            ///     <para xml:lang="zh-CN">数据包中的线格式版本。</para>
            /// </summary>
            public ushort WireFormatVersion { get; }

            /// <summary>
            ///     <para xml:lang="en">Decoded flags with unknown bits cleared.</para>
            ///     <para xml:lang="zh-CN">未知位已清除的已解码标志。</para>
            /// </summary>
            public RitsuLibSidecarWireFlags Flags { get; }

            /// <summary>
            ///     <para xml:lang="en">64-bit opcode from <see cref="RitsuLibSidecarOpcodes.For" /> or a framework constant.</para>
            ///     <para xml:lang="zh-CN">来自 <see cref="RitsuLibSidecarOpcodes.For" /> 或框架常量的 64 位操作码。</para>
            /// </summary>
            public ulong Opcode { get; }

            /// <summary>
            ///     <para xml:lang="en">Opaque header extension; version-1 senders typically use an empty extension.</para>
            ///     <para xml:lang="zh-CN">不透明头扩展；版本 1 发送方通常使用空扩展。</para>
            /// </summary>
            public ReadOnlyMemory<byte> HeaderExtension { get; }

            /// <summary>
            ///     <para xml:lang="en">Logical payload after any compression indicated by the flags is removed.</para>
            ///     <para xml:lang="zh-CN">移除标志指示的任何压缩后的逻辑载荷。</para>
            /// </summary>
            public ReadOnlyMemory<byte> Payload { get; }
        }
    }
}
