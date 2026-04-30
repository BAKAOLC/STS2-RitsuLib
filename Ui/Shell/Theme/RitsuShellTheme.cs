using System.Text.Json;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Immutable snapshot of a resolved shell theme. Exposes typed groups of color, text, surface,
    ///     component, metric, and font tokens as well as path-based dynamic accessors and per-mod extension
    ///     blobs.
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
        /// </summary>
        public static RitsuShellTheme Current => RitsuShellThemeRuntime.Current;

        /// <summary>
        ///     Resolved theme id (lowercase).
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Top-level palette colors (white, transparent, divider, ...).
        /// </summary>
        public ColorTokens Color { get; }

        /// <summary>
        ///     Typography colors (rich text, labels, hints).
        /// </summary>
        public TextTokens Text { get; }

        /// <summary>
        ///     Surface backgrounds (panes + entry chrome).
        /// </summary>
        public SurfaceTokens Surface { get; }

        /// <summary>
        ///     Component tokens (toggle, dropdown, sidebar button, ...).
        /// </summary>
        public ComponentTokens Component { get; }

        /// <summary>
        ///     Numeric metrics (radius, border width, sizing, font size, ...).
        /// </summary>
        public MetricTokens Metric { get; }

        /// <summary>
        ///     Theme-resolved fonts.
        /// </summary>
        public FontTokens Font { get; }

        /// <summary>
        ///     Resolves a color leaf at <paramref name="path" /> (e.g. <c>components.toggle.on.bg</c>).
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved color, or <see cref="Colors.Magenta" /> when missing.</returns>
        public Color GetColor(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsColor(leaf, out var color))
                return color;
            return Colors.Magenta;
        }

        /// <summary>
        ///     Tries to resolve a color leaf at <paramref name="path" />.
        /// </summary>
        public bool TryGetColor(string path, out Color color)
        {
            color = Colors.Transparent;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsColor(leaf, out color);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="float" />.
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved number, or <c>0</c> when missing.</returns>
        public float GetDimension(string path)
        {
            return (float)GetDimensionDouble(path);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="double" />.
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved number, or <c>0</c> when missing.</returns>
        public double GetDimensionDouble(string path)
        {
            if (TryFindLeaf(path, out var leaf) &&
                RitsuShellThemeValueCoerce.TryAsDouble(leaf, out var value))
                return value;
            return 0d;
        }

        /// <summary>
        ///     Tries to resolve a numeric leaf at <paramref name="path" /> as <see cref="double" />.
        /// </summary>
        public bool TryGetNumber(string path, out double value)
        {
            value = 0d;
            return TryFindLeaf(path, out var leaf) && RitsuShellThemeValueCoerce.TryAsDouble(leaf, out value);
        }

        /// <summary>
        ///     Resolves a numeric leaf at <paramref name="path" /> as <see cref="int" /> (rounded).
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved integer, or <c>0</c> when missing.</returns>
        public int GetDimensionInt(string path)
        {
            return (int)Math.Round(GetDimensionDouble(path), MidpointRounding.AwayFromZero);
        }

        /// <summary>
        ///     Resolves a boolean leaf at <paramref name="path" />.
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved boolean, or <c>false</c> when missing.</returns>
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
        /// </summary>
        /// <param name="path">Dotted DTFM path.</param>
        /// <returns>The resolved font.</returns>
        public Font GetFontFamily(string path)
        {
            TryFindLeaf(path, out var leaf);
            return RitsuShellThemeValueCoerce.AsFont(leaf);
        }

        /// <summary>
        ///     Returns the merged extension blob owned by <paramref name="modId" />.
        /// </summary>
        /// <param name="modId">Mod identifier.</param>
        /// <param name="json">Extension JSON, or <see langword="default" /> when none.</param>
        /// <returns><see langword="true" /> if an extension entry exists for the mod.</returns>
        public bool TryGetExtension(string modId, out JsonElement json)
        {
            return _extensions.TryGetValue(modId, out json);
        }

        /// <summary>
        ///     Mod ids that contributed an <c>extensions.&lt;modId&gt;</c> blob to this snapshot.
        /// </summary>
        /// <returns>Sorted mod identifier list.</returns>
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
