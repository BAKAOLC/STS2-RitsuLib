namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Exposes optional receive-progress notifications for chunked Sidecar transfers.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         公开 Sidecar 分块传输的可选接收进度通知。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarChunkTransferNotifications
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs synchronously after a segment is accepted or reassembly completes. Subscriber failures are
        ///         logged and do not prevent later subscribers or the transfer completion path.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在接受分段或完成重组后同步发生。订阅者失败会被记录，且不会阻止后续订阅者或传输完成路径。
        ///     </para>
        /// </summary>
        public static event Action<RitsuLibSidecarChunkReceiveProgress>? ReceiveProgress;

        internal static void RaiseReceive(in RitsuLibSidecarChunkReceiveProgress progress)
        {
            var subscribers = ReceiveProgress;
            if (subscribers == null)
                return;

            foreach (Action<RitsuLibSidecarChunkReceiveProgress> subscriber in subscribers.GetInvocationList())
            {
                try
                {
                    subscriber(progress);
                }
                catch (Exception ex)
                {
                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        $"chunk-receive-progress-subscriber-exception:{subscriber.Method.DeclaringType?.FullName}:{subscriber.Method.Name}:{ex.GetType().FullName}",
                        $"[Sidecar] Chunk receive-progress subscriber failed: {ex.Message}");
                }
            }
        }
    }
}
