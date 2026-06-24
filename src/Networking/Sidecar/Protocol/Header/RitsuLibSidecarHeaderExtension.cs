namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Layout of the first byte in <see cref="RitsuLibSidecarEnvelope" /> <c>headerExtension</c> when
    ///     <see cref="RitsuLibSidecarHighLevelSend" /> / <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />
    ///     (delivery-aware helpers) are used. Additional extension bytes, if any, follow this byte in the same buffer.
    ///     使用投递感知辅助方法时，<see cref="RitsuLibSidecarEnvelope" /> <c>headerExtension</c> 中第一个字节的布局：
    ///     <see cref="RitsuLibSidecarHighLevelSend" /> / <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />
    ///     。若存在额外扩展字节，它们跟随此字节位于同一缓冲区中。
    /// </summary>
    public static class RitsuLibSidecarHeaderExtension
    {
        /// <summary>
        ///     Minimum <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> when delivery is explicit.
        ///     显式投递时 <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> 的最小长度。
        /// </summary>
        public const int MinBytesWithDelivery = RitsuLibSidecarBinaryLayout.ByteSize;

        /// <summary>
        ///     Reads the delivery field; if length is 0, returns <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />.
        ///     读取投递字段；如果长度为 0，则返回 <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />。
        /// </summary>
        public static RitsuLibSidecarDeliverySemantics GetDeliveryOrUnspecified(ReadOnlyMemory<byte> extension)
        {
            return extension.Length == 0
                ? RitsuLibSidecarDeliverySemantics.Unspecified
                : (RitsuLibSidecarDeliverySemantics)extension.Span[
                    RitsuLibSidecarEnvelopeLayout.DeliveryTagOffsetInExtension];
        }
    }
}
