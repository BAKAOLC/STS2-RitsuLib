namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides resource limits for built-in sidecar buffering and chunk reassembly.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供内置 sidecar 缓冲和分块重组所使用的资源限制。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarResourcePolicy
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum number of sidecar synchronization messages retained while waiting for vanilla message
        ///         or location buffers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         等待原版消息缓冲区或位置缓冲区时，最多保留的 sidecar 同步消息数。
        ///     </para>
        /// </summary>
        public static int MaxBufferedSyncContexts => 256;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum total logical payload size retained by sidecar synchronization buffers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         sidecar 同步缓冲区最多保留的逻辑载荷总字节数。
        ///     </para>
        /// </summary>
        public static long MaxBufferedSyncBytes => 8 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum number of incomplete chunk streams retained across all senders.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         所有发送方合计最多保留的未完成分块流数。
        ///     </para>
        /// </summary>
        public static int MaxChunkReassemblyStreamsGlobal => 64;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum number of incomplete chunk streams retained for one sender.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         单个发送方最多保留的未完成分块流数。
        ///     </para>
        /// </summary>
        public static int MaxChunkReassemblyStreamsPerSender => 16;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum number of segments accepted for one chunk stream.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         单个分块流最多接受的分段数。
        ///     </para>
        /// </summary>
        public static int MaxChunkReassemblyPartCount => 1024;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum total logical payload size reserved by incomplete chunk streams across all senders.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         所有发送方的未完成分块流最多预留的逻辑载荷总字节数。
        ///     </para>
        /// </summary>
        public static long MaxChunkReassemblyLogicalBytesGlobal => 16 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     <para xml:lang="en">
        ///         The maximum total logical payload size reserved by incomplete chunk streams for one sender.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         单个发送方的未完成分块流最多预留的逻辑载荷总字节数。
        ///     </para>
        /// </summary>
        public static long MaxChunkReassemblyLogicalBytesPerSender => 8 * RitsuLibSidecarBinaryLayout.MiB;
    }
}
