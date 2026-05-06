using System.Buffers.Binary;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Parses and builds sidecar envelopes: fixed magic, wire version, flags, 64-bit opcode, optional header
    ///     extension, then payload.
    /// </summary>
    public static class RitsuLibSidecarEnvelope
    {
        /// <summary>
        ///     Result of parsing an on-wire envelope.
        /// </summary>
        public enum ParseOutcome
        {
            /// <summary>
            ///     Parse succeeded.
            /// </summary>
            Ok,

            /// <summary>
            ///     Packet shorter than the minimum header.
            /// </summary>
            TooSmall,

            /// <summary>
            ///     Magic mismatch.
            /// </summary>
            BadMagic,

            /// <summary>
            ///     Wire format version is zero or greater than <see cref="RitsuLibSidecarWire.SupportedWireFormatVersionMax" />.
            /// </summary>
            WireVersionUnsupported,

            /// <summary>
            ///     Declared payload length invalid, gzip corrupt, or decompressed size over cap.
            /// </summary>
            PayloadLengthInvalid,

            /// <summary>
            ///     Header extension length over cap.
            /// </summary>
            ExtensionLengthInvalid,

            /// <summary>
            ///     Total packet length does not match header fields.
            /// </summary>
            TotalLengthMismatch,
        }

        private const uint KnownWireFlagsMask = (uint)RitsuLibSidecarWireFlags.PayloadGzip;

        /// <summary>
        ///     Parses an envelope from a byte array backing store.
        /// </summary>
        /// <param name="packet">Full on-wire bytes; slice views reference this array.</param>
        /// <param name="parsed">Populated when the return value is <see cref="ParseOutcome.Ok" />.</param>
        public static ParseOutcome TryParse(byte[] packet, out ParsedEnvelope parsed)
        {
            return TryParse(packet.AsSpan(), packet, out parsed);
        }

        /// <summary>
        ///     Parses an envelope; <paramref name="backing" /> must be the same array as <paramref name="packet" /> spans.
        /// </summary>
        /// <param name="packet">Full on-wire bytes as a span over <paramref name="backing" />.</param>
        /// <param name="backing">Array used to construct <see cref="ReadOnlyMemory{T}" /> for extension and payload.</param>
        /// <param name="parsed">Populated when the return value is <see cref="ParseOutcome.Ok" />.</param>
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
        /// </summary>
        /// <param name="wireFormatVersion">Wire format version field; must be within the supported range.</param>
        /// <param name="flags">Wire flags; gzip may be set when <paramref name="gzipLogicalPayload" /> is <c>true</c>.</param>
        /// <param name="opcode">64-bit sidecar opcode.</param>
        /// <param name="headerExtension">Optional bytes after the fixed header, before the payload.</param>
        /// <param name="payloadLogical">
        ///     Uncompressed logical payload; may be compressed when
        ///     <paramref name="gzipLogicalPayload" /> is <c>true</c>.
        /// </param>
        /// <param name="gzipLogicalPayload">
        ///     When <c>true</c>, compresses the payload and ORs in
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
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
        /// </summary>
        public readonly struct ParsedEnvelope
        {
            /// <summary>
            ///     Creates a parsed envelope value.
            /// </summary>
            /// <param name="wireFormatVersion">Wire version from the packet.</param>
            /// <param name="flags">Decoded wire flags.</param>
            /// <param name="opcode">64-bit opcode from the packet.</param>
            /// <param name="headerExtension">Optional extension segment.</param>
            /// <param name="payload">Logical payload (decompressed if gzip was set).</param>
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
            /// </summary>
            public ushort WireFormatVersion { get; }

            /// <summary>
            ///     Decoded flags (unknown bits cleared).
            /// </summary>
            public RitsuLibSidecarWireFlags Flags { get; }

            /// <summary>
            ///     64-bit opcode (from <see cref="RitsuLibSidecarOpcodes.For" /> or a framework constant).
            /// </summary>
            public ulong Opcode { get; }

            /// <summary>
            ///     Opaque header extension; v1 senders typically use length 0.
            /// </summary>
            public ReadOnlyMemory<byte> HeaderExtension { get; }

            /// <summary>
            ///     Logical payload (after optional gzip decompression).
            /// </summary>
            public ReadOnlyMemory<byte> Payload { get; }
        }
    }
}
