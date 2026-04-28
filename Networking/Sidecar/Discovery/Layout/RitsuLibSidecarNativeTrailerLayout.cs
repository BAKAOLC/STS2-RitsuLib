namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarNativeTrailerLayout
    {
        internal const int SignatureSize = RitsuLibSidecarWire.MagicLength;
        internal const int VersionSize = RitsuLibSidecarBinaryLayout.U16Size;
        internal const int FlagsSize = RitsuLibSidecarBinaryLayout.U16Size;
        internal const int PayloadLengthSize = RitsuLibSidecarBinaryLayout.U32Size;
        internal const int PayloadCrc32Size = RitsuLibSidecarBinaryLayout.U32Size;
        internal const int FooterSignatureSize = SignatureSize;

        internal const int HeaderSignatureOffset = 0;
        internal const int VersionOffset = HeaderSignatureOffset + SignatureSize;
        internal const int FlagsOffset = VersionOffset + VersionSize;
        internal const int PayloadLengthOffset = FlagsOffset + FlagsSize;
        internal const int PayloadCrc32Offset = PayloadLengthOffset + PayloadLengthSize;
        internal const int FooterSignatureOffset = PayloadCrc32Offset + PayloadCrc32Size;
        internal const int TrailerSize = FooterSignatureOffset + FooterSignatureSize;

        internal const ushort Version = 1;
        internal const ushort FlagSidecarSupported = 1 << 0;

        internal static ReadOnlySpan<byte> HeaderSignature => "STS2RitsuLib"u8;
        internal static ReadOnlySpan<byte> FooterSignature => "biLustiR2STS"u8;
    }
}
