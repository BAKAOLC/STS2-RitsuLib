using System.IO.Compression;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.StateDivergence.Patches
{
    internal sealed class StateDivergenceGetLogsArchivePatch : IPatchMethod
    {
        public static string PatchId => "state_divergence_get_logs_archive";
        public static bool IsCritical => false;

        public static string Description =>
            "Sanitize and preserve RitsuLib state divergence archives when the game collects logs and feedback.";

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
            if (!StateDivergenceLogBundleWriter.IsPublishedBundlePath(file))
                return true;

            byte[] submissionBundle;
            try
            {
                submissionBundle = StateDivergenceLogBundleWriter.BuildSanitizedSubmissionBundle(file);
            }
            catch (FileNotFoundException)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[State divergence diagnostics] Diagnostic bundle disappeared before log collection: {file}");
                return false;
            }

            var archiveEntry = archive.CreateEntry(entryName.Replace("\\", "/"), CompressionLevel.Optimal);
            using var destination = archiveEntry.Open();
            destination.Write(submissionBundle);
            return false;
        }
    }
}
