using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    internal static class LocTableCompatibilityPatchHelper
    {
        private static readonly FieldInfo? NameField = typeof(LocTable)
            .GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? TranslationsField = typeof(LocTable)
            .GetField("_translations", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? FallbackField = typeof(LocTable)
            .GetField("_fallback", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly HashSet<string> AliasSupportedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "achievements",
            "ancients",
            "cards",
            "characters",
            "encounters",
            "enchantments",
            "events",
            "modifiers",
            "monsters",
            "potions",
            "powers",
            "relics",
        };

        private static readonly Lock WarnLock = new();
        private static readonly HashSet<string> WarnedMissingKeys = [];
        private static readonly Lock AliasLock = new();
        private static readonly HashSet<string> LoggedAliasKeys = [];
        private static Dictionary<string, string[]>? _cachedAliases;

        internal static bool TryRewriteCompatKey(LocTable table, ref string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !AliasSupportedTables.Contains(GetTableName(table)) ||
                HasEntryRaw(table, key))
            {
                return false;
            }

            var aliases = GetAliasMap();
            if (aliases.Count == 0)
                return false;

            var segments = key.Split('.');
            var aliasedSegments = new List<(int Index, string[] Replacements)>();
            for (var i = 0; i < segments.Length; i++)
            {
                if (aliases.TryGetValue(segments[i], out var replacements) && replacements.Length > 0)
                {
                    aliasedSegments.Add((i, replacements));
                }
            }

            if (aliasedSegments.Count == 0)
                return false;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in EnumerateAliasCandidates(
                         segments,
                         aliasedSegments,
                         0,
                         replacedAny: false,
                         seen))
            {
                if (!HasEntryRaw(table, candidate))
                    continue;

                LogAliasRewriteOnce(GetTableName(table), key, candidate);
                key = candidate;
                return true;
            }

            return false;
        }

        internal static bool ShouldUsePlaceholder(LocTable table, string key, string methodName, out string tableName)
        {
            tableName = GetTableName(table);

            if (!RitsuLibSettingsStore.IsLocTableCompatEnabled())
                return false;

            if (HasEntryRaw(table, key))
                return false;

            WarnMissingKeyOnce(tableName, key, methodName);
            return true;
        }

        internal static string GetTableName(LocTable table)
        {
            return NameField?.GetValue(table) as string ?? "<unknown>";
        }

        private static IReadOnlyDictionary<string, string[]> GetAliasMap()
        {
            lock (AliasLock)
            {
                if (_cachedAliases is { Count: > 0 })
                    return _cachedAliases;

                _cachedAliases = BuildAliasMap();
                if (_cachedAliases.Count > 0)
                {
                    RitsuLibFramework.Logger.Info(
                        $"[Localization][AndroidCompat] Built {_cachedAliases.Count} model localization alias mapping(s).");
                }

                return _cachedAliases;
            }
        }

        private static Dictionary<string, string[]> BuildAliasMap()
        {
            var aliases = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var snapshot in ModContentRegistry.GetRegisteredTypeSnapshots())
            {
                var bareEntry = snapshot.ModelDbId?.Entry;
                if (string.IsNullOrWhiteSpace(bareEntry))
                    continue;

                AddAlias(aliases, bareEntry, snapshot.ExpectedPublicEntry);
                AddAlias(aliases, bareEntry, BuildLegacyEntry(snapshot.ModId, bareEntry));
            }

            return aliases.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value
                    .Where(static alias => !string.IsNullOrWhiteSpace(alias))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
        }

        private static void AddAlias(IDictionary<string, List<string>> aliases, string bareEntry, string? alias)
        {
            if (string.IsNullOrWhiteSpace(bareEntry) ||
                string.IsNullOrWhiteSpace(alias) ||
                string.Equals(bareEntry, alias, StringComparison.Ordinal))
            {
                return;
            }

            if (!aliases.TryGetValue(bareEntry, out var values))
            {
                values = [];
                aliases[bareEntry] = values;
            }

            values.Add(alias);
        }

        private static string BuildLegacyEntry(string modId, string bareEntry)
        {
            var legacyStem = new string(modId.Where(static c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
            return string.IsNullOrWhiteSpace(legacyStem)
                ? bareEntry
                : $"{legacyStem}-{bareEntry}";
        }

        private static IEnumerable<string> EnumerateAliasCandidates(
            string[] segments,
            IReadOnlyList<(int Index, string[] Replacements)> aliasedSegments,
            int aliasIndex,
            bool replacedAny,
            ISet<string> seen)
        {
            if (aliasIndex >= aliasedSegments.Count)
            {
                if (replacedAny)
                {
                    var candidate = string.Join(".", segments);
                    if (seen.Add(candidate))
                        yield return candidate;
                }

                yield break;
            }

            foreach (var candidate in EnumerateAliasCandidates(
                         segments,
                         aliasedSegments,
                         aliasIndex + 1,
                         replacedAny,
                         seen))
            {
                yield return candidate;
            }

            var (segmentIndex, replacements) = aliasedSegments[aliasIndex];
            var originalSegment = segments[segmentIndex];
            foreach (var replacement in replacements)
            {
                if (string.Equals(originalSegment, replacement, StringComparison.Ordinal))
                    continue;

                segments[segmentIndex] = replacement;
                foreach (var candidate in EnumerateAliasCandidates(
                             segments,
                             aliasedSegments,
                             aliasIndex + 1,
                             replacedAny: true,
                             seen))
                {
                    yield return candidate;
                }
            }

            segments[segmentIndex] = originalSegment;
        }

        private static bool HasEntryRaw(LocTable table, string key)
        {
            for (var current = table; current != null; current = GetFallback(current))
            {
                if (GetTranslations(current)?.ContainsKey(key) == true)
                    return true;
            }

            return false;
        }

        private static Dictionary<string, string>? GetTranslations(LocTable table)
        {
            return TranslationsField?.GetValue(table) as Dictionary<string, string>;
        }

        private static LocTable? GetFallback(LocTable table)
        {
            return FallbackField?.GetValue(table) as LocTable;
        }

        private static void WarnMissingKeyOnce(string tableName, string key, string methodName)
        {
            var warnKey = $"{tableName}:{key}:{methodName}";

            lock (WarnLock)
            {
                if (!WarnedMissingKeys.Add(warnKey))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[Localization][DebugCompat] Missing localization key '{key}' in table '{tableName}' during {methodName}. " +
                "Resolving to key placeholder (debug compat).");
        }

        private static void LogAliasRewriteOnce(string tableName, string sourceKey, string rewrittenKey)
        {
            var logKey = $"{tableName}:{sourceKey}:{rewrittenKey}";

            lock (AliasLock)
            {
                if (!LoggedAliasKeys.Add(logKey))
                    return;
            }

            RitsuLibFramework.Logger.Info(
                $"[Localization][AndroidCompat] Rewrote key '{sourceKey}' -> '{rewrittenKey}' in table '{tableName}'.");
        }
    }

    /// <summary>
    ///     Rewrites bare Android-safe-mode ModelDb keys to fixed public or legacy localization entries before
    ///     <c>LocTable.HasEntry</c> evaluates them. This keeps <c>LocString.Exists</c> and
    ///     <c>LocString.GetIfExists</c> working for modded content when identity detours are intentionally disabled.
    /// </summary>
    public class LocTableHasEntryCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_has_entry_android_compat";

        /// <inheritdoc />
        public static string Description =>
            "Rewrite Android-safe-mode mod localization keys before LocTable.HasEntry runs";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.HasEntry), [typeof(string)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Mutates the lookup key in-place so downstream existence checks see the aliased key.
        /// </summary>
        public static void Prefix(LocTable __instance, ref string key)
            // ReSharper restore InconsistentNaming
        {
            LocTableCompatibilityPatchHelper.TryRewriteCompatKey(__instance, ref key);
        }
    }

    /// <summary>
    ///     When <see cref="RitsuLibSettingsStore.IsLocTableCompatEnabled" /> is true, returns a placeholder
    ///     <c>LocString</c> and logs <c>[Localization][DebugCompat]</c> once per key for misses in
    ///     <c>LocTable.GetLocString</c>. When false, vanilla throw-on-miss behavior applies.
    /// </summary>
    public class LocTableGetLocStringCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_get_loc_string_debug_compat";

        /// <inheritdoc />
        public static string Description =>
            "Use key placeholder for LocTable.GetLocString missing entries in debug compatibility mode";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.GetLocString), [typeof(string)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Short-circuits the target method with a synthesized loc string when a placeholder is required.
        /// </summary>
        public static bool Prefix(LocTable __instance, ref string key, ref LocString __result)
            // ReSharper restore InconsistentNaming
        {
            LocTableCompatibilityPatchHelper.TryRewriteCompatKey(__instance, ref key);

            if (!LocTableCompatibilityPatchHelper.ShouldUsePlaceholder(
                    __instance,
                    key,
                    nameof(LocTable.GetLocString),
                    out var tableName))
                return true;

            __result = new(tableName, key);
            return false;
        }
    }

    /// <summary>
    ///     When <see cref="RitsuLibSettingsStore.IsLocTableCompatEnabled" /> is true, returns the raw key
    ///     string and logs <c>[Localization][DebugCompat]</c> once per key for misses in <c>LocTable.GetRawText</c>.
    ///     When false, vanilla throw-on-miss behavior applies.
    /// </summary>
    public class LocTableGetRawTextCompatibilityPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_get_raw_text_debug_compat";

        /// <inheritdoc />
        public static string Description =>
            "Use key placeholder for LocTable.GetRawText missing entries in debug compatibility mode";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.GetRawText), [typeof(string)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Short-circuits the target method with the key as raw text when a placeholder is required.
        /// </summary>
        public static bool Prefix(LocTable __instance, ref string key, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            LocTableCompatibilityPatchHelper.TryRewriteCompatKey(__instance, ref key);

            if (!LocTableCompatibilityPatchHelper.ShouldUsePlaceholder(
                    __instance,
                    key,
                    nameof(LocTable.GetRawText),
                    out _))
                return true;

            __result = key;
            return false;
        }
    }
}
