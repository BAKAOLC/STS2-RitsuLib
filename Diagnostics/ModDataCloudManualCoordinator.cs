using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform.Steam;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Diagnostics
{
    internal static class ModDataCloudManualCoordinator
    {
        private static int _manualCloudBusy;

        internal static void TryManualPushFromSettings()
        {
            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (!SteamInitializer.Initialized)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableSteam",
                        "Steam is not active. Run the game through Steam with cloud saves enabled."));
                return;
            }

            if (ModDataCloudMirror.TryGetCloudSaveStore() == null)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableStore",
                        "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                return;
            }

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
                return;

            ModSettingsUiFactory.ShowModCloudSyncScopePicker(
                tree.Root,
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.pushTitle", "Upload to Steam"),
                ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.pushBody",
                    "Choose which mod data to upload."),
                ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionGlobal", "Global only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionProfile", "This profile only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionAll", "Global and this profile"),
                scope =>
                {
                    if (!scope.HasValue)
                        return;
                    _ = RunPushWithProgressAsync(scope.Value);
                });
        }

        internal static void TryManualPullFromSettings()
        {
            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (!SteamInitializer.Initialized)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableSteam",
                        "Steam is not active. Run the game through Steam with cloud saves enabled."));
                return;
            }

            if (ModDataCloudMirror.TryGetCloudSaveStore() == null)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableStore",
                        "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                return;
            }

            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
                return;

            ModSettingsUiFactory.ShowModCloudSyncScopePicker(
                tree.Root,
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.pullTitle", "Download from Steam"),
                ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.pullBody",
                    "Choose which mod data to download."),
                ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionGlobal", "Global only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionProfile", "This profile only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionAll", "Global and this profile"),
                scope =>
                {
                    if (!scope.HasValue)
                        return;
                    _ = RunPullWithProgressAsync(scope.Value);
                });
        }

        internal static void TryClearRegisteredModDataFromSettings()
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root == null)
                return;

            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (!SteamInitializer.Initialized)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableSteam",
                        "Steam is not active. Run the game through Steam with cloud saves enabled."));
                return;
            }

            if (ModDataCloudMirror.TryGetCloudSaveStore() == null)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.unavailableStore",
                        "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                return;
            }

            ModSettingsUiFactory.ShowModCloudSyncScopePicker(
                tree.Root,
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.clearTitle", "Clear from Steam Cloud"),
                ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.clearBody",
                    "Choose which mod data to remove from the cloud. Local files are not deleted."),
                ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionGlobal", "Global only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionProfile", "This profile only"),
                ModSettingsLocalization.Get("ritsulib.modCloud.scope.optionAll", "Global and this profile"),
                scope =>
                {
                    if (!scope.HasValue)
                        return;
                    var chosen = scope.Value;
                    var cancel = ModSettingsLocalization.Get("baselib.restoreDefaults.cancel", "Cancel");
                    var confirm =
                        ModSettingsLocalization.Get("ritsulib.modCloud.clear.confirmAction", "Delete from cloud");
                    var header = ModSettingsLocalization.Get(
                        "ritsulib.modCloud.clear.confirm.title",
                        "Delete mod data from Steam Cloud?");
                    var body = string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.clear.confirmScopeBody",
                            "Scope: {0}. Remote copies only; local files stay. Other profiles are not affected."),
                        GetScopeLabel(chosen));

                    ModSettingsUiFactory.ShowStyledConfirm(
                        tree.Root,
                        header,
                        body,
                        cancel,
                        confirm,
                        true,
                        () => _ = RunClearAsync(chosen));
                });
        }

        private static async Task RunClearAsync(ModCloudSyncScope scope)
        {
            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (Interlocked.CompareExchange(ref _manualCloudBusy, 1, 0) != 0)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.busy",
                        "Another mod cloud operation is already running."));
                return;
            }

            try
            {
                var tree = ResolveSceneTree();
                if (tree == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.noSceneTree",
                            "Cannot start: game scene tree is not available."));
                    return;
                }

                if (!SteamInitializer.Initialized)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.unavailableSteam",
                            "Steam is not active. Run the game through Steam with cloud saves enabled."));
                    return;
                }

                var cloud = ModDataCloudMirror.TryGetCloudSaveStore();
                if (cloud == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.unavailableStore",
                            "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                    return;
                }

                try
                {
                    ProfileManager.Instance.RefreshCurrentProfile();
                }
                catch
                {
                    // best-effort
                }

                var (clearedCloud, fail) =
                    await ModDataCloudMirror.ClearRegisteredPathsFromCloudAsync(tree, cloud, scope);

                var body = string.Format(
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.clearDone",
                        "Removed {0} file(s) from Steam Cloud. Failures: {1}."),
                    clearedCloud,
                    fail);

                RitsuLibFramework.Logger.Info($"[ModCloud][ClearCloud] cloud={clearedCloud} fail={fail}");
                ShowPrompt(title, body);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud][Clear] {ex.Message}");
                ShowPrompt(title,
                    string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.progressFailed",
                            "Operation failed: {0}"),
                        ex.Message));
            }
            finally
            {
                Interlocked.Exchange(ref _manualCloudBusy, 0);
            }
        }

        private static async Task RunPushWithProgressAsync(ModCloudSyncScope scope)
        {
            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (Interlocked.CompareExchange(ref _manualCloudBusy, 1, 0) != 0)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.busy",
                        "Another mod cloud upload or download is already running."));
                return;
            }

            ModDataCloudProgressOverlay? overlay = null;
            try
            {
                var tree = ResolveSceneTree();
                if (tree == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.noSceneTree",
                            "Cannot start: game scene tree is not available."));
                    return;
                }

                var cloud = ModDataCloudMirror.TryGetCloudSaveStore();
                if (cloud == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.unavailableStore",
                            "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                    return;
                }

                try
                {
                    ProfileManager.Instance.RefreshCurrentProfile();
                }
                catch
                {
                    // best-effort
                }

                var paths = await ModDataCloudMirror.CollectDistinctLocalModDataPathsAsync(tree, scope);
                var progressTitle = ModSettingsLocalization.Get(
                    "ritsulib.modCloud.progress.title.push",
                    "Uploading to Steam Cloud…");

                Node host = NGame.Instance != null ? NGame.Instance : tree.Root;
                overlay = ModDataCloudProgressOverlay.Attach(host, Math.Max(1, paths.Count), progressTitle);
                overlay.SetProgress(0, Math.Max(1, paths.Count), paths.Count > 0 ? paths[0] : null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

                var (uploaded, skipped, failed) = await ModDataCloudMirror.PushPathsAsync(
                    cloud,
                    paths,
                    tree,
                    (done, total, cur) => overlay?.SetProgress(done, total, cur));

                var body = string.Format(
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.pushSummary",
                        "Uploaded: {0}. Skipped (no local file): {1}. Failed: {2}."),
                    uploaded,
                    skipped,
                    failed);

                RitsuLibFramework.Logger.Info(
                    $"[ModCloud][ManualPush] uploaded={uploaded} skipped={skipped} failed={failed} scope={scope}");
                ShowPrompt(title, body);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud][ManualPush] {ex.Message}");
                ShowPrompt(title,
                    string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.progressFailed",
                            "Operation failed: {0}"),
                        ex.Message));
            }
            finally
            {
                if (GodotObject.IsInstanceValid(overlay))
                    overlay.Detach();
                Interlocked.Exchange(ref _manualCloudBusy, 0);
            }
        }

        private static async Task RunPullWithProgressAsync(ModCloudSyncScope scope)
        {
            var title = ModSettingsLocalization.Get(
                "ritsulib.modCloud.prompt.title",
                "Mod data (Steam Cloud)");

            if (Interlocked.CompareExchange(ref _manualCloudBusy, 1, 0) != 0)
            {
                ShowPrompt(title,
                    ModSettingsLocalization.Get(
                        "ritsulib.modCloud.busy",
                        "Another mod cloud upload or download is already running."));
                return;
            }

            ModDataCloudProgressOverlay? overlay = null;
            try
            {
                var tree = ResolveSceneTree();
                if (tree == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.noSceneTree",
                            "Cannot start: game scene tree is not available."));
                    return;
                }

                var cloud = ModDataCloudMirror.TryGetCloudSaveStore();
                if (cloud == null)
                {
                    ShowPrompt(title,
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.unavailableStore",
                            "Steam Cloud is not active for this session (same requirement as vanilla cloud saves)."));
                    return;
                }

                try
                {
                    ProfileManager.Instance.RefreshCurrentProfile();
                }
                catch
                {
                    // best-effort
                }

                var paths = await ModDataCloudMirror.CollectDistinctRemoteModDataPathsForPullAsync(tree, scope);
                var progressTitle = ModSettingsLocalization.Get(
                    "ritsulib.modCloud.progress.title.pull",
                    "Downloading from Steam Cloud…");

                Node host = NGame.Instance != null ? NGame.Instance : tree.Root;
                overlay = ModDataCloudProgressOverlay.Attach(host, Math.Max(1, paths.Count), progressTitle);
                overlay.SetProgress(0, Math.Max(1, paths.Count), paths.Count > 0 ? paths[0] : null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

                var (downloaded, failed) = await ModDataCloudMirror.PullPathsAsync(
                    cloud,
                    paths,
                    tree,
                    (done, total, cur) => overlay?.SetProgress(done, total, cur));

                var body = failed == 0
                    ? string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.pullDone",
                            "Downloaded {0} file(s) from Steam Cloud. Mod data was reloaded from disk."),
                        downloaded)
                    : string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.pullPartial",
                            "Downloaded {0} file(s); {1} failed. See game log for details."),
                        downloaded,
                        failed);

                RitsuLibFramework.Logger.Info($"[ModCloud][ManualPull] downloaded={downloaded} failed={failed}");
                ShowPrompt(title, body);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud][ManualPull] {ex.Message}");
                ShowPrompt(title,
                    string.Format(
                        ModSettingsLocalization.Get(
                            "ritsulib.modCloud.progressFailed",
                            "Operation failed: {0}"),
                        ex.Message));
            }
            finally
            {
                if (GodotObject.IsInstanceValid(overlay))
                    overlay.Detach();
                Interlocked.Exchange(ref _manualCloudBusy, 0);
            }
        }

        private static string GetScopeLabel(ModCloudSyncScope scope)
        {
            return scope switch
            {
                ModCloudSyncScope.GlobalOnly => ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.name.global",
                    "Global only"),
                ModCloudSyncScope.ProfileOnly => ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.name.profile",
                    "This profile only"),
                _ => ModSettingsLocalization.Get(
                    "ritsulib.modCloud.scope.name.all",
                    "Global and this profile"),
            };
        }

        private static SceneTree? ResolveSceneTree()
        {
            if (NGame.Instance != null)
                return NGame.Instance.GetTree();

            return Engine.GetMainLoop() as SceneTree;
        }

        private static void ShowPrompt(string title, string message)
        {
            try
            {
                var tree = Engine.GetMainLoop() as SceneTree;
                if (tree?.Root == null)
                    return;

                var dismiss = ModSettingsLocalization.Get("clipboard.pasteErrorOk", "OK");
                ModSettingsUiFactory.ShowStyledNotice(tree.Root, title, message, dismiss);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[ModCloud][Prompt] Failed to show prompt: {ex.Message}");
            }
        }
    }
}
