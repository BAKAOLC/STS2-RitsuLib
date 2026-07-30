namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Sidecar diagnostic settings unrelated to log-level filtering.</para>
    ///     <para xml:lang="zh-CN">与日志级别过滤无关的 Sidecar 诊断设置。</para>
    /// </summary>
    public static class RitsuLibSidecarNetDiagnosticsOptions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Incomplete chunked streams older than this duration are discarded by the receiver. The default is
        ///         two minutes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收方会丢弃存在时间超过此时长的未完成分块流。默认值为两分钟。
        ///     </para>
        /// </summary>
        public static TimeSpan IncompleteChunkStreamRetention { get; set; } =
            RitsuLibSidecarChunkReassembly.IncompleteStreamRetentionDefault;
    }
}
