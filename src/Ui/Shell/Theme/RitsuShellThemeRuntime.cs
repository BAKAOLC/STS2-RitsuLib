using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Manages the current shell-theme snapshot, theme application and reloads, change notifications,
    ///         and mod token registrations. Public state transitions are synchronized.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         管理当前 Shell 主题快照、主题应用与重新加载、变更通知及模组令牌注册。公开的状态转换均会同步。
    ///     </para>
    /// </summary>
    public static class RitsuShellThemeRuntime
    {
        private const string DefaultThemeId = "default";

        private static readonly Lock Gate = new();

        private static readonly Dictionary<string, RitsuShellThemeModRegistration> ModRegistrations =
            new(StringComparer.OrdinalIgnoreCase);

        private static RitsuShellTheme? _current;

        private static bool _fontSnapshotInvalidated;

        private static bool _fontRefreshQueued;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the normalized identifier of the active snapshot. The initial value is <c>default</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当前活动快照的规范化标识符；初始值为 <c>default</c>。
        ///     </para>
        /// </summary>
        public static string ActiveThemeId { get; private set; } = DefaultThemeId;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the current theme snapshot. The first access builds <c>default</c> when necessary, and
        ///         later accesses rebuild invalidated font resources lazily.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当前主题快照。首次访问会在需要时构建 <c>default</c>；后续访问会延迟重建已失效的字体资源。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Occurs after <see cref="ApplyThemeId" /> successfully publishes a snapshot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <see cref="ApplyThemeId" /> 成功发布主题快照后发生。
        ///     </para>
        /// </summary>
        public static event Action? ThemeChanged;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds and publishes the <c>default</c> baseline snapshot if no snapshot exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若尚无主题快照，则构建并发布 <c>default</c> 基线快照。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Applies a theme by identifier. A <see langword="null" /> or blank value selects
        ///         <c>default</c>. If the requested theme cannot be built, the method tries <c>default</c>;
        ///         if that also fails, the current snapshot is preserved.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按标识符应用主题。<see langword="null" /> 或空白值选择 <c>default</c>。请求的主题无法构建时，
        ///         方法会尝试 <c>default</c>；若后者也失败，则保留当前快照。
        ///     </para>
        /// </summary>
        /// <param name="themeId">
        ///     <para xml:lang="en">The case-insensitive theme identifier to apply.</para>
        ///     <para xml:lang="zh-CN">要应用的不区分大小写主题标识符。</para>
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
                _fontSnapshotInvalidated = false;
            }

            NotifyChanged(snapshot!);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clears cached fonts and reapplies <see cref="ActiveThemeId" />, optionally invalidating the
        ///         catalog first so disk changes are loaded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         清除缓存的字体并重新应用 <see cref="ActiveThemeId" />；可选择先使主题目录失效，以加载磁盘变更。
        ///     </para>
        /// </summary>
        /// <param name="forceReloadCatalog">
        ///     <para xml:lang="en">
        ///         <see langword="true" /> to reload embedded and on-disk theme documents.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若要重新加载内嵌及磁盘主题文档，则为 <see langword="true" />。
        ///     </para>
        /// </param>
        public static void ReapplyActiveTheme(bool forceReloadCatalog)
        {
            if (forceReloadCatalog)
                RitsuShellThemeCatalog.InvalidateCache();
            RitsuShellThemeValueCoerce.InvalidateFontCache();
            ApplyThemeId(ActiveThemeId);
        }

        internal static void NotifyExternalFontCacheCleared()
        {
            lock (Gate)
            {
                _fontSnapshotInvalidated = true;
                RitsuShellThemeValueCoerce.InvalidateFontCache();
                if (_fontRefreshQueued)
                    return;

                _fontRefreshQueued = true;
            }

            try
            {
                Callable.From(FlushExternalFontCacheCleared).CallDeferred();
            }
            catch (Exception ex)
            {
                lock (Gate)
                {
                    _fontRefreshQueued = false;
                }

                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not defer the external font-cache refresh: {ex}");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers or replaces a mod's default token contribution and optional apply callback, then
        ///         reapplies the active theme. Registered defaults are merged before the selected theme's
        ///         inheritance chain.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册或替换模组的默认令牌贡献及可选应用回调，然后重新应用当前主题。已注册的默认令牌会在所选主题的
        ///         继承链之前合并。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">
        ///         The case-insensitive mod identifier. Surrounding whitespace is removed; a blank value is ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不区分大小写的模组标识符。首尾空白会被移除；空白值会被忽略。
        ///     </para>
        /// </param>
        /// <param name="defaults">
        ///     <para xml:lang="en">
        ///         The optional Design Tokens Format Module object to clone and merge before theme documents.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的设计令牌格式模块对象；注册时会克隆，并在主题文档之前合并。
        ///     </para>
        /// </param>
        /// <param name="onApply">
        ///     <para xml:lang="en">
        ///         The optional callback invoked after a successful theme application publishes a snapshot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         成功应用主题并发布快照后调用的可选回调。
        ///     </para>
        /// </param>
        public static void RegisterModTokens(string modId, JsonElement? defaults,
            Action<RitsuShellTheme>? onApply = null)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return;

            var normalizedModId = modId.Trim();
            var ownedDefaults = defaults?.Clone();
            lock (Gate)
            {
                ModRegistrations[normalizedModId] = new(normalizedModId, ownedDefaults, onApply);
            }

            ReapplyActiveTheme(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes a mod token registration and reapplies the active theme when an entry was present.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除模组令牌注册；若原条目存在，则重新应用当前主题。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">
        ///         The case-insensitive mod identifier. Surrounding whitespace is removed; a blank value is ignored.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         不区分大小写的模组标识符。首尾空白会被移除；空白值会被忽略。
        ///     </para>
        /// </param>
        public static void UnregisterModTokens(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return;

            var normalizedModId = modId.Trim();
            lock (Gate)
            {
                if (!ModRegistrations.Remove(normalizedModId))
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
                if (_current == null ||
                    (!_fontSnapshotInvalidated &&
                     AreThemeFontsValid(_current.Font) &&
                     RitsuShellThemeValueCoerce.AreFontTokensCurrent(_current.Font)))
                    return;

                if (TryBuildSnapshotLocked(ActiveThemeId, out var resolvedId, out var snapshot) && snapshot != null)
                {
                    _current = snapshot;
                    ActiveThemeId = resolvedId;
                    _fontSnapshotInvalidated = false;
                    return;
                }

                if (!TryBuildSnapshotLocked(DefaultThemeId, out resolvedId, out snapshot) || snapshot == null) return;
                _current = snapshot;
                ActiveThemeId = resolvedId;
                _fontSnapshotInvalidated = false;
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
                modSnapshot = [.. ModRegistrations.Values];
            }

            var handlers = ThemeChanged?.GetInvocationList();
            if (handlers != null)
                foreach (var handler in handlers)
                    try
                    {
                        ((Action)handler).Invoke();
                    }
                    catch (Exception ex)
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[ShellTheme] ThemeChanged callback failed: {ex}");
                    }

            foreach (var reg in modSnapshot)
                try
                {
                    reg.OnApply?.Invoke(snapshot);
                }
                catch (Exception ex)
                {
                    RitsuLibFramework.CreateLogger(reg.ModId)
                        .Warn($"[ShellTheme] Theme apply callback failed: {ex}");
                }
        }

        private static void FlushExternalFontCacheCleared()
        {
            lock (Gate)
            {
                _fontRefreshQueued = false;
            }

            ReapplyActiveTheme(false);
        }
    }
}
