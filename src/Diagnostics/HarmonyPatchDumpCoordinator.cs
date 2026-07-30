using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Coordinates manual and first-main-menu Harmony patch dumps using persisted RitsuLib settings.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据持久化的 RitsuLib 设置协调手动转储和首次进入主菜单时的 Harmony 补丁转储。
    ///     </para>
    /// </summary>
    internal static class HarmonyPatchDumpCoordinator
    {
        private static int _autoDumpIssuedForSession;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Handles the deferred call made after
        ///         <see cref="MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu" /> becomes ready. When enabled, an
        ///         automatic dump is attempted at most once per process.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         处理 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu" /> 就绪后的延迟调用。启用该设置
        ///         时，每个进程最多尝试一次自动转储。
        ///     </para>
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
