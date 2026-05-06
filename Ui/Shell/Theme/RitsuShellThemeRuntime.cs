using System.Text.Json;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Holds the <see cref="RitsuShellTheme.Current" /> snapshot and the public lifecycle (apply theme,
    ///     reapply on disk change, listen for changes, register mod tokens). All members are thread-safe.
    /// </summary>
    public static class RitsuShellThemeRuntime
    {
        private const string DefaultThemeId = "default";

        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, RitsuShellThemeModRegistration> ModRegistrations =
            new(StringComparer.Ordinal);

        private static RitsuShellTheme? _current;

        /// <summary>
        ///     Last applied theme id (lowercase). Defaults to <c>default</c> until a successful apply.
        /// </summary>
        public static string ActiveThemeId { get; private set; } = DefaultThemeId;

        /// <summary>
        ///     Current theme snapshot. Calling this also lazily builds <c>default</c> if no theme has been
        ///     applied yet.
        /// </summary>
        public static RitsuShellTheme Current
        {
            get
            {
                EnsureBaseline();
                return _current!;
            }
        }

        /// <summary>
        ///     Fired after the current snapshot has been replaced.
        /// </summary>
        public static event Action? ThemeChanged;

        /// <summary>
        ///     Builds the baseline snapshot if not yet built (uses <c>default</c>).
        /// </summary>
        public static void EnsureBaseline()
        {
            lock (Gate)
            {
                if (_current != null) return;
                if (!TryBuildSnapshotLocked(DefaultThemeId, out var resolvedId, out var theme)) return;
                _current = theme;
                ActiveThemeId = resolvedId;
            }
        }

        /// <summary>
        ///     Applies the named theme. <see langword="null" /> / blank picks <c>default</c>; if the lookup
        ///     fails the current snapshot is preserved (or rebuilt as default).
        /// </summary>
        /// <param name="themeId">Target theme id (case-insensitive).</param>
        public static void ApplyThemeId(string? themeId)
        {
            RitsuShellTheme? snapshot;
            lock (Gate)
            {
                if (!TryBuildSnapshotLocked(themeId ?? DefaultThemeId, out var resolvedId, out snapshot))
                    if (!TryBuildSnapshotLocked(DefaultThemeId, out resolvedId, out snapshot))
                        return;

                _current = snapshot;
                ActiveThemeId = resolvedId;
            }

            NotifyChanged(snapshot!);
        }

        /// <summary>
        ///     Re-applies the current <see cref="ActiveThemeId" />, optionally clearing the catalog cache so
        ///     disk changes are picked up.
        /// </summary>
        /// <param name="forceReloadCatalog">When <see langword="true" />, the on-disk catalog is reloaded.</param>
        public static void ReapplyActiveTheme(bool forceReloadCatalog)
        {
            if (forceReloadCatalog)
                RitsuShellThemeCatalog.InvalidateCache();
            ApplyThemeId(ActiveThemeId);
        }

        /// <summary>
        ///     Registers a mod's default DTFM tokens and optional apply callback. Subsequent
        ///     <see cref="ApplyThemeId" /> calls merge these defaults before chain documents and invoke
        ///     <paramref name="onApply" /> on every rebuild.
        /// </summary>
        /// <param name="modId">Mod identifier.</param>
        /// <param name="defaults">DTFM JSON tree (object) merged before chain documents.</param>
        /// <param name="onApply">Optional callback fired after every rebuild.</param>
        public static void RegisterModTokens(string modId, JsonElement? defaults,
            Action<RitsuShellTheme>? onApply = null)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return;
            lock (Gate)
            {
                ModRegistrations[modId] = new(modId, defaults, onApply);
            }

            ReapplyActiveTheme(false);
        }

        /// <summary>
        ///     Removes a previous <see cref="RegisterModTokens" /> entry.
        /// </summary>
        /// <param name="modId">Mod identifier.</param>
        public static void UnregisterModTokens(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return;
            lock (Gate)
            {
                if (!ModRegistrations.Remove(modId))
                    return;
            }

            ReapplyActiveTheme(false);
        }

        private static bool TryBuildSnapshotLocked(string themeId, out string resolvedId,
            out RitsuShellTheme? theme)
        {
            var snapshot = ModRegistrations.Values.ToArray();
            return RitsuShellThemeCatalog.TryBuildSnapshot(themeId, snapshot, out resolvedId, out theme);
        }

        private static void NotifyChanged(RitsuShellTheme snapshot)
        {
            RitsuShellThemeModRegistration[] modSnapshot;
            lock (Gate)
            {
                modSnapshot = ModRegistrations.Values.ToArray();
            }

            ThemeChanged?.Invoke();

            foreach (var reg in modSnapshot)
                reg.OnApply?.Invoke(snapshot);
        }
    }
}
