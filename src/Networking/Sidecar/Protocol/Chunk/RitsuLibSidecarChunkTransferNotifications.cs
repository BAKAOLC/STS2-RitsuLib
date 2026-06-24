namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Optional notifications for chunked sidecar transfers (receive path). Subscribe for UI such as image
    ///     download progress; keep handlers short and avoid blocking the multiplayer receive thread unless using
    ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />.
    ///     分块 sidecar 传输的可选通知（接收路径）。为图像下载进度等 UI 订阅；
    ///     保持处理器简短，除非使用
    ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />，否则避免阻塞多人接收线程。
    /// </summary>
    public static class RitsuLibSidecarChunkTransferNotifications
    {
        /// <summary>
        ///     Raised after a new segment is accepted or when reassembly completes.
        ///     接受新 segment 后或重组完成时引发。
        /// </summary>
        public static event Action<RitsuLibSidecarChunkReceiveProgress>? ReceiveProgress;

        internal static void RaiseReceive(in RitsuLibSidecarChunkReceiveProgress progress)
        {
            ReceiveProgress?.Invoke(progress);
        }
    }
}
