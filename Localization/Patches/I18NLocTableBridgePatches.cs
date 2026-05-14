using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
    /// <summary>
    ///     Bridges <c>LocTable.HasEntry</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c> tables.
    ///     将 <c>LocTable.HasEntry</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c> table。
    /// </summary>
    public class LocTableHasEntryI18NBridgePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_has_entry_i18n_bridge";

        /// <inheritdoc />
        public static string Description => "Resolve LocTable.HasEntry via registered I18N virtual tables";

        /// <inheritdoc />
        public static bool IsCritical => true;

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
        ///     Resolves the key lookup through <see cref="STS2RitsuLib.Utils.I18N" /> when the table name matches a
        ///     registered virtual I18N table id.
        ///     当表名匹配已注册的虚拟 I18N 表 id 时，通过 <see cref="STS2RitsuLib.Utils.I18N" />
        ///     解析 key 查找。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            var tableName = LocTableCompatibilityPatchHelper.GetTableName(__instance);
            if (!I18NLocTableBridge.TryGet(tableName, out var i18N))
                return true;

            __result = i18N.ContainsKey(key);
            return false;
        }
    }

    /// <summary>
    ///     Bridges <c>LocTable.GetRawText</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c> tables.
    ///     将 <c>LocTable.GetRawText</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c> table。
    /// </summary>
    public class LocTableGetRawTextI18NBridgePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_get_raw_text_i18n_bridge";

        /// <inheritdoc />
        public static string Description => "Resolve LocTable.GetRawText via registered I18N virtual tables";

        /// <inheritdoc />
        public static bool IsCritical => true;

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
        ///     Returns the I18N-backed raw template when the table name matches a registered virtual I18N table id.
        ///     当 table name 匹配已注册的虚拟 I18N table id 时，返回 I18N-backed raw template。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref string __result)
            // ReSharper restore InconsistentNaming
        {
            var tableName = LocTableCompatibilityPatchHelper.GetTableName(__instance);
            if (!I18NLocTableBridge.TryGet(tableName, out var i18N))
                return true;

            if (!i18N.TryGet(key, out var text))
                return true;

            __result = text;
            return false;
        }
    }

    /// <summary>
    ///     Bridges <c>LocTable.GetLocString</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c>
    ///     tables.
    ///     将 <c>LocTable.GetLocString</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c>
    ///     table。
    /// </summary>
    public class LocTableGetLocStringI18NBridgePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "loc_table_get_loc_string_i18n_bridge";

        /// <inheritdoc />
        public static string Description => "Resolve LocTable.GetLocString via registered I18N virtual tables";

        /// <inheritdoc />
        public static bool IsCritical => true;

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
        ///     Returns a <see cref="LocString" /> pointing at the virtual table id when the I18N dictionary contains the key.
        ///     当 I18N 字典包含该 key 时，返回指向虚拟表 id 的 <see cref="LocString" />。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref LocString __result)
            // ReSharper restore InconsistentNaming
        {
            var tableName = LocTableCompatibilityPatchHelper.GetTableName(__instance);
            if (!I18NLocTableBridge.TryGet(tableName, out var i18N))
                return true;

            if (!i18N.ContainsKey(key))
                return true;

            __result = new(tableName, key);
            return false;
        }
    }
}
