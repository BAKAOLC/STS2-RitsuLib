using Godot;
using STS2RitsuLib.Data;
using STS2RitsuLib.Diagnostics;
using Environment = System.Environment;

namespace STS2RitsuLib.Utils.Persistence
{
    internal static class RitsuLibLegacyGeneratedDataCleanup
    {
        private const string LegacyFmodCachePath = "user://ritsulib/fmod-cache/audio";
        private const string LegacySelfCheckStagingDirectoryName = ".ritsulib-self-check-staging";
        private const string LegacyStateDivergenceStagingDirectoryName = ".ritsulib-state-divergence-staging";
        private const string LegacyCoreDumpPattern = "sts2_coredump_*.core";
        private static readonly TimeSpan ActiveCoreDumpProtection = TimeSpan.FromHours(1);
        private static int _cleanupAttempted;

        internal static void RunOnce()
        {
            if (Interlocked.Exchange(ref _cleanupAttempted, 1) == 1)
                return;

            var removedItems = 0;
            CleanupLegacyFmodCache(ref removedItems);

            var userDataDirectory = ResolveLegacyUserDataDirectory();
            CleanupDirectory(
                Path.Combine(userDataDirectory, LegacyStateDivergenceStagingDirectoryName),
                ref removedItems);
            CleanupSelfCheckStaging(userDataDirectory, ref removedItems);
            CleanupLegacySteamInputManifest(ref removedItems);
            CleanupLegacyCoreDumps(ref removedItems);

            if (removedItems > 0)
                RitsuLibFramework.Logger.Info(
                    $"[Storage] Removed {removedItems} obsolete generated-data item(s) from earlier RitsuLib versions.");
        }

        private static void CleanupLegacyFmodCache(ref int removedItems)
        {
            var audioDirectory = ProjectSettings.GlobalizePath(LegacyFmodCachePath);
            CleanupDirectory(audioDirectory, ref removedItems);
            CleanupEmptyDirectory(Path.GetDirectoryName(audioDirectory));
            CleanupEmptyDirectory(Path.GetDirectoryName(Path.GetDirectoryName(audioDirectory)));
        }

        private static void CleanupSelfCheckStaging(string userDataDirectory, ref int removedItems)
        {
            CleanupDirectory(
                Path.Combine(userDataDirectory, LegacySelfCheckStagingDirectoryName),
                ref removedItems);

            var (outputFolder, _) = RitsuLibSettingsStore.GetSelfCheckOptions();
            var outputDirectory = SelfCheckBundleWriter.TryResolveOutputDirectory(outputFolder);
            if (outputDirectory == null)
                return;

            var parentDirectory = Directory.GetParent(outputDirectory)?.FullName ?? outputDirectory;
            CleanupDirectory(
                Path.Combine(parentDirectory, LegacySelfCheckStagingDirectoryName),
                ref removedItems);
        }

        private static void CleanupLegacySteamInputManifest(ref int removedItems)
        {
            var userDataDirectory = OS.GetUserDataDir();
            if (string.IsNullOrWhiteSpace(userDataDirectory))
                userDataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SlayTheSpire2");

            var ritsuLibDirectory = Path.Combine(userDataDirectory, "mods", "RitsuLib");
            CleanupDirectory(Path.Combine(ritsuLibDirectory, "steam_input"), ref removedItems);
            CleanupEmptyDirectory(ritsuLibDirectory);
        }

        private static void CleanupLegacyCoreDumps(ref int removedItems)
        {
            var cutoff = DateTime.UtcNow - ActiveCoreDumpProtection;
            try
            {
                foreach (var path in Directory.EnumerateFiles(
                             Path.GetTempPath(),
                             LegacyCoreDumpPattern,
                             SearchOption.TopDirectoryOnly))
                    CleanupLegacyCoreDump(path, cutoff, ref removedItems);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogCleanupFailure(Path.GetTempPath(), ex);
            }
        }

        private static void CleanupLegacyCoreDump(string path, DateTime cutoff, ref int removedItems)
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) > cutoff)
                    return;

                File.Delete(path);
                removedItems++;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogCleanupFailure(path, ex);
            }
        }

        private static string ResolveLegacyUserDataDirectory()
        {
            var userDataDirectory = OS.GetUserDataDir();
            return string.IsNullOrWhiteSpace(userDataDirectory) ? AppContext.BaseDirectory : userDataDirectory;
        }

        private static void CleanupDirectory(string path, ref int removedItems)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                Directory.Delete(path, true);
                removedItems++;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogCleanupFailure(path, ex);
            }
        }

        private static void CleanupEmptyDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                    Directory.Delete(path);
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                LogCleanupFailure(path, ex);
            }
        }

        private static void LogCleanupFailure(string path, Exception exception)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Storage] Failed to remove obsolete generated data at '{path}': {exception.Message}");
        }
    }
}
