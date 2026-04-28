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

        internal const int DeliveryTagSize = RitsuLibSidecarBinaryLayout.ByteSize;
        internal const int DeliveryTagOffsetInExtension = 0;
        internal const int CorrelationOffsetInExtension = DeliveryTagSize;
        internal static int WireVersionOffset => RitsuLibSidecarWire.MagicLength;
        internal static int FlagsOffset => WireVersionOffset + WireVersionSize;
        internal static int OpcodeOffset => FlagsOffset + FlagsSize;
        internal static int PayloadLengthOffset => OpcodeOffset + OpcodeSize;
        internal static int ExtensionLengthOffset => PayloadLengthOffset + PayloadLengthSize;
        internal static int FixedHeaderSize => ExtensionLengthOffset + ExtensionLengthSize;
    }
}
