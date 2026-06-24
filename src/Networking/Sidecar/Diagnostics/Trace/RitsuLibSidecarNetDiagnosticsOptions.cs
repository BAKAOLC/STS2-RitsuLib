namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar diagnostics settings that are not related to log-level gating.
    ///     与日志级别 gating 无关的 sidecar 诊断设置。
    /// </summary>
    public static class RitsuLibSidecarNetDiagnosticsOptions
    {
        /// <summary>
        ///     Incomplete chunked streams older than this span are discarded server-side (receiver); defaults to two
        ///     minutes.
        ///     服务端（接收方）会丢弃早于此时间跨度的未完成分块流；默认值为两
        ///     分钟。
        /// </summary>
        public static TimeSpan IncompleteChunkStreamRetention { get; set; } =
            RitsuLibSidecarChunkReassembly.IncompleteStreamRetentionDefault;
    }
}
