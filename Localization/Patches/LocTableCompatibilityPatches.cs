using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    internal static class LocTableCompatibilityPatchHelper
    {
        private static readonly FieldInfo? NameField = typeof(LocTable)
            .GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Lock WarnLock = new();
        private static readonly HashSet<string> WarnedMissingKeys = [];

        internal static bool ShouldUsePlaceholder(LocTable table, string key, string methodName, out string tableName)
        {
            tableName = GetTableName(table);

            if (!RitsuLibSettingsStore.IsLocTableCompatEnabled())
                return false;

            if (table.HasEntry(key))
                return false;

            WarnMissingKeyOnce(tableName, key, methodName);
            return true;
        }

        internal static string GetTableName(LocTable table)
        {
            return NameField?.GetValue(table) as string ?? "<unknown>";
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
    }

    /// <summary>
    ///     When <see cref="RitsuLibSettingsStore.IsLocTableCompatEnabled" /> is true, returns a placeholder
    ///     <c>LocString</c> and logs <c>[Localization][DebugCompat]</c> once per key for misses in
    ///     <c>LocTable.GetLocString</c>. When false, vanilla throw-on-miss behavior applies.
    ///     当 <see cref="RitsuLibSettingsStore.IsLocTableCompatEnabled" /> 为 true 时，为
    ///     <c>LocTable.GetLocString</c> 中的缺失项返回占位 <c>LocString</c>，并按每个键记录一次 <c>[Localization][DebugCompat]</c>。为 false
    ///     时，使用原版缺失即抛出的行为。
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

        /// <summary>
        ///     Short-circuits the target method with a synthesized loc string when a placeholder is required.
        ///     需要 placeholder 时，用合成的 loc string 短路目标方法。
        /// </summary>
        public static bool Prefix(LocTable __instance, string key, ref LocString __result)
        {
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
    ///     当 <see cref="RitsuLibSettingsStore.IsLocTableCompatEnabled" /> 为 true 时，返回 raw key
    ///     字符串，并为 <c>LocTable.GetRawText</c> 中的缺失项按每个键记录一次 <c>[Localization][DebugCompat]</c>。
    ///     为 false 时，使用原版缺失即抛出的行为。
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

        /// <summary>
        ///     Short-circuits the target method with the key as raw text when a placeholder is required.
        ///     需要占位时，以键作为 raw text 短路目标方法。
        /// </summary>
        public static bool Prefix(LocTable __instance, string key, ref string __result)
        {
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
