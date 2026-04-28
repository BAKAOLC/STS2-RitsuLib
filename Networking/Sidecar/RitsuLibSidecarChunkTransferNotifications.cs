namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Optional notifications for chunked sidecar transfers (receive path). Subscribe for UI such as image
    ///     download progress; keep handlers short and avoid blocking the multiplayer receive thread unless using
    ///     <see cref="RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop" />.
    /// </summary>
    public static class RitsuLibSidecarChunkTransferNotifications
    {
        /// <summary>Raised after a new segment is accepted or when reassembly completes.</summary>
        public static event Action<RitsuLibSidecarChunkReceiveProgress>? ReceiveProgress;

        internal static void RaiseReceive(in RitsuLibSidecarChunkReceiveProgress progress)
        {
            ReceiveProgress?.Invoke(progress);
        }
    }
}
