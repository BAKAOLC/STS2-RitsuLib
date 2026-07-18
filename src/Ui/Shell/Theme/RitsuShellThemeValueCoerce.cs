using System.Globalization;
using System.Reflection;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using FileAccess = Godot.FileAccess;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Coerces resolved <see cref="LeafToken" /> values to typed CLR values used by the snapshot
    ///     (<see cref="Color" />, <see cref="float" />, <see cref="int" />, <see cref="bool" />, <see cref="Font" />).
    ///     强制转换 解析后的 <see cref="LeafToken" /> 值为类型化 CLR 值 由快照使用
    /// </summary>
    internal static class RitsuShellThemeValueCoerce
    {
        /// <summary>
        ///     Default font fallback path used when a font token cannot be loaded.
        ///     字体令牌无法加载时使用的默认字体回退路径。
        /// </summary>
        public const string DefaultFontFallbackPath = "res://themes/kreon_regular_shared.tres";

        private const string GameFallbacksAppliedMetaKey = "_ritsulib_game_font_fallbacks_applied";

        private static readonly Lock FontGate = new();

        private static readonly System.Collections.Generic.Dictionary<string, Font> FontCache =
            new(StringComparer.Ordinal);

        private static readonly string[] GameFallbackFontPaths =
        [
            "res://themes/fonts/zhs/noto_sans_mono_cjksc_regular_shared.tres",
            "res://themes/fonts/jpn/noto_sans_cjkjp_regular_shared.tres",
            "res://themes/fonts/kor/gyeonggi_cheonnyeon_batang_bold_shared.tres",
            "res://themes/fonts/tha/cs_chat_thai_ui_shared.tres",
            "res://themes/fonts/rus/fira_sans_extra_condensed_regular_shared.tres",
        ];

        private static readonly HashSet<string> GameLocaleFontResourcePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "res://themes/fonts/zhs/noto_sans_mono_cjksc_regular_shared.tres",
            "res://themes/fonts/zhs/source_han_serif_sc_bold_shared.tres",
            "res://themes/fonts/zhs/source_han_serif_sc_medium_shared.tres",
            "res://themes/fonts/jpn/noto_sans_cjkjp_regular_shared.tres",
            "res://themes/fonts/jpn/noto_sans_cjkjp_bold_shared.tres",
            "res://themes/fonts/jpn/noto_sans_cjkjp_medium_shared.tres",
            "res://themes/fonts/kor/gyeonggi_cheonnyeon_batang_bold_shared.tres",
            "res://themes/fonts/tha/cs_chat_thai_ui_shared.tres",
            "res://themes/fonts/rus/fira_sans_extra_condensed_regular_shared.tres",
            "res://themes/fonts/rus/fira_sans_extra_condensed_bold_shared.tres",
            "res://themes/fonts/rus/fira_sans_extra_condensed_italic_shared.tres",
        };

        private static Font? _fallbackFont;

        /// <summary>
        ///     Coerces a leaf token to <see cref="Color" />.
        ///     将叶令牌强制转换为 <see cref="Color" />。
        /// </summary>
        public static bool TryAsColor(LeafToken? leaf, out Color color)
        {
            color = Colors.Transparent;
            return leaf?.Value is string s && TryParseHexColor(s, out color);
        }

        /// <summary>
        ///     Coerces a leaf token to a <see cref="double" /> dimension.
        ///     将叶令牌强制转换为 <see cref="double" /> 尺寸值。
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
        ///     将叶令牌强制转换为 <see cref="bool" />。
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
        ///     从叶令牌加载字体（Godot 资源路径或主题相对文件）。路径无法解析时，回退到共享的
        ///     Kreon Regular 字体。
        /// </summary>
        public static Font AsFont(LeafToken? leaf, FontType fontType = FontType.Regular)
        {
            if (TryGetExternalFontSubstitution(fontType, out var externalFont))
                return externalFont;

            var path = leaf?.Value as string;
            return TryLoadFont(path);
        }

        internal static void InvalidateFontCache()
        {
            lock (FontGate)
            {
                FontCache.Clear();
                _fallbackFont = null;
            }
        }

        internal static bool AreFontTokensCurrent(FontTokens fonts)
        {
            return IsFontTokenCurrent(fonts.Body, FontType.Regular) &&
                   IsFontTokenCurrent(fonts.BodyBold, FontType.Bold) &&
                   IsFontTokenCurrent(fonts.Button, FontType.Bold);
        }

        /// <summary>
        ///     Parses <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.
        ///     解析 <c>#RRGGBB</c> or <c>#RRGGBBAA</c>。
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
                ApplyGameFallbacks(loaded);

                FontCache[s] = loaded;
                return loaded;
            }
        }

        private static bool TryGetExternalFontSubstitution(FontType fontType, out Font font)
        {
            font = null!;
            if (!HasExternalFontSubstitutionPatch())
                return false;

            var language = LocManager.Instance?.Language;
            if (string.IsNullOrWhiteSpace(language) || !FontManager.NeedsFontSubstitution(language))
                return false;

            var substitute = FontManager.GetSubstituteFont(language, fontType);
            if (substitute == null || !GodotObject.IsInstanceValid(substitute))
                return false;
            if (IsGameLocaleFontResource(substitute))
                return false;

            ApplyGameFallbacks(substitute);
            font = substitute;
            return true;
        }

        private static bool IsFontTokenCurrent(Font font, FontType fontType)
        {
            if (!HasExternalFontSubstitutionPatch())
                return true;

            var language = LocManager.Instance?.Language;
            if (string.IsNullOrWhiteSpace(language) || !FontManager.NeedsFontSubstitution(language))
                return true;

            var substitute = FontManager.GetSubstituteFont(language, fontType);
            if (substitute != null && IsGameLocaleFontResource(substitute))
                return true;

            return substitute == null || ReferenceEquals(substitute, font);
        }

        private static bool HasExternalFontSubstitutionPatch()
        {
            return HasHarmonyPatch(
                       AccessTools.Method(
                           typeof(FontManager),
                           "GetFontForLanguage",
                           [typeof(string), typeof(FontType)])) ||
                   HasHarmonyPatch(
                       AccessTools.Method(
                           typeof(FontManager),
                           nameof(FontManager.GetSubstituteFont),
                           [typeof(string), typeof(FontType)]));

            static bool HasHarmonyPatch(MethodBase? method)
            {
                if (method == null)
                    return false;

                var patchInfo = Harmony.GetPatchInfo(method);
                return patchInfo != null &&
                       (patchInfo.Prefixes.Count > 0 ||
                        patchInfo.Postfixes.Count > 0 ||
                        patchInfo.Transpilers.Count > 0 ||
                        patchInfo.Finalizers.Count > 0);
            }
        }

        private static bool IsGameLocaleFontResource(Font font)
        {
            while (true)
            {
                var path = font.ResourcePath;
                if (!string.IsNullOrWhiteSpace(path) && GameLocaleFontResourcePaths.Contains(path)) return true;

                if (font is FontVariation { BaseFont: { } baseFont })
                {
                    font = baseFont;
                    continue;
                }

                if (font is not FontFile fontFile) return false;
                var basePath = fontFile.ResourcePath;
                return !string.IsNullOrWhiteSpace(basePath) && GameLocaleFontResourcePaths.Contains(basePath);
            }
        }

        private static void ApplyGameFallbacks(Font font)
        {
            var baseFont = FindBaseFontFile(font);
            if (baseFont != null)
                AddGameFallbacks(baseFont);
        }

        private static FontFile? FindBaseFontFile(Font font)
        {
            var visited = new HashSet<Font>(ReferenceEqualityComparer.Instance);
            while (visited.Add(font))
            {
                if (font is FontFile fontFile)
                    return fontFile;

                if (font is not FontVariation { BaseFont: { } baseFont })
                    return null;

                font = baseFont;
            }

            return null;
        }

        private static void AddGameFallbacks(FontFile baseFont)
        {
            if (baseFont.HasMeta(GameFallbacksAppliedMetaKey))
                return;

            var combined = new Array<Font>();
            var existing = baseFont.GetFallbacks();
            if (existing != null)
                foreach (var f in existing)
                    combined.Add(f);

            foreach (var path in GameFallbackFontPaths)
                AddFontFallbackIfAvailable(baseFont, combined, path);

            baseFont.SetFallbacks(combined);
            baseFont.SetMeta(GameFallbacksAppliedMetaKey, true);
        }

        private static void AddFontFallbackIfAvailable(FontFile baseFont, Array<Font> target, string path)
        {
            if (TryLoadFontResource(path, out var resourceFont))
            {
                if (ReferenceEquals(resourceFont, baseFont) ||
                    ReferenceEquals(FindBaseFontFile(resourceFont), baseFont))
                    return;

                target.Add(resourceFont);
                return;
            }

            if (!RawFontFileExists(path))
                return;

            var font = new FontFile();
            if (font.LoadDynamicFont(path) == Error.Ok)
            {
                target.Add(font);
                return;
            }

            font.Dispose();
        }

        private static bool TryLoadFontResource(string path, out Font font)
        {
            font = null!;
            if (!IsGodotPath(path) || !ResourceLoader.Exists(path))
                return false;

            var loaded = ResourceLoader.Load<Font>(path);
            if (loaded == null || !GodotObject.IsInstanceValid(loaded))
                return false;

            font = loaded;
            return true;
        }

        private static bool RawFontFileExists(string path)
        {
            return IsGodotPath(path)
                ? FileAccess.FileExists(path)
                : File.Exists(path);
        }

        private static bool IsGodotPath(string path)
        {
            return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("user://", StringComparison.OrdinalIgnoreCase);
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

                ApplyGameFallbacks(loaded);

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
