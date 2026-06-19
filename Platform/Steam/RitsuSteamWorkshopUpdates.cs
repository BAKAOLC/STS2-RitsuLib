using System.Reflection;

namespace STS2RitsuLib.Platform.Steam
{
    internal static class RitsuSteamWorkshopUpdates
    {
        private static readonly Lazy<Bindings?> LazyBindings = new(CreateBindings);

        internal static bool IsAvailable => LazyBindings.Value != null;

        internal static RitsuSteamWorkshopUpdateResult TriggerMissingUpdates()
        {
            var bindings = LazyBindings.Value;
            return bindings == null
                ? RitsuSteamWorkshopUpdateResult.Unavailable("Steamworks Workshop bindings are unavailable.")
                : bindings.TriggerMissingUpdates();
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
                if (steamUgc == null || publishedFileIdType == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamWorkshopUpdate] Steamworks Workshop binding unavailable: SteamUGC or PublishedFileId_t type was not found.");
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
                var downloadItem = steamUgc.GetMethod(
                    "DownloadItem",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [publishedFileIdType, typeof(bool)],
                    null);

                if (publishedFileIdCtor == null ||
                    publishedFileIdValue == null ||
                    getNumSubscribedItems == null ||
                    getSubscribedItems == null ||
                    getItemState == null ||
                    downloadItem == null)
                {
                    RitsuLibFramework.Logger.Warn(
                        "[SteamWorkshopUpdate] Steamworks Workshop binding unavailable: required SteamUGC methods or PublishedFileId_t members were not found.");
                    return null;
                }

                var flags = ResolveItemStateFlags(steamworksAssembly);
                RitsuLibFramework.Logger.Info(
                    $"[SteamWorkshopUpdate] Steamworks Workshop binding initialized. Flags: NeedsUpdate={flags.NeedsUpdate}, Downloading={flags.Downloading}, DownloadPending={flags.DownloadPending}.");

                return new(
                    publishedFileIdType,
                    publishedFileIdValue,
                    getNumSubscribedItems,
                    getSubscribedItems,
                    getItemState,
                    downloadItem,
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
            MethodInfo downloadItem,
            ItemStateFlags itemStateFlags)
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal RitsuSteamWorkshopUpdateResult TriggerMissingUpdates()
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
                    var inspected = 0;
                    var needsUpdate = 0;
                    var triggered = 0;
                    var alreadyQueued = 0;
                    var failed = 0;

                    for (var i = 0; i < actualCount; i++)
                    {
                        var item = itemArray.GetValue(i);
                        if (item == null)
                            continue;

                        inspected++;
                        var state = Convert.ToUInt32(getItemState.Invoke(null, [item]));
                        if (!HasFlag(state, itemStateFlags.NeedsUpdate))
                            continue;

                        var itemId = GetItemId(item);
                        needsUpdate++;
                        RitsuLibFramework.Logger.Info(
                            $"[SteamWorkshopUpdate] Workshop item {itemId} needs update. State={state}.");
                        if (HasFlag(state, itemStateFlags.Downloading) ||
                            HasFlag(state, itemStateFlags.DownloadPending))
                        {
                            alreadyQueued++;
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Workshop item {itemId} already has a download queued or running.");
                            continue;
                        }

                        if (InvokeDownloadItem(item))
                        {
                            triggered++;
                            RitsuLibFramework.Logger.Info(
                                $"[SteamWorkshopUpdate] Triggered Steam Workshop update for item {itemId}.");
                        }
                        else
                        {
                            failed++;
                            RitsuLibFramework.Logger.Warn(
                                $"[SteamWorkshopUpdate] Steam rejected update trigger for item {itemId}.");
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
            uint NeedsUpdate,
            uint Downloading,
            uint DownloadPending)
        {
            internal static ItemStateFlags Default => new(8, 16, 32);
        }
    }
}
