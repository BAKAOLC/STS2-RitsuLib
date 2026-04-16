using System.Reflection;

namespace STS2RitsuLib.Settings
{
    internal enum ModSettingsMirrorSource
    {
        BaseLib,
        BaseLibToRitsuGenerated,
        ModConfig,
        RuntimeInterop,
    }

    internal static class ModSettingsMirrorInteropPolicy
    {
        private const string DirectivePrefix = "RitsuLib.ModSettingsMirror.";
        private const string GlobalScope = "Global";
        private const string ModScope = "Mod";
        private const string TypeScope = "Type";
        private const string DisableSourcesField = "DisableSources";
        private const string PreferredSourceField = "PreferredSource";

        public static bool ShouldMirror(ModSettingsMirrorSource source, string modId, Type? settingsType = null)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return true;

            var disabled = CollectDisabledSources(modId, settingsType);
            if (disabled.Contains(source))
                return false;

            var preferred = ResolvePreferredSource(modId, settingsType);
            return preferred is not { } preferredSource || preferredSource == source;
        }

        private static HashSet<ModSettingsMirrorSource> CollectDisabledSources(string modId, Type? settingsType)
        {
            var disabled = new HashSet<ModSettingsMirrorSource>();
            MergeDisabled(disabled, BuildTypeDirectiveKey(settingsType, DisableSourcesField));
            MergeDisabled(disabled, BuildModDirectiveKey(modId, DisableSourcesField));
            MergeDisabled(disabled, BuildGlobalDirectiveKey(DisableSourcesField));
            return disabled;
        }

        private static void MergeDisabled(HashSet<ModSettingsMirrorSource> bucket, string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            foreach (var value in ReadDirectiveValues(key))
            {
                var parsed = ParseSourceList(value);
                foreach (var source in parsed)
                    bucket.Add(source);
            }
        }

        private static ModSettingsMirrorSource? ResolvePreferredSource(string modId, Type? settingsType)
        {
            var typePreferred = ResolvePreferredSourceByKey(BuildTypeDirectiveKey(settingsType, PreferredSourceField));
            if (typePreferred != null)
                return typePreferred;

            var modPreferred = ResolvePreferredSourceByKey(BuildModDirectiveKey(modId, PreferredSourceField));
            return modPreferred ?? ResolvePreferredSourceByKey(BuildGlobalDirectiveKey(PreferredSourceField));
        }

        private static ModSettingsMirrorSource? ResolvePreferredSourceByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            foreach (var value in ReadDirectiveValues(key))
            {
                var parsed = ParseSingleSource(value);
                if (parsed != null)
                    return parsed;
            }

            return null;
        }

        private static IEnumerable<string> ReadDirectiveValues(string key)
        {
            var values = new List<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                object[] attrs;
                try
                {
                    attrs = asm.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
                }
                catch
                {
                    continue;
                }

                foreach (var attr in attrs)
                {
                    if (attr is not AssemblyMetadataAttribute metadata)
                        continue;

                    if (!string.Equals(metadata.Key, key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrWhiteSpace(metadata.Value))
                        continue;

                    values.Add(metadata.Value);
                }
            }

            return values;
        }

        private static HashSet<ModSettingsMirrorSource> ParseSourceList(string value)
        {
            var result = new HashSet<ModSettingsMirrorSource>();
            if (string.IsNullOrWhiteSpace(value))
                return result;

            var tokens = value.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(ModSettingsMirrorSource.BaseLib);
                    result.Add(ModSettingsMirrorSource.BaseLibToRitsuGenerated);
                    result.Add(ModSettingsMirrorSource.ModConfig);
                    result.Add(ModSettingsMirrorSource.RuntimeInterop);
                    continue;
                }

                if (TryParseSourceToken(token, out var source))
                    result.Add(source);
            }

            return result;
        }

        private static ModSettingsMirrorSource? ParseSingleSource(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var token = value.Trim();
            return TryParseSourceToken(token, out var source) ? source : null;
        }

        private static bool TryParseSourceToken(string token, out ModSettingsMirrorSource source)
        {
            source = default;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Equals("baselib", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("base_lib", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("base-lib", StringComparison.OrdinalIgnoreCase))
            {
                source = ModSettingsMirrorSource.BaseLib;
                return true;
            }

            if (token.Equals("generated", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("baselibtoritsugenerated", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("base_lib_to_ritsu_generated", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("base-lib-to-ritsu-generated", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("baselib-generated", StringComparison.OrdinalIgnoreCase))
            {
                source = ModSettingsMirrorSource.BaseLibToRitsuGenerated;
                return true;
            }

            if (token.Equals("modconfig", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("mod_config", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("mod-config", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("modsettings", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("mod_settings", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("mod-settings", StringComparison.OrdinalIgnoreCase))
            {
                source = ModSettingsMirrorSource.ModConfig;
                return true;
            }

            if (!token.Equals("runtimeinterop", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("runtime_interop", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("runtime-interop", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("reflection", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("reflectioninterop", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("reflection_interop", StringComparison.OrdinalIgnoreCase) &&
                !token.Equals("reflection-interop", StringComparison.OrdinalIgnoreCase)) return false;
            source = ModSettingsMirrorSource.RuntimeInterop;
            return true;
        }

        private static string BuildGlobalDirectiveKey(string field)
        {
            return $"{DirectivePrefix}{GlobalScope}.{field}";
        }

        private static string BuildModDirectiveKey(string modId, string field)
        {
            return $"{DirectivePrefix}{ModScope}.{modId}.{field}";
        }

        private static string? BuildTypeDirectiveKey(Type? settingsType, string field)
        {
            return settingsType?.FullName == null
                ? null
                : $"{DirectivePrefix}{TypeScope}.{settingsType.FullName}.{field}";
        }
    }
}
