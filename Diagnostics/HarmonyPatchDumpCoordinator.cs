using STS2RitsuLib.Data;

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

            TryDumpToConfiguredPath(path, "[HarmonyDump][Auto]");
        }

        internal static void TryManualDumpFromSettings()
        {
            var (path, _) = RitsuLibSettingsStore.GetHarmonyPatchDumpOptions();
            TryDumpToConfiguredPath(path, "[HarmonyDump][Manual]");
        }

        private static void TryDumpToConfiguredPath(string rawPath, string logPrefix)
        {
            var resolved = HarmonyPatchDumpWriter.TryResolveFilesystemPath(rawPath);
            if (string.IsNullOrEmpty(resolved))
            {
                RitsuLibFramework.Logger.Warn(
                    $"{logPrefix} Output path is empty or invalid. Set a path in RitsuLib settings (or use Browse).");
                return;
            }

            if (!HarmonyPatchDumpWriter.TryWrite(resolved, out var err))
            {
                RitsuLibFramework.Logger.Warn($"{logPrefix} Failed to write dump: {err}");
                return;
            }

            RitsuLibFramework.Logger.Info($"{logPrefix} Wrote Harmony patch dump to: {resolved}");
        }
    }
}
