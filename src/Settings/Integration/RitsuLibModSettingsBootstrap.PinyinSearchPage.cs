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
                        "Initialize and configure pinyin matching for RitsuLib local searches."))
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
                                "Downloads the verified Unicode source and generates the local search cache after confirmation."))
                        .WithEntryVisibleWhen("pinyin_initialize", static () => PinyinSearchDataManager.Data == null)
                        .WithEntryEnabledWhen("pinyin_initialize",
                            static () => !PinyinSearchDataManager.GetStatus().IsBusy)
                        .AddToggle(
                            "pinyin_search_enabled",
                            T("ritsulib.searchExtensions.pinyin.enabled.label", "Use pinyin in local searches"),
                            ui.PinyinSearchEnabled,
                            T("ritsulib.searchExtensions.pinyin.enabled.description",
                                "Adds full pinyin, alternate readings, and initials without changing original-text matching."))
                        .WithEntryVisibleWhen("pinyin_search_enabled",
                            static () => PinyinSearchDataManager.Data != null))
                    .AddSection("pinyin_acquisition", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.acquisition.title", "Data acquisition"))
                        .WithDescription(T("ritsulib.searchExtensions.pinyin.acquisition.description",
                            "The reading table is never bundled with RitsuLib. Downloads use the pinned source shown below."))
                        .AddToggle(
                            "pinyin_automatic_downloads",
                            T("ritsulib.searchExtensions.autoDownload.label", "Automatically restore missing data"),
                            ui.PinyinAutomaticDownloads,
                            T("ritsulib.searchExtensions.autoDownload.description",
                                "When pinyin search is enabled, fetch and regenerate its data on startup if the local cache is missing."))
                        .AddToggle(
                            "pinyin_keep_source",
                            T("ritsulib.searchExtensions.keepSource.label", "Keep downloaded Unicode source archive"),
                            ui.PinyinKeepSourceArchive,
                            T("ritsulib.searchExtensions.keepSource.description",
                                "Retain the verified source archive so the compact cache can be regenerated without another download."))
                        .AddParagraph(
                            "pinyin_source",
                            ModSettingsText.Dynamic(FormatPinyinSource))
                        .AddButton(
                            "pinyin_open_source",
                            T("ritsulib.searchExtensions.source.label", "Official Unicode source"),
                            T("ritsulib.searchExtensions.openSource.button", "Open source"),
                            () => OS.ShellOpen(source.SourceUri.AbsoluteUri),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.source.openDescription",
                                "Opens the exact source URL in your browser.")))
                    .AddSection("pinyin_maintenance", section => section
                        .WithTitle(T("ritsulib.searchExtensions.pinyin.maintenance.title", "Data maintenance"))
                        .WithDescription(T("ritsulib.searchExtensions.pinyin.maintenance.description",
                            "Reload, regenerate, or remove data after initialization."))
                        .WithVisibleWhen(HasPinyinCache)
                        .AddButton(
                            "pinyin_reload",
                            T("ritsulib.searchExtensions.pinyin.reload.label", "Reload pinyin data"),
                            T("ritsulib.searchExtensions.pinyin.reload.button", "Reload..."),
                            RequestPinyinReload,
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.pinyin.reload.description",
                                "Verifies the retained source or downloads it again, then replaces the generated cache."))
                        .WithEntryEnabledWhen("pinyin_reload", static () => !PinyinSearchDataManager.GetStatus().IsBusy)
                        .AddButton(
                            "pinyin_rebuild",
                            T("ritsulib.searchExtensions.pinyin.rebuild.label", "Regenerate from retained source"),
                            T("ritsulib.searchExtensions.rebuild.button", "Regenerate"),
                            host => RunPinyinOperation(host, PinyinSearchDataManager.RebuildFromCachedSourceAsync),
                            ModSettingsButtonTone.Normal,
                            T("ritsulib.searchExtensions.pinyin.rebuild.description",
                                "Rebuilds the generated cache without downloading the source again."))
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
                                "Disables pinyin search and removes its generated cache and retained source."))
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
                        "RitsuLib will download Unicode {0} Unihan.zip ({1}) from unicode.org, verify it, and generate a local search cache. Continue?"),
                    source.UnicodeVersion,
                    FormatBytes(source.ExpectedLength)),
                L("ritsulib.searchExtensions.pinyin.initialize.dialog.confirm", "Download and initialize"),
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
                L("ritsulib.searchExtensions.pinyin.reload.dialog.title", "Reload Mandarin pinyin data?"),
                L("ritsulib.searchExtensions.pinyin.reload.dialog.body",
                    "The current generated cache will be replaced after the pinned source is verified or downloaded again."),
                L("ritsulib.searchExtensions.pinyin.reload.dialog.confirm", "Reload data"),
                false,
                () => RunPinyinOperation(
                    host,
                    token => PinyinSearchDataManager.DownloadAndInstallAsync(true, token)));
        }

        private static void RequestPinyinRemoval(IModSettingsUiActionHost host)
        {
            ShowPinyinConfirmation(
                host,
                L("ritsulib.searchExtensions.pinyin.remove.dialog.title", "Remove Mandarin pinyin data?"),
                L("ritsulib.searchExtensions.pinyin.remove.dialog.body",
                    "Pinyin matching will be disabled. The generated cache and any retained Unicode source archive will be removed."),
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
                    L("ritsulib.searchExtensions.status.notInstalled", "Uninitialized"),
                PinyinSearchDataState.Loading =>
                    L("ritsulib.searchExtensions.status.loading", "Loading local cache"),
                PinyinSearchDataState.Downloading => string.Format(
                    L("ritsulib.searchExtensions.status.downloading", "Downloading: {0} / {1}"),
                    FormatBytes(status.BytesReceived),
                    FormatBytes(status.TotalBytes)),
                PinyinSearchDataState.Generating =>
                    L("ritsulib.searchExtensions.status.generating", "Generating local search cache"),
                PinyinSearchDataState.Ready => string.Format(
                    L("ritsulib.searchExtensions.status.ready", "Ready · Unicode {0} · cached {1}"),
                    status.UnicodeVersion,
                    FormatBytes(status.CacheBytes)),
                PinyinSearchDataState.Failed => string.Format(
                    L("ritsulib.searchExtensions.status.failed", "Needs attention: {0}"),
                    status.Error ?? L("ritsulib.searchExtensions.status.unknownError", "unknown error")),
                _ => status.State.ToString(),
            };
            var enabled = PinyinSearchDataManager.Data != null &&
                          RitsuSearchSettingsStore.IsProviderEnabled(
                              PinyinSearchExpansionProvider.ProviderId,
                              false)
                ? L("ritsulib.searchExtensions.status.searchEnabled", "Pinyin matching is enabled.")
                : L("ritsulib.searchExtensions.status.searchDisabled", "Pinyin matching is disabled.");
            return $"{state}\n{enabled}";
        }

        private static string FormatPinyinSource()
        {
            var source = PinyinSearchDataSource.Current;
            var retained = PinyinSearchDataManager.GetStatus().SourceArchiveCached
                ? L("ritsulib.searchExtensions.status.sourceRetained", "Source archive retained")
                : L("ritsulib.searchExtensions.status.sourceNotRetained", "Source archive not retained");
            return string.Format(
                L("ritsulib.searchExtensions.source.description",
                    "Unicode {0} Unihan.zip · {1}\nPinned SHA-256: {2}\n{3}"),
                source.UnicodeVersion,
                FormatBytes(source.ExpectedLength),
                source.ExpectedSha256,
                retained);
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
