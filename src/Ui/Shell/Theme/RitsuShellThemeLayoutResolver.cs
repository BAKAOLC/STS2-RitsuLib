using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">Stores resolved left, top, right, and bottom edge values.</para>
    ///     <para xml:lang="zh-CN">存储已解析的左、上、右、下边缘值。</para>
    /// </summary>
    /// <param name="Left">
    ///     <para xml:lang="en">Left signed pixel value.</para>
    ///     <para xml:lang="zh-CN">Left 的有符号像素值。</para>
    /// </param>
    /// <param name="Top">
    ///     <para xml:lang="en">Top signed pixel value.</para>
    ///     <para xml:lang="zh-CN">Top 的有符号像素值。</para>
    /// </param>
    /// <param name="Right">
    ///     <para xml:lang="en">Right signed pixel value.</para>
    ///     <para xml:lang="zh-CN">Right 的有符号像素值。</para>
    /// </param>
    /// <param name="Bottom">
    ///     <para xml:lang="en">Bottom signed pixel value.</para>
    ///     <para xml:lang="zh-CN">Bottom 的有符号像素值。</para>
    /// </param>
    public readonly record struct BoxEdges(int Left, int Top, int Right, int Bottom);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores the resolved top-left, top-right, bottom-right, and bottom-left corner radii of a
    ///         <see cref="StyleBoxFlat" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         存储 <see cref="StyleBoxFlat" /> 已解析的左上、右上、右下及左下圆角半径。
    ///     </para>
    /// </summary>
    /// <param name="TopLeft">
    ///     <para xml:lang="en">TopLeft signed pixel value.</para>
    ///     <para xml:lang="zh-CN">TopLeft 的有符号像素值。</para>
    /// </param>
    /// <param name="TopRight">
    ///     <para xml:lang="en">TopRight signed pixel value.</para>
    ///     <para xml:lang="zh-CN">TopRight 的有符号像素值。</para>
    /// </param>
    /// <param name="BottomRight">
    ///     <para xml:lang="en">BottomRight signed pixel value.</para>
    ///     <para xml:lang="zh-CN">BottomRight 的有符号像素值。</para>
    /// </param>
    /// <param name="BottomLeft">
    ///     <para xml:lang="en">BottomLeft signed pixel value.</para>
    ///     <para xml:lang="zh-CN">BottomLeft 的有符号像素值。</para>
    /// </param>
    public readonly record struct BoxCorners(int TopLeft, int TopRight, int BottomRight, int BottomLeft);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Resolves optional layout tokens against fallback values for the current shell theme.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         根据当前 Shell 主题解析可选布局令牌，并在令牌不可用时使用备用值。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Queries do not change the selected theme. Each snapshot caches at most 4096 box results;
    ///         additional results are computed without caching. Paths must be nonblank and at most 512 characters.
    ///     </para>
    ///     <para xml:lang="zh-CN">查询不会更改已选主题。每个快照最多缓存 4096 组边缘和圆角结果；更多结果直接计算而不缓存。路径不得为空白且最多 512 个字符。</para>
    /// </remarks>
    public static class RitsuShellThemeLayoutResolver
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
        private const int MaximumCachedBoxes = 4096;

        private static readonly ConditionalWeakTable<RitsuShellTheme, ThemeBoxMemo> BoxMemos = [];

        private static ThemeBoxMemo MemoFor(RitsuShellTheme theme)
        {
            return BoxMemos.GetValue(theme, static _ => new());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves and rounds an integer token using midpoint-to-even rounding; missing or
        ///         unrepresentable values use the fallback.
        ///     </para>
        ///     <para xml:lang="zh-CN">解析整数令牌并按中点到偶数舍入；缺失或无法表示的值使用备用值。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">Nonblank token path, at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">非空白令牌路径，最多 512 个字符。</para>
        /// </param>
        /// <param name="fallback">
        ///     <para xml:lang="en">Signed fallback value.</para>
        ///     <para xml:lang="zh-CN">有符号备用值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Resolved integer or fallback.</para>
        ///     <para xml:lang="zh-CN">已解析的整数或备用值。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or longer than 512 characters.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或超过 512 个字符。</para>
        /// </exception>
        public static int ResolveInt(string path, int fallback)
        {
            ValidatePath(path);
            return ReadInt(RitsuShellTheme.Current, path, fallback);
        }

        private static int ReadInt(RitsuShellTheme theme, string path, int fallback)
        {
            // Keep lookup and rounded-range validation as explicit stages.
            // ReSharper disable once InvertIf
            if (theme.TryGetNumber(path, out var value))
            {
                var rounded = Math.Round(value);
                if (rounded is >= int.MinValue and <= int.MaxValue)
                    return (int)rounded;
            }

            return fallback;
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves a finite float token; missing or unrepresentable values use the fallback.</para>
        ///     <para xml:lang="zh-CN">解析有限浮点令牌；缺失或无法表示的值使用备用值。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">Nonblank token path, at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">非空白令牌路径，最多 512 个字符。</para>
        /// </param>
        /// <param name="fallback">
        ///     <para xml:lang="en">Finite fallback; otherwise ArgumentOutOfRangeException is thrown.</para>
        ///     <para xml:lang="zh-CN">有限备用值；否则抛出 ArgumentOutOfRangeException。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Resolved float or fallback.</para>
        ///     <para xml:lang="zh-CN">已解析的浮点值或备用值。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or longer than 512 characters.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或超过 512 个字符。</para>
        /// </exception>
        public static float ResolveFloat(string path, float fallback)
        {
            ValidatePath(path);
            if (!float.IsFinite(fallback))
                throw new ArgumentOutOfRangeException(nameof(fallback));
            return ReadFloat(RitsuShellTheme.Current, path, fallback);
        }

        private static float ReadFloat(RitsuShellTheme theme, string path, float fallback)
        {
            return theme.TryGetNumber(path, out var value) &&
                   value is >= -float.MaxValue and <= float.MaxValue
                ? (float)value
                : fallback;
        }

        /// <summary>
        ///     <para xml:lang="en">Resolves the base value, then all, then individual edge tokens from one theme snapshot.</para>
        ///     <para xml:lang="zh-CN">从同一主题快照依次解析基础值、all 值和独立边缘令牌。</para>
        /// </summary>
        /// <param name="basePath">
        ///     <para xml:lang="en">Nonblank base path, at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">非空白基础路径，最多 512 个字符。</para>
        /// </param>
        /// <param name="fallbackAll">
        ///     <para xml:lang="en">Signed fallback for all edges.</para>
        ///     <para xml:lang="zh-CN">全部边缘的有符号备用值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Resolved left, top, right, and bottom values.</para>
        ///     <para xml:lang="zh-CN">已解析的左、上、右、下值。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or longer than 512 characters.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或超过 512 个字符。</para>
        /// </exception>
        public static BoxEdges ResolveEdges(string basePath, int fallbackAll)
        {
            ValidatePath(basePath);
            var theme = RitsuShellTheme.Current;
            var memo = MemoFor(theme);
            var key = new BoxMemoKey(basePath, fallbackAll);
            lock (memo.Sync)
            {
                if (memo.Edges.TryGetValue(key, out var cached))
                    return cached;
                var value = ComputeEdges(theme, basePath, fallbackAll);
                if (memo.Edges.Count + memo.Corners.Count < MaximumCachedBoxes)
                    memo.Edges.TryAdd(key, value);
                return value;
            }
        }

        private static BoxEdges ComputeEdges(RitsuShellTheme theme, string basePath, int fallbackAll)
        {
            var all = ReadInt(theme, basePath, fallbackAll);
            all = ReadInt(theme, basePath + ".all", all);
            var left = ReadInt(theme, basePath + ".left", all);
            var top = ReadInt(theme, basePath + ".top", all);
            var right = ReadInt(theme, basePath + ".right", all);
            var bottom = ReadInt(theme, basePath + ".bottom", all);
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
        ///     <para xml:lang="en">The nonblank base path of the corner-radius token group, at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">圆角半径令牌组的非空白基础路径，最多 512 个字符。</para>
        /// </param>
        /// <param name="fallbackUniform">
        ///     <para xml:lang="en">The value used for all corners when no applicable token exists.</para>
        ///     <para xml:lang="zh-CN">没有适用令牌时用于全部圆角的值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved radii for all four corners.</para>
        ///     <para xml:lang="zh-CN">四个角的已解析半径。</para>
        /// </returns>
        public static BoxCorners ResolveCornerRadii(string basePath, int fallbackUniform)
        {
            ValidatePath(basePath);
            var theme = RitsuShellTheme.Current;
            var memo = MemoFor(theme);
            var key = new BoxMemoKey(basePath, fallbackUniform);
            lock (memo.Sync)
            {
                if (memo.Corners.TryGetValue(key, out var cached))
                    return cached;
                var value = ComputeCornerRadii(theme, basePath, fallbackUniform);
                if (memo.Edges.Count + memo.Corners.Count < MaximumCachedBoxes)
                    memo.Corners.TryAdd(key, value);
                return value;
            }
        }

        private static BoxCorners ComputeCornerRadii(RitsuShellTheme theme, string basePath, int fallbackUniform)
        {
            var all = ReadInt(theme, basePath, fallbackUniform);
            all = ReadInt(theme, basePath + ".all", all);
            var tl = ReadInt(theme, basePath + ".topLeft", all);
            var tr = ReadInt(theme, basePath + ".topRight", all);
            var br = ReadInt(theme, basePath + ".bottomRight", all);
            var bl = ReadInt(theme, basePath + ".bottomLeft", all);
            return new(tl, tr, br, bl);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves width/height followed by minWidth/minHeight from one snapshot; negative results become
        ///         zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">从同一快照先解析 width/height，再解析 minWidth/minHeight；负结果按零处理。</para>
        /// </summary>
        /// <param name="basePath">
        ///     <para xml:lang="en">Nonblank base path, at most 512 characters.</para>
        ///     <para xml:lang="zh-CN">非空白基础路径，最多 512 个字符。</para>
        /// </param>
        /// <param name="fallback">
        ///     <para xml:lang="en">Finite nonnegative dimensions; invalid values throw ArgumentOutOfRangeException.</para>
        ///     <para xml:lang="zh-CN">有限且非负的尺寸；无效值抛出 ArgumentOutOfRangeException。</para>
        /// </param>
        /// <param name="allowOverride">
        ///     <para xml:lang="en">False returns the validated fallback without querying theme overrides.</para>
        ///     <para xml:lang="zh-CN">false 表示不查询主题覆盖，直接返回经校验的备用值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">Finite nonnegative dimensions.</para>
        ///     <para xml:lang="zh-CN">有限且非负的尺寸。</para>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">The path is null, blank, or longer than 512 characters.</para>
        ///     <para xml:lang="zh-CN">路径为 null、空白或超过 512 个字符。</para>
        /// </exception>
        public static Vector2 ResolveMinSize(string basePath, Vector2 fallback, bool allowOverride = true)
        {
            ValidatePath(basePath);
            if (!float.IsFinite(fallback.X) || !float.IsFinite(fallback.Y) || fallback.X < 0 || fallback.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(fallback));
            if (!allowOverride)
                return fallback;

            var theme = RitsuShellTheme.Current;
            var width = ReadFloat(theme, basePath + ".width", fallback.X);
            width = ReadFloat(theme, basePath + ".minWidth", width);
            var height = ReadFloat(theme, basePath + ".height", fallback.Y);
            height = ReadFloat(theme, basePath + ".minHeight", height);
            return new(Math.Max(0, width), Math.Max(0, height));
        }

        private static void ValidatePath(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            if (path.Length > 512)
                throw new ArgumentException("A layout token path may contain at most 512 characters.", nameof(path));
        }

        private readonly record struct BoxMemoKey(string BasePath, int Fallback);

        private sealed class ThemeBoxMemo
        {
            public readonly ConcurrentDictionary<BoxMemoKey, BoxCorners> Corners = new();
            public readonly ConcurrentDictionary<BoxMemoKey, BoxEdges> Edges = new();
            public readonly object Sync = new();
        }
    }
}
