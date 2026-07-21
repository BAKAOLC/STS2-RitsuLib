namespace STS2RitsuLib.Updates
{
    internal static class UpdateCheckSessionHistory
    {
        private static readonly Lock SyncRoot = new();
        private static readonly HashSet<string> LoggedVersions = new(StringComparer.Ordinal);
        private static readonly HashSet<string> NotifiedVersions = new(StringComparer.Ordinal);

        internal static bool TryRecordLoggedVersion(string modId, string? version)
        {
            return TryRecordVersion(LoggedVersions, modId, version);
        }

        internal static bool TryRecordNotifiedVersion(string modId, string? version)
        {
            return TryRecordVersion(NotifiedVersions, modId, version);
        }

        private static bool TryRecordVersion(HashSet<string> versions, string modId, string? version)
        {
            var normalizedModId = modId.Trim();
            var normalizedVersion = version?.Trim();
            if (normalizedModId.Length == 0 || string.IsNullOrEmpty(normalizedVersion))
                return false;

            var key = $"{normalizedModId}\n{normalizedVersion.ToUpperInvariant()}";
            lock (SyncRoot)
            {
                return versions.Add(key);
            }
        }
    }
}
