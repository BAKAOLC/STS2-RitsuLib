namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarNativeTrailerLayout
    {
        internal const int VersionSize = RitsuLibSidecarBinaryLayout.U16Size;
        internal const int FlagsSize = RitsuLibSidecarBinaryLayout.U16Size;
        internal const int PayloadLengthSize = RitsuLibSidecarBinaryLayout.U32Size;
        internal const int PayloadCrc32Size = RitsuLibSidecarBinaryLayout.U32Size;

        internal const int HeaderSignatureOffset = 0;

        internal const ushort Version = 1;
        internal const ushort FlagSidecarSupported = 1 << 0;
        internal static int SignatureSize => RitsuLibSidecarWire.MagicLength;
        internal static int FooterSignatureSize => SignatureSize;
        internal static int VersionOffset => HeaderSignatureOffset + SignatureSize;
        internal static int FlagsOffset => VersionOffset + VersionSize;
        internal static int PayloadLengthOffset => FlagsOffset + FlagsSize;
        internal static int PayloadCrc32Offset => PayloadLengthOffset + PayloadLengthSize;
        internal static int FooterSignatureOffset => PayloadCrc32Offset + PayloadCrc32Size;
        internal static int TrailerSize => FooterSignatureOffset + FooterSignatureSize;

        internal static ReadOnlySpan<byte> HeaderSignature => "STS2RitsuLib"u8;
        internal static ReadOnlySpan<byte> FooterSignature => "biLustiR2STS"u8;
    }
}
