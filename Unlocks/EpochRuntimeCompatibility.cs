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

            if (!RitsuLibSettingsStore.IsUnlockEpochCompatEnabled())
                throw new InvalidOperationException(
                    $"Missing or invalid epoch id '{epochId}' during {context}. " +
                    "Enable RitsuLib debug compatibility (master + Unlock epoch) to skip this grant with a warning, " +
                    "or register/fix the epoch in your timeline/unlock rules.");

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

            RitsuLibFramework.Logger.Warn(
                $"[Unlocks][DebugCompat] Missing epoch '{epochId}' during {context}. " +
                "Skipping this unlock attempt and continuing execution.");
        }
    }
}
