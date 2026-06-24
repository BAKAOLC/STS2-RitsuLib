using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    internal readonly record struct BoxEdges(int Left, int Top, int Right, int Bottom);

    /// <summary>
    ///     Per-corner radii for <see cref="StyleBoxFlat" />, resolved like <see cref="BoxEdges" /> with
    ///     <c>all</c> plus optional <c>topLeft</c> / <c>topRight</c> / <c>bottomRight</c> / <c>bottomLeft</c> leaves.
    ///     <see cref="StyleBoxFlat" /> 的逐角半径，解析方式类似 <see cref="BoxEdges" />，包含
    ///     <c>all</c> 以及可选的 <c>topLeft</c> / <c>topRight</c> / <c>bottomRight</c> / <c>bottomLeft</c> 叶节点。
    /// </summary>
    internal readonly record struct BoxCorners(int TopLeft, int TopRight, int BottomRight, int BottomLeft);

    internal static class RitsuShellThemeLayoutResolver
    {
        /// <summary>
        ///     Per-theme memo of resolved <see cref="BoxEdges" /> / <see cref="BoxCorners" />, keyed by
        ///     <c>(basePath, fallback)</c>. Both <see cref="ResolveEdges" /> and <see cref="ResolveCornerRadii" />
        ///     issue up to six token reads plus six path concatenations per call, and are invoked many times per
        ///     <see cref="StyleBoxFlat" /> while building a settings page. The table is keyed by the immutable
        ///     theme snapshot, so it is dropped automatically (via <see cref="ConditionalWeakTable{TKey,TValue}" />)
        ///     when <see cref="RitsuShellTheme.Current" /> is replaced on a theme change.
        ///     逐主题缓存已解析的 <see cref="BoxEdges" /> / <see cref="BoxCorners" />，键为 <c>(basePath, fallback)</c>。
        ///     <see cref="ResolveEdges" /> 与 <see cref="ResolveCornerRadii" /> 每次调用都会做多达六次令牌读取加六次路径拼接，
        ///     并在构建设置页面时为每个 <see cref="StyleBoxFlat" /> 多次调用。该表以不可变主题快照为键，故在主题变化导致
        ///     <see cref="RitsuShellTheme.Current" /> 被替换时通过 <see cref="ConditionalWeakTable{TKey,TValue}" /> 自动失效。
        /// </summary>
        private static readonly ConditionalWeakTable<RitsuShellTheme, ThemeBoxMemo> BoxMemos = new();

        private static ThemeBoxMemo MemoFor(RitsuShellTheme theme)
        {
            return BoxMemos.GetValue(theme, static _ => new());
        }

        internal static int ResolveInt(string path, int fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value)
                ? (int)Math.Round(value)
                : fallback;
        }

        internal static float ResolveFloat(string path, float fallback)
        {
            return RitsuShellTheme.Current.TryGetNumber(path, out var value)
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
        ///     Resolves corner radii at <paramref name="basePath" /> using the same <c>all</c> + per-side pattern as
        ///     <see cref="ResolveEdges" />.
        ///     解析 <paramref name="basePath" /> 处的圆角半径，使用相同的 <c>all</c> + 单边模式，
        ///     与 <see cref="ResolveEdges" /> 一致。
        /// </summary>
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
