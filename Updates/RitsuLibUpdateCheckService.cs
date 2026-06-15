using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Updates
{
    internal static class RitsuLibUpdateCheckService
    {
        private const double UpdateCheckToastDurationSeconds = 8.0d;
        private static readonly Uri ManifestUri = new("https://sts2-ritsulib.ritsukage.com/ritsulib-update.json");
        private static readonly Uri ReleasePageFallbackUri = new("https://sts2-ritsulib.ritsukage.com/");
        private static readonly Lock SyncRoot = new();
        private static bool _initialized;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                _initialized = true;
                RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(evt =>
                {
                    if (!RitsuLibSettingsStore.IsUpdateCheckEnabled())
                        return;

                    if (RitsuLibBuildInfo.IsDevBuild)
                    {
                        ShowDevBuildToast();
                        return;
                    }

                    _ = CheckAsync(false);
                });
            }
        }

        internal static void CheckNowFromSettings()
        {
            if (RitsuLibBuildInfo.IsDevBuild)
            {
                PostToMainLoop(ShowDevBuildToast);
                return;
            }

            _ = CheckAsync(true);
        }

        private static async Task CheckAsync(bool showCompletionToast)
        {
            try
            {
                var options = BuildOptions();
                var result = await ModUpdateChecker.CheckAsync(options).ConfigureAwait(false);
                PostToMainLoop(() => ShowResultToast(options, result, showCompletionToast));
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[UpdateCheck] RitsuLib update check failed: {ex.Message}");
                if (showCompletionToast)
                    PostToMainLoop(() => ShowUpdateCheckToast(
                        Format("ritsulib.updateCheck.toast.failed", "Update check failed: {0}", ex.Message),
                        L("ritsulib.updateCheck.toast.title", "RitsuLib update check"),
                        RitsuToastLevel.Warning));
            }
        }

        private static void ShowResultToast(
            ModUpdateCheckOptions options,
            ModUpdateCheckResult result,
            bool showCompletionToast)
        {
            switch (result.Status)
            {
                case ModUpdateCheckStatus.UpdateAvailable when result.ReleasePageUri != null:
                    RitsuToastService.Show(new(
                        result.Message ?? Format(
                            "ritsulib.updateCheck.toast.availableBody",
                            "Version {0} of RitsuLib is available. Click to open the release page.",
                            result.LatestVersion ?? ""),
                        result.Title ?? L("ritsulib.updateCheck.toast.availableTitle", "RitsuLib update available"),
                        null,
                        RitsuToastLevel.Info,
                        options.ToastDurationSeconds,
                        () => OpenReleasePage(result.ReleasePageUri))
                    {
                        IsPersistent = false,
                    });
                    break;

                case ModUpdateCheckStatus.UpToDate when showCompletionToast:
                    ShowUpdateCheckToast(
                        Format(
                            "ritsulib.updateCheck.toast.upToDateBody",
                            "RitsuLib is up to date ({0}).",
                            result.CurrentVersion),
                        L("ritsulib.updateCheck.toast.title", "RitsuLib update check"),
                        RitsuToastLevel.Info);
                    break;

                case ModUpdateCheckStatus.InvalidData:
                case ModUpdateCheckStatus.RequestFailed:
                    RitsuLibFramework.Logger.Warn(
                        $"[UpdateCheck] RitsuLib check skipped: {result.Message ?? result.Status.ToString()}");
                    if (showCompletionToast)
                        ShowUpdateCheckToast(
                            Format(
                                "ritsulib.updateCheck.toast.failed",
                                "Update check failed: {0}",
                                result.Message ?? result.Status.ToString()),
                            L("ritsulib.updateCheck.toast.title", "RitsuLib update check"),
                            RitsuToastLevel.Warning);
                    break;
            }
        }

        private static void ShowUpdateCheckToast(string body, string title, RitsuToastLevel level)
        {
            RitsuToastService.Show(new(
                body,
                title,
                null,
                level,
                UpdateCheckToastDurationSeconds));
        }

        private static void ShowDevBuildToast()
        {
            ShowUpdateCheckToast(
                Format(
                    "ritsulib.updateCheck.toast.devBuildBody",
                    "This is a RitsuLib dev build ({0}). Stable release update checks are skipped.",
                    RitsuLibBuildInfo.InformationalVersion),
                L("ritsulib.updateCheck.toast.devBuildTitle", "RitsuLib dev build"),
                RitsuToastLevel.Info);
        }

        private static void OpenReleasePage(Uri releasePageUri)
        {
            var error = OS.ShellOpen(releasePageUri.ToString());
            if (error != Error.Ok)
                RitsuLibFramework.Logger.Warn(
                    $"[UpdateCheck] Failed to open release page '{releasePageUri}': {error}");
        }

        private static void PostToMainLoop(Action action)
        {
            if (Engine.GetMainLoop() is SceneTree)
            {
                Callable.From(action).CallDeferred();
                return;
            }

            action();
        }

        private static ModUpdateCheckOptions BuildOptions()
        {
            return new()
            {
                ModId = Const.ModId,
                DisplayName = Const.Name,
                CurrentVersion = Const.Version,
                ManifestUri = ManifestUri,
                ReleasePageUri = ReleasePageFallbackUri,
                ToastDurationSeconds = UpdateCheckToastDurationSeconds,
            };
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static string Format(string key, string fallback, params object[] args)
        {
            return string.Format(L(key, fallback), args);
        }
    }
}
