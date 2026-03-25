namespace STS2RitsuLib.Settings
{
    public static class ModSettingsRegistry
    {
        private static readonly Dictionary<string, ModSettingsText> ModDisplayNames =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSettingsPage> PagesById =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool HasPages
        {
            get
            {
                lock (SyncRoot)
                {
                    return PagesById.Count > 0;
                }
            }
        }

        public static void Register(ModSettingsPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            lock (SyncRoot)
            {
                PagesById[CreateCompositeId(page.ModId, page.Id)] = page;
            }
        }

        public static void RegisterModDisplayName(string modId, ModSettingsText displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(displayName);

            lock (SyncRoot)
            {
                ModDisplayNames[modId] = displayName;
            }
        }

        public static ModSettingsText? GetModDisplayName(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                return ModDisplayNames.GetValueOrDefault(modId);
            }
        }

        public static void Register(string modId, Action<ModSettingsPageBuilder> configure, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(configure);

            var builder = new ModSettingsPageBuilder(modId, pageId);
            configure(builder);
            Register(builder.Build());
        }

        public static bool TryGetPage(string modId, string pageId, out ModSettingsPage? page)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (SyncRoot)
            {
                return PagesById.TryGetValue(CreateCompositeId(modId, pageId), out page);
            }
        }

        public static IReadOnlyList<ModSettingsPage> GetPages()
        {
            lock (SyncRoot)
            {
                return PagesById.Values
                    .OrderBy(page => page.SortOrder)
                    .ThenBy(page => page.ModId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(page => page.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        private static string CreateCompositeId(string modId, string pageId)
        {
            return $"{modId}::{pageId}";
        }
    }
}
