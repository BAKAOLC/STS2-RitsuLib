using System.Collections;
using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal static class ModSettingsMirrorTextPolicy
    {
        public static string ResolveLangMap(IDictionary? map, string fallback, Func<string?> resolveCurrentLang)
        {
            if (map == null || map.Count == 0)
                return fallback;

            var lang = resolveCurrentLang();
            if (!string.IsNullOrEmpty(lang) && map.Contains(lang) && map[lang] is string exact)
                return exact;

            foreach (DictionaryEntry entry in map)
            {
                if (entry.Key is not string key || entry.Value is not string value)
                    continue;
                if (lang != null && (lang.StartsWith(key, StringComparison.OrdinalIgnoreCase) ||
                                     key.StartsWith(lang, StringComparison.OrdinalIgnoreCase)))
                    return value;
            }

            if (map.Contains("en") && map["en"] is string en)
                return en;

            foreach (DictionaryEntry entry in map)
                if (entry.Value is string value)
                    return value;

            return fallback;
        }

        public static string? TryI18N(string? key, string fallback, MethodInfo? i18NGet)
        {
            if (i18NGet == null || string.IsNullOrWhiteSpace(key))
                return null;

            try
            {
                return i18NGet.Invoke(null, [key, fallback]) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
