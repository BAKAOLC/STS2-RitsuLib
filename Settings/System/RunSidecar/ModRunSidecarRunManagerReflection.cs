using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Settings.RunSidecar
{
    internal static class ModRunSidecarRunManagerReflection
    {
        private static readonly AccessTools.FieldRef<RunManager, long> StartTimeUnix =
            AccessTools.FieldRefAccess<RunManager, long>("_startTime");

        internal static long? TryGetRunStartTimeUnix(RunManager runManager)
        {
            try
            {
                return StartTimeUnix(runManager);
            }
            catch
            {
                return null;
            }
        }
    }
}
