namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds Sidecar envelopes for the current wire layout. Opcodes use <see cref="RitsuLibSidecarOpcodes.For" />
    ///         or <see cref="RitsuLibSidecarControlOpcodes" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为当前线路格式构建 Sidecar 信封。操作码由 <see cref="RitsuLibSidecarOpcodes.For" /> 生成，
    ///         或使用 <see cref="RitsuLibSidecarControlOpcodes" /> 中的控制操作码。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecar
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds an envelope. <paramref name="headerExtension" /> is opaque; to record delivery semantics use
        ///         <see cref="CreateEnvelopeWithDelivery" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建信封。<paramref name="headerExtension" /> 是不透明数据；如需记录投递语义，请使用
        ///         <see cref="CreateEnvelopeWithDelivery" />。
        ///     </para>
        /// </summary>
        /// <param name="opcode">
        ///     <para xml:lang="en">Sidecar opcode (user or control).</para>
        ///     <para xml:lang="zh-CN">Sidecar 用户或控制操作码。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">Logical payload after the fixed header and optional extension.</para>
        ///     <para xml:lang="zh-CN">固定标头和可选扩展之后的逻辑载荷。</para>
        /// </param>
        /// <param name="extraFlags">
        ///     <para xml:lang="en">Wire flags combined into the envelope using bitwise OR, such as gzip.</para>
        ///     <para xml:lang="zh-CN">以按位或合并到信封中的线路标志，例如 gzip。</para>
        /// </param>
        /// <param name="gzipPayload">
        ///     <para xml:lang="en">
        ///         When <see langword="true" />, compresses <paramref name="payload" /> and sets
        ///         <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，压缩 <paramref name="payload" /> 并设置
        ///         <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。
        ///     </para>
        /// </param>
        /// <param name="headerExtension">
        ///     <para xml:lang="en">Optional bytes between the fixed header and payload.</para>
        ///     <para xml:lang="zh-CN">固定标头与载荷之间的可选字节。</para>
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
        ///     <para xml:lang="en">Builds an envelope with an explicit payload compression mode.</para>
        ///     <para xml:lang="zh-CN">使用显式载荷压缩模式构建信封。</para>
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
        ///     <para xml:lang="en">
        ///         Builds an envelope with a 1-byte delivery tag plus optional <paramref name="additionalHeaderExtension" />.
        ///         <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> omits the tag; extension is only
        ///         <paramref name="additionalHeaderExtension" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建包含单字节投递标签和可选 <paramref name="additionalHeaderExtension" /> 的信封。
        ///         使用 <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 时省略标签，扩展内容仅为
        ///         <paramref name="additionalHeaderExtension" />。
        ///     </para>
        /// </summary>
        /// <param name="opcode">
        ///     <para xml:lang="en">Sidecar opcode (user or control).</para>
        ///     <para xml:lang="zh-CN">Sidecar 用户或控制操作码。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">Logical payload after the fixed header and optional extension.</para>
        ///     <para xml:lang="zh-CN">固定标头和可选扩展之后的逻辑载荷。</para>
        /// </param>
        /// <param name="delivery">
        ///     <para xml:lang="en">
        ///         First byte of the header extension when not
        ///         <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不为
        ///         <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 时，标头扩展的第一个字节。
        ///     </para>
        /// </param>
        /// <param name="extraFlags">
        ///     <para xml:lang="en">Wire flags combined into the envelope using bitwise OR, such as gzip.</para>
        ///     <para xml:lang="zh-CN">以按位或合并到信封中的线路标志，例如 gzip。</para>
        /// </param>
        /// <param name="gzipPayload">
        ///     <para xml:lang="en">
        ///         When <see langword="true" />, compresses <paramref name="payload" /> and sets
        ///         <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <see langword="true" /> 时，压缩 <paramref name="payload" /> 并设置
        ///         <see cref="RitsuLibSidecarWireFlags.PayloadGzip" />。
        ///     </para>
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     <para xml:lang="en">Bytes after the 1-byte delivery tag in the extension.</para>
        ///     <para xml:lang="zh-CN">扩展中 1 字节投递标签之后的字节。</para>
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
        ///     <para xml:lang="en">Builds an envelope with delivery metadata and an explicit payload compression mode.</para>
        ///     <para xml:lang="zh-CN">使用投递元数据和显式载荷压缩模式构建信封。</para>
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
