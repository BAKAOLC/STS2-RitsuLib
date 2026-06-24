using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarRepeatedWarningLog
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan StaleRetention = TimeSpan.FromMinutes(5);
        private static readonly Lock Gate = new();
        private static readonly Dictionary<string, Entry> Entries = [];

        internal static void Warn(string key, string message)
        {
            Warn(RitsuLibFramework.Logger, key, message);
        }

        internal static void Warn(Logger logger, string key, string message)
        {
            var now = DateTime.UtcNow;
            var suppressed = 0;
            var shouldLog = false;

            lock (Gate)
            {
                SweepStale_NoLock(now);

                if (!Entries.TryGetValue(key, out var entry))
                {
                    Entries[key] = new(now, 0);
                    shouldLog = true;
                }
                else if (now - entry.LastLoggedUtc < DefaultInterval)
                {
                    Entries[key] = entry with { Suppressed = entry.Suppressed + 1 };
                }
                else
                {
                    suppressed = entry.Suppressed;
                    Entries[key] = new(now, 0);
                    shouldLog = true;
                }
            }

            if (!shouldLog)
                return;

            if (suppressed > 0)
            {
                logger.Warn(
                    $"{message} (suppressed {suppressed} similar sidecar warning(s) during the previous {DefaultInterval.TotalSeconds:0}s)");
                return;
            }

            logger.Warn(message);
        }

        private static void SweepStale_NoLock(DateTime now)
        {
            if (Entries.Count == 0)
                return;

            List<string>? stale = null;
            foreach (var (key, entry) in Entries)
            {
                if (now - entry.LastLoggedUtc <= StaleRetention)
                    continue;

                stale ??= [];
                stale.Add(key);
            }

            if (stale == null)
                return;

            foreach (var key in stale)
                Entries.Remove(key);
        }

        private readonly record struct Entry(DateTime LastLoggedUtc, int Suppressed);
    }
}
