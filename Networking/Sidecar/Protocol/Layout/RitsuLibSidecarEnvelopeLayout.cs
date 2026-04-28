namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarEnvelopeLayout
    {
        internal const int WireVersionSize = RitsuLibSidecarBinaryLayout.U16Size;
        internal const int FlagsSize = RitsuLibSidecarBinaryLayout.U32Size;
        internal const int OpcodeSize = RitsuLibSidecarBinaryLayout.U64Size;
        internal const int PayloadLengthSize = RitsuLibSidecarBinaryLayout.U32Size;
        internal const int ExtensionLengthSize = RitsuLibSidecarBinaryLayout.U32Size;

        internal const int MagicOffset = 0;
        internal const int WireVersionOffset = RitsuLibSidecarWire.MagicLength;
        internal const int FlagsOffset = WireVersionOffset + WireVersionSize;
        internal const int OpcodeOffset = FlagsOffset + FlagsSize;
        internal const int PayloadLengthOffset = OpcodeOffset + OpcodeSize;
        internal const int ExtensionLengthOffset = PayloadLengthOffset + PayloadLengthSize;
        internal const int FixedHeaderSize = ExtensionLengthOffset + ExtensionLengthSize;

        internal const int DeliveryTagSize = RitsuLibSidecarBinaryLayout.ByteSize;
        internal const int DeliveryTagOffsetInExtension = 0;
        internal const int CorrelationOffsetInExtension = DeliveryTagSize;
    }
}
