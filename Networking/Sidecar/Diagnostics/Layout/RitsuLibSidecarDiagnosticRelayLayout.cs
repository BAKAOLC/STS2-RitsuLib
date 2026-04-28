namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticRelayLayout
    {
        internal const int OriginatingSenderNetIdOffset = 0;
        internal const int OriginatingSenderNetIdSize = RitsuLibSidecarBinaryLayout.U64Size;

        internal const int TagOffset = OriginatingSenderNetIdOffset + OriginatingSenderNetIdSize;
        internal const int TagSize = RitsuLibSidecarBinaryLayout.U16Size;

        internal const int FanoutPayloadSize = TagOffset + TagSize;
    }
}
