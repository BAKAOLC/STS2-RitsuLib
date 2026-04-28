namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar diagnostics settings that are not related to log-level gating.
    /// </summary>
    public static class RitsuLibSidecarNetDiagnosticsOptions
    {
        /// <summary>
        ///     Incomplete chunked streams older than this span are discarded server-side (receiver); defaults to two
        ///     minutes.
        /// </summary>
        public static TimeSpan IncompleteChunkStreamRetention { get; set; } =
            RitsuLibSidecarChunkReassembly.IncompleteStreamRetentionDefault;
    }
}
