namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Layout of the first byte in <see cref="RitsuLibSidecarEnvelope" /> <c>headerExtension</c> when
    ///     <see cref="RitsuLibSidecarHighLevelSend" /> / <see cref="RitsuLibSidecar.CreateEnvelopeWithDelivery" />
    ///     (delivery-aware helpers) are used. Additional extension bytes, if any, follow this byte in the same buffer.
    /// </summary>
    public static class RitsuLibSidecarHeaderExtension
    {
        /// <summary>Minimum <see cref="RitsuLibSidecarEnvelope.ParsedEnvelope.HeaderExtension" /> when delivery is explicit.</summary>
        public const int MinBytesWithDelivery = 1;

        /// <summary>Reads the delivery field; if length is 0, returns <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" />.</summary>
        public static RitsuLibSidecarDeliverySemantics GetDeliveryOrUnspecified(ReadOnlyMemory<byte> extension)
        {
            return extension.Length == 0
                ? RitsuLibSidecarDeliverySemantics.Unspecified
                : (RitsuLibSidecarDeliverySemantics)extension.Span[0];
        }
    }
}
