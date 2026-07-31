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
    ///     <para xml:lang="en">
    ///         Converts resolved <see cref="LeafToken" /> values into colors, finite numbers, Boolean values,
    ///         and fonts used by shell-theme snapshots.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将已解析的 <see cref="LeafToken" /> 值转换为 Shell 主题快照所用的颜色、有限数值、布尔值及字体。
    ///     </para>
    /// </summary>
    internal static class RitsuShellThemeValueCoerce
    {
        /// <summary>
        ///     <para xml:lang="en">The default font resource used when a font token cannot be loaded.</para>
        ///     <para xml:lang="zh-CN">字体令牌无法加载时使用的默认字体资源。</para>
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
        ///     <para xml:lang="en">Tries to convert a hexadecimal string leaf to a <see cref="Color" />.</para>
        ///     <para xml:lang="zh-CN">尝试将十六进制字符串叶令牌转换为 <see cref="Color" />。</para>
        /// </summary>
        /// <param name="leaf">
        ///     <para xml:lang="en">The leaf token to convert.</para>
        ///     <para xml:lang="zh-CN">要转换的叶令牌。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">
        ///         Receives the converted color, or <see cref="Colors.Transparent" /> on failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收转换后的颜色；转换失败时为 <see cref="Colors.Transparent" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the leaf contains a supported color; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若叶令牌包含受支持的颜色，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryAsColor(LeafToken? leaf, out Color color)
        {
            color = Colors.Transparent;
            return leaf?.Value is string s && TryParseHexColor(s, out color);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to convert a numeric, Boolean, or invariant-culture numeric string leaf to a finite
        ///         <see cref="double" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将数值、布尔值或使用固定区域性格式的数字字符串叶令牌转换为有限 <see cref="double" />。
        ///     </para>
        /// </summary>
        /// <param name="leaf">
        ///     <para xml:lang="en">The leaf token to convert.</para>
        ///     <para xml:lang="zh-CN">要转换的叶令牌。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">Receives the converted value, or <c>0</c> on failure.</para>
        ///     <para xml:lang="zh-CN">接收转换后的值；转换失败时为 <c>0</c>。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if conversion produces a finite value; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若转换得到有限数值，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryAsDouble(LeafToken? leaf, out double value)
        {
            value = 0;
            switch (leaf?.Value)
            {
                case null:
                    return false;
                case double d when double.IsFinite(d):
                    value = d;
                    return true;
                case double:
                    return false;
                case long l:
                    value = l;
                    return true;
                case int i:
                    value = i;
                    return true;
                case bool b:
                    value = b ? 1d : 0d;
                    return true;
                case string s when double.TryParse(
                    s,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed) && double.IsFinite(parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to convert a Boolean, numeric, or Boolean string leaf to <see cref="bool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将布尔值、数值或布尔字符串叶令牌转换为 <see cref="bool" />。
        ///     </para>
        /// </summary>
        /// <param name="leaf">
        ///     <para xml:lang="en">The leaf token to convert.</para>
        ///     <para xml:lang="zh-CN">要转换的叶令牌。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">Receives the converted value, or <see langword="false" /> on failure.</para>
        ///     <para xml:lang="zh-CN">接收转换后的值；转换失败时为 <see langword="false" />。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the leaf can be converted; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若叶令牌可以转换，则为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
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
                case double d when double.IsFinite(d):
                    value = d >= 0.5;
                    return true;
                case double:
                    return false;
                case string s when bool.TryParse(s, out var parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves a font from a Godot resource path or theme-relative path stored in a leaf token.
        ///         Compatible external locale-font substitutions take precedence; unresolved paths use
        ///         <see cref="DefaultFontFallbackPath" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据叶令牌中的 Godot 资源路径或主题相对路径解析字体。兼容的外部本地化字体替换优先；
        ///         路径无法解析时使用 <see cref="DefaultFontFallbackPath" />。
        ///     </para>
        /// </summary>
        /// <param name="leaf">
        ///     <para xml:lang="en">The leaf token containing the font path.</para>
        ///     <para xml:lang="zh-CN">包含字体路径的叶令牌。</para>
        /// </param>
        /// <param name="fontType">
        ///     <para xml:lang="en">The font role requested from an external locale-font substitution.</para>
        ///     <para xml:lang="zh-CN">向外部本地化字体替换请求的字体角色。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved font or fallback font.</para>
        ///     <para xml:lang="zh-CN">解析出的字体或回退字体。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Tries to parse an <c>RRGGBB</c> or <c>RRGGBBAA</c> hexadecimal color, with an optional leading
        ///         number sign.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试解析 <c>RRGGBB</c> 或 <c>RRGGBBAA</c> 十六进制颜色，可带前导井号。
        ///     </para>
        /// </summary>
        /// <param name="raw">
        ///     <para xml:lang="en">The hexadecimal color text.</para>
        ///     <para xml:lang="zh-CN">十六进制颜色文本。</para>
        /// </param>
        /// <param name="color">
        ///     <para xml:lang="en">
        ///         Receives the parsed color, or <see cref="Colors.Transparent" /> on failure.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         接收解析出的颜色；解析失败时为 <see cref="Colors.Transparent" />。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if <paramref name="raw" /> has a supported valid format; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若 <paramref name="raw" /> 为受支持的有效格式，则为 <see langword="true" />；否则为
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        public static bool TryParseHexColor(string raw, out Color color)
        {
            color = Colors.Transparent;
            var s = raw.Trim();
            if (s.Length > 0 && s[0] == '#')
                s = s[1..];

            if (s.Length != 6 && s.Length != 8)
                return false;

            if (!byte.TryParse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !byte.TryParse(s[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !byte.TryParse(s[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return false;

            byte a = 255;
            if (s.Length == 8 &&
                !byte.TryParse(s[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                return false;

            color = new(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
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

            try
            {
                if (Path.IsPathRooted(path))
                    return false;

                var themesRoot = Path.GetFullPath(themesAbs);
                var absolutePath = Path.GetFullPath(Path.Combine(themesRoot, path));
                var relativePath = Path.GetRelativePath(themesRoot, absolutePath);
                if (relativePath == ".." ||
                    relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                    Path.IsPathRooted(relativePath))
                    return false;

                path = ProjectSettings.LocalizePath(absolutePath);
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[ShellTheme] Could not resolve theme-relative font path '{path}': {ex}");
                return false;
            }
        }
    }
}
