using STS2RitsuLib.Diagnostics.Logging;

namespace STS2RitsuLib.Settings
{
    internal static partial class RitsuLibModSettingsBootstrap
    {
        private static void RegisterDebugLogViewerPage(RitsuLibModSettingsUiBindings ui)
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .AsChildOf(Const.ModId)
                    .WithSortOrder(-240)
                    .WithTitle(T("ritsulib.debugLogViewer.title", "Debug log viewer"))
                    .WithDescription(T("ritsulib.debugLogViewer.description",
                        "Configure the browser-based live log viewer. Port changes apply on next game launch."))
                    .AddSection("viewer", section => section
                        .AddToggle(
                            "debug_log_viewer_auto_open",
                            T("ritsulib.debugLogViewer.autoOpen.label", "Open browser automatically"),
                            ui.DebugLogViewerAutoOpen,
                            T("ritsulib.debugLogViewer.autoOpen.description",
                                "When enabled, RitsuLib waits 3 seconds after starting the viewer. If no browser page is listening, it opens one."))
                        .AddIntSlider(
                            "debug_log_viewer_port",
                            T("ritsulib.debugLogViewer.port.label", "Viewer port"),
                            ui.DebugLogViewerPort,
                            1,
                            65535,
                            1,
                            value => value.ToString(),
                            T("ritsulib.debugLogViewer.port.description",
                                "Loopback HTTP port. If the port is busy, RitsuLib tries the following ports in order. Changes apply on next launch."))
                        .AddButton(
                            "debug_log_viewer_open_now",
                            T("ritsulib.debugLogViewer.openNow.label", "Open viewer"),
                            T("ritsulib.debugLogViewer.openNow.button", "Open log viewer"),
                            OpenDebugLogViewerFromSettings,
                            ModSettingsButtonTone.Accent,
                            T("ritsulib.debugLogViewer.openNow.description",
                                "Opens the currently running local log viewer in your system browser."))),
                "debug-log-viewer");
        }

        private static void OpenDebugLogViewerFromSettings()
        {
            var result = RitsuDebugLogPipeline.TryOpenViewerInBrowser();
            if (!result.Success)
                RitsuLibFramework.Logger.Warn($"[DebugLogViewer] {result.Message}");
        }
    }
}
