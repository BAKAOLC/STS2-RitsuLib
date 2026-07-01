using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class RitsuSteamWorkshopUpdates
    {
        private const int QueryBatchSize = 20;
        private const uint SearchResultsPerPage = 50;
        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan QueryBatchDelay = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan ItemProcessingDelay = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan DownloadProgressPollInterval = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan DownloadProgressIdleTimeout = TimeSpan.FromSeconds(30);
        private static readonly Lazy<Bindings?> LazyBindings = new(CreateBindings);
        private static readonly Lock TriggeredDownloadSyncRoot = new();
        private static readonly HashSet<ulong> TriggeredDownloadItemIds = [];

        internal static bool IsAvailable => LazyBindings.Value != null;

        internal static bool IsSearchAvailable => LazyBindings.Value?.SupportsSearch == true;

        internal static bool IsTriggeredDownloadItem(ulong itemId)
        {
            lock (TriggeredDownloadSyncRoot)
            {
                return TriggeredDownloadItemIds.Contains(itemId);
            }
        }

        internal static Task<RitsuSteamWorkshopUpdateResult> TriggerMissingUpdatesAsync(
            IProgress<RitsuSteamWorkshopUpdateProgress>? progress = null,
            IReadOnlyCollection<ulong>? itemIds = null,
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? Task.FromResult(
                    RitsuSteamWorkshopUpdateResult.Unavailable("Steamworks Workshop bindings are unavailable."))
                : bindings.TriggerMissingUpdatesAsync(progress, itemIds, cancellationToken);
        }

        internal static Task<bool> MonitorDownloadsAsync(
            IReadOnlyCollection<RitsuSteamWorkshopDownloadItem> items,
            IProgress<RitsuSteamWorkshopDownloadProgress>? progress = null,
            bool stopWhenIdle = true,
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null || items.Count == 0
                ? Task.FromResult(false)
                : bindings.MonitorDownloadsAsync(items, progress, stopWhenIdle, cancellationToken);
        }

        internal static Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsAsync(
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? Task.FromResult<IReadOnlyList<RitsuSteamWorkshopItem>>([])
                : bindings.ListSubscribedItemsAsync(cancellationToken);
        }

        internal static Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsFromCacheAsync(
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? Task.FromResult<IReadOnlyList<RitsuSteamWorkshopItem>>([])
                : bindings.ListSubscribedItemsFromCacheAsync(cancellationToken);
        }

        internal static Task<IReadOnlyList<RitsuSteamWorkshopItem>> QueryItemsAsync(
            IReadOnlyCollection<ulong> itemIds,
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null || itemIds.Count == 0
                ? Task.FromResult<IReadOnlyList<RitsuSteamWorkshopItem>>([])
                : bindings.QueryItemsAsync(itemIds, cancellationToken);
        }

        internal static Task<RitsuSteamWorkshopSearchResult> SearchItemsAsync(
            string query,
            uint page = 1,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1u, page);
            var bindings = LazyBindings.Value;
            return bindings is not { SupportsSearch: true } || string.IsNullOrWhiteSpace(query)
                ? Task.FromResult(new RitsuSteamWorkshopSearchResult([], page, SearchResultsPerPage, null))
                : bindings.SearchItemsAsync(query, page, cancellationToken);
        }

        internal static RitsuSteamWorkshopActionResult RequestSubscribe(ulong itemId)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? RitsuSteamWorkshopActionResult.Unavailable(
                    itemId,
                    "Steamworks Workshop bindings are unavailable.")
                : bindings.RequestSubscribe(itemId);
        }

        internal static RitsuSteamWorkshopActionResult RequestUnsubscribe(ulong itemId)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? RitsuSteamWorkshopActionResult.Unavailable(
                    itemId,
                    "Steamworks Workshop bindings are unavailable.")
                : bindings.RequestUnsubscribe(itemId);
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
                var steamAppIdType = steamworksAssembly.GetType("Steamworks.AppId_t", false);
                var eUgcQueryType = steamworksAssembly.GetType("Steamworks.EUGCQuery", false);
                var eUgcMatchingType = steamworksAssembly.GetType("Steamworks.EUGCMatchingUGCType", false);
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
                var steamAppIdCtor = steamAppIdType?.GetConstructor([typeof(uint)]);
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
                var subscribeItem = steamUgc.GetMethod(
                    "SubscribeItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType],
                    null);
                var unsubscribeItem = steamUgc.GetMethod(
                    "UnsubscribeItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType],
                    null);
                var createQueryUgcDetailsRequest = steamUgc.GetMethod(
                    "CreateQueryUGCDetailsRequest",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType.MakeArrayType(), typeof(uint)],
                    null);
                var createQueryAllUgcRequest =
                    steamAppIdType == null || eUgcQueryType == null || eUgcMatchingType == null
                        ? null
                        : steamUgc.GetMethod(
                            "CreateQueryAllUGCRequest",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            [eUgcQueryType, eUgcMatchingType, steamAppIdType, steamAppIdType, typeof(uint)],
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
                var setSearchText =
                    createQueryUgcDetailsRequest == null
                        ? null
                        : steamUgc.GetMethod(
                            "SetSearchText",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            [createQueryUgcDetailsRequest.ReturnType, typeof(string)],
                            null);
                var setReturnLongDescription =
                    createQueryUgcDetailsRequest == null
                        ? null
                        : steamUgc.GetMethod(
                            "SetReturnLongDescription",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            [createQueryUgcDetailsRequest.ReturnType, typeof(bool)],
                            null);
                var getQueryUgcPreviewUrl =
                    createQueryUgcDetailsRequest == null
                        ? null
                        : steamUgc.GetMethod(
                            "GetQueryUGCPreviewURL",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            [
                                createQueryUgcDetailsRequest.ReturnType, typeof(uint), typeof(string).MakeByRefType(),
                                typeof(uint),
                            ],
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
                var steamUgcDetailsDescription = steamUgcDetailsType.GetField(
                                                     "m_rgchDescription",
                                                     BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.Instance)
                                                 ?? steamUgcDetailsType.GetField(
                                                     "m_rgchDescription_",
                                                     BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.Instance);
                var steamUgcDetailsOwner = steamUgcDetailsType.GetField(
                    "m_ulSteamIDOwner",
                    BindingFlags.Public | BindingFlags.Instance);
                var steamUgcDetailsUpdated = steamUgcDetailsType.GetField(
                    "m_rtimeUpdated",
                    BindingFlags.Public | BindingFlags.Instance);
                var queryCompletedResult = steamUgcQueryCompletedType.GetField(
                    "m_eResult",
                    BindingFlags.Public | BindingFlags.Instance);
                var queryCompletedReturned = steamUgcQueryCompletedType.GetField(
                    "m_unNumResultsReturned",
                    BindingFlags.Public | BindingFlags.Instance);
                var queryCompletedTotalMatching = steamUgcQueryCompletedType.GetField(
                    "m_unTotalMatchingResults",
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
                    subscribeItem == null ||
                    unsubscribeItem == null ||
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
                    steamAppIdCtor,
                    createQueryAllUgcRequest,
                    getNumSubscribedItems,
                    getSubscribedItems,
                    getItemState,
                    getItemInstallInfo,
                    getItemDownloadInfo,
                    downloadItem,
                    subscribeItem,
                    unsubscribeItem,
                    createQueryUgcDetailsRequest,
                    sendQueryUgcRequest,
                    getQueryUgcResult,
                    releaseQueryUgcRequest,
                    setAllowCachedResponse,
                    setSearchText,
                    setReturnLongDescription,
                    getQueryUgcPreviewUrl,
                    eUgcQueryType,
                    eUgcMatchingType,
                    steamUgcDetailsType,
                    steamUgcDetailsItemId,
                    steamUgcDetailsTitle,
                    steamUgcDetailsDescription,
                    steamUgcDetailsOwner,
                    steamUgcDetailsUpdated,
                    queryCompletedResult,
                    queryCompletedReturned,
                    queryCompletedTotalMatching,
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
                GetEnumFlag(type, "k_EItemStateSubscribed", ItemStateFlags.Default.Subscribed),
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
            ConstructorInfo? steamAppIdCtor,
            MethodInfo? createQueryAllUgcRequest,
            MethodInfo getNumSubscribedItems,
            MethodInfo getSubscribedItems,
            MethodInfo getItemState,
            MethodInfo getItemInstallInfo,
            MethodInfo getItemDownloadInfo,
            MethodInfo downloadItem,
            MethodInfo subscribeItem,
            MethodInfo unsubscribeItem,
            MethodInfo createQueryUgcDetailsRequest,
            MethodInfo sendQueryUgcRequest,
            MethodInfo getQueryUgcResult,
            MethodInfo releaseQueryUgcRequest,
            MethodInfo setAllowCachedResponse,
            MethodInfo? setSearchText,
            MethodInfo? setReturnLongDescription,
            MethodInfo? getQueryUgcPreviewUrl,
            Type? eUgcQueryType,
            Type? eUgcMatchingType,
            Type steamUgcDetailsType,
            FieldInfo steamUgcDetailsItemId,
            FieldInfo? steamUgcDetailsTitle,
            FieldInfo? steamUgcDetailsDescription,
            FieldInfo? steamUgcDetailsOwner,
            FieldInfo steamUgcDetailsUpdated,
            FieldInfo queryCompletedResult,
            FieldInfo queryCompletedReturned,
            FieldInfo? queryCompletedTotalMatching,
            FieldInfo steamApiCallValue,
            MethodInfo callResultCreate,
            MethodInfo callResultSet,
            MethodInfo callResultDispose,
            Type callResultDelegateType,
            ItemStateFlags itemStateFlags)
        {
            private static readonly Lock ManifestCacheSyncRoot = new();

            private static readonly Dictionary<string, LocalWorkshopManifest> LocalManifestCache =
                new(StringComparer.OrdinalIgnoreCase);

            internal bool SupportsSearch =>
                steamAppIdCtor != null &&
                createQueryAllUgcRequest != null &&
                setSearchText != null &&
                eUgcQueryType != null &&
                eUgcMatchingType != null;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<RitsuSteamWorkshopUpdateResult> TriggerMissingUpdatesAsync(
                IProgress<RitsuSteamWorkshopUpdateProgress>? progress,
                IReadOnlyCollection<ulong>? itemIds,
                CancellationToken cancellationToken)
            {
                try
                {
                    progress?.Report(new(RitsuSteamWorkshopUpdateProgressStage.Starting, 0, 1));
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
                    var localManifests = BuildLocalManifestByWorkshopItemId();

                    var inspected = 0;
                    var needsUpdate = 0;
                    var triggered = 0;
                    var alreadyQueued = 0;
                    var failed = 0;
                    List<RitsuSteamWorkshopDownloadItem> triggeredItems = [];
                    List<RitsuSteamWorkshopDownloadItem> monitorItems = [];
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
                                                item.Install.LocalTimestamp is { } localTimestamp &&
                                                remoteDetails.Updated > localTimestamp;
                        if (!stateNeedsUpdate && !remoteNeedsUpdate)
                        {
                            ReportInspectionProgress();
                            continue;
                        }

                        needsUpdate++;
                        var displayName = hasRemoteDetails
                            ? ResolveItemDisplayName(item, remoteDetails, localManifests)
                            : $"Workshop item {item.Id}";
                        if (hasRemoteDetails &&
                            (!previousUpdateTimes.TryGetValue(item.Id, out var previous) ||
                             previous.Updated != remoteDetails.Updated))
                            changedItems.Add(new(item.Id, displayName, remoteDetails.Updated));
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Workshop item {item.Id} needs update. State={item.State}, LocalTimestamp={item.Install.LocalTimestamp?.ToString() ?? "<unknown>"}, RemoteUpdated={(hasRemoteDetails ? remoteDetails.Updated.ToString() : "<unknown>")}.");
                        if (HasFlag(item.State, itemStateFlags.Downloading) ||
                            HasFlag(item.State, itemStateFlags.DownloadPending))
                        {
                            alreadyQueued++;
                            monitorItems.Add(new(item.Id, displayName));
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Workshop item {item.Id} already has a download queued or running.");
                            ReportInspectionProgress();
                            continue;
                        }

                        if (InvokeDownloadItem(item.Handle))
                        {
                            triggered++;
                            triggeredItems.Add(new(item.Id, displayName));
                            monitorItems.Add(new(item.Id, displayName));
                            MarkTriggeredDownloadItem(item.Id);
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Triggered Steam Workshop update for item {item.Id}.");
                        }
                        else
                        {
                            failed++;
                            RitsuLibFramework.Logger.Warn(
                                $"[SteamWorkshopUpdate] Steam rejected update trigger for item {item.Id}.");
                        }

                        ReportInspectionProgress();
                    }

                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Scan complete. Inspected={inspected}, NeedsUpdate={needsUpdate}, Triggered={triggered}, AlreadyQueued={alreadyQueued}, Failed={failed}.");
                    if (remoteUpdateTimes.Count > 0 || items.Count == 0)
                        SaveRemoteUpdateSnapshot(remoteUpdateTimes, scopedScan);
                    return new(true, inspected, needsUpdate, triggered, alreadyQueued, failed, null, triggeredItems,
                        monitorItems, changedItems);

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

            private static void MarkTriggeredDownloadItem(ulong itemId)
            {
                lock (TriggeredDownloadSyncRoot)
                {
                    TriggeredDownloadItemIds.Add(itemId);
                }
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<bool> MonitorDownloadsAsync(
                IReadOnlyCollection<RitsuSteamWorkshopDownloadItem> downloadItems,
                IProgress<RitsuSteamWorkshopDownloadProgress>? progress,
                bool stopWhenIdle,
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
                HashSet<ulong> loggedUnavailableProgressItems = [];
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
                            if (loggedUnavailableProgressItems.Add(itemId))
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
                    else if (stopWhenIdle && DateTimeOffset.UtcNow - idleSince >= DownloadProgressIdleTimeout)
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

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsAsync(
                CancellationToken cancellationToken)
            {
                var subscribedCount = Convert.ToUInt32(getNumSubscribedItems.Invoke(null, null));
                if (subscribedCount == 0)
                    return [];

                var itemArray = Array.CreateInstance(publishedFileIdType, subscribedCount);
                var actualCount = Convert.ToUInt32(getSubscribedItems.Invoke(null, [itemArray, subscribedCount]));
                var snapshots = await BuildItemSnapshotsAsync(itemArray, actualCount, null, cancellationToken)
                    .ConfigureAwait(false);
                return await BuildWorkshopItemsAsync(snapshots, cancellationToken).ConfigureAwait(false);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<IReadOnlyList<RitsuSteamWorkshopItem>> ListSubscribedItemsFromCacheAsync(
                CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return BuildWorkshopItemsFromCache(ReadSubscribedItemSnapshots());
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<IReadOnlyList<RitsuSteamWorkshopItem>> QueryItemsAsync(
                IReadOnlyCollection<ulong> itemIds,
                CancellationToken cancellationToken)
            {
                var ids = itemIds
                    .Where(static id => id != 0)
                    .Distinct()
                    .ToArray();
                if (ids.Length == 0)
                    return [];

                var snapshots = await BuildItemSnapshotsAsync(ids, null, cancellationToken).ConfigureAwait(false);
                return await BuildWorkshopItemsAsync(snapshots, cancellationToken).ConfigureAwait(false);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal async Task<RitsuSteamWorkshopSearchResult> SearchItemsAsync(
                string query,
                uint page,
                CancellationToken cancellationToken)
            {
                page = Math.Max(1u, page);
                if (!SupportsSearch || string.IsNullOrWhiteSpace(query))
                    return new([], page, SearchResultsPerPage, null);

                var appId = steamAppIdCtor!.Invoke([Const.Sts2SteamAppId]);
                var queryHandle = createQueryAllUgcRequest!.Invoke(null,
                [
                    Enum.Parse(eUgcQueryType!, "k_EUGCQuery_RankedByTextSearch"),
                    Enum.Parse(eUgcMatchingType!, "k_EUGCMatchingUGCType_Items"),
                    appId,
                    appId,
                    page,
                ]);
                if (queryHandle == null)
                    return new([], page, SearchResultsPerPage, null);

                try
                {
                    setSearchText!.Invoke(null, [queryHandle, query.Trim()]);
                    setReturnLongDescription?.Invoke(null, [queryHandle, true]);
                    setAllowCachedResponse.Invoke(null, [queryHandle, 0u]);
                    var apiCall = sendQueryUgcRequest.Invoke(null, [queryHandle]);
                    if (apiCall == null || Convert.ToUInt64(steamApiCallValue.GetValue(apiCall)) == 0)
                    {
                        RitsuLibFramework.Logger.Warn(
                            "[SteamWorkshopUpdate] Steam rejected Workshop search query.");
                        return new([], page, SearchResultsPerPage, null);
                    }

                    var queryCompleted = await WaitForQueryAsync(apiCall, cancellationToken)
                        .ConfigureAwait(false);
                    if (queryCompleted == null)
                        return new([], page, SearchResultsPerPage, null);

                    if (!IsResultOk(queryCompleted))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[SteamWorkshopUpdate] Workshop search query failed: {queryCompletedResult.GetValue(queryCompleted)}.");
                        return new([], page, SearchResultsPerPage, null);
                    }

                    var returned = GetReturnedCount(queryCompleted);
                    var details = ReadQueryResults(queryHandle, returned);
                    var localManifests = BuildLocalManifestByWorkshopItemId();
                    var items = details.Values
                        .Select(detail => BuildWorkshopItem(detail, localManifests))
                        .Where(static item => item.Id != 0)
                        .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(static item => item.Id)
                        .ToArray();
                    return new(items, page, SearchResultsPerPage, GetTotalMatchingResults(queryCompleted));
                }
                finally
                {
                    releaseQueryUgcRequest.Invoke(null, [queryHandle]);
                }
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal RitsuSteamWorkshopActionResult RequestSubscribe(ulong itemId)
            {
                return InvokeItemAction(itemId, subscribeItem, "subscribe");
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal RitsuSteamWorkshopActionResult RequestUnsubscribe(ulong itemId)
            {
                return InvokeItemAction(itemId, unsubscribeItem, "unsubscribe");
            }

            private IReadOnlyList<ItemSnapshot> ReadSubscribedItemSnapshots()
            {
                var subscribedCount = Convert.ToUInt32(getNumSubscribedItems.Invoke(null, null));
                if (subscribedCount == 0)
                    return [];

                var itemArray = Array.CreateInstance(publishedFileIdType, subscribedCount);
                var actualCount = Convert.ToUInt32(getSubscribedItems.Invoke(null, [itemArray, subscribedCount]));
                List<ItemSnapshot> items = [];
                for (var i = 0; i < actualCount; i++)
                {
                    var item = itemArray.GetValue(i);
                    if (item == null)
                        continue;

                    var state = Convert.ToUInt32(getItemState.Invoke(null, [item]));
                    var itemId = GetItemId(item);
                    if (itemId == 0)
                        continue;

                    items.Add(new(
                        item,
                        itemId,
                        state,
                        TryGetInstallSnapshot(item, state)));
                }

                return items;
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
                        TryGetInstallSnapshot(item, state)));
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

            private async Task<IReadOnlyList<RitsuSteamWorkshopItem>> BuildWorkshopItemsAsync(
                IReadOnlyList<ItemSnapshot> snapshots,
                CancellationToken cancellationToken)
            {
                if (snapshots.Count == 0)
                    return [];

                var localManifests = BuildLocalManifestByWorkshopItemId();
                var details = await QueryRemoteUpdateTimesAsync(snapshots, null, cancellationToken)
                    .ConfigureAwait(false);
                return snapshots
                    .Select(snapshot =>
                    {
                        details.TryGetValue(snapshot.Id, out var detail);
                        var manifest = localManifests.TryGetValue(snapshot.Id, out var modManagerManifest)
                            ? modManagerManifest
                            : TryReadLocalManifest(snapshot.Install.FolderPath);
                        return new RitsuSteamWorkshopItem(
                            snapshot.Id,
                            ResolveItemDisplayName(snapshot, detail, manifest),
                            manifest.ModId,
                            manifest.Author,
                            detail.OwnerSteamId,
                            detail.Description,
                            detail.PreviewUrl,
                            HasFlag(snapshot.State, itemStateFlags.Subscribed),
                            HasFlag(snapshot.State, itemStateFlags.Installed),
                            HasFlag(snapshot.State, itemStateFlags.NeedsUpdate),
                            HasFlag(snapshot.State, itemStateFlags.Downloading),
                            HasFlag(snapshot.State, itemStateFlags.DownloadPending),
                            snapshot.Install.LocalTimestamp,
                            detail.Updated == 0 ? null : detail.Updated);
                    })
                    .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static item => item.Id)
                    .ToArray();
            }

            private IReadOnlyList<RitsuSteamWorkshopItem> BuildWorkshopItemsFromCache(
                IReadOnlyList<ItemSnapshot> snapshots)
            {
                if (snapshots.Count == 0)
                    return [];

                var localManifests = BuildLocalManifestByWorkshopItemId();
                var cachedDetails = SteamWorkshopUpdateSnapshotStore.GetItems();
                return snapshots
                    .Select(snapshot =>
                    {
                        var manifest = localManifests.TryGetValue(snapshot.Id, out var modManagerManifest)
                            ? modManagerManifest
                            : TryReadLocalManifest(snapshot.Install.FolderPath);
                        cachedDetails.TryGetValue(snapshot.Id, out var cached);
                        var detail = new RemoteItemDetails(
                            snapshot.Id,
                            cached.Updated,
                            NormalizeString(cached.Title),
                            null,
                            null,
                            null);
                        return new RitsuSteamWorkshopItem(
                            snapshot.Id,
                            ResolveItemDisplayName(snapshot, detail, manifest),
                            manifest.ModId,
                            manifest.Author,
                            null,
                            null,
                            null,
                            HasFlag(snapshot.State, itemStateFlags.Subscribed),
                            HasFlag(snapshot.State, itemStateFlags.Installed),
                            HasFlag(snapshot.State, itemStateFlags.NeedsUpdate),
                            HasFlag(snapshot.State, itemStateFlags.Downloading),
                            HasFlag(snapshot.State, itemStateFlags.DownloadPending),
                            snapshot.Install.LocalTimestamp,
                            cached.Updated == 0 ? null : cached.Updated);
                    })
                    .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static item => item.Id)
                    .ToArray();
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
                        TryGetInstallSnapshot(item, state)));
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
                    setReturnLongDescription?.Invoke(null, [queryHandle, true]);
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
                    if (itemId != 0)
                        detailsByItem[itemId] = new(
                            itemId,
                            updated,
                            ReadItemTitle(detail),
                            ReadItemDescription(detail),
                            TryReadOwnerSteamId(detail),
                            ReadPreviewUrl(queryHandle, i));
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

            private uint? GetTotalMatchingResults(object queryCompleted)
            {
                if (queryCompletedTotalMatching == null)
                    return null;

                var value = queryCompletedTotalMatching.GetValue(queryCompleted);
                return value == null ? null : Convert.ToUInt32(value);
            }

            private bool InvokeDownloadItem(object item)
            {
                try
                {
                    return downloadItem.Invoke(null, [item, true]) is true;
                }
                catch
                {
                    return false;
                }
            }

            private RitsuSteamWorkshopActionResult InvokeItemAction(
                ulong itemId,
                MethodInfo action,
                string actionName)
            {
                if (itemId == 0)
                    return new(true, false, itemId, "Workshop item id must be positive.");

                if (CreatePublishedFileId(itemId) is not { } item)
                    return new(true, false, itemId, "Could not create Steam Workshop item id.");

                try
                {
                    var apiCall = action.Invoke(null, [item]);
                    var accepted = apiCall != null && Convert.ToUInt64(steamApiCallValue.GetValue(apiCall)) != 0;
                    if (!accepted)
                        RitsuLibFramework.Logger.Warn(
                            $"[SteamWorkshopUpdate] Steam rejected Workshop {actionName} request for item {itemId}.");
                    return new(true, accepted, itemId, accepted ? null : "Steam rejected the request.");
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Workshop {actionName} request failed for item {itemId}: {ex.Message}");
                    return new(true, false, itemId, ex.Message);
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

            private string ResolveItemDisplayName(
                ItemSnapshot item,
                RemoteItemDetails remoteDetails,
                IReadOnlyDictionary<ulong, LocalWorkshopManifest> localManifests)
            {
                return ResolveItemDisplayName(
                    item,
                    remoteDetails,
                    localManifests.TryGetValue(item.Id, out var manifest)
                        ? manifest
                        : TryReadLocalManifest(item.Install.FolderPath));
            }

            private static string ResolveItemDisplayName(
                ItemSnapshot item,
                RemoteItemDetails remoteDetails,
                LocalWorkshopManifest manifest)
            {
                if (!string.IsNullOrWhiteSpace(remoteDetails.Title))
                    return remoteDetails.Title;
                if (!string.IsNullOrWhiteSpace(manifest.Name))
                    return manifest.Name;
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (!string.IsNullOrWhiteSpace(manifest.ModId))
                    return manifest.ModId;
                return $"Workshop item {item.Id}";
            }

            private RitsuSteamWorkshopItem BuildWorkshopItem(
                RemoteItemDetails detail,
                IReadOnlyDictionary<ulong, LocalWorkshopManifest> localManifests)
            {
                if (detail.Id == 0 || CreatePublishedFileId(detail.Id) is not { } item)
                    return new(
                        0,
                        "Workshop item 0",
                        null,
                        null,
                        detail.OwnerSteamId,
                        detail.Description,
                        detail.PreviewUrl,
                        false,
                        false,
                        false,
                        false,
                        false,
                        null,
                        detail.Updated == 0 ? null : detail.Updated);

                var state = Convert.ToUInt32(getItemState.Invoke(null, [item]));
                var install = TryGetInstallSnapshot(item, state);
                var manifest = localManifests.TryGetValue(detail.Id, out var modManagerManifest)
                    ? modManagerManifest
                    : TryReadLocalManifest(install.FolderPath);
                return new(
                    detail.Id,
                    string.IsNullOrWhiteSpace(detail.Title)
                        ? !string.IsNullOrWhiteSpace(manifest.Name)
                            ? manifest.Name
                            : $"Workshop item {detail.Id}"
                        : detail.Title,
                    manifest.ModId,
                    manifest.Author,
                    detail.OwnerSteamId,
                    detail.Description,
                    detail.PreviewUrl,
                    HasFlag(state, itemStateFlags.Subscribed),
                    HasFlag(state, itemStateFlags.Installed),
                    HasFlag(state, itemStateFlags.NeedsUpdate),
                    HasFlag(state, itemStateFlags.Downloading),
                    HasFlag(state, itemStateFlags.DownloadPending),
                    install.LocalTimestamp,
                    detail.Updated == 0 ? null : detail.Updated);
            }

            private static IReadOnlyDictionary<ulong, LocalWorkshopManifest> BuildLocalManifestByWorkshopItemId()
            {
                try
                {
                    Dictionary<ulong, LocalWorkshopManifest> manifests = [];
                    foreach (var mod in Sts2ModManagerCompat.BuildModInfos(source: RitsuModSource.SteamWorkshop))
                    {
                        if (mod.WorkshopItemId is not { } workshopItemId || workshopItemId == 0)
                            continue;

                        manifests.TryAdd(workshopItemId, new(mod.Id, mod.Name, mod.Author));
                    }

                    return manifests;
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Failed to read Workshop manifest data from ModManager: {ex.Message}");
                    return new Dictionary<ulong, LocalWorkshopManifest>();
                }
            }

            private string? ReadItemTitle(object details)
            {
                return ReadItemText(details, steamUgcDetailsTitle);
            }

            private string? ReadItemDescription(object details)
            {
                return ReadItemText(details, steamUgcDetailsDescription);
            }

            private string? ReadPreviewUrl(object queryHandle, uint itemIndex)
            {
                if (getQueryUgcPreviewUrl == null)
                    return null;

                try
                {
                    object?[] args = [queryHandle, itemIndex, string.Empty, 1024u];
                    return getQueryUgcPreviewUrl.Invoke(null, args) is true &&
                           args[2] is string url &&
                           !string.IsNullOrWhiteSpace(url)
                        ? url.Trim()
                        : null;
                }
                catch
                {
                    return null;
                }
            }

            private static string? ReadItemText(object details, FieldInfo? field)
            {
                if (field == null)
                    return null;

                try
                {
                    return field.GetValue(details) switch
                    {
                        string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
                        char[] chars => TrimText(new(chars)),
                        byte[] bytes => TrimText(Encoding.UTF8.GetString(bytes)),
                        _ => null,
                    };
                }
                catch
                {
                    return null;
                }
            }

            private static string? TrimText(string text)
            {
                return NormalizeString(text.TrimEnd('\0'));
            }

            private InstalledItemSnapshot TryGetInstallSnapshot(object item, uint state)
            {
                if (!HasFlag(state, itemStateFlags.Installed))
                    return default;

                try
                {
                    object?[] args = [item, 0UL, string.Empty, 4096u, 0u];
                    if (getItemInstallInfo.Invoke(null, args) is not true)
                        return default;

                    return new(
                        args[2] as string,
                        Convert.ToUInt32(args[4]));
                }
                catch
                {
                    return default;
                }
            }

            private ulong? TryReadOwnerSteamId(object details)
            {
                if (steamUgcDetailsOwner == null)
                    return null;

                try
                {
                    var value = steamUgcDetailsOwner.GetValue(details);
                    return value == null ? null : Convert.ToUInt64(value);
                }
                catch
                {
                    return null;
                }
            }

            private static LocalWorkshopManifest TryReadLocalManifest(string? folderPath)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                    return default;

                var key = folderPath.Trim();
                lock (ManifestCacheSyncRoot)
                {
                    if (LocalManifestCache.TryGetValue(key, out var cached))
                        return cached;
                }

                try
                {
                    var manifestPath = FindLocalManifestPath(key);
                    if (manifestPath == null)
                        return Cache(default);

                    using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
                    var root = document.RootElement;
                    return Cache(new(
                        ReadString(root, "id") ?? ReadString(root, "mod_id"),
                        ReadString(root, "name") ?? ReadString(root, "display_name"),
                        ReadAuthor(root)));
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Failed to read local Workshop manifest '{key}': {ex.Message}");
                    return Cache(default);
                }

                LocalWorkshopManifest Cache(LocalWorkshopManifest manifest)
                {
                    lock (ManifestCacheSyncRoot)
                    {
                        LocalManifestCache[key] = manifest;
                    }

                    return manifest;
                }
            }

            private static string? FindLocalManifestPath(string folderPath)
            {
                var direct = Path.Combine(folderPath, "mod_manifest.json");
                return File.Exists(direct) ? direct : null;
            }

            private static string? ReadAuthor(JsonElement root)
            {
                var author = ReadString(root, "author");
                if (!string.IsNullOrWhiteSpace(author))
                    return author;

                if (!root.TryGetProperty("authors", out var authors))
                    return null;

                if (authors.ValueKind == JsonValueKind.String)
                    return NormalizeString(authors.GetString());

                if (authors.ValueKind != JsonValueKind.Array)
                    return null;

                var values = authors.EnumerateArray()
                    .Select(static entry =>
                        entry.ValueKind == JsonValueKind.String ? NormalizeString(entry.GetString()) : null)
                    .Where(static entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();
                return values.Length == 0 ? null : string.Join(", ", values);
            }

            private static string? ReadString(JsonElement root, string propertyName)
            {
                return root.TryGetProperty(propertyName, out var property) &&
                       property.ValueKind == JsonValueKind.String
                    ? NormalizeString(property.GetString())
                    : null;
            }

            private static string? NormalizeString(string? value)
            {
                var trimmed = StripBbCode(value?.Trim() ?? string.Empty).Trim();
                return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
            }

            private static string StripBbCode(string value)
            {
                if (string.IsNullOrEmpty(value) || !value.Contains('['))
                    return value;

                var builder = new StringBuilder(value.Length);
                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] != '[')
                    {
                        builder.Append(value[i]);
                        continue;
                    }

                    var end = value.IndexOf(']', i + 1);
                    if (end < 0 || !IsLikelyBbCodeTag(value.AsSpan(i + 1, end - i - 1)))
                    {
                        builder.Append(value[i]);
                        continue;
                    }

                    i = end;
                }

                return builder.ToString();
            }

            private static bool IsLikelyBbCodeTag(ReadOnlySpan<char> tag)
            {
                tag = tag.Trim();
                if (tag.IsEmpty)
                    return false;

                if (tag[0] == '/')
                    tag = tag[1..].TrimStart();
                if (tag.IsEmpty || !char.IsLetter(tag[0]))
                    return false;

                for (var i = 1; i < tag.Length; i++)
                {
                    var c = tag[i];
                    if (c == '=' || char.IsWhiteSpace(c))
                        return true;
                    if (!char.IsLetterOrDigit(c) && c != '_')
                        return false;
                }

                return true;
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
            uint Subscribed,
            uint Installed,
            uint NeedsUpdate,
            uint Downloading,
            uint DownloadPending)
        {
            internal static ItemStateFlags Default => new(1, 4, 8, 16, 32);
        }

        private sealed record ItemSnapshot(
            object Handle,
            ulong Id,
            uint State,
            InstalledItemSnapshot Install);

        private readonly record struct InstalledItemSnapshot(string? FolderPath, uint? LocalTimestamp);

        private readonly record struct RemoteItemDetails(
            ulong Id,
            uint Updated,
            string? Title,
            string? Description,
            ulong? OwnerSteamId,
            string? PreviewUrl);

        private readonly record struct LocalWorkshopManifest(string? ModId, string? Name, string? Author);

        private sealed record DownloadMonitorItem(object Handle, string DisplayName);
    }
}
