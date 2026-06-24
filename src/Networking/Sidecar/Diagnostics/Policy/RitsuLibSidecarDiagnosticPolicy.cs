namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarDiagnosticPolicy
    {
        public const ushort DivergenceRelayTag = 1;

        public const int MaxRelaySessionsPerPeerPerWindow = 2;
        public const int MaxRelaySessionsGlobalPerWindow = 8;
        public const int MaxLocalDumpsGlobalPerWindow = 8;

        public static readonly TimeSpan RelaySessionTtl = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan RelayRateWindow = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan LocalDumpRateWindow = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan FanoutFutureTolerance = TimeSpan.FromSeconds(5);
    }
}
