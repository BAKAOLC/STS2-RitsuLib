using Godot;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Data;
using STS2RitsuLib.Platform.Steam;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class ModDataCloudMirror
    {
        private const int SteamRemoteScanYieldEvery = 32;
        private const int LocalPathCollectYieldEvery = 64;

        private static bool MayEnumerateSteamRemoteStorage =>
            ModDataCloudHost.MayEnumerateNativeSteamRemoteStorage;

        internal static CloudSaveStore? TryGetCloudSaveStore()
        {
            return ModDataCloudHost.TryGetCloudSaveStore();
        }

        internal static bool ShouldRunCloudMirror()
        {
            return RitsuLibSettingsStore.IsSyncModDataToCloudEnabled() && ModDataCloudHost.CanUseModDataCloud();
        }

        internal static void MirrorLocalFileAfterWriteIfEnabled(string godotUserPath)
        {
            if (!ShouldRunCloudMirror())
                return;

            if (!ModAccountRelativePath.TryGetRelativeAccountPath(godotUserPath, out var relative))
                return;

            var pid = ProfileManager.Instance.CurrentProfileId;
            if (!ModCloudSyncPathRegistry.IsRegisteredRelativePath(relative, pid))
                return;

            Callable.From(() => { _ = MirrorAfterWriteOneFileAsync(relative); }).CallDeferred();
        }

        internal static void ScheduleReconcileModDataWithCloud()
        {
            if (!ShouldRunCloudMirror())
                return;

            Callable.From(StartReconcileModDataDeferred).CallDeferred();
        }

        private static void StartReconcileModDataDeferred()
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
                return;

            _ = ReconcileModDataWithCloudAsync(tree);
        }

        private static async Task MirrorAfterWriteOneFileAsync(string relative)
        {
            try
            {
                if (Engine.GetMainLoop() is SceneTree tree)
                    await WaitProcessFramesAsync(tree, 1);

                var cloud = TryGetCloudSaveStore();
                if (cloud == null)
                    return;

                await QueueCloudWriteFromLocalAsync(cloud, relative);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud] push '{relative}': {ex.Message}");
            }
        }

        internal static async Task ReconcileModDataWithCloudAsync(SceneTree tree)
        {
            if (!ShouldRunCloudMirror())
                return;

            var cloud = TryGetCloudSaveStore();
            if (cloud == null)
                return;

            await WaitProcessFramesAsync(tree, 1);

            try
            {
                ProfileManager.Instance.RefreshCurrentProfile();
            }
            catch
            {
                // best-effort before touching paths
            }

            try
            {
                await ReconcileCoreAsync(cloud, tree);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud] reconcile failed: {ex.Message}");
            }

            try
            {
                _ = ModDataStore.ReloadAllIfPathChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud] reload mod data after reconcile: {ex.Message}");
            }
        }

        internal static async Task<List<string>> CollectDistinctLocalModDataPathsAsync(SceneTree tree,
            ModCloudSyncScope scope,
            CancellationToken ct = default)
        {
            var pid = ProfileManager.Instance.CurrentProfileId;
            return await CollectWhitelistRelativePathsForProfileAsync(tree, pid, scope, ct);
        }

        internal static async Task<List<string>> CollectDistinctRemoteModDataPathsForPullAsync(SceneTree tree,
            ModCloudSyncScope scope,
            CancellationToken ct = default)
        {
            var pid = ProfileManager.Instance.CurrentProfileId;
            return await CollectWhitelistRelativePathsForProfileAsync(tree, pid, scope, ct);
        }

        internal static async Task<List<string>> CollectWhitelistRelativePathsForProfileAsync(SceneTree tree,
            int profileId,
            ModCloudSyncScope scope,
            CancellationToken ct = default)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            StorageSyncPathEnumerator.CollectWhitelistedRelativePaths(profileId, set, scope);

            var list = set.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (i > 0 && i % LocalPathCollectYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1, ct);
            }

            return list;
        }

        internal static async Task<(int clearedCloud, int fail)> ClearRegisteredPathsFromCloudAsync(
            SceneTree tree,
            CloudSaveStore cloud,
            ModCloudSyncScope scope,
            CancellationToken ct = default)
        {
            var clearedCloud = 0;
            var fail = 0;
            var pid = ProfileManager.Instance.CurrentProfileId;
            var paths = await CollectWhitelistRelativePathsForProfileAsync(tree, pid, scope, ct);
            var i = 0;

            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();
                if (++i % LocalPathCollectYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1, ct);

                try
                {
                    if (!cloud.CloudStore.FileExists(path))
                        continue;

                    cloud.CloudStore.DeleteFile(path);
                    clearedCloud++;
                }
                catch (Exception ex)
                {
                    fail++;
                    RitsuLibFramework.Logger.Warn($"[ModCloud][ClearCloud] '{path}': {ex.Message}");
                }
            }

            return (clearedCloud, fail);
        }

        internal static async Task<(int queued, int skipped, int fail)> PushPathsAsync(
            CloudSaveStore cloud,
            IReadOnlyList<string> paths,
            SceneTree tree,
            Action<int, int, string?> reportProgress,
            CancellationToken ct = default)
        {
            var queued = 0;
            var skipped = 0;
            var fail = 0;
            var total = paths.Count;
            reportProgress(0, total, total > 0 ? paths[0] : null);

            for (var i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var path = paths[i];
                reportProgress(i, total, path);

                try
                {
                    if (!cloud.LocalStore.FileExists(path))
                    {
                        skipped++;
                        reportProgress(i + 1, total, path);
                        await WaitProcessFramesAsync(tree, 1, ct);
                        continue;
                    }

                    await QueueCloudWriteFromLocalAsync(cloud, path);
                    queued++;
                }
                catch (Exception ex)
                {
                    fail++;
                    RitsuLibFramework.Logger.Warn($"[ModCloud][Push] '{path}': {ex.Message}");
                }

                reportProgress(i + 1, total, path);
                await WaitProcessFramesAsync(tree, 1, ct);
            }

            return (queued, skipped, fail);
        }

        internal static async Task<(int ok, int fail)> PullPathsAsync(
            CloudSaveStore cloud,
            IReadOnlyList<string> paths,
            SceneTree tree,
            Action<int, int, string?> reportProgress,
            CancellationToken ct = default)
        {
            var ok = 0;
            var fail = 0;
            var total = paths.Count;
            reportProgress(0, total, total > 0 ? paths[0] : null);

            for (var i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var path = paths[i];
                reportProgress(i, total, path);

                try
                {
                    if (!cloud.CloudStore.FileExists(path))
                    {
                        reportProgress(i + 1, total, path);
                        await WaitProcessFramesAsync(tree, 1, ct);
                        continue;
                    }

                    var text = await cloud.CloudStore.ReadFileAsync(path);
                    if (text == null)
                    {
                        reportProgress(i + 1, total, path);
                        await WaitProcessFramesAsync(tree, 1, ct);
                        continue;
                    }

                    await cloud.LocalStore.WriteFileAsync(path, text);
                    cloud.LocalStore.SetLastModifiedTime(path, cloud.CloudStore.GetLastModifiedTime(path));
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    RitsuLibFramework.Logger.Warn($"[ModCloud][Pull] '{path}': {ex.Message}");
                }

                reportProgress(i + 1, total, path);
                await WaitProcessFramesAsync(tree, 1, ct);
            }

            ReloadModStoresAfterCloudPull();
            return (ok, fail);
        }

        internal static void ReloadModStoresAfterCloudPull()
        {
            try
            {
                _ = ModDataStore.ReloadAllIfPathChanged();
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud] reload after pull: {ex.Message}");
            }
        }

        internal static async Task WaitProcessFramesAsync(SceneTree tree, int count, CancellationToken ct = default)
        {
            for (var i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }

        internal static void ScheduleDeleteCloudModDataForProfile(int profileId)
        {
            if (TryGetCloudSaveStore() == null)
                return;

            Callable.From(() => { _ = DeleteCloudModDataForProfileAsync(profileId); }).CallDeferred();
        }

        private static async Task DeleteCloudModDataForProfileAsync(int profileId)
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
                return;

            var cloud = TryGetCloudSaveStore();
            if (cloud == null)
                return;

            var exact = new HashSet<string>(StringComparer.Ordinal);
            ModCloudSyncPathRegistry.CollectProfileScopedRegisteredRelativePaths(profileId, exact);

            if (!MayEnumerateSteamRemoteStorage)
            {
                foreach (var path in exact)
                    try
                    {
                        if (!cloud.CloudStore.FileExists(path))
                            continue;
                        cloud.CloudStore.DeleteFile(path);
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ModCloud] delete cloud '{path}' (registered paths only): {ex.Message}");
                    }

                return;
            }

            if (!RitsuLibSteamworks.TryGetRemoteFileCount(out var n))
                return;

            for (var i = 0; i < n; i++)
            {
                if (i > 0 && i % SteamRemoteScanYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1);

                if (!RitsuLibSteamworks.TryGetRemoteFileNameAndSize(i, out var path))
                    continue;

                path = path.Replace('\\', '/');
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!path.StartsWith("mod_data/", StringComparison.Ordinal))
                    continue;

                if (!exact.Contains(path))
                    continue;

                try
                {
                    cloud.CloudStore.DeleteFile(path);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[ModCloud] delete cloud '{path}': {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     Enumerates every <c>mod_data/</c> path present on Steam Remote Storage. No settings UI uses this yet;
        ///     reserved for a future full cloud file manager (browse / delete unrelated keys, etc.).
        ///     枚举 Steam Remote Storage 上存在的每个 <c>mod_data/</c> 路径。当前还没有设置 UI 使用它；
        ///     保留给未来完整云文件管理器使用（浏览 / 删除无关键等）。
        /// </summary>
        internal static async Task<List<string>> CollectRemoteModDataRelativePathsAsync(SceneTree tree,
            CancellationToken ct = default)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (!MayEnumerateSteamRemoteStorage || !RitsuLibSteamworks.TryGetRemoteFileCount(out var n))
                return [];

            for (var i = 0; i < n; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (i > 0 && i % SteamRemoteScanYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1, ct);

                if (!RitsuLibSteamworks.TryGetRemoteFileNameAndSize(i, out var name))
                    continue;

                name = name.Replace('\\', '/');
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!name.StartsWith("mod_data/", StringComparison.Ordinal))
                    continue;
                set.Add(name);
            }

            return set.OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        private static async Task ReconcileCoreAsync(CloudSaveStore cloud, SceneTree tree)
        {
            var profileId = ProfileManager.Instance.CurrentProfileId;
            var paths = await CollectWhitelistRelativePathsForProfileAsync(tree, profileId, ModCloudSyncScope.All);

            foreach (var path in paths)
            {
                try
                {
                    var cloudExists = cloud.CloudStore.FileExists(path);
                    var localExists = cloud.LocalStore.FileExists(path);
                    switch (cloudExists)
                    {
                        case false when !localExists:
                            await WaitProcessFramesAsync(tree, 1);
                            continue;
                        case true when !localExists:
                        {
                            var text = await cloud.CloudStore.ReadFileAsync(path);
                            if (text == null)
                            {
                                await WaitProcessFramesAsync(tree, 1);
                                continue;
                            }

                            await cloud.LocalStore.WriteFileAsync(path, text);
                            cloud.LocalStore.SetLastModifiedTime(path, cloud.CloudStore.GetLastModifiedTime(path));
                            await WaitProcessFramesAsync(tree, 1);
                            continue;
                        }
                        case false when localExists:
                            await QueueCloudWriteFromLocalAsync(cloud, path);
                            await WaitProcessFramesAsync(tree, 1);
                            continue;
                        case true when localExists:
                        {
                            var ct = cloud.CloudStore.GetLastModifiedTime(path);
                            var lt = cloud.LocalStore.GetLastModifiedTime(path);
                            if (ct > lt)
                            {
                                var text = await cloud.CloudStore.ReadFileAsync(path);
                                if (text == null)
                                {
                                    await WaitProcessFramesAsync(tree, 1);
                                    continue;
                                }

                                await cloud.LocalStore.WriteFileAsync(path, text);
                                cloud.LocalStore.SetLastModifiedTime(path, ct);
                            }
                            else if (lt > ct)
                            {
                                await QueueCloudWriteFromLocalAsync(cloud, path);
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.Logger.Warn($"[ModCloud] reconcile '{path}': {ex.Message}");
                }

                await WaitProcessFramesAsync(tree, 1);
            }
        }

        private static async Task QueueCloudWriteFromLocalAsync(CloudSaveStore cloud, string path)
        {
            if (!cloud.LocalStore.FileExists(path))
                throw new FileNotFoundException($"Local mod data file not found: {path}");

            var content = await cloud.LocalStore.ReadFileAsync(path);
            if (content == null)
                throw new InvalidOperationException($"Local mod data file could not be read: {path}");

            await cloud.CloudStore.WriteFileAsync(path, content);
            TrySyncLocalTimestampFromCloud(cloud, path);
        }

        private static void TrySyncLocalTimestampFromCloud(CloudSaveStore cloud, string path)
        {
            try
            {
                cloud.LocalStore.SetLastModifiedTime(path, cloud.CloudStore.GetLastModifiedTime(path));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud] sync timestamp '{path}': {ex.Message}");
            }
        }
    }
}
