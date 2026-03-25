using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Data;

namespace STS2RitsuLib.Unlocks
{
    internal static class EpochRuntimeCompatibility
    {
        private static readonly Lock WarnLock = new();
        private static readonly HashSet<string> WarnedMissingEpochs = [];

        internal static bool CanUseEpochId(string epochId, string context)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentException.ThrowIfNullOrWhiteSpace(context);

            if (EpochModel.IsValid(epochId))
                return true;

            WarnMissingEpochOnce(epochId, context);
            return false;
        }

        private static void WarnMissingEpochOnce(string epochId, string context)
        {
            var warnKey = $"{epochId}:{context}";

            lock (WarnLock)
            {
                if (!WarnedMissingEpochs.Add(warnKey))
                    return;
            }

            if (RitsuLibSettingsStore.IsDebugCompatibilityModeEnabled())
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Unlocks][DebugCompat] Missing epoch '{epochId}' during {context}. " +
                    "Skipping this unlock attempt and continuing execution.");
                return;
            }

            RitsuLibFramework.Logger.Error(
                $"[Unlocks] Missing epoch '{epochId}' during {context}. " +
                "Skipping this unlock attempt to avoid terminating the current run.");
        }
    }
}
