using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics
{
    /// <summary>
    ///     Orchestrates manual and first-main-menu Harmony patch dumps using persisted RitsuLib settings.
    /// </summary>
    internal static class HarmonyPatchDumpCoordinator
    {
        private static int _autoDumpIssuedForSession;

        /// <summary>
        ///     Invoked deferred from <see cref="MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu" /> readiness; runs at
        ///     most once per process when the setting is enabled.
        /// </summary>
        internal static void TryAutoDumpOnFirstMainMenu()
        {
            var (path, onFirstMainMenu) = RitsuLibSettingsStore.GetHarmonyPatchDumpOptions();
            if (!onFirstMainMenu)
                return;

            if (Interlocked.CompareExchange(ref _autoDumpIssuedForSession, 1, 0) != 0)
                return;

            TryDumpToConfiguredPath(path, "[HarmonyDump][Auto]", false);
        }

        internal static void TryManualDumpFromSettings()
        {
            var (path, _) = RitsuLibSettingsStore.GetHarmonyPatchDumpOptions();
            TryDumpToConfiguredPath(path, "[HarmonyDump][Manual]", true);
        }

        private static void TryDumpToConfiguredPath(string rawPath, string logPrefix, bool showPrompt)
        {
            var promptTitle = ModSettingsLocalization.Get(
                "ritsulib.harmonyDump.prompt.title",
                "Harmony patch dump");
            var resolved = HarmonyPatchDumpWriter.TryResolveFilesystemPath(rawPath);
            if (string.IsNullOrEmpty(resolved))
            {
                RitsuLibFramework.Logger.Warn(
                    $"{logPrefix} Output path is empty or invalid. Set a path in RitsuLib settings (or use Browse).");
                if (!showPrompt) return;
                var message = ModSettingsLocalization.Get(
                    "ritsulib.harmonyDump.prompt.invalidPath",
                    "Export did not run: output path is empty or invalid. Configure a valid path first.");
                ShowPrompt(promptTitle, message);

                return;
            }

            if (!HarmonyPatchDumpWriter.TryWrite(resolved, out var err))
            {
                RitsuLibFramework.Logger.Warn($"{logPrefix} Failed to write dump: {err}");
                if (!showPrompt) return;
                var messagePattern = ModSettingsLocalization.Get(
                    "ritsulib.harmonyDump.prompt.failed",
                    "Export failed: {0}");
                ShowPrompt(promptTitle, string.Format(messagePattern, err));

                return;
            }

            RitsuLibFramework.Logger.Info($"{logPrefix} Wrote Harmony patch dump to: {resolved}");
            if (!showPrompt) return;
            {
                var messagePattern = ModSettingsLocalization.Get(
                    "ritsulib.harmonyDump.prompt.success",
                    "Export complete: {0}");
                ShowPrompt(promptTitle, string.Format(messagePattern, NormalizePathForDisplay(resolved)));
            }
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
                RitsuLibFramework.Logger.Warn($"[HarmonyDump][Prompt] Failed to show result prompt: {ex.Message}");
            }
        }

        private static string NormalizePathForDisplay(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;
            return path.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
