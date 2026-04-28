using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Toggles sidecar diagnostics aligned with vanilla Network logging verbosity. For aggregate byte and packet
    ///     counts see <see cref="RitsuLibSidecarTrafficCounters" />.
    /// </summary>
    public static class RitsuLibSidecarNetDiagnosticsOptions
    {
        /// <summary>
        ///     When true, each successfully parsed inbound sidecar envelope logs one <c>Info</c> line on the sidecar
        ///     trace logger from <see cref="RitsuLibFramework.CreateLogger(string, LogType)" /> (visible in default
        ///     <c>godot.log</c> alongside other Network lines).
        /// </summary>
        public static bool TraceIncomingPackets { get; set; } = true;

        /// <summary>
        ///     When true, each successful <see cref="RitsuLibSidecarSend" /> logs one <c>Info</c> line on the same
        ///     network logger.
        /// </summary>
        public static bool TraceOutgoingPackets { get; set; } = true;

        /// <summary>
        ///     When true, host/client sidecar send paths log why a packet is skipped for a peer that is not
        ///     <c>Supported</c> yet.
        /// </summary>
        public static bool TraceSkippedSends { get; set; } = true;

        /// <summary>
        ///     When true, session lifecycle and reachability transitions emit diagnostic lines (bind/unbind, provider
        ///     verdict, handshake gating decisions).
        /// </summary>
        public static bool TraceSessionState { get; set; } = true;

        /// <summary>
        ///     Incomplete chunked streams older than this span are discarded server-side (receiver); defaults to two
        ///     minutes.
        /// </summary>
        public static TimeSpan IncompleteChunkStreamRetention { get; set; } =
            RitsuLibSidecarChunkReassembly.IncompleteStreamRetentionDefault;
    }
}
