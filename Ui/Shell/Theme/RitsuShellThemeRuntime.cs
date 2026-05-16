using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Holds the <see cref="RitsuShellTheme.Current" /> snapshot and the public lifecycle (apply theme,
    ///     reapply on disk change, listen for changes, register mod tokens). All members are thread-safe.
    ///     持有 <see cref="RitsuShellTheme.Current" /> 快照和公共生命周期（应用主题、
    ///     在磁盘变更后重新应用、监听变更、注册 mod 令牌）。所有成员都是线程安全的。
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
        ///     最后应用的主题 id (小写). 默认为 <c>default</c> 直到成功应用。
        /// </summary>
        public static string ActiveThemeId { get; private set; } = DefaultThemeId;

        /// <summary>
        ///     Current theme snapshot. Calling this also lazily builds <c>default</c> if no theme has been
        ///     applied yet.
        ///     当前主题快照. 调用此项也会惰性构建 <c>default</c> if no theme h为 been
        ///     应用 尚未。
        /// </summary>
        public static RitsuShellTheme Current
        {
            get
            {
                EnsureBaseline();
                EnsureCurrentSnapshotResourcesValid();
                return _current!;
            }
        }

        /// <summary>
        ///     Fired after the current snapshot has been replaced.
        ///     当前快照被替换后触发。
        /// </summary>
        public static event Action? ThemeChanged;

        /// <summary>
        ///     Builds the baseline snapshot if not yet built (uses <c>default</c>).
        ///     构建基线快照 if 尚未构建 (使用 <c>default</c>)。
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
        ///     应用指定名称的主题。<see langword="null" /> / 空白会选择 <c>default</c>；如果查找
        ///     失败，则保留当前快照（或重建为默认快照）。
        /// </summary>
        /// <param name="themeId">
        ///     Target theme id (case-insensitive).
        ///     目标主题 id (不区分大小写)。
        /// </param>
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
        ///     重新应用当前 <see cref="ActiveThemeId" />，并可选择清除目录缓存，使
        ///     磁盘变更被拾取。
        /// </summary>
        /// <param name="forceReloadCatalog">
        ///     When <see langword="true" />, the on-disk catalog is reloaded.
        ///     当为 <see langword="true" /> 时，重新加载磁盘目录。
        /// </param>
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
        ///     注册 mod 的默认 DTFM 令牌和可选应用回调。后续
        ///     <see cref="ApplyThemeId" /> 调用会先合并这些默认值，再合并链式文档，并在每次重建时调用
        ///     <paramref name="onApply" />。
        /// </summary>
        /// <param name="modId">
        ///     Mod identifier.
        ///     Mod 标识符。
        /// </param>
        /// <param name="defaults">
        ///     DTFM JSON tree (object) merged before chain documents.
        ///     DTFM JSON tree (对象) 合并后 在链式文档之前。
        /// </param>
        /// <param name="onApply">
        ///     Optional callback fired after every rebuild.
        ///     可选 回调 fired 之后 every rebuild。
        /// </param>
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
        ///     移除先前的 <see cref="RegisterModTokens" /> 条目。
        /// </summary>
        /// <param name="modId">
        ///     Mod identifier.
        ///     Mod 标识符。
        /// </param>
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

        private static void EnsureCurrentSnapshotResourcesValid()
        {
            lock (Gate)
            {
                if (_current == null || AreThemeFontsValid(_current.Font))
                    return;

                if (TryBuildSnapshotLocked(ActiveThemeId, out var resolvedId, out var snapshot) && snapshot != null)
                {
                    _current = snapshot;
                    ActiveThemeId = resolvedId;
                    return;
                }

                if (!TryBuildSnapshotLocked(DefaultThemeId, out resolvedId, out snapshot) || snapshot == null) return;
                _current = snapshot;
                ActiveThemeId = resolvedId;
            }
        }

        private static bool AreThemeFontsValid(FontTokens fonts)
        {
            return GodotObject.IsInstanceValid(fonts.Body) &&
                   GodotObject.IsInstanceValid(fonts.BodyBold) &&
                   GodotObject.IsInstanceValid(fonts.Button);
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
