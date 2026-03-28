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

            if (!RitsuLibSettingsStore.IsDebugCompatibilityModeEnabled())
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
                "Using key placeholder instead of throwing LocException.");
        }
    }

    /// <summary>
    ///     When debug compatibility mode is enabled, returns a placeholder <c>LocString</c> instead of throwing
    ///     for missing keys in <c>LocTable.GetLocString</c>.
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
        public static bool Prefix(LocTable __instance, string key, ref LocString __result)
            // ReSharper restore InconsistentNaming
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
    ///     When debug compatibility mode is enabled, returns the raw key string instead of throwing for missing
    ///     entries in <c>LocTable.GetRawText</c>.
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
        public static bool Prefix(LocTable __instance, string key, ref string __result)
            // ReSharper restore InconsistentNaming
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
