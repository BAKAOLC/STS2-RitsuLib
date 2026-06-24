namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarHandshakeLayout
    {
        internal const int WireFormatVersionOffset = 0;

        internal const int SupportedWireFormatVersionMaxOffset =
            WireFormatVersionOffset + RitsuLibSidecarBinaryLayout.U16Size;

        internal const int FeaturesOffset = SupportedWireFormatVersionMaxOffset + RitsuLibSidecarBinaryLayout.U16Size;
        internal const int HandshakePayloadSize = FeaturesOffset + RitsuLibSidecarBinaryLayout.U32Size;

        internal const int AckSelectedWireFormatVersionOffset = 0;
        internal const int AckOkOffset = AckSelectedWireFormatVersionOffset + RitsuLibSidecarBinaryLayout.U16Size;
        internal const int AckSenderFeaturesOffset = AckOkOffset + RitsuLibSidecarBinaryLayout.ByteSize;
        internal const int AckPayloadSize = AckSenderFeaturesOffset + RitsuLibSidecarBinaryLayout.U32Size;
    }
}
