namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Global registry of mod settings pages and optional per-mod display names for the UI.
    /// </summary>
    public static class ModSettingsRegistry
    {
        private static readonly Dictionary<string, ModSettingsText> ModDisplayNames =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModSettingsPage> PagesById =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     True after at least one page has been registered.
        /// </summary>
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

        /// <summary>
        ///     Registers a built <see cref="ModSettingsPage" /> (typically from <see cref="ModSettingsPageBuilder" />).
        /// </summary>
        public static void Register(ModSettingsPage page)
        {
            ArgumentNullException.ThrowIfNull(page);

            lock (SyncRoot)
            {
                PagesById[CreateCompositeId(page.ModId, page.Id)] = page;
            }
        }

        /// <summary>
        ///     Registers localized (or literal) text shown for <paramref name="modId" /> in the settings chrome.
        /// </summary>
        public static void RegisterModDisplayName(string modId, ModSettingsText displayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(displayName);

            lock (SyncRoot)
            {
                ModDisplayNames[modId] = displayName;
            }
        }

        /// <summary>
        ///     Returns the display name for <paramref name="modId" />, if any.
        /// </summary>
        public static ModSettingsText? GetModDisplayName(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                return ModDisplayNames.GetValueOrDefault(modId);
            }
        }

        /// <summary>
        ///     Fluent helper: builds a page via <paramref name="configure" /> and registers it.
        /// </summary>
        public static void Register(string modId, Action<ModSettingsPageBuilder> configure, string? pageId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(configure);

            var builder = new ModSettingsPageBuilder(modId, pageId);
            configure(builder);
            Register(builder.Build());
        }

        /// <summary>
        ///     Looks up a page by mod id and page id.
        /// </summary>
        public static bool TryGetPage(string modId, string pageId, out ModSettingsPage? page)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(pageId);

            lock (SyncRoot)
            {
                return PagesById.TryGetValue(CreateCompositeId(modId, pageId), out page);
            }
        }

        /// <summary>
        ///     All registered pages, ordered for stable sidebar display.
        /// </summary>
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
