using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Localization.Patches
{
    internal static class I18NLocTablePatchHelper
    {
        internal static bool TryGetBackingI18N(LocTable table, out I18N i18N)
        {
            if (table is not I18NLocTable i18NLocTable)
                return I18NLocTableBridge.TryGet(LocTableCompatibilityPatchHelper.GetTableName(table), out i18N);
            i18N = i18NLocTable.I18N;
            return true;
        }
    }

    /// <summary>
    ///     Resolves registered virtual I18N tables through <c>LocManager.GetTable</c>.
    ///     通过 <c>LocManager.GetTable</c> 解析已注册的虚拟 I18N table。
    /// </summary>
    internal class LocManagerGetTableI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_manager_get_table_i18n_bridge";
        public static string Description => "Resolve registered I18N virtual tables from LocManager.GetTable";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocManager), nameof(LocManager.GetTable), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Returns the I18N-backed table instance for registered virtual table ids.
        ///     对已注册的虚拟 table id 返回 I18N-backed table 实例。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string name, ref LocTable __result)
        {
            if (!I18NLocTableBridge.TryGetLocTable(name, out var locTable))
                return true;

            __result = locTable;
            return false;
        }
    }

    /// <summary>
    ///     Bridges <c>LocTable.HasEntry</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c> tables.
    ///     将 <c>LocTable.HasEntry</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c> table。
    /// </summary>
    internal class LocTableHasEntryI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_table_has_entry_i18n_bridge";
        public static string Description => "Resolve LocTable.HasEntry via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.HasEntry), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Resolves the key lookup through <see cref="STS2RitsuLib.Utils.I18N" /> when the table name matches a
        ///     registered virtual I18N table id.
        ///     当表名匹配已注册的虚拟 I18N 表 id 时，通过 <see cref="STS2RitsuLib.Utils.I18N" />
        ///     解析 key 查找。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref bool __result)
        {
            if (!I18NLocTablePatchHelper.TryGetBackingI18N(__instance, out var i18N))
                return true;

            __result = i18N.ContainsKey(key);
            return false;
        }
    }

    /// <summary>
    ///     Bridges <c>LocTable.IsLocalKey</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c>
    ///     tables.
    ///     将 <c>LocTable.IsLocalKey</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c>
    ///     table。
    /// </summary>
    internal class LocTableIsLocalKeyI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_table_is_local_key_i18n_bridge";
        public static string Description => "Resolve LocTable.IsLocalKey via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.IsLocalKey), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Reports I18N-backed keys as local keys for SmartFormat culture selection.
        ///     为 SmartFormat culture 选择把 I18N-backed keys 报告为本地 key。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref bool __result)
        {
            if (!I18NLocTablePatchHelper.TryGetBackingI18N(__instance, out var i18N))
                return true;

            __result = i18N.ContainsKey(key);
            return false;
        }
    }

    /// <summary>
    ///     Bridges <c>LocTable.GetRawText</c> to <see cref="I18NLocTableBridge" /> for virtual <c>MODID_I18N_STEM</c> tables.
    ///     将 <c>LocTable.GetRawText</c> 桥接到 <see cref="I18NLocTableBridge" />，用于虚拟 <c>MODID_I18N_STEM</c> table。
    /// </summary>
    internal class LocTableGetRawTextI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_table_get_raw_text_i18n_bridge";
        public static string Description => "Resolve LocTable.GetRawText via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.GetRawText), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Returns the I18N-backed raw template when the table name matches a registered virtual I18N table id.
        ///     当 table name 匹配已注册的虚拟 I18N table id 时，返回 I18N-backed raw template。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref string __result)
        {
            if (!I18NLocTablePatchHelper.TryGetBackingI18N(__instance, out var i18N))
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
    internal class LocTableGetLocStringI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_table_get_loc_string_i18n_bridge";
        public static string Description => "Resolve LocTable.GetLocString via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocTable), nameof(LocTable.GetLocString), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Returns a <see cref="LocString" /> pointing at the virtual table id when the I18N dictionary contains the key.
        ///     当 I18N 字典包含该 key 时，返回指向虚拟表 id 的 <see cref="LocString" />。
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref LocString __result)
        {
            var tableName = __instance is I18NLocTable i18NLocTable
                ? i18NLocTable.Name
                : LocTableCompatibilityPatchHelper.GetTableName(__instance);
            if (!I18NLocTablePatchHelper.TryGetBackingI18N(__instance, out var i18N))
                return true;

            if (!i18N.ContainsKey(key))
                return true;

            __result = new(tableName, key);
            return false;
        }
    }
}
