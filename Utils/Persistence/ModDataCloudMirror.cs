using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using Steamworks;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings.RunSidecar;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class ModDataCloudMirror
    {
        private const string RunSidecarSegment = "run_sidecar/v1";
        private const int SteamRemoteScanYieldEvery = 32;
        private const int LocalPathCollectYieldEvery = 64;
        private static FieldInfo? _saveStoreField;

        internal static CloudSaveStore? TryGetCloudSaveStore()
        {
            try
            {
                _saveStoreField ??= typeof(SaveManager).GetField("_saveStore",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var store = _saveStoreField?.GetValue(SaveManager.Instance);
                return store as CloudSaveStore;
            }
            catch
            {
                return null;
            }
        }

        internal static bool ShouldRunCloudMirror()
        {
            if (!RitsuLibSettingsStore.IsSyncModDataToCloudEnabled())
                return false;
            if (!SteamInitializer.Initialized)
                return false;
            return TryGetCloudSaveStore() != null;
        }

        internal static void MirrorLocalFileAfterWriteIfEnabled(string godotUserPath)
        {
            if (!ShouldRunCloudMirror())
                return;

            if (!ModAccountRelativePath.TryGetRelativeAccountPath(godotUserPath, out var relative))
                return;

            var pid = ProfileManager.Instance.CurrentProfileId;
            if (!ModCloudSyncPathRegistry.IsRegisteredRelativePath(relative, pid) &&
                !ModRunSidecarStore.IsActiveRunSidecarRelativeAccountPath(relative, pid))
                return;

            Callable.From(() => _ = MirrorAfterWriteOneFileAsync(relative)).CallDeferred();
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

                await cloud.OverwriteCloudWithLocal(relative);
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
            ModCloudSyncPathRegistry.CollectRegisteredRelativePaths(profileId, set, scope);
            if (scope != ModCloudSyncScope.GlobalOnly && profileId >= 0)
                ModRunSidecarStore.AppendActiveRunSidecarSyncRelativePaths(profileId, set);

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

        internal static async Task<(int uploaded, int skipped, int fail)> PushPathsAsync(
            CloudSaveStore cloud,
            IReadOnlyList<string> paths,
            SceneTree tree,
            Action<int, int, string?> reportProgress,
            CancellationToken ct = default)
        {
            var uploaded = 0;
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

                    await cloud.OverwriteCloudWithLocal(path);
                    uploaded++;
                }
                catch (Exception ex)
                {
                    fail++;
                    RitsuLibFramework.Logger.Warn($"[ModCloud][Push] '{path}': {ex.Message}");
                }

                reportProgress(i + 1, total, path);
                await WaitProcessFramesAsync(tree, 1, ct);
            }

            return (uploaded, skipped, fail);
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
            if (!SteamInitializer.Initialized)
                return;

            if (TryGetCloudSaveStore() == null)
                return;

            Callable.From(() => _ = DeleteCloudModDataForProfileAsync(profileId)).CallDeferred();
        }

        private static async Task DeleteCloudModDataForProfileAsync(int profileId)
        {
            if (Engine.GetMainLoop() is not SceneTree tree)
                return;

            var cloud = TryGetCloudSaveStore();
            if (cloud == null)
                return;

            if (!SteamInitializer.Initialized)
                return;

            var exact = new HashSet<string>(StringComparer.Ordinal);
            ModCloudSyncPathRegistry.CollectProfileScopedRegisteredRelativePaths(profileId, exact);

            string? sidecarPrefix = null;
            var profileBase = ProfileManager.GetBasePath(SaveScope.Profile, profileId);
            var sidecarRootUser = $"{profileBase}/{RunSidecarSegment}";
            if (ModAccountRelativePath.TryGetRelativeAccountPath(sidecarRootUser, out var sidecarRootRel))
                sidecarPrefix = sidecarRootRel + "/";

            var n = SteamRemoteStorage.GetFileCount();
            for (var i = 0; i < n; i++)
            {
                if (i > 0 && i % SteamRemoteScanYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1);

                var path = SteamRemoteStorage.GetFileNameAndSize(i, out _).Replace('\\', '/');
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!path.StartsWith("mod_data/", StringComparison.Ordinal))
                    continue;

                var match = exact.Contains(path);
                if (!match && sidecarPrefix != null && path.StartsWith(sidecarPrefix, StringComparison.Ordinal))
                    match = true;

                if (!match)
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
        /// </summary>
        internal static async Task<List<string>> CollectRemoteModDataRelativePathsAsync(SceneTree tree,
            CancellationToken ct = default)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (!SteamInitializer.Initialized)
                return [];

            var n = SteamRemoteStorage.GetFileCount();
            for (var i = 0; i < n; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (i > 0 && i % SteamRemoteScanYieldEvery == 0)
                    await WaitProcessFramesAsync(tree, 1, ct);

                var name = SteamRemoteStorage.GetFileNameAndSize(i, out _).Replace('\\', '/');
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
                            await cloud.OverwriteCloudWithLocal(path);
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
                                await cloud.OverwriteCloudWithLocal(path);
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
    }
}
