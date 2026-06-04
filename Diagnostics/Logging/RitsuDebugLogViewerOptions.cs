namespace STS2RitsuLib.Diagnostics.Logging
{
    internal sealed record RitsuDebugLogViewerOptions(
        bool Enabled,
        bool MirrorGameLogs,
        bool AutoOpen,
        bool LanAccessEnabled,
        int Port,
        int PortFallbackCount,
        string AccessToken,
        int RingBufferCapacity,
        int QueueCapacity);
}
