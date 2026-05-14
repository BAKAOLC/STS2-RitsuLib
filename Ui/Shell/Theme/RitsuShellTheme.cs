using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Immutable snapshot of a resolved shell theme. Exposes typed groups of color, text, surface,
    ///     component, metric, and font tokens as well as path-based dynamic accessors and per-mod extension
    ///     blobs.
    ///     已解析 shell 主题的不可变快照。公开颜色、文本、表面、
    ///     组件、度量和字体令牌的类型化分组，以及基于路径的动态访问器和按 mod 划分的扩展
    ///     blob。
    /// </summary>
    public sealed class RitsuShellTheme
    {
        private readonly Dictionary<string, JsonElement> _extensions;
        private readonly Dictionary<string, object?> _root;

        internal RitsuShellTheme(string id,
            Dictionary<string, object?> root,
            ColorTokens color, TextTokens text, SurfaceTokens surface,
            ComponentTokens component, MetricTokens metric, FontTokens font,
            Dictionary<string, JsonElement> extensions)
        {
            Id = id;
            _root = root;
            Color = color;
            Text = text;
            Surface = surface;
            Component = component;
            Metric = metric;
            Font = font;
            _extensions = extensions;
        }

        /// <summary>
        ///     Convenience accessor for <see cref="RitsuShellThemeRuntime.Current" />.
        ///     <see cref="RitsuShellThemeRuntime.Current" /> 的便捷访问器。
        /// </summary>
        public static RitsuShellTheme Current => RitsuShellThemeRuntime.Current;

        /// <summary>
        ///     Resolved theme id (lowercase).
        ///     resolved theme id (lowercase).
        ///     解析后的主题 id (小写)。
        ///     解析后的 theme id (小写)。
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Top-level palette colors (white, transparent, divider, ...).
        ///     顶层调色板颜色 (white, transparent, divider,...)。
        /// </summary>
        public ColorTokens Color { get; }

        /// <summary>
        ///     Typography colors (rich text, labels, hints).
        ///     排版颜色 (富文本, 标签, 提示)。
        /// </summary>
        public TextTokens Text { get; }

        /// <summary>
        ///     Surface backgrounds (panes + entry chrome).
        ///     表面背景 (窗格 + 条目 chrome)。
        /// </summary>
        public SurfaceTokens Surface { get; }

        /// <summary>
        ///     Component tokens (toggle, dropdown, sidebar button, ...).
        ///     组件令牌（开关、下拉框、侧边栏按钮等）。
        /// </summary>
        public ComponentTokens Component { get; }

        /// <summary>
        ///     Numeric metrics (radius, border width, sizing, font size, ...).
        ///     数值指标 (半径, 边框宽度, 尺寸, 字体大小,...)。
        /// </summary>
        public MetricTokens Metric { get; }

        /// <summary>
        ///     Theme-resolved fonts.
        ///     Theme-resolved fonts.
        ///     主题解析后的 字体。
        ///     主题解析后的 字体。
        /// </summary>
        public FontTokens Font { get; }

        /// <summary>
        ///     Resolves a color leaf at <paramref name="path" /> (e.g. <c>components.toggle.on.bg</c>).
        ///     解析 <paramref name="path" /> 处的颜色叶节点（例如 <c>components.toggle.on.bg</c>）。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved color, or <see cref="Colors.Magenta" /> when missing.
        ///     解析出的颜色；缺失时为 <see cref="Colors.Magenta" />。
        /// </returns>
        public Color GetColor(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsColor(leaf, out var color))
                return color;
            return Colors.Magenta;
        }

        /// <summary>
        ///     Tries to resolve a color leaf at <paramref name="path" />.
        ///     尝试解析 <paramref name="path" /> 处的颜色叶节点。
        /// </summary>
        public bool TryGetColor(string path, out Color color)
        {
            color = Colors.Transparent;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsColor(leaf, out color);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="float" />.
        ///     将 <paramref name="path" /> 处的数值叶节点解析为 <see cref="float" />。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved number, or <c>0</c> when missing.
        ///     解析出的数字；缺失时为 <c>0</c>。
        /// </returns>
        public float GetDimension(string path)
        {
            return (float)GetDimensionDouble(path);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="double" />.
        ///     将 <paramref name="path" /> 处的数值叶节点解析为 <see cref="double" />。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved number, or <c>0</c> when missing.
        ///     解析出的数字；缺失时为 <c>0</c>。
        /// </returns>
        public double GetDimensionDouble(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsDouble(leaf, out var value))
                return value;
            return 0d;
        }

        /// <summary>
        ///     Tries to resolve a numeric leaf at <paramref name="path" /> as <see cref="double" />.
        ///     尝试将 <paramref name="path" /> 处的数值叶节点解析为 <see cref="double" />。
        /// </summary>
        public bool TryGetNumber(string path, out double value)
        {
            value = 0d;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsDouble(leaf, out value);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="int" /> (rounded).
        ///     将 <paramref name="path" /> 处的数值叶节点解析为 <see cref="int" />（四舍五入）。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved integer, or <c>0</c> when missing.
        ///     解析出的整数；缺失时为 <c>0</c>。
        /// </returns>
        public int GetDimensionInt(string path)
        {
            return (int)Math.Round(GetDimensionDouble(path), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        ///     Resolves a boolean leaf at <paramref name="path" />.
        ///     解析 <paramref name="path" /> 处的布尔叶节点。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved boolean, or <c>false</c> when missing.
        ///     解析出的布尔值；缺失时为 <c>false</c>。
        /// </returns>
        public bool GetBool(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsBool(leaf, out var value))
                return value;
            return false;
        }

        /// <summary>
        ///     Resolves a font family leaf at <paramref name="path" />. Falls back to the shared body font
        ///     when the resource cannot be loaded.
        ///     解析 <paramref name="path" /> 处的字体族叶节点。资源无法加载时，
        ///     回退到共享的正文字体。
        /// </summary>
        /// <param name="path">
        ///     Dotted DTFM path.
        ///     点分隔的 DTFM 路径。
        /// </param>
        /// <returns>
        ///     The resolved font.
        ///     解析出的字体。
        /// </returns>
        public Font GetFontFamily(string path)
        {
            TryFindLeaf(path, out var leaf);
            return RitsuShellThemeValueCoerce.AsFont(leaf);
        }

        /// <summary>
        ///     Returns the merged extension blob owned by <paramref name="modId" />.
        ///     返回 合并后扩展 blob 拥有者 <paramref name="modId" />。
        /// </summary>
        /// <param name="modId">
        ///     Mod identifier.
        ///     Mod 标识符。
        /// </param>
        /// <param name="json">
        ///     Extension JSON, or <see langword="default" /> when none.
        ///     扩展 JSON, or <see langword="default" /> 没有时。
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an extension entry exists for the mod.
        ///     如果该 mod 存在扩展条目，则为 <see langword="true" />。
        /// </returns>
        public bool TryGetExtension(string modId, out JsonElement json)
        {
            return _extensions.TryGetValue(modId, out json);
        }

        /// <summary>
        ///     Mod ids that contributed an <c>extensions.&lt;modId&gt;</c> blob to this snapshot.
        ///     向此快照贡献了 <c>extensions.&lt;modId&gt;</c> blob 的 mod id。
        /// </summary>
        /// <returns>
        ///     Sorted mod identifier list.
        ///     排序后的 mod 标识符列表。
        /// </returns>
        public IReadOnlyList<string> ListExtensionModIds()
        {
            var keys = _extensions.Keys.ToArray();
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }

        private bool TryFindLeaf(string path, out LeafToken? leaf)
        {
            return RitsuShellThemeReferenceResolver.TryFindLeaf(_root, path, out leaf);
        }
    }
}
