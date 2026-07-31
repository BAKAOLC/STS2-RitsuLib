namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the delivery tag at the start of an envelope header extension created by
    ///         <see cref="RitsuLibSidecarHighLevelSend" /> or
    ///         <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />. Additional extension bytes follow the tag.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义由 <see cref="RitsuLibSidecarHighLevelSend" /> 或
    ///         <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" /> 创建的信封标头扩展起始处的投递标签。
    ///         其他扩展字节位于该标签之后。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarHeaderExtension
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The minimum header-extension length when explicit delivery semantics are present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         包含显式投递语义时标头扩展的最小长度。
        ///     </para>
        /// </summary>
        public const int MinBytesWithDelivery = RitsuLibSidecarBinaryLayout.ByteSize;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reads the delivery tag, or returns <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> when
        ///         the extension is empty.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         读取投递标签；扩展为空时返回
        ///         <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />。
        ///     </para>
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
