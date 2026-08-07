using Godot;
using STS2RitsuLib.Search;
using STS2RitsuLib.Search.Pinyin;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterPinyinSearchSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            var source = PinyinSearchDataSource.Current;
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf("search-expansions")
                    .WithSortOrder(-1000)
                    .WithTitle(T("ritsulib.searchExtensions.pinyin.page.title", "Mandarin pinyin"))
                    .WithDescription(T("ritsulib.searchExtensions.pinyin.page.description",
                        "Find Chinese text using full pinyin or initials."))
                    .AddSection("pinyin_status", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.status.title", "Status"))
                        .AddParagraph(
                            "pinyin_status_summary",
                            ModSettingsText.Dynamic(FormatPinyinStatus)))
                    .AddSection("pinyin_search", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.search.title", "Pinyin search"))
                        .AddButton(
                            "pinyin_initialize",
                            T("ritsulib.searchExtensions.pinyin.initialize.label", "Initialize pinyin data"),
                            T("ritsulib.searchExtensions.pinyin.initialize.button", "Initialize..."),
                            RequestPinyinInitialization,
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.searchExtensions.pinyin.initialize.description",
                                "Downloads the data needed for pinyin search."))
                        .WithEntryVisibleWhen("pinyin_initialize", static () => PinyinSearchDataManager.Data == null)
                        .WithEntryEnabledWhen("pinyin_initialize",
                            static () => !PinyinSearchDataManager.GetStatus().IsBusy)
                        .AddToggle(
                            "pinyin_search_enabled",
                            T("ritsulib.searchExtensions.pinyin.enabled.label", "Use pinyin in local searches"),
                            ui.PinyinSearchEnabled,
                            T("ritsulib.searchExtensions.pinyin.enabled.description",
                                "Matches Chinese text by full pinyin, alternate readings, or initials."))
                        .WithEntryVisibleWhen("pinyin_search_enabled",
                            static () => PinyinSearchDataManager.Data != null))
                    .AddSection("pinyin_acquisition", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.acquisition.title", "Downloads"))
                        .WithDescription(T("ritsulib.searchExtensions.pinyin.acquisition.description",
                            "Choose how pinyin search data is downloaded and kept."))
                        .AddToggle(
                            "pinyin_automatic_downloads",
                            T("ritsulib.searchExtensions.autoDownload.label", "Restore missing data automatically"),
                            ui.PinyinAutomaticDownloads,
                            T("ritsulib.searchExtensions.autoDownload.description",
                                "Redownload it on startup when pinyin search is enabled and its data is missing."))
                        .AddToggle(
                            "pinyin_keep_source",
                            T("ritsulib.searchExtensions.keepSource.label", "Keep download for offline repair"),
                            ui.PinyinKeepSourceArchive,
                            T("ritsulib.searchExtensions.keepSource.description",
                                "Lets you repair pinyin search later without downloading again."))
                        .AddParagraph(
                            "pinyin_source",
                            ModSettingsText.Dynamic(FormatPinyinSource))
                        .AddButton(
                            "pinyin_open_source",
                            T("ritsulib.searchExtensions.source.label", "Official download"),
                            T("ritsulib.searchExtensions.openSource.button", "Open"),
                            () => OS.ShellOpen(source.SourceUri.AbsoluteUri),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.source.openDescription",
                                "Opens the exact Unicode download URL in your browser.")))
                    .AddSection("pinyin_maintenance", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.maintenance.title", "Repair or remove"))
                        .WithDescription(T("ritsulib.searchExtensions.pinyin.maintenance.description",
                            "Repair pinyin search or remove its downloaded data."))
                        .WithVisibleWhen(HasPinyinCache)
                        .AddButton(
                            "pinyin_reload",
                            T("ritsulib.searchExtensions.pinyin.reload.label", "Repair with a fresh download"),
                            T("ritsulib.searchExtensions.pinyin.reload.button", "Repair..."),
                            RequestPinyinReload,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.pinyin.reload.description",
                                "Downloads a fresh copy and restores pinyin search."))
                        .WithEntryEnabledWhen("pinyin_reload", static () => !PinyinSearchDataManager.GetStatus().IsBusy)
                        .AddButton(
                            "pinyin_rebuild",
                            T("ritsulib.searchExtensions.pinyin.rebuild.label", "Repair without downloading"),
                            T("ritsulib.searchExtensions.rebuild.button", "Repair"),
                            host => RunPinyinOperation(host, PinyinSearchDataManager.RebuildFromCachedSourceAsync),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.pinyin.rebuild.description",
                                "Uses the saved download to restore pinyin search."))
                        .WithEntryVisibleWhen("pinyin_rebuild",
                            static () => PinyinSearchDataManager.GetStatus().SourceArchiveCached)
                        .WithEntryEnabledWhen("pinyin_rebuild",
                            static () => !PinyinSearchDataManager.GetStatus().IsBusy)
                        .AddButton(
                            "pinyin_remove",
                            T("ritsulib.searchExtensions.pinyin.remove.label", "Remove pinyin data"),
                            T("ritsulib.searchExtensions.remove.button", "Remove..."),
                            RequestPinyinRemoval,
                            ModSettingsButtonTone.Danger,
                            T("ritsulib.searchExtensions.pinyin.remove.description",
                                "Turns off pinyin search and deletes its downloaded data."))
                        .WithEntryEnabledWhen("pinyin_remove",
                            static () => !PinyinSearchDataManager.GetStatus().IsBusy)),
                "search-expansions-pinyin");
        }

        private static void RequestPinyinInitialization(IModSettingsUiActionHost host)
        {
            var source = PinyinSearchDataSource.Current;
            ShowPinyinConfirmation(
                host,
                L("ritsulib.searchExtensions.pinyin.initialize.dialog.title", "Initialize Mandarin pinyin?"),
                string.Format(
                    L("ritsulib.searchExtensions.pinyin.initialize.dialog.body",
                        "Download {1} of official Unicode {0} data to enable pinyin search?"),
                    source.UnicodeVersion,
                    FormatBytes(source.ExpectedLength)),
                L("ritsulib.searchExtensions.pinyin.initialize.dialog.confirm", "Download and enable"),
                false,
                () => RunPinyinOperation(
                    host,
                    token => PinyinSearchDataManager.DownloadAndInstallAsync(false, token),
                    static () => RitsuSearchSettingsStore.SetProviderEnabled(
                        PinyinSearchExpansionProvider.ProviderId,
                        true)));
        }

        private static void RequestPinyinReload(IModSettingsUiActionHost host)
        {
            ShowPinyinConfirmation(
                host,
                L("ritsulib.searchExtensions.pinyin.reload.dialog.title", "Repair pinyin search?"),
                L("ritsulib.searchExtensions.pinyin.reload.dialog.body",
                    "Download a fresh copy of the required data?"),
                L("ritsulib.searchExtensions.pinyin.reload.dialog.confirm", "Download and repair"),
                false,
                () => RunPinyinOperation(
                    host,
                    token => PinyinSearchDataManager.DownloadAndInstallAsync(true, token)));
        }

        private static void RequestPinyinRemoval(IModSettingsUiActionHost host)
        {
            ShowPinyinConfirmation(
                host,
                L("ritsulib.searchExtensions.pinyin.remove.dialog.title", "Remove pinyin search data?"),
                L("ritsulib.searchExtensions.pinyin.remove.dialog.body",
                    "Pinyin search will be turned off and its downloaded data will be deleted."),
                L("ritsulib.searchExtensions.pinyin.remove.dialog.confirm", "Remove data"),
                true,
                () => RunPinyinOperation(
                    host,
                    PinyinSearchDataManager.DeleteCachedDataAsync,
                    static () => RitsuSearchSettingsStore.SetProviderEnabled(
                        PinyinSearchExpansionProvider.ProviderId,
                        false)));
        }

        private static void ShowPinyinConfirmation(
            IModSettingsUiActionHost host,
            string title,
            string body,
            string confirmText,
            bool confirmIsDanger,
            Action onConfirm)
        {
            if (host is not ModSettingsUiContext context)
            {
                RitsuLibFramework.Logger.Warn("[Search/Pinyin] Settings confirmation has no compatible UI host.");
                return;
            }

            ModSettingsUiFactory.ShowStyledConfirm(
                context.ModalHost,
                title,
                body,
                L("button.cancel", "Cancel"),
                confirmText,
                confirmIsDanger,
                onConfirm);
        }

        private static async void RunPinyinOperation(
            IModSettingsUiActionHost host,
            Func<CancellationToken, Task> operation,
            Action? onSuccess = null)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(operation);
            if (PinyinSearchDataManager.GetStatus().IsBusy)
                return;

            try
            {
                host.RequestRefresh();
                await PinyinSearchOperationRunner.RunAsync(
                    () => operation(CancellationToken.None),
                    onSuccess,
                    host.RequestRefresh);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn($"[Search/Pinyin] Settings operation failed: {ex.Message}");
            }
            finally
            {
                host.RequestRefresh();
            }
        }

        private static bool HasPinyinCache()
        {
            var status = PinyinSearchDataManager.GetStatus();
            return PinyinSearchDataManager.Data != null || status.CacheBytes > 0 || status.SourceArchiveCached;
        }

        private static string FormatPinyinStatus()
        {
            var status = PinyinSearchDataManager.GetStatus();
            var state = status.State switch
            {
                PinyinSearchDataState.NotInstalled =>
                    L("ritsulib.searchExtensions.status.notInstalled", "Not set up"),
                PinyinSearchDataState.Loading =>
                    L("ritsulib.searchExtensions.status.loading", "Loading"),
                PinyinSearchDataState.Downloading => string.Format(
                    L("ritsulib.searchExtensions.status.downloading", "Downloading: {0} / {1}"),
                    FormatBytes(status.BytesReceived),
                    FormatBytes(status.TotalBytes)),
                PinyinSearchDataState.Generating =>
                    L("ritsulib.searchExtensions.status.generating", "Finishing setup"),
                PinyinSearchDataState.Ready =>
                    L("ritsulib.searchExtensions.status.ready", "Ready"),
                PinyinSearchDataState.Failed => string.Format(
                    L("ritsulib.searchExtensions.status.failed", "Unavailable: {0}"),
                    status.Error ?? L("ritsulib.searchExtensions.status.unknownError", "unknown error")),
                _ => status.State.ToString(),
            };
            var enabled = PinyinSearchDataManager.Data != null &&
                          RitsuSearchSettingsStore.IsProviderEnabled(
                              PinyinSearchExpansionProvider.ProviderId,
                              false)
                ? L("ritsulib.searchExtensions.status.searchEnabled", "Enabled")
                : L("ritsulib.searchExtensions.status.searchDisabled", "Disabled");
            return $"{state}\n{enabled}";
        }

        private static string FormatPinyinSource()
        {
            var source = PinyinSearchDataSource.Current;
            return string.Format(
                L("ritsulib.searchExtensions.source.description",
                    "Unicode {0} · Unihan.zip · {1}\n{2} · Integrity checked automatically"),
                source.UnicodeVersion,
                FormatBytes(source.ExpectedLength),
                source.SourceUri.Host);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024d:F1} KiB";
            return $"{bytes / (1024d * 1024d):F1} MiB";
        }
    }
}
