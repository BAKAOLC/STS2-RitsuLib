namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Toggles sidecar diagnostics aligned with vanilla Network logging verbosity.</summary>
    public static class RitsuLibSidecarNetDiagnosticsOptions
    {
        /// <summary>
        ///     When true, each parsed inbound envelope logs one Network-category line (similar to vanilla
        ///     <c>NetMessageBus</c> handler traces).
        /// </summary>
        public static bool TraceIncomingPackets { get; set; } = true;

        /// <summary>
        ///     Incomplete chunked streams older than this span are discarded server-side (receiver); defaults to two
        ///     minutes.
        /// </summary>
        public static TimeSpan IncompleteChunkStreamRetention { get; set; } =
            RitsuLibSidecarChunkReassembly.IncompleteStreamRetentionDefault;
    }
}
