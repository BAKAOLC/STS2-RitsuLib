using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Stores resolved left, top, right, and bottom edge values.</para>
    ///     <para xml:lang="zh-CN">存储已解析的左、上、右、下边缘值。</para>
    /// </summary>
    internal readonly record struct BoxEdges(int Left, int Top, int Right, int Bottom);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores the resolved top-left, top-right, bottom-right, and bottom-left corner radii of a
    ///         <see cref="StyleBoxFlat" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         存储 <see cref="StyleBoxFlat" /> 已解析的左上、右上、右下及左下圆角半径。
    ///     </para>
    /// </summary>
    internal readonly record struct BoxCorners(int TopLeft, int TopRight, int BottomRight, int BottomLeft);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves optional layout tokens against fallback values for the current shell theme.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据当前 Shell 主题解析可选布局令牌，并在令牌不可用时使用备用值。
    ///     </para>
    /// </summary>
    internal static class RitsuShellThemeLayoutResolver
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Caches resolved edge and corner sets by theme snapshot, token path, and fallback value. When a
        ///         snapshot is replaced, its cache becomes eligible for collection with the snapshot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按主题快照、令牌路径及备用值缓存已解析的边缘和圆角集合。主题快照被替换后，对应缓存会随快照一起
        ///         进入可回收状态。
        ///     </para>
        /// </summary>
        private static readonly ConditionalWeakTable<RitsuShellTheme, ThemeBoxMemo> BoxMemos = new();

        private static ThemeBoxMemo MemoFor(RitsuShellTheme theme)
        {
            return BoxMemos.GetValue(theme, static _ => new());
        }

        internal static int ResolveInt(string path, int fallback)
        {
            if (RitsuShellTheme.Current.TryGetNumber(path, out var value))
            {
                var rounded = Math.Round(value);
                if (rounded is >= int.MinValue and <= int.MaxValue)
                    return (int)rounded;
            }

            return fallback;
        }

        internal static float ResolveFloat(string path, float fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value) &&
                   value is >= -float.MaxValue and <= float.MaxValue
                ? (float)value
                : fallback;
        }

        internal static BoxEdges ResolveEdges(string basePath, int fallbackAll)
        {
            return MemoFor(RitsuShellTheme.Current).Edges
                .GetOrAdd(new(basePath, fallbackAll), static key => ComputeEdges(key.BasePath, key.Fallback));
        }

        private static BoxEdges ComputeEdges(string basePath, int fallbackAll)
        {
            var all = ResolveInt(basePath, fallbackAll);
            all = ResolveInt(basePath + ".all", all);
            var left = ResolveInt(basePath + ".left", all);
            var top = ResolveInt(basePath + ".top", all);
            var right = ResolveInt(basePath + ".right", all);
            var bottom = ResolveInt(basePath + ".bottom", all);
            return new(left, top, right, bottom);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves corner radii beneath <paramref name="basePath" />, applying an optional <c>all</c>
        ///         value before the individual corner values.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析 <paramref name="basePath" /> 下的圆角半径，先应用可选的 <c>all</c> 值，再应用各个角的值。
        ///     </para>
        /// </summary>
        /// <param name="basePath">
        ///     <para xml:lang="en">The base path of the corner-radius token group.</para>
        ///     <para xml:lang="zh-CN">圆角半径令牌组的基础路径。</para>
        /// </param>
        /// <param name="fallbackUniform">
        ///     <para xml:lang="en">The value used for all corners when no applicable token exists.</para>
        ///     <para xml:lang="zh-CN">没有适用令牌时用于全部圆角的值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved radii for all four corners.</para>
        ///     <para xml:lang="zh-CN">四个角的已解析半径。</para>
        /// </returns>
        internal static BoxCorners ResolveCornerRadii(string basePath, int fallbackUniform)
        {
            return MemoFor(RitsuShellTheme.Current).Corners
                .GetOrAdd(new(basePath, fallbackUniform),
                    static key => ComputeCornerRadii(key.BasePath, key.Fallback));
        }

        private static BoxCorners ComputeCornerRadii(string basePath, int fallbackUniform)
        {
            var all = ResolveInt(basePath, fallbackUniform);
            all = ResolveInt(basePath + ".all", all);
            var tl = ResolveInt(basePath + ".topLeft", all);
            var tr = ResolveInt(basePath + ".topRight", all);
            var br = ResolveInt(basePath + ".bottomRight", all);
            var bl = ResolveInt(basePath + ".bottomLeft", all);
            return new(tl, tr, br, bl);
        }

        internal static Vector2 ResolveMinSize(string basePath, Vector2 fallback, bool allowOverride = true)
        {
            if (!allowOverride)
                return fallback;

            var width = ResolveFloat(basePath + ".width", fallback.X);
            width = ResolveFloat(basePath + ".minWidth", width);
            var height = ResolveFloat(basePath + ".height", fallback.Y);
            height = ResolveFloat(basePath + ".minHeight", height);
            return new(width, height);
        }

        private readonly record struct BoxMemoKey(string BasePath, int Fallback);

        private sealed class ThemeBoxMemo
        {
            public readonly ConcurrentDictionary<BoxMemoKey, BoxCorners> Corners = new();
            public readonly ConcurrentDictionary<BoxMemoKey, BoxEdges> Edges = new();
        }
    }
}
