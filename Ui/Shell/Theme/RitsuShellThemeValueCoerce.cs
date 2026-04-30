using System.Globalization;
using Godot;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Coerces resolved <see cref="LeafToken" /> values to typed CLR values used by the snapshot
    ///     (<see cref="Color" />, <see cref="float" />, <see cref="int" />, <see cref="bool" />, <see cref="Font" />).
    /// </summary>
    internal static class RitsuShellThemeValueCoerce
    {
        /// <summary>
        ///     Default font fallback path used when a font token cannot be loaded.
        /// </summary>
        public const string DefaultFontFallbackPath = "res://themes/kreon_regular_shared.tres";

        private static readonly Lock FontGate = new();
        private static readonly Dictionary<string, Font> FontCache = new(StringComparer.Ordinal);
        private static Font? _fallbackFont;

        /// <summary>
        ///     Coerces a leaf token to <see cref="Color" />.
        /// </summary>
        public static bool TryAsColor(LeafToken? leaf, out Color color)
        {
            color = Colors.Transparent;
            return leaf?.Value is string s && TryParseHexColor(s, out color);
        }

        /// <summary>
        ///     Coerces a leaf token to a <see cref="double" /> dimension.
        /// </summary>
        public static bool TryAsDouble(LeafToken? leaf, out double value)
        {
            value = 0;
            switch (leaf?.Value)
            {
                case null:
                    return false;
                case double d:
                    value = d;
                    return true;
                case long l:
                    value = l;
                    return true;
                case int i:
                    value = i;
                    return true;
                case bool b:
                    value = b ? 1d : 0d;
                    return true;
                case string s:
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Coerces a leaf token to <see cref="bool" />.
        /// </summary>
        public static bool TryAsBool(LeafToken? leaf, out bool value)
        {
            value = false;
            switch (leaf?.Value)
            {
                case bool b:
                    value = b;
                    return true;
                case long l:
                    value = l != 0;
                    return true;
                case double d:
                    value = d >= 0.5;
                    return true;
                case string s when bool.TryParse(s, out var parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Loads a font from a leaf token (Godot resource path or theme-relative file). Falls back to the shared
        ///     Kreon Regular font when the path cannot be resolved.
        /// </summary>
        public static Font AsFont(LeafToken? leaf)
        {
            var path = leaf?.Value as string;
            return TryLoadFont(path);
        }

        /// <summary>
        ///     Parses <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.
        /// </summary>
        public static bool TryParseHexColor(string raw, out Color color)
        {
            color = Colors.Transparent;
            var s = raw.Trim();
            if (s.Length > 0 && s[0] == '#')
                s = s[1..];

            if (s.Length != 6 && s.Length != 8)
                return false;

            try
            {
                var r = byte.Parse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var g = byte.Parse(s[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var b = byte.Parse(s[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                byte a = 255;
                if (s.Length == 8)
                    a = byte.Parse(s[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                color = new(r / 255f, g / 255f, b / 255f, a / 255f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Font TryLoadFont(string? rawPath)
        {
            var fallback = GetFallbackFont();
            if (string.IsNullOrWhiteSpace(rawPath))
                return fallback;

            var s = rawPath.Trim();
            if (!TryNormalizeFontPath(ref s))
                return fallback;

            lock (FontGate)
            {
                if (FontCache.TryGetValue(s, out var cached) && GodotObject.IsInstanceValid(cached))
                    return cached;

                var loaded = ResourceLoader.Load<Font>(s);

                if (loaded == null || !GodotObject.IsInstanceValid(loaded))
                    loaded = fallback;

                FontCache[s] = loaded;
                return loaded;
            }
        }

        private static Font GetFallbackFont()
        {
            lock (FontGate)
            {
                if (_fallbackFont != null && GodotObject.IsInstanceValid(_fallbackFont))
                    return _fallbackFont;

                var loaded = ResourceLoader.Load<Font>(DefaultFontFallbackPath);
                if (loaded == null || !GodotObject.IsInstanceValid(loaded))
                    loaded = new FontVariation();

                _fallbackFont = loaded;
                FontCache[DefaultFontFallbackPath] = loaded;
                return loaded;
            }
        }

        private static bool TryNormalizeFontPath(ref string path)
        {
            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!RitsuShellThemePaths.TryEnsureShellThemesDirectory(out var themesAbs))
                return false;

            var abs = Path.Combine(themesAbs, path);
            path = ProjectSettings.LocalizePath(abs);
            return true;
        }
    }
}
