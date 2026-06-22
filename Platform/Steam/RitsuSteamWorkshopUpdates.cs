using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class RitsuSteamWorkshopUpdates
    {
        private const int QueryBatchSize = 20;
        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan QueryBatchDelay = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan ItemProcessingDelay = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan DownloadProgressPollInterval = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan DownloadProgressIdleTimeout = TimeSpan.FromSeconds(30);
        private static readonly Lazy<Bindings?> LazyBindings = new(CreateBindings);

        internal static bool IsAvailable => LazyBindings.Value != null;

        internal static Task<RitsuSteamWorkshopUpdateResult> TriggerMissingUpdatesAsync(
            SteamWorkshopDownloadTriggerMode mode = SteamWorkshopDownloadTriggerMode.HighPriorityImmediate,
            IProgress<RitsuSteamWorkshopUpdateProgress>? progress = null,
            IReadOnlyCollection<ulong>? itemIds = null,
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? Task.FromResult(
                    RitsuSteamWorkshopUpdateResult.Unavailable("Steamworks Workshop bindings are unavailable."))
                : bindings.TriggerMissingUpdatesAsync(mode, progress, itemIds, cancellationToken);
        }

        internal static Task<bool> MonitorDownloadsAsync(
            IReadOnlyCollection<RitsuSteamWorkshopDownloadItem> items,
            IProgress<RitsuSteamWorkshopDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null || items.Count == 0
                ? Task.FromResult(false)
                : bindings.MonitorDownloadsAsync(items, progress, cancellationToken);
        }

        private static Bindings? CreateBindings()
        {
            if (RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
            {
                RitsuLibFramework.Logger.Info(
                    "[SteamWorkshopUpdate] Steamworks Workshop binding skipped: native Steam integration is suppressed.");
                return null;
            }

            try
            {
                var steamworksAssembly = ResolveSteamworksAssembly();
                if (steamworksAssembly == null)
                {
                    RitsuLibFramework.Logger.Info(
                        "[SteamWorkshopUpdate] Steamworks Workshop binding unavailable: Steamworks assembly was not found.");
                    return null;
                }

                var steamUgc = steamworksAssembly.GetType("Steamworks.SteamUGC", false);
                var publishedFileIdType = steamworksAssembly.GetType("Steamworks.PublishedFileId_t", false);
                var steamUgcDetailsType = steamworksAssembly.GetType("Steamworks.SteamUGCDetails_t", false);
                var steamUgcQueryCompletedType =
                    steamworksAssembly.GetType("Steamworks.SteamUGCQueryCompleted_t", false);
                var steamApiCallType = steamworksAssembly.GetType("Steamworks.SteamAPICall_t", false);
                if (steamUgc == null ||
                    publishedFileIdType == null ||
                    steamUgcDetailsType == null ||
                    steamUgcQueryCompletedType == null ||
                    steamApiCallType == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamWorkshopUpdate] Steamworks Workshop binding unavailable: required Steamworks UGC types were not found.");
                    return null;
                }

                var publishedFileIdCtor = publishedFileIdType.GetConstructor([typeof(ulong)]);
                var publishedFileIdValue =
                    publishedFileIdType.GetField("m_PublishedFileId", BindingFlags.Public | BindingFlags.Instance);
                var getNumSubscribedItems = steamUgc.GetMethod(
                    "GetNumSubscribedItems",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
                var getSubscribedItems = steamUgc.GetMethod(
                    "GetSubscribedItems",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType.MakeArrayType(), typeof(uint)],
                    null);
                var getItemState = steamUgc.GetMethod(
                    "GetItemState",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType],
                    null);
                var getItemInstallInfo = steamUgc.GetMethod(
                    "GetItemInstallInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [
                        publishedFileIdType, typeof(ulong).MakeByRefType(), typeof(string).MakeByRefType(),
                        typeof(uint), typeof(uint).MakeByRefType(),
                    ],
                    null);
                var getItemDownloadInfo = steamUgc.GetMethod(
                    "GetItemDownloadInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType, typeof(ulong).MakeByRefType(), typeof(ulong).MakeByRefType()],
                    null);
                var downloadItem = steamUgc.GetMethod(
                    "DownloadItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType, typeof(bool)],
                    null);
                var suspendDownloads = steamUgc.GetMethod(
                    "SuspendDownloads",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(bool)],
                    null);
                var createQueryUgcDetailsRequest = steamUgc.GetMethod(
                    "CreateQueryUGCDetailsRequest",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType.MakeArrayType(), typeof(uint)],
                    null);
                var sendQueryUgcRequest = steamUgc.GetMethod(
                    "SendQueryUGCRequest",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [createQueryUgcDetailsRequest?.ReturnType ?? typeof(object)],
                    null);
                var getQueryUgcResult = steamUgc.GetMethod(
                    "GetQueryUGCResult",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [
                        createQueryUgcDetailsRequest?.ReturnType ?? typeof(object), typeof(uint),
                        steamUgcDetailsType.MakeByRefType(),
                    ],
                    null);
                var releaseQueryUgcRequest = steamUgc.GetMethod(
                    "ReleaseQueryUGCRequest",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [createQueryUgcDetailsRequest?.ReturnType ?? typeof(object)],
                    null);
                var setAllowCachedResponse = steamUgc.GetMethod(
                    "SetAllowCachedResponse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [createQueryUgcDetailsRequest?.ReturnType ?? typeof(object), typeof(uint)],
                    null);
                var steamUgcDetailsItemId = steamUgcDetailsType.GetField(
                    "m_nPublishedFileId",
                    BindingFlags.Public | BindingFlags.Instance);
                var steamUgcDetailsTitle = steamUgcDetailsType.GetField(
                                               "m_rgchTitle",
                                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                           ?? steamUgcDetailsType.GetField(
                                               "m_rgchTitle_",
                                               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var steamUgcDetailsUpdated = steamUgcDetailsType.GetField(
                    "m_rtimeUpdated",
                    BindingFlags.Public | BindingFlags.Instance);
                var queryCompletedResult = steamUgcQueryCompletedType.GetField(
                    "m_eResult",
                    BindingFlags.Public | BindingFlags.Instance);
                var queryCompletedReturned = steamUgcQueryCompletedType.GetField(
                    "m_unNumResultsReturned",
                    BindingFlags.Public | BindingFlags.Instance);
                var steamApiCallValue = steamApiCallType.GetField(
                    "m_SteamAPICall",
                    BindingFlags.Public | BindingFlags.Instance);
                var callResultType = steamworksAssembly
                    .GetType("Steamworks.CallResult`1", false)
                    ?.MakeGenericType(steamUgcQueryCompletedType);
                var callResultCreate = callResultType?.GetMethod(
                    "Create",
                    BindingFlags.Public | BindingFlags.Static);
                var callResultDelegateType = callResultCreate?.GetParameters().FirstOrDefault()?.ParameterType;
                var callResultSet = callResultType
                    ?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method =>
                    {
                        if (method.Name != "Set")
                            return false;

                        var parameters = method.GetParameters();
                        return parameters.Length == 2 &&
                               parameters[0].ParameterType == steamApiCallType &&
                               parameters[1].ParameterType == callResultDelegateType;
                    });
                var callResultDispose = callResultType?.GetMethod(
                    "Dispose",
                    BindingFlags.Public | BindingFlags.Instance);

                if (publishedFileIdCtor == null ||
                    publishedFileIdValue == null ||
                    getNumSubscribedItems == null ||
                    getSubscribedItems == null ||
                    getItemState == null ||
                    getItemInstallInfo == null ||
                    getItemDownloadInfo == null ||
                    downloadItem == null ||
                    suspendDownloads == null ||
                    createQueryUgcDetailsRequest == null ||
                    sendQueryUgcRequest == null ||
                    getQueryUgcResult == null ||
                    releaseQueryUgcRequest == null ||
                    setAllowCachedResponse == null ||
                    steamUgcDetailsItemId == null ||
                    steamUgcDetailsUpdated == null ||
                    queryCompletedResult == null ||
                    queryCompletedReturned == null ||
                    steamApiCallValue == null ||
                    callResultCreate == null ||
                    callResultSet == null ||
                    callResultDispose == null ||
                    callResultDelegateType == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamWorkshopUpdate] Steamworks Workshop binding unavailable: required SteamUGC methods or PublishedFileId_t members were not found.");
                    return null;
                }

                var flags = ResolveItemStateFlags(steamworksAssembly);
                RitsuLibFramework.Logger.Info("[SteamWorkshopUpdate] Steamworks Workshop binding initialized.");

                return new(
                    publishedFileIdType,
                    publishedFileIdValue,
                    getNumSubscribedItems,
                    getSubscribedItems,
                    getItemState,
                    getItemInstallInfo,
                    getItemDownloadInfo,
                    downloadItem,
                    suspendDownloads,
                    createQueryUgcDetailsRequest,
                    sendQueryUgcRequest,
                    getQueryUgcResult,
                    releaseQueryUgcRequest,
                    setAllowCachedResponse,
                    steamUgcDetailsType,
                    steamUgcDetailsItemId,
                    steamUgcDetailsTitle,
                    steamUgcDetailsUpdated,
                    queryCompletedResult,
                    queryCompletedReturned,
                    steamApiCallValue,
                    callResultCreate,
                    callResultSet,
                    callResultDispose,
                    callResultDelegateType,
                    flags);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SteamWorkshopUpdate] Steamworks Workshop binding failed: {ex.Message}");
                return null;
            }
        }

        private static Assembly? ResolveSteamworksAssembly()
        {
            var cSteamId = Type.GetType("Steamworks.CSteamID, Steamworks.NET", false);
            if (cSteamId != null)
                return cSteamId.Assembly;

            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetType("Steamworks.SteamUGC", false) != null);
        }

        private static ItemStateFlags ResolveItemStateFlags(Assembly steamworksAssembly)
        {
            var type = steamworksAssembly.GetType("Steamworks.EItemState", false);
            if (type == null)
                return ItemStateFlags.Default;

            return new(
                GetEnumFlag(type, "k_EItemStateInstalled", ItemStateFlags.Default.Installed),
                GetEnumFlag(type, "k_EItemStateNeedsUpdate", ItemStateFlags.Default.NeedsUpdate),
                GetEnumFlag(type, "k_EItemStateDownloading", ItemStateFlags.Default.Downloading),
                GetEnumFlag(type, "k_EItemStateDownloadPending", ItemStateFlags.Default.DownloadPending));
        }

        private static uint GetEnumFlag(Type enumType, string name, uint fallback)
        {
            try
            {
                var value = Enum.Parse(enumType, name, false);
                return Convert.ToUInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool HasFlag(uint state, uint flag)
        {
            return flag != 0 && (state & flag) == flag;
        }

        private sealed class Bindings(
            Type publishedFileIdType,
            FieldInfo publishedFileIdValue,
            MethodInfo getNumSubscribedItems,
            MethodInfo getSubscribedItems,
            MethodInfo getItemState,
            MethodInfo getItemInstallInfo,
            MethodInfo getItemDownloadInfo,
            MethodInfo downloadItem,
            MethodInfo suspendDownloads,
            MethodInfo createQueryUgcDetailsRequest,
            MethodInfo sendQueryUgcRequest,
            MethodInfo getQueryUgcResult,
            MethodInfo releaseQueryUgcRequest,
            MethodInfo setAllowCachedResponse,
            Type steamUgcDetailsType,
            FieldInfo steamUgcDetailsItemId,
            FieldInfo? steamUgcDetailsTitle,
            FieldInfo steamUgcDetailsUpdated,
            FieldInfo queryCompletedResult,
            FieldInfo queryCompletedReturned,
            FieldInfo steamApiCallValue,
            MethodInfo callResultCreate,
            MethodInfo callResultSet,
            MethodInfo callResultDispose,
            Type callResultDelegateType,
            ItemStateFlags itemStateFlags)
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<RitsuSteamWorkshopUpdateResult> TriggerMissingUpdatesAsync(
                SteamWorkshopDownloadTriggerMode mode,
                IProgress<RitsuSteamWorkshopUpdateProgress>? progress,
                IReadOnlyCollection<ulong>? itemIds,
                CancellationToken cancellationToken)
            {
                try
                {
                    progress?.Report(new(RitsuSteamWorkshopUpdateProgressStage.Starting, 0, 1));
                    var suspendDownloadsUntilGameExit =
                        mode == SteamWorkshopDownloadTriggerMode.QueueSuspendedUntilGameExit;
                    var selectedItemIds = itemIds?
                        .Where(static itemId => itemId != 0)
                        .Distinct()
                        .ToArray();
                    var scopedScan = selectedItemIds is { Length: > 0 };
                    IReadOnlyList<ItemSnapshot> items;
                    if (selectedItemIds is { Length: > 0 })
                    {
                        progress?.Report(new(
                            RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions,
                            0,
                            Math.Max(1, selectedItemIds.Length)));
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Scanning selected Workshop item(s). Count={selectedItemIds.Length}.");
                        items = await BuildItemSnapshotsAsync(selectedItemIds, progress, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var subscribedCount = Convert.ToUInt32(getNumSubscribedItems.Invoke(null, null));
                        progress?.Report(new(
                            RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions,
                            0,
                            (int)Math.Max(1u, subscribedCount)));
                        if (subscribedCount == 0)
                        {
                            RitsuLibFramework.Logger.Info(
                                "[SteamWorkshopUpdate] Scan complete: no subscribed Workshop items.");
                            progress?.Report(new(RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions, 1, 1));
                            return new(true, 0, 0, 0, 0, 0);
                        }

                        var itemArray = Array.CreateInstance(publishedFileIdType, subscribedCount);
                        var actualCount =
                            Convert.ToUInt32(getSubscribedItems.Invoke(null, [itemArray, subscribedCount]));
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Scanning subscribed Workshop items. Reported={subscribedCount}, Returned={actualCount}.");

                        items = await BuildItemSnapshotsAsync(itemArray, actualCount, progress, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    progress?.Report(new(
                        RitsuSteamWorkshopUpdateProgressStage.RefreshingDetails,
                        0,
                        Math.Max(1, items.Count)));
                    var remoteUpdateTimes = await QueryRemoteUpdateTimesAsync(items, progress, cancellationToken)
                        .ConfigureAwait(false);
                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Refreshed Workshop details for {remoteUpdateTimes.Count}/{items.Count} subscribed item(s).");
                    var previousUpdateTimes = SteamWorkshopUpdateSnapshotStore.GetItems();

                    var inspected = 0;
                    var needsUpdate = 0;
                    var triggered = 0;
                    var alreadyQueued = 0;
                    var failed = 0;
                    var downloadsSuspended = false;
                    List<RitsuSteamWorkshopDownloadItem> triggeredItems = [];
                    List<RitsuSteamWorkshopChangedItem> changedItems = [];
                    progress?.Report(new(
                        RitsuSteamWorkshopUpdateProgressStage.InspectingItems,
                        0,
                        Math.Max(1, items.Count)));

                    foreach (var item in items)
                    {
                        if (inspected > 0)
                            await Task.Delay(ItemProcessingDelay, cancellationToken).ConfigureAwait(false);

                        inspected++;
                        var hasRemoteDetails = remoteUpdateTimes.TryGetValue(item.Id, out var remoteDetails);
                        var stateNeedsUpdate = HasFlag(item.State, itemStateFlags.NeedsUpdate);
                        var remoteNeedsUpdate = hasRemoteDetails &&
                                                item.LocalTimestamp is { } localTimestamp &&
                                                remoteDetails.Updated > localTimestamp;
                        if (!stateNeedsUpdate && !remoteNeedsUpdate)
                        {
                            ReportInspectionProgress();
                            continue;
                        }

                        needsUpdate++;
                        var displayName = hasRemoteDetails
                            ? ResolveItemDisplayName(item, remoteDetails)
                            : $"Workshop item {item.Id}";
                        if (hasRemoteDetails &&
                            (!previousUpdateTimes.TryGetValue(item.Id, out var previous) ||
                             previous.Updated != remoteDetails.Updated))
                            changedItems.Add(new(item.Id, displayName, remoteDetails.Updated));
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Workshop item {item.Id} needs update. State={item.State}, LocalTimestamp={item.LocalTimestamp?.ToString() ?? "<unknown>"}, RemoteUpdated={(hasRemoteDetails ? remoteDetails.Updated.ToString() : "<unknown>")}.");
                        if (HasFlag(item.State, itemStateFlags.Downloading) ||
                            HasFlag(item.State, itemStateFlags.DownloadPending))
                        {
                            alreadyQueued++;
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Workshop item {item.Id} already has a download queued or running.");
                            ReportInspectionProgress();
                            continue;
                        }

                        if (suspendDownloadsUntilGameExit && !downloadsSuspended)
                        {
                            InvokeSuspendDownloads(true);
                            downloadsSuspended = true;
                            RitsuLibFramework.Logger.Info(
                                "[SteamWorkshopUpdate] Suspended Steam Workshop downloads until game exit before queueing updates.");
                        }

                        if (InvokeDownloadItem(item.Handle, mode))
                        {
                            triggered++;
                            triggeredItems.Add(new(item.Id, displayName));
                            RitsuLibFramework.Logger.Info(
                                suspendDownloadsUntilGameExit
                                    ? $"[SteamWorkshopUpdate] Queued Steam Workshop update for item {item.Id}; downloads remain suspended until game exit."
                                    : $"[SteamWorkshopUpdate] Triggered Steam Workshop update for item {item.Id}.");
                        }
                        else
                        {
                            failed++;
                            RitsuLibFramework.Logger.Warn(
                                $"[SteamWorkshopUpdate] Steam rejected update trigger for item {item.Id}.");
                        }

                        ReportInspectionProgress();
                    }

                    if (downloadsSuspended && triggered == 0)
                    {
                        InvokeSuspendDownloads(false);
                        RitsuLibFramework.Logger.Info(
                            "[SteamWorkshopUpdate] Resumed Steam Workshop downloads because no update downloads were queued.");
                    }

                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Scan complete. Inspected={inspected}, NeedsUpdate={needsUpdate}, Triggered={triggered}, AlreadyQueued={alreadyQueued}, Failed={failed}.");
                    if (remoteUpdateTimes.Count > 0 || items.Count == 0)
                        SaveRemoteUpdateSnapshot(remoteUpdateTimes, scopedScan);
                    return new(true, inspected, needsUpdate, triggered, alreadyQueued, failed, null, triggeredItems,
                        changedItems);

                    void ReportInspectionProgress()
                    {
                        progress?.Report(new(
                            RitsuSteamWorkshopUpdateProgressStage.InspectingItems,
                            inspected,
                            Math.Max(1, items.Count),
                            needsUpdate,
                            triggered,
                            alreadyQueued,
                            failed));
                    }
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[SteamWorkshopUpdate] Check failed: {ex.Message}");
                    return RitsuSteamWorkshopUpdateResult.Unavailable(ex.Message);
                }
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<bool> MonitorDownloadsAsync(
                IReadOnlyCollection<RitsuSteamWorkshopDownloadItem> downloadItems,
                IProgress<RitsuSteamWorkshopDownloadProgress>? progress,
                CancellationToken cancellationToken)
            {
                Dictionary<ulong, DownloadMonitorItem> items = [];
                foreach (var workshopDownloadItem in downloadItems.Where(static item => item.Id != 0)
                             .DistinctBy(static item => item.Id))
                    if (CreatePublishedFileId(workshopDownloadItem.Id) is { } item)
                        items[workshopDownloadItem.Id] = new(item, workshopDownloadItem.DisplayName);

                if (items.Count == 0)
                    return false;

                var idleSince = DateTimeOffset.UtcNow;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var completed = 0;
                    var bytesDownloaded = 0UL;
                    var bytesTotal = 0UL;
                    var active = false;

                    string? currentItemName = null;
                    foreach (var (itemId, item) in items)
                    {
                        var state = Convert.ToUInt32(getItemState.Invoke(null, [item.Handle]));
                        if (!HasFlag(state, itemStateFlags.NeedsUpdate) &&
                            !HasFlag(state, itemStateFlags.Downloading) &&
                            !HasFlag(state, itemStateFlags.DownloadPending))
                        {
                            completed++;
                            continue;
                        }

                        currentItemName ??= item.DisplayName;
                        if (TryGetDownloadInfo(item.Handle, out var itemDownloaded, out var itemTotal))
                        {
                            bytesDownloaded += itemDownloaded;
                            bytesTotal += itemTotal;
                            active = true;
                            if (itemTotal > 0 && itemDownloaded >= itemTotal)
                                completed++;
                        }
                        else
                        {
                            RitsuLibFramework.Logger.Debug(
                                $"[SteamWorkshopUpdate] Download progress unavailable for Workshop item {itemId}. State={state}.");
                        }
                    }

                    progress?.Report(new(completed, items.Count, bytesDownloaded, bytesTotal, currentItemName));
                    if (completed >= items.Count)
                        return true;

                    if (active)
                    {
                        idleSince = DateTimeOffset.UtcNow;
                    }
                    else if (DateTimeOffset.UtcNow - idleSince >= DownloadProgressIdleTimeout)
                    {
                        RitsuLibFramework.Logger.Warn(
                            "[SteamWorkshopUpdate] Stopped monitoring Workshop download progress because no active download progress was reported.");
                        return false;
                    }

                    await Task.Delay(DownloadProgressPollInterval, cancellationToken)
                        .ConfigureAwait(false);
                }

                return false;
            }

            private async Task<IReadOnlyList<ItemSnapshot>> BuildItemSnapshotsAsync(
                Array itemArray,
                uint actualCount,
                IProgress<RitsuSteamWorkshopUpdateProgress>? progress,
                CancellationToken cancellationToken)
            {
                List<ItemSnapshot> items = [];
                for (var i = 0; i < actualCount; i++)
                {
                    if (i > 0)
                        await Task.Delay(ItemProcessingDelay, cancellationToken).ConfigureAwait(false);

                    var item = itemArray.GetValue(i);
                    if (item == null)
                    {
                        ReportProgress(i + 1);
                        continue;
                    }

                    var state = Convert.ToUInt32(getItemState.Invoke(null, [item]));
                    var itemId = GetItemId(item);
                    if (itemId == 0)
                    {
                        ReportProgress(i + 1);
                        continue;
                    }

                    items.Add(new(
                        item,
                        itemId,
                        state,
                        TryGetLocalTimestamp(item, state)));
                    ReportProgress(i + 1);
                }

                return items;

                void ReportProgress(int completed)
                {
                    progress?.Report(new(
                        RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions,
                        Math.Min((int)actualCount, completed),
                        (int)Math.Max(1u, actualCount)));
                }
            }

            private async Task<IReadOnlyList<ItemSnapshot>> BuildItemSnapshotsAsync(
                IReadOnlyList<ulong> itemIds,
                IProgress<RitsuSteamWorkshopUpdateProgress>? progress,
                CancellationToken cancellationToken)
            {
                List<ItemSnapshot> items = [];
                for (var i = 0; i < itemIds.Count; i++)
                {
                    if (i > 0)
                        await Task.Delay(ItemProcessingDelay, cancellationToken).ConfigureAwait(false);

                    if (CreatePublishedFileId(itemIds[i]) is not { } item)
                    {
                        ReportProgress(i + 1);
                        continue;
                    }

                    var state = Convert.ToUInt32(getItemState.Invoke(null, [item]));
                    items.Add(new(
                        item,
                        itemIds[i],
                        state,
                        TryGetLocalTimestamp(item, state)));
                    ReportProgress(i + 1);
                }

                return items;

                void ReportProgress(int completed)
                {
                    progress?.Report(new(
                        RitsuSteamWorkshopUpdateProgressStage.ReadingSubscriptions,
                        Math.Min(itemIds.Count, completed),
                        Math.Max(1, itemIds.Count)));
                }
            }

            private async Task<IReadOnlyDictionary<ulong, RemoteItemDetails>> QueryRemoteUpdateTimesAsync(
                IReadOnlyList<ItemSnapshot> items,
                IProgress<RitsuSteamWorkshopUpdateProgress>? progress,
                CancellationToken cancellationToken)
            {
                if (items.Count == 0)
                    return new Dictionary<ulong, RemoteItemDetails>();

                Dictionary<ulong, RemoteItemDetails> details = [];
                for (var offset = 0; offset < items.Count; offset += QueryBatchSize)
                {
                    if (offset > 0)
                        await Task.Delay(QueryBatchDelay, cancellationToken).ConfigureAwait(false);

                    var batch = items
                        .Skip(offset)
                        .Take(QueryBatchSize)
                        .ToArray();
                    var batchTimes = await QueryRemoteUpdateTimesBatchAsync(batch, cancellationToken)
                        .ConfigureAwait(false);
                    foreach (var (itemId, itemDetails) in batchTimes)
                        details[itemId] = itemDetails;
                    progress?.Report(new(
                        RitsuSteamWorkshopUpdateProgressStage.RefreshingDetails,
                        Math.Min(items.Count, offset + batch.Length),
                        Math.Max(1, items.Count)));
                }

                return details;
            }

            private static void SaveRemoteUpdateSnapshot(
                IReadOnlyDictionary<ulong, RemoteItemDetails> remoteUpdateTimes,
                bool merge)
            {
                Dictionary<ulong, SteamWorkshopStoredUpdateItem> snapshot = [];
                foreach (var (itemId, details) in remoteUpdateTimes)
                    snapshot[itemId] = new(details.Updated, details.Title);
                if (merge)
                    SteamWorkshopUpdateSnapshotStore.Merge(snapshot);
                else
                    SteamWorkshopUpdateSnapshotStore.Replace(snapshot);
            }

            private async Task<IReadOnlyDictionary<ulong, RemoteItemDetails>> QueryRemoteUpdateTimesBatchAsync(
                IReadOnlyList<ItemSnapshot> items,
                CancellationToken cancellationToken)
            {
                var itemArray = Array.CreateInstance(publishedFileIdType, items.Count);
                for (var i = 0; i < items.Count; i++)
                    itemArray.SetValue(items[i].Handle, i);

                var queryHandle = createQueryUgcDetailsRequest.Invoke(null, [itemArray, (uint)items.Count]);
                if (queryHandle == null)
                    return new Dictionary<ulong, RemoteItemDetails>();

                try
                {
                    setAllowCachedResponse.Invoke(null, [queryHandle, 0u]);
                    var apiCall = sendQueryUgcRequest.Invoke(null, [queryHandle]);
                    if (apiCall == null || Convert.ToUInt64(steamApiCallValue.GetValue(apiCall)) == 0)
                    {
                        RitsuLibFramework.Logger.Warn(
                            "[SteamWorkshopUpdate] Steam rejected Workshop details query.");
                        return new Dictionary<ulong, RemoteItemDetails>();
                    }

                    var queryCompleted = await WaitForQueryAsync(apiCall, cancellationToken)
                        .ConfigureAwait(false);
                    if (queryCompleted == null)
                        return new Dictionary<ulong, RemoteItemDetails>();

                    if (IsResultOk(queryCompleted))
                        return ReadQueryResults(queryHandle, GetReturnedCount(queryCompleted));
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Workshop details query failed: {queryCompletedResult.GetValue(queryCompleted)}.");
                    return new Dictionary<ulong, RemoteItemDetails>();
                }
                finally
                {
                    releaseQueryUgcRequest.Invoke(null, [queryHandle]);
                }
            }

            private async Task<object?> WaitForQueryAsync(
                object apiCall,
                CancellationToken cancellationToken)
            {
                var completion = new TaskCompletionSource<object?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                void OnCompleted(object result, bool ioFailure)
                {
                    completion.TrySetResult(ioFailure ? null : result);
                }

                var callback = CreateQueryCompletedDelegate(OnCompleted);
                var callResult = callResultCreate.Invoke(null, [callback]);
                if (callResult == null)
                    return null;

                try
                {
                    callResultSet.Invoke(callResult, [apiCall, callback]);
                    var timeoutTask = Task.Delay(QueryTimeout, cancellationToken);
                    var completed = await Task.WhenAny(completion.Task, timeoutTask).ConfigureAwait(false);
                    if (completed == completion.Task)
                        return await completion.Task.ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                        RitsuLibFramework.Logger.Warn(
                            "[SteamWorkshopUpdate] Workshop details query timed out.");
                    return null;
                }
                finally
                {
                    callResultDispose.Invoke(callResult, null);
                }
            }

            private Delegate CreateQueryCompletedDelegate(Action<object, bool> onCompleted)
            {
                var invoke = callResultDelegateType.GetMethod("Invoke")!;
                var result = Expression.Parameter(invoke.GetParameters()[0].ParameterType, "result");
                var ioFailure = Expression.Parameter(typeof(bool), "ioFailure");
                var body = Expression.Call(
                    Expression.Constant(onCompleted),
                    nameof(Action<object, bool>.Invoke),
                    null,
                    Expression.Convert(result, typeof(object)),
                    ioFailure);
                return Expression.Lambda(callResultDelegateType, body, result, ioFailure).Compile();
            }

            private IReadOnlyDictionary<ulong, RemoteItemDetails> ReadQueryResults(object queryHandle,
                uint returnedCount)
            {
                Dictionary<ulong, RemoteItemDetails> detailsByItem = [];
                for (var i = 0u; i < returnedCount; i++)
                {
                    var details = Activator.CreateInstance(steamUgcDetailsType);
                    object?[] args = [queryHandle, i, details];
                    if (getQueryUgcResult.Invoke(null, args) is not true || args[2] == null)
                        continue;

                    var item = steamUgcDetailsItemId.GetValue(args[2]);
                    if (item == null)
                        continue;

                    var itemId = GetItemId(item);
                    var detail = args[2]!;
                    var updated = Convert.ToUInt32(steamUgcDetailsUpdated.GetValue(detail));
                    if (itemId != 0 && updated != 0)
                        detailsByItem[itemId] = new(updated, ReadItemTitle(detail));
                }

                return detailsByItem;
            }

            private bool IsResultOk(object queryCompleted)
            {
                var result = queryCompletedResult.GetValue(queryCompleted);
                return result != null && Convert.ToUInt32(result) == 1;
            }

            private uint GetReturnedCount(object queryCompleted)
            {
                return Convert.ToUInt32(queryCompletedReturned.GetValue(queryCompleted));
            }

            private bool InvokeDownloadItem(object item, SteamWorkshopDownloadTriggerMode mode)
            {
                try
                {
                    var highPriority = mode == SteamWorkshopDownloadTriggerMode.HighPriorityImmediate;
                    return downloadItem.Invoke(null, [item, highPriority]) is true;
                }
                catch
                {
                    return false;
                }
            }

            private bool TryGetDownloadInfo(object item, out ulong bytesDownloaded, out ulong bytesTotal)
            {
                bytesDownloaded = 0;
                bytesTotal = 0;
                try
                {
                    object?[] args = [item, 0UL, 0UL];
                    if (getItemDownloadInfo.Invoke(null, args) is not true)
                        return false;

                    bytesDownloaded = Convert.ToUInt64(args[1]);
                    bytesTotal = Convert.ToUInt64(args[2]);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private string ResolveItemDisplayName(ItemSnapshot item, RemoteItemDetails remoteDetails)
            {
                return string.IsNullOrWhiteSpace(remoteDetails.Title)
                    ? $"Workshop item {item.Id}"
                    : remoteDetails.Title;
            }

            private string? ReadItemTitle(object details)
            {
                if (steamUgcDetailsTitle == null)
                    return null;

                try
                {
                    return steamUgcDetailsTitle.GetValue(details) switch
                    {
                        string title when !string.IsNullOrWhiteSpace(title) => title.Trim(),
                        char[] chars => new string(chars).TrimEnd('\0').Trim(),
                        byte[] bytes => Encoding.UTF8.GetString(bytes).TrimEnd('\0').Trim(),
                        _ => null,
                    };
                }
                catch
                {
                    return null;
                }
            }

            private void InvokeSuspendDownloads(bool suspend)
            {
                try
                {
                    suspendDownloads.Invoke(null, [suspend]);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Failed to {(suspend ? "suspend" : "resume")} Steam Workshop downloads: {ex.Message}");
                }
            }

            private uint? TryGetLocalTimestamp(object item, uint state)
            {
                if (!HasFlag(state, itemStateFlags.Installed))
                    return null;

                try
                {
                    object?[] args = [item, 0UL, string.Empty, 4096u, 0u];
                    return getItemInstallInfo.Invoke(null, args) is true
                        ? Convert.ToUInt32(args[4])
                        : null;
                }
                catch
                {
                    return null;
                }
            }

            private ulong GetItemId(object item)
            {
                try
                {
                    return Convert.ToUInt64(publishedFileIdValue.GetValue(item));
                }
                catch
                {
                    return 0;
                }
            }

            private object? CreatePublishedFileId(ulong itemId)
            {
                try
                {
                    return Activator.CreateInstance(publishedFileIdType, itemId);
                }
                catch
                {
                    return null;
                }
            }
        }

        private readonly record struct ItemStateFlags(
            uint Installed,
            uint NeedsUpdate,
            uint Downloading,
            uint DownloadPending)
        {
            internal static ItemStateFlags Default => new(4, 8, 16, 32);
        }

        private sealed record ItemSnapshot(
            object Handle,
            ulong Id,
            uint State,
            uint? LocalTimestamp);

        private readonly record struct RemoteItemDetails(uint Updated, string? Title);

        private sealed record DownloadMonitorItem(object Handle, string DisplayName);
    }

    internal enum SteamWorkshopDownloadTriggerMode
    {
        HighPriorityImmediate,
        QueueSuspendedUntilGameExit,
    }
}
