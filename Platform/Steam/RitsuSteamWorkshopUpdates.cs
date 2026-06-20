using System.Linq.Expressions;
using System.Reflection;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class RitsuSteamWorkshopUpdates
    {
        private const int QueryBatchSize = 50;
        private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);
        private static readonly Lazy<Bindings?> LazyBindings = new(CreateBindings);

        internal static bool IsAvailable => LazyBindings.Value != null;

        internal static Task<RitsuSteamWorkshopUpdateResult> TriggerMissingUpdatesAsync(
            CancellationToken cancellationToken = default)
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? Task.FromResult(
                    RitsuSteamWorkshopUpdateResult.Unavailable("Steamworks Workshop bindings are unavailable."))
                : bindings.TriggerMissingUpdatesAsync(cancellationToken);
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
                var downloadItem = steamUgc.GetMethod(
                    "DownloadItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType, typeof(bool)],
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
                    downloadItem == null ||
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
                    downloadItem,
                    createQueryUgcDetailsRequest,
                    sendQueryUgcRequest,
                    getQueryUgcResult,
                    releaseQueryUgcRequest,
                    setAllowCachedResponse,
                    steamUgcDetailsType,
                    steamUgcDetailsItemId,
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
            MethodInfo downloadItem,
            MethodInfo createQueryUgcDetailsRequest,
            MethodInfo sendQueryUgcRequest,
            MethodInfo getQueryUgcResult,
            MethodInfo releaseQueryUgcRequest,
            MethodInfo setAllowCachedResponse,
            Type steamUgcDetailsType,
            FieldInfo steamUgcDetailsItemId,
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
                CancellationToken cancellationToken)
            {
                try
                {
                    var subscribedCount = Convert.ToUInt32(getNumSubscribedItems.Invoke(null, null));
                    if (subscribedCount == 0)
                    {
                        RitsuLibFramework.Logger.Info(
                            "[SteamWorkshopUpdate] Scan complete: no subscribed Workshop items.");
                        return new(true, 0, 0, 0, 0, 0);
                    }

                    var itemArray = Array.CreateInstance(publishedFileIdType, subscribedCount);
                    var actualCount = Convert.ToUInt32(getSubscribedItems.Invoke(null, [itemArray, subscribedCount]));
                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Scanning subscribed Workshop items. Reported={subscribedCount}, Returned={actualCount}.");

                    var items = BuildItemSnapshots(itemArray, actualCount);
                    var remoteUpdateTimes = await QueryRemoteUpdateTimesAsync(items, cancellationToken)
                        .ConfigureAwait(false);
                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Refreshed Workshop details for {remoteUpdateTimes.Count}/{items.Count} subscribed item(s).");

                    var inspected = 0;
                    var needsUpdate = 0;
                    var triggered = 0;
                    var alreadyQueued = 0;
                    var failed = 0;

                    foreach (var item in items)
                    {
                        inspected++;
                        var hasRemoteUpdated = remoteUpdateTimes.TryGetValue(item.Id, out var remoteUpdated);
                        var stateNeedsUpdate = HasFlag(item.State, itemStateFlags.NeedsUpdate);
                        var remoteNeedsUpdate = hasRemoteUpdated &&
                                                item.LocalTimestamp is { } localTimestamp &&
                                                remoteUpdated > localTimestamp;
                        if (!stateNeedsUpdate && !remoteNeedsUpdate)
                            continue;

                        needsUpdate++;
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Workshop item {item.Id} needs update. State={item.State}, LocalTimestamp={item.LocalTimestamp?.ToString() ?? "<unknown>"}, RemoteUpdated={(hasRemoteUpdated ? remoteUpdated.ToString() : "<unknown>")}.");
                        if (HasFlag(item.State, itemStateFlags.Downloading) ||
                            HasFlag(item.State, itemStateFlags.DownloadPending))
                        {
                            alreadyQueued++;
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Workshop item {item.Id} already has a download queued or running.");
                            continue;
                        }

                        if (InvokeDownloadItem(item.Handle))
                        {
                            triggered++;
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Triggered Steam Workshop update for item {item.Id}.");
                        }
                        else
                        {
                            failed++;
                            RitsuLibFramework.Logger.Warn(
                                $"[SteamWorkshopUpdate] Steam rejected update trigger for item {item.Id}.");
                        }
                    }

                    RitsuLibFramework.Logger.Info(
                        $"[SteamWorkshopUpdate] Scan complete. Inspected={inspected}, NeedsUpdate={needsUpdate}, Triggered={triggered}, AlreadyQueued={alreadyQueued}, Failed={failed}.");
                    return new(true, inspected, needsUpdate, triggered, alreadyQueued, failed);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[SteamWorkshopUpdate] Check failed: {ex.Message}");
                    return RitsuSteamWorkshopUpdateResult.Unavailable(ex.Message);
                }
            }

            private IReadOnlyList<ItemSnapshot> BuildItemSnapshots(Array itemArray, uint actualCount)
            {
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
                        TryGetLocalTimestamp(item, state)));
                }

                return items;
            }

            private async Task<IReadOnlyDictionary<ulong, uint>> QueryRemoteUpdateTimesAsync(
                IReadOnlyList<ItemSnapshot> items,
                CancellationToken cancellationToken)
            {
                if (items.Count == 0)
                    return new Dictionary<ulong, uint>();

                Dictionary<ulong, uint> updateTimes = [];
                for (var offset = 0; offset < items.Count; offset += QueryBatchSize)
                {
                    var batch = items
                        .Skip(offset)
                        .Take(QueryBatchSize)
                        .ToArray();
                    var batchTimes = await QueryRemoteUpdateTimesBatchAsync(batch, cancellationToken)
                        .ConfigureAwait(false);
                    foreach (var (itemId, updated) in batchTimes)
                        updateTimes[itemId] = updated;
                }

                return updateTimes;
            }

            private async Task<IReadOnlyDictionary<ulong, uint>> QueryRemoteUpdateTimesBatchAsync(
                IReadOnlyList<ItemSnapshot> items,
                CancellationToken cancellationToken)
            {
                var itemArray = Array.CreateInstance(publishedFileIdType, items.Count);
                for (var i = 0; i < items.Count; i++)
                    itemArray.SetValue(items[i].Handle, i);

                var queryHandle = createQueryUgcDetailsRequest.Invoke(null, [itemArray, (uint)items.Count]);
                if (queryHandle == null)
                    return new Dictionary<ulong, uint>();

                try
                {
                    setAllowCachedResponse.Invoke(null, [queryHandle, 0u]);
                    var apiCall = sendQueryUgcRequest.Invoke(null, [queryHandle]);
                    if (apiCall == null || Convert.ToUInt64(steamApiCallValue.GetValue(apiCall)) == 0)
                    {
                        RitsuLibFramework.Logger.Warn(
                            "[SteamWorkshopUpdate] Steam rejected Workshop details query.");
                        return new Dictionary<ulong, uint>();
                    }

                    var queryCompleted = await WaitForQueryAsync(apiCall, cancellationToken)
                        .ConfigureAwait(false);
                    if (queryCompleted == null)
                        return new Dictionary<ulong, uint>();

                    if (IsResultOk(queryCompleted))
                        return ReadQueryResults(queryHandle, GetReturnedCount(queryCompleted));
                    RitsuLibFramework.Logger.Warn(
                        $"[SteamWorkshopUpdate] Workshop details query failed: {queryCompletedResult.GetValue(queryCompleted)}.");
                    return new Dictionary<ulong, uint>();
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

            private IReadOnlyDictionary<ulong, uint> ReadQueryResults(object queryHandle, uint returnedCount)
            {
                Dictionary<ulong, uint> updateTimes = [];
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
                    var updated = Convert.ToUInt32(steamUgcDetailsUpdated.GetValue(args[2]));
                    if (itemId != 0 && updated != 0)
                        updateTimes[itemId] = updated;
                }

                return updateTimes;
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
    }
}
