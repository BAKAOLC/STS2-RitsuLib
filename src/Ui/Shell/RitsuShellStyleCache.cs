using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     Flyweight cache for parameter-free (or small fixed-variant) <see cref="StyleBoxFlat" /> chrome.
    ///     A <see cref="StyleBoxFlat" /> is an immutable-after-build Godot <see cref="Resource" /> that can be
    ///     shared by any number of controls, so the settings UI does not need a fresh instance per node. Building
    ///     a page would otherwise allocate hundreds of identical styleboxes (entry surface, field frames, toggle
    ///     states, …). Entries are keyed by the immutable <see cref="RitsuShellTheme" /> snapshot and a style key,
    ///     so the whole cache is dropped automatically when <see cref="RitsuShellTheme.Current" /> is replaced on
    ///     a theme change.
    ///     无参（或少量固定变体）<see cref="StyleBoxFlat" /> chrome 的享元缓存。<see cref="StyleBoxFlat" /> 是构建后不可变的
    ///     Godot <see cref="Resource" />，可被任意数量的控件共享，因此设置 UI 无需为每个节点新建实例。否则构建一个页面会分配
    ///     成百个完全相同的 stylebox（条目表面、字段边框、开关状态等）。缓存项以不可变 <see cref="RitsuShellTheme" /> 快照加
    ///     样式键为键，故在主题变化导致 <see cref="RitsuShellTheme.Current" /> 被替换时整张缓存自动失效。
    ///     <para>
    ///         Callers must treat the returned instance as read-only. Any factory whose result is subsequently
    ///         mutated (e.g. a base style tweaked into a hover variant) must not be routed through this cache.
    ///         调用方必须将返回实例视为只读。任何其结果随后会被修改的工厂（例如把基础样式改成悬停变体）都不得经由本缓存。
    ///     </para>
    /// </summary>
    internal static class RitsuShellStyleCache
    {
        private static readonly ConditionalWeakTable<RitsuShellTheme, ConcurrentDictionary<string, StyleBoxFlat>>
            Cache = new();

        /// <summary>
        ///     Returns the shared stylebox for <paramref name="key" /> under the current theme, building it once
        ///     via <paramref name="build" /> on first use.
        ///     返回当前主题下 <paramref name="key" /> 对应的共享 stylebox，首次使用时通过 <paramref name="build" /> 构建一次。
        /// </summary>
        internal static StyleBoxFlat GetOrBuild(string key, Func<StyleBoxFlat> build)
        {
            var map = Cache.GetValue(RitsuShellTheme.Current,
                static _ => new(StringComparer.Ordinal));
            return map.TryGetValue(key, out var cached)
                ? cached
                : map.GetOrAdd(key, static (_, factory) => factory(), build);
        }
    }
}
