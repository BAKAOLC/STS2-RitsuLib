namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Runtime resource limits used by built-in sidecar buffering and chunk reassembly.
    ///     内置 sidecar 缓冲和分块重组使用的运行时资源限制。
    /// </summary>
    public static class RitsuLibSidecarResourcePolicy
    {
        /// <summary>
        ///     Maximum number of sidecar sync messages retained while waiting for vanilla message or location buffers.
        ///     等待原版消息缓冲或位置缓冲时，最多保留的 sidecar sync 消息数量。
        /// </summary>
        public static int MaxBufferedSyncContexts => 256;

        /// <summary>
        ///     Maximum total logical payload bytes retained by sidecar sync buffers.
        ///     sidecar sync 缓冲最多保留的逻辑载荷总字节数。
        /// </summary>
        public static long MaxBufferedSyncBytes => 8 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     Maximum number of incomplete chunk streams retained across all senders.
        ///     所有发送方合计最多保留的未完成分块流数量。
        /// </summary>
        public static int MaxChunkReassemblyStreamsGlobal => 64;

        /// <summary>
        ///     Maximum number of incomplete chunk streams retained for one sender.
        ///     单个发送方最多保留的未完成分块流数量。
        /// </summary>
        public static int MaxChunkReassemblyStreamsPerSender => 16;

        /// <summary>
        ///     Maximum number of parts accepted for one chunk stream.
        ///     单个分块流最多接受的分片数量。
        /// </summary>
        public static int MaxChunkReassemblyPartCount => 1024;

        /// <summary>
        ///     Maximum total logical bytes reserved by incomplete chunk streams across all senders.
        ///     所有发送方的未完成分块流最多预留的逻辑字节总数。
        /// </summary>
        public static long MaxChunkReassemblyLogicalBytesGlobal => 16 * RitsuLibSidecarBinaryLayout.MiB;

        /// <summary>
        ///     Maximum total logical bytes reserved by incomplete chunk streams for one sender.
        ///     单个发送方的未完成分块流最多预留的逻辑字节总数。
        /// </summary>
        public static long MaxChunkReassemblyLogicalBytesPerSender => 8 * RitsuLibSidecarBinaryLayout.MiB;
    }
}
