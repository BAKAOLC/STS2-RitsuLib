namespace STS2RitsuLib.Platform.Steam
{
    internal sealed record RitsuSteamWorkshopUpdateResult(
        bool Available,
        int InspectedCount,
        int NeedsUpdateCount,
        int TriggeredCount,
        int AlreadyQueuedCount,
        int FailedCount,
        string? ErrorMessage = null)
    {
        internal static RitsuSteamWorkshopUpdateResult Unavailable(string? errorMessage = null)
        {
            return new(false, 0, 0, 0, 0, 0, errorMessage);
        }
    }
}
