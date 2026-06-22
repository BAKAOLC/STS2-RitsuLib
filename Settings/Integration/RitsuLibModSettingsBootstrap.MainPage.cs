using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content.Patches;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Ui.Toast;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterMainSettingsPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(Const.ModId, page => page
                .WithModDisplayName(T("ritsulib.mod.displayName", "RitsuLib"))
                .WithModSidebarOrder(-10_000)
                .WithTitle(T("ritsulib.page.title", "Settings"))
                .WithDescription(T("ritsulib.page.description",
                    "Framework settings and settings UI reference entries."))
                .WithSortOrder(-1000)
                .AddSection("categories", section => section
                    .WithTitle(T("ritsulib.section.categories.title", "Categories"))
                    .AddSubpage(
                        "category_core_open",
                        T("ritsulib.category.core.label", "Core settings"),
                        "core",
                        T("button.open", "Open"),
                        T("ritsulib.category.core.description",
                            "Interface theme, main menu shortcut, and deterministic ModelDb controls."))
                    .AddSubpage(
                        "category_content_source_open",
                        T("ritsulib.modSourceHoverTips.pageLink.label", "Content source display"),
                        "content-source-hover-tips",
                        T("button.open", "Open"),
                        T("ritsulib.modSourceHoverTips.pageLink.description",
                            "Choose which content groups show source hover tips."))
                    .AddSubpage(
                        "category_content_load_order_open",
                        T("ritsulib.contentModLoadOrder.pageLink.label", "Content mod load order"),
                        "content-mod-load-order",
                        T("button.open", "Open"),
                        T("ritsulib.contentModLoadOrder.pageLink.description",
                            "Sort, copy, or apply the saved load order for relevant content mods, framework libraries, and dependencies."))
                    .AddSubpage(
                        "category_toast_open",
                        T("ritsulib.toast.pageLink.label", "Toast notifications"),
                        "toast",
                        T("button.open", "Open"),
                        T("ritsulib.toast.pageLink.description",
                            "Configure stack placement, queue limits, and animation for global toast notifications."))
                    .AddSubpage(
                        "category_compatibility_open",
                        T("ritsulib.category.compatibility.label", "Compatibility fallbacks"),
                        "compatibility",
                        T("button.open", "Open"),
                        T("ritsulib.category.compatibility.description",
                            "Debug compatibility mode and fallback shims."))
                    .AddSubpage(
                        "category_updates_open",
                        T("ritsulib.category.updates.label", "Updates"),
                        "updates",
                        T("button.open", "Open"),
                        T("ritsulib.category.updates.description",
                            "RitsuLib and Steam Workshop update checks."))
                    .AddSubpage(
                        "category_cloud_open",
                        T("ritsulib.category.cloud.label", "Steam Cloud"),
                        "cloud",
                        T("button.open", "Open"),
                        T("ritsulib.category.cloud.description",
                            "Mod data sync and manual Steam Cloud actions."))
                    .AddSubpage(
                        "category_developer_tools_open",
                        T("ritsulib.category.developerTools.label", "Developer tools"),
                        "developer-tools",
                        T("button.open", "Open"),
                        T("ritsulib.category.developerTools.description",
                            "Console fixes, diagnostics, self-checks, and export tools."))
                    .AddSubpage(
                        "category_runtime_hotkeys_open",
                        T("ritsulib.page.runtimeHotkeys.title", "Registered hotkeys"),
                        "runtime-hotkeys",
                        T("button.open", "Open"),
                        T("ritsulib.page.runtimeHotkeys.description",
                            "Inspect currently registered runtime hotkeys and their active bindings."))
                    .AddSubpage(
                        "category_control_preview_open",
                        T("ritsulib.reference.gallery.label", "Control preview"),
                        "debug-showcase",
                        T("button.open", "Open"),
                        T("ritsulib.reference.gallery.description",
                            "Reference page only. Values on this page are not persisted."))));
        }

        private static void TryResetExistingThemeFiles()
        {
            if (!RitsuShellThemeCatalog.TryRestoreAllExistingDiskThemesFromEmbedded(out _))
                return;
            RitsuShellThemeRuntime.ReapplyActiveTheme(true);
        }

        private static void TryRebuildModelDbDeterministicCacheFromSettings()
        {
            var title = L("ritsulib.modelDbDeterministicSort.toast.title", "ModelDb deterministic sort");
            if (!CanManuallyRebuildModelDbDeterministicCache())
            {
                var body = L(
                    "ritsulib.modelDbDeterministicSort.toast.blockedActiveSession",
                    "ModelDb deterministic sort can only be applied before a run, lobby, or network session starts.");
                RitsuLibFramework.Logger.Warn(
                    "[ModelIdSerializationCache] Manual deterministic rebuild blocked: active run, lobby, or network session.");
                RitsuToastService.ShowWarning(body, title);
                return;
            }

            var result = ModelIdSerializationCacheDynamicContentPatch.RebuildDeterministicCacheForSettings();
            if (!result.Applied)
            {
                var reason = result.Reason ?? L("ritsulib.modelDbDeterministicSort.toast.notAppliedUnknown",
                    "The cache could not be rebuilt.");
                RitsuLibFramework.Logger.Warn(
                    $"[ModelIdSerializationCache] Manual deterministic rebuild skipped: {reason}");
                RitsuToastService.ShowWarning(
                    string.Format(
                        L("ritsulib.modelDbDeterministicSort.toast.notApplied",
                            "ModelDb deterministic sort was not applied: {0}"),
                        reason),
                    title);
                return;
            }

            RitsuLibFramework.Logger.Info(
                $"[ModelIdSerializationCache] Manual deterministic rebuild applied. Hash: {result.InitialHash} -> {result.FinalHash}.");
            RitsuToastService.ShowInfo(
                string.Format(
                    L("ritsulib.modelDbDeterministicSort.toast.applied",
                        "ModelDb deterministic sort applied. Hash: {0} -> {1}."),
                    result.InitialHash,
                    result.FinalHash),
                title);
        }

        private static bool CanManuallyRebuildModelDbDeterministicCache()
        {
            try
            {
                var runManager = RunManager.Instance;
                if (runManager == null)
                    return true;

                return runManager is not
                {
                    IsInProgress: true,
                } && runManager.RunLobby == null && runManager.NetService?.IsConnected != true;
            }
            catch
            {
                return false;
            }
        }
    }
}
