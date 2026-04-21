using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics
{
    internal static class SelfCheckBundleCoordinator
    {
        private static int _autoRunIssuedForSession;

        internal static void TryAutoRunOnFirstMainMenu()
        {
            var (outputPath, runOnFirstMainMenu) = RitsuLibSettingsStore.GetSelfCheckOptions();
            if (!runOnFirstMainMenu)
                return;

            if (Interlocked.CompareExchange(ref _autoRunIssuedForSession, 1, 0) != 0)
                return;

            TryRunWithConfiguredPath(outputPath, "[SelfCheck][Auto]", false, out _, out _);
        }

        internal static void TryManualRunFromSettings()
        {
            var (outputPath, _) = RitsuLibSettingsStore.GetSelfCheckOptions();
            TryRunWithConfiguredPath(outputPath, "[SelfCheck][Manual]", true, out _, out _);
        }

        internal static bool TryManualRunFromConsole(out string message)
        {
            var (outputPath, _) = RitsuLibSettingsStore.GetSelfCheckOptions();
            return TryRunWithConfiguredPath(outputPath, "[SelfCheck][Console]", false, out _, out message);
        }

        internal static void TryOpenOutputFolderFromSettings()
        {
            var (outputPath, _) = RitsuLibSettingsStore.GetSelfCheckOptions();
            var resolvedOutputDirectory = SelfCheckBundleWriter.TryResolveOutputDirectory(outputPath);
            if (string.IsNullOrEmpty(resolvedOutputDirectory))
            {
                RitsuLibFramework.Logger.Warn(
                    "[SelfCheck][OpenFolder] Output folder is empty or invalid. Configure a valid path in RitsuLib settings.");
                return;
            }

            try
            {
                Directory.CreateDirectory(resolvedOutputDirectory);
                var uri = new Uri(resolvedOutputDirectory + Path.DirectorySeparatorChar).AbsoluteUri;
                var shellOpenError = OS.ShellOpen(uri);
                if (shellOpenError != Error.Ok)
                {
                    RitsuLibFramework.Logger.Warn(
                        $"[SelfCheck][OpenFolder] Failed to open folder '{resolvedOutputDirectory}' (Error: {shellOpenError}).");
                    return;
                }

                RitsuLibFramework.Logger.Info(
                    $"[SelfCheck][OpenFolder] Opened output folder: {resolvedOutputDirectory}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[SelfCheck][OpenFolder] Failed to open output folder '{resolvedOutputDirectory}': {ex.Message}");
            }
        }

        private static bool TryRunWithConfiguredPath(string outputPath, string logPrefix, bool showPrompt,
            out string? zipPath, out string message)
        {
            zipPath = null;
            var promptTitle = ModSettingsLocalization.Get(
                "ritsulib.selfCheck.prompt.title",
                "RitsuLib Self-check");
            var resolvedOutputDirectory = SelfCheckBundleWriter.TryResolveOutputDirectory(outputPath);
            if (string.IsNullOrEmpty(resolvedOutputDirectory))
            {
                RitsuLibFramework.Logger.Warn(
                    $"{logPrefix} Output folder is empty or invalid. Configure a valid path in RitsuLib settings.");
                message = ModSettingsLocalization.Get(
                    "ritsulib.selfCheck.prompt.invalidPath",
                    "Self-check did not run: output folder is empty or invalid. Configure a valid path first.");
                if (showPrompt)
                    ShowCompletionPrompt(promptTitle, message);
                return false;
            }

            RitsuLibFramework.Logger.Info($"{logPrefix} Starting self-check bundle export...");

            if (!SelfCheckBundleWriter.TryWriteBundle(resolvedOutputDirectory, out zipPath, out var error))
            {
                RitsuLibFramework.Logger.Warn($"{logPrefix} Export failed: {error}");
                var failedPattern = ModSettingsLocalization.Get(
                    "ritsulib.selfCheck.prompt.failed",
                    "Self-check export failed: {0}");
                message = string.Format(failedPattern, error);
                if (showPrompt)
                    ShowCompletionPrompt(promptTitle, message);
                return false;
            }

            RitsuLibFramework.Logger.Info($"{logPrefix} Export complete. Zip: {zipPath}");
            var successPattern = ModSettingsLocalization.Get(
                "ritsulib.selfCheck.prompt.success",
                "Self-check complete. Exported zip: {0}");
            message = string.Format(successPattern, NormalizePathForDisplay(zipPath));
            if (showPrompt)
                ShowCompletionPrompt(promptTitle, message);
            return true;
        }

        private static void ShowCompletionPrompt(string title, string message)
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
                RitsuLibFramework.Logger.Warn($"[SelfCheck][Prompt] Failed to show completion prompt: {ex.Message}");
            }
        }

        private static string NormalizePathForDisplay(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path ?? string.Empty;
            return path.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
