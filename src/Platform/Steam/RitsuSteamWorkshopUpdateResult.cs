namespace STS2RitsuLib.Platform.Steam
{
    internal sealed record RitsuSteamWorkshopUpdateResult(
        bool Available,
        int InspectedCount,
        int NeedsUpdateCount,
        int TriggeredCount,
        int AlreadyQueuedCount,
        int FailedCount,
        string? ErrorMessage = null,
        IReadOnlyList<RitsuSteamWorkshopDownloadItem>? TriggeredItems = null,
        IReadOnlyList<RitsuSteamWorkshopDownloadItem>? MonitorItems = null,
        IReadOnlyList<RitsuSteamWorkshopChangedItem>? ChangedItems = null)
    {
        internal static RitsuSteamWorkshopUpdateResult Unavailable(string? errorMessage = null)
        {
            return new(false, 0, 0, 0, 0, 0, errorMessage);
        }
    }

    internal sealed record RitsuSteamWorkshopUpdateProgress(
        RitsuSteamWorkshopUpdateProgressStage Stage,
        int CompletedCount,
        int TotalCount,
        int NeedsUpdateCount = 0,
        int QueuedCount = 0,
        int AlreadyQueuedCount = 0,
        int FailedCount = 0);

    internal enum RitsuSteamWorkshopUpdateProgressStage
    {
        Starting,
        ReadingSubscriptions,
        RefreshingDetails,
        InspectingItems,
    }

    internal sealed record RitsuSteamWorkshopDownloadProgress(
        int CompletedCount,
        int TotalCount,
        ulong BytesDownloaded,
        ulong BytesTotal,
        string? CurrentItemName = null);

    internal sealed record RitsuSteamWorkshopDownloadItem(ulong Id, string DisplayName);

    internal sealed record RitsuSteamWorkshopChangedItem(ulong Id, string DisplayName, uint RemoteUpdated);

    internal sealed record RitsuSteamWorkshopSearchResult(
        IReadOnlyList<RitsuSteamWorkshopItem> Items,
        uint Page,
        uint PageSize,
        uint? TotalMatchingResults)
    {
        internal uint? TotalPages => TotalMatchingResults is { } total
            ? Math.Max(1u, (total + PageSize - 1) / PageSize)
            : null;

        internal bool HasPreviousPage => Page > 1;

        internal bool HasNextPage => TotalPages is { } totalPages
            ? Page < totalPages
            : (uint)Items.Count >= PageSize;
    }

    internal sealed record RitsuSteamWorkshopItem(
        ulong Id,
        string DisplayName,
        string? ModId,
        string? Author,
        ulong? OwnerSteamId,
        string? Description,
        string? PreviewUrl,
        bool IsSubscribed,
        bool IsInstalled,
        bool NeedsUpdate,
        bool IsDownloading,
        bool IsDownloadPending,
        uint? LocalUpdated,
        uint? RemoteUpdated);

    internal sealed record RitsuSteamWorkshopActionResult(
        bool Available,
        bool Accepted,
        ulong ItemId,
        string? ErrorMessage = null)
    {
        internal static RitsuSteamWorkshopActionResult Unavailable(ulong itemId, string? errorMessage = null)
        {
            return new(false, false, itemId, errorMessage);
        }
    }
}
