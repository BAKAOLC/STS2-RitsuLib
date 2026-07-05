namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Builds sidecar envelopes for the current wire layout. Opcodes use <see cref="RitsuLibSidecarOpcodes.For" />
    ///     or <see cref="RitsuLibSidecarControlOpcodes" />.
    ///     为当前线格式构建 sidecar envelope。opcode 使用 <see cref="RitsuLibSidecarOpcodes.For" />
    ///     或 <see cref="RitsuLibSidecarControlOpcodes" />。
    /// </summary>
    public static class RitsuLibSidecar
    {
        /// <summary>
        ///     Builds an envelope. <paramref name="headerExtension" /> is opaque; to record delivery semantics use
        ///     <see cref="CreateEnvelopeWithDelivery" />.
        ///     构建 envelope。<paramref name="headerExtension" /> 是不透明数据；如需记录投递语义，请使用
        ///     <see cref="CreateEnvelopeWithDelivery" />。
        /// </summary>
        /// <param name="opcode">
        ///     Sidecar opcode (user or control).
        ///     Sidecar opcode（用户或控制）。
        /// </param>
        /// <param name="payload">
        ///     Logical payload after the fixed header and optional extension.
        ///     固定 header 和可选扩展之后的逻辑载荷。
        /// </param>
        /// <param name="extraFlags">
        ///     Wire flags OR’d into the envelope (e.g. gzip).
        ///     按 OR 写入 envelope 的线标志（例如 gzip）。
        /// </param>
        /// <param name="gzipPayload">
        ///     When <c>true</c>, compresses <paramref name="payload" /> and sets
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     为 <c>true</c> 时，压缩 <paramref name="payload" /> 并设置
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。
        /// </param>
        /// <param name="headerExtension">
        ///     Optional bytes between the fixed header and payload.
        ///     固定 header 与载荷之间的可选字节。
        /// </param>
        public static byte[] CreateEnvelope(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzipPayload = false,
            ReadOnlySpan<byte> headerExtension = default)
        {
            return RitsuLibSidecarEnvelope.Build(
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                extraFlags,
                opcode,
                headerExtension,
                payload,
                gzipPayload);
        }

        /// <summary>
        ///     Builds an envelope with an explicit payload compression mode.
        ///     使用显式 payload 压缩模式构建 envelope。
        /// </summary>
        public static byte[] CreateEnvelopeCompressed(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarPayloadCompression compression,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            ReadOnlySpan<byte> headerExtension = default)
        {
            return RitsuLibSidecarEnvelope.Build(
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                extraFlags,
                opcode,
                headerExtension,
                payload,
                compression);
        }

        /// <summary>
        ///     Builds an envelope with a 1-byte delivery tag plus optional <paramref name="additionalHeaderExtension" />.
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> omits the tag; extension is only
        ///     <paramref name="additionalHeaderExtension" />.
        ///     构建带 1 字节投递标签和可选 <paramref name="additionalHeaderExtension" /> 的 envelope。
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 会省略该标签；扩展仅为
        ///     <paramref name="additionalHeaderExtension" />。
        /// </summary>
        /// <param name="opcode">
        ///     Sidecar opcode (user or control).
        ///     Sidecar opcode（用户或控制）。
        /// </param>
        /// <param name="payload">
        ///     Logical payload after the fixed header and optional extension.
        ///     固定 header 和可选扩展之后的逻辑载荷。
        /// </param>
        /// <param name="delivery">
        ///     First byte of the header extension when not
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />.
        ///     不为
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 时，header 扩展的第一个字节。
        /// </param>
        /// <param name="extraFlags">
        ///     Wire flags OR’d into the envelope (e.g. gzip).
        ///     按 OR 写入 envelope 的线标志（例如 gzip）。
        /// </param>
        /// <param name="gzipPayload">
        ///     When <c>true</c>, compresses <paramref name="payload" /> and sets
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     为 <c>true</c> 时，压缩 <paramref name="payload" /> 并设置
        ///     <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     Bytes after the 1-byte delivery tag in the extension.
        ///     扩展中 1 字节投递标签之后的字节。
        /// </param>
        public static byte[] CreateEnvelopeWithDelivery(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics delivery,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzipPayload = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            if (delivery is RitsuLibSidecarDeliverySemantics.Unspecified)
                return CreateEnvelope(opcode, payload, extraFlags, gzipPayload, additionalHeaderExtension);

            var ext = new byte[1 + additionalHeaderExtension.Length];
            ext[0] = (byte)delivery;
            additionalHeaderExtension.CopyTo(ext.AsSpan(1));
            return CreateEnvelope(opcode, payload, extraFlags, gzipPayload, ext);
        }

        /// <summary>
        ///     Builds an envelope with delivery metadata and an explicit payload compression mode.
        ///     使用投递元数据和显式 payload 压缩模式构建 envelope。
        /// </summary>
        public static byte[] CreateEnvelopeWithDeliveryCompressed(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics delivery,
            RitsuLibSidecarPayloadCompression compression,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            if (delivery is RitsuLibSidecarDeliverySemantics.Unspecified)
                return CreateEnvelopeCompressed(opcode, payload, compression, extraFlags, additionalHeaderExtension);

            var ext = new byte[1 + additionalHeaderExtension.Length];
            ext[0] = (byte)delivery;
            additionalHeaderExtension.CopyTo(ext.AsSpan(1));
            return CreateEnvelopeCompressed(opcode, payload, compression, extraFlags, ext);
        }
    }
}
