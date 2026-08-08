using System.IO.Compression;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.Networking.StateDivergence;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Diagnostics.Patches
{
    internal sealed class RitsuLibGetLogsArchivePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_get_logs_archive";
        public static bool IsCritical => false;

        public static string Description =>
            "Preserve RitsuLib diagnostic archives when the game collects logs and feedback.";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(GetLogsConsoleCmd),
                    "ArchiveLogFile",
                    [typeof(string), typeof(ZipArchive), typeof(string), typeof(long)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string file, ZipArchive archive, string entryName)
        {
            Func<string, byte[]>? readBundle = null;
            if (StateDivergenceLogBundleWriter.IsPublishedBundlePath(file))
                readBundle = StateDivergenceLogBundleWriter.BuildSanitizedSubmissionBundle;
            else if (SelfCheckBundleWriter.IsPublishedBundlePath(file))
                readBundle = SelfCheckBundleWriter.ReadPublishedBundle;

            if (readBundle == null)
                return true;

            byte[] submissionBundle;
            try
            {
                submissionBundle = readBundle(file);
            }
            catch (FileNotFoundException)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Diagnostics] Diagnostic bundle disappeared before log collection: {file}");
                return false;
            }
            catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Diagnostics] Diagnostic bundle could not be collected intact and was skipped: {file}: {ex.Message}");
                return false;
            }

            var archiveEntry = archive.CreateEntry(entryName.Replace("\\", "/"), CompressionLevel.Optimal);
            using var destination = archiveEntry.Open();
            destination.Write(submissionBundle);
            return false;
        }
    }
}
