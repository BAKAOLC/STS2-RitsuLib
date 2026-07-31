using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace STS2RitsuLib.Ui.Shell
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Caches parameter-free style boxes and a small number of fixed variants. Each cache is associated
    ///         with an immutable <see cref="RitsuShellTheme" /> snapshot, so entries from a replaced theme become
    ///         eligible for collection with that snapshot.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         缓存无参数样式框及少量固定变体。每份缓存都与一个不可变的 <see cref="RitsuShellTheme" /> 快照关联，
    ///         因此主题被替换后，旧主题的缓存项会随对应快照一起进入可回收状态。
    ///     </para>
    ///     <para xml:lang="en">
    ///         Callers must treat the returned instance as read-only. Any factory whose result is subsequently
    ///         modified, such as when deriving a hover variant, must not use this cache.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         调用方必须将返回的实例视为只读。若工厂的结果之后还会被修改（例如用于派生悬停变体），则不得使用此缓存。
    ///     </para>
    /// </summary>
    internal static class RitsuShellStyleCache
    {
        private static readonly ConditionalWeakTable<RitsuShellTheme, ConcurrentDictionary<string, StyleBoxFlat>>
            Cache = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the shared style box identified by <paramref name="key" /> for the current theme, creating
        ///         it with <paramref name="build" /> when necessary.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当前主题中由 <paramref name="key" /> 标识的共享样式框，并在需要时通过
        ///         <paramref name="build" /> 创建该样式框。
        ///     </para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">The style key within the current theme snapshot.</para>
        ///     <para xml:lang="zh-CN">当前主题快照内的样式键。</para>
        /// </param>
        /// <param name="build">
        ///     <para xml:lang="en">The factory used when the key has no cached style box.</para>
        ///     <para xml:lang="zh-CN">该键尚无缓存样式框时使用的工厂。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The shared, read-only style-box instance.</para>
        ///     <para xml:lang="zh-CN">共享的只读样式框实例。</para>
        /// </returns>
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
