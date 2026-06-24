namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticRelayLayout
    {
        internal const int OriginatingSenderNetIdOffset = 0;
        internal const int OriginatingSenderNetIdSize = RitsuLibSidecarBinaryLayout.U64Size;

        internal const int TagOffset = OriginatingSenderNetIdOffset + OriginatingSenderNetIdSize;
        internal const int TagSize = RitsuLibSidecarBinaryLayout.U16Size;

        internal const int ChecksumIdOffset = TagOffset + TagSize;
        internal const int ChecksumIdSize = RitsuLibSidecarBinaryLayout.U32Size;

        internal const int NonceOffset = ChecksumIdOffset + ChecksumIdSize;
        internal const int NonceSize = RitsuLibSidecarBinaryLayout.U64Size;

        internal const int IssuedUnixMillisecondsOffset = NonceOffset + NonceSize;
        internal const int IssuedUnixMillisecondsSize = sizeof(long);

        internal const int FanoutPayloadSize = IssuedUnixMillisecondsOffset + IssuedUnixMillisecondsSize;
    }
}
