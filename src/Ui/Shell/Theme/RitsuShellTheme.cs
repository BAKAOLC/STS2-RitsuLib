using System.Collections.Concurrent;
using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents an immutable snapshot of a resolved shell theme. It exposes typed color, text, surface,
    ///         component, metric, and font tokens, as well as path-based access and per-mod extension data.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示已解析外壳主题的不可变快照。该快照公开类型化的颜色、文本、表面、组件、度量及字体令牌，
    ///         同时提供基于路径的访问方式和各模组的扩展数据。
    ///     </para>
    /// </summary>
    public sealed class RitsuShellTheme
    {
        private readonly Dictionary<string, JsonElement> _extensions;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Caches leaf lookup results by dotted path. Because the token tree is immutable after snapshot
        ///         construction, both successful lookups and misses can be reused safely.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按点分路径缓存叶令牌查找结果。令牌树在快照构建后保持不变，因此命中结果和未命中结果均可安全复用。
        ///     </para>
        /// </summary>
        private readonly ConcurrentDictionary<string, LeafToken?> _leafCache = new(StringComparer.Ordinal);

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
        ///     <para xml:lang="en">
        ///         Gets the snapshot currently published by <see cref="RitsuShellThemeRuntime" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="RitsuShellThemeRuntime" /> 当前发布的主题快照。
        ///     </para>
        /// </summary>
        public static RitsuShellTheme Current => RitsuShellThemeRuntime.Current;

        /// <summary>
        ///     <para xml:lang="en">Gets the normalized, lowercase identifier of the resolved theme.</para>
        ///     <para xml:lang="zh-CN">获取已解析主题的规范化小写标识符。</para>
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the top-level palette and shadow colors.</para>
        ///     <para xml:lang="zh-CN">获取顶层调色板及阴影颜色。</para>
        /// </summary>
        public ColorTokens Color { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the colors used for rich text, labels, hints, and related text.</para>
        ///     <para xml:lang="zh-CN">获取富文本、标签、提示及相关文本所用的颜色。</para>
        /// </summary>
        public TextTokens Text { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the colors used by panes, entries, and framed surfaces.</para>
        ///     <para xml:lang="zh-CN">获取窗格、条目及带框表面所用的颜色。</para>
        /// </summary>
        public SurfaceTokens Surface { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the visual tokens for shell UI components.</para>
        ///     <para xml:lang="zh-CN">获取外壳界面组件的视觉令牌。</para>
        /// </summary>
        public ComponentTokens Component { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets radii, border widths, dimensions, font sizes, and other metrics.</para>
        ///     <para xml:lang="zh-CN">获取圆角半径、边框宽度、尺寸、字号及其他度量。</para>
        /// </summary>
        public MetricTokens Metric { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the fonts resolved for the theme.</para>
        ///     <para xml:lang="zh-CN">获取为该主题解析的字体。</para>
        /// </summary>
        public FontTokens Font { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the color at <paramref name="path" />, such as <c>components.toggle.on.bg</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="path" /> 处的颜色，例如 <c>components.toggle.on.bg</c>。
        ///     </para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The resolved color, or <see cref="Colors.Magenta" /> if the token is missing or invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析出的颜色；令牌缺失或无效时为 <see cref="Colors.Magenta" />。
        ///     </para>
        /// </returns>
        public Color GetColor(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsColor(leaf, out var color))
                return color;
            return Colors.Magenta;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the color at <paramref name="path" />.</para>
        ///     <para xml:lang="zh-CN">尝试获取 <paramref name="path" /> 处的颜色。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">
        ///         Receives the resolved color, or <see cref="Colors.Transparent" /> on failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收解析出的颜色；解析失败时为 <see cref="Colors.Transparent" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if a valid color was resolved; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若解析出有效颜色，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public bool TryGetColor(string path, out Color color)
        {
            color = Colors.Transparent;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsColor(leaf, out color);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the numeric token at <paramref name="path" /> as a <see cref="float" />.</para>
        ///     <para xml:lang="zh-CN">以 <see cref="float" /> 获取 <paramref name="path" /> 处的数值令牌。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The resolved value, or <c>0</c> if the token is missing, invalid, or outside the
        ///         <see cref="float" /> range.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析出的值；令牌缺失、无效或超出 <see cref="float" /> 范围时为 <c>0</c>。
        ///     </para>
        /// </returns>
        public float GetDimension(string path)
        {
            var value = GetDimensionDouble(path);
            return value is >= -float.MaxValue and <= float.MaxValue
                ? (float)value
                : 0f;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the numeric token at <paramref name="path" /> as a <see cref="double" />.</para>
        ///     <para xml:lang="zh-CN">以 <see cref="double" /> 获取 <paramref name="path" /> 处的数值令牌。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The resolved finite value, or <c>0</c> if the token is missing or invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析出的有限数值；令牌缺失或无效时为 <c>0</c>。
        ///     </para>
        /// </returns>
        public double GetDimensionDouble(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsDouble(leaf, out var value))
                return value;
            return 0d;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the numeric token at <paramref name="path" /> as a finite <see cref="double" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试以有限 <see cref="double" /> 获取 <paramref name="path" /> 处的数值令牌。
        ///     </para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">Receives the resolved value, or <c>0</c> on failure.</para>
        ///     <para xml:lang="zh-CN">接收解析出的值；解析失败时为 <c>0</c>。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if a valid finite value was resolved; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若解析出有效的有限数值，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public bool TryGetNumber(string path, out double value)
        {
            value = 0d;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsDouble(leaf, out value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the numeric token at <paramref name="path" /> as an <see cref="int" />, rounding midpoint
        ///         values away from zero.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         以 <see cref="int" /> 获取 <paramref name="path" /> 处的数值令牌，中点值向远离零的方向舍入。
        ///     </para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The rounded value, or <c>0</c> if the token is missing, invalid, or outside the
        ///         <see cref="int" /> range.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         舍入后的值；令牌缺失、无效或超出 <see cref="int" /> 范围时为 <c>0</c>。
        ///     </para>
        /// </returns>
        public int GetDimensionInt(string path)
        {
            var rounded = Math.Round(GetDimensionDouble(path), MidpointRounding.AwayFromZero);
            return rounded is >= int.MinValue and <= int.MaxValue
                ? (int)rounded
                : 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the Boolean token at <paramref name="path" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="path" /> 处的布尔令牌。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The resolved value, or <see langword="false" /> if the token is missing or invalid.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析出的值；令牌缺失或无效时为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public bool GetBool(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsBool(leaf, out var value))
                return value;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the font family at <paramref name="path" />, using the configured fallback font when the
        ///         token cannot be loaded.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="path" /> 处的字体族；令牌无法加载时使用配置的后备字体。
        ///     </para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">The dotted token path.</para>
        ///     <para xml:lang="zh-CN">点分令牌路径。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved font or the configured fallback font.</para>
        ///     <para xml:lang="zh-CN">解析出的字体或配置的后备字体。</para>
        /// </returns>
        public Font GetFontFamily(string path)
        {
            TryFindLeaf(path, out var leaf);
            return RitsuShellThemeValueCoerce.AsFont(leaf);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the merged extension data contributed under <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取以 <paramref name="modId" /> 名义贡献并合并后的扩展数据。
        ///     </para>
        /// </summary>
        /// <param name="modId">
        ///     <para xml:lang="en">The exact mod identifier used by the extension entry.</para>
        ///     <para xml:lang="zh-CN">扩展条目使用的精确模组标识符。</para>
        /// </param>
        /// <param name="json">
        ///     <para xml:lang="en">
        ///         Receives the extension JSON, or <see langword="default" /> if no entry exists.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收扩展 JSON；条目不存在时为 <see langword="default" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if an extension entry exists for the identifier; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若该标识符存在扩展条目，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public bool TryGetExtension(string modId, out JsonElement json)
        {
            return _extensions.TryGetValue(modId, out json);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Lists the mod identifiers that contributed <c>extensions.&lt;modId&gt;</c> data to this snapshot.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         列出向此快照贡献 <c>extensions.&lt;modId&gt;</c> 数据的模组标识符。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">A new list sorted by ordinal identifier order.</para>
        ///     <para xml:lang="zh-CN">按标识符序数顺序排列的新列表。</para>
        /// </returns>
        public IReadOnlyList<string> ListExtensionModIds()
        {
            var keys = _extensions.Keys.ToArray();
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }

        private bool TryFindLeaf(string path, out LeafToken? leaf)
        {
            if (_leafCache.TryGetValue(path, out leaf))
                return leaf is not null;

            var found = RitsuShellThemeReferenceResolver.TryFindLeaf(_root, path, out leaf);
            _leafCache[path] = found ? leaf : null;
            return found;
        }
    }
}
