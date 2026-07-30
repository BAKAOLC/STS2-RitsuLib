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
    ///     <para xml:lang="en">Resolves registered virtual I18N tables through <c>LocManager.GetTable</c>.</para>
    ///     <para xml:lang="zh-CN">使 <c>LocManager.GetTable</c> 能够解析已注册的虚拟 I18N 表。</para>
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
        ///     <para xml:lang="en">
        ///         Returns the I18N-backed table instance for a registered virtual table ID and skips the original
        ///         method.
        ///     </para>
        ///     <para xml:lang="zh-CN">为已注册的虚拟表 ID 返回由 I18N 支持的表实例，并跳过原方法。</para>
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
    ///     <para xml:lang="en">
    ///         Routes <c>LocTable.HasEntry</c> through <see cref="I18NLocTableBridge" /> for virtual
    ///         <c>MODID_I18N_STEM</c> tables.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         针对虚拟 <c>MODID_I18N_STEM</c> 表，将 <c>LocTable.HasEntry</c> 查询转交给
    ///         <see cref="I18NLocTableBridge" />。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Queries the backing <see cref="STS2RitsuLib.Utils.I18N" /> dictionary when the table maps to a
        ///         registered virtual table ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">当表映射到已注册的虚拟表 ID 时，查询其背后的 <see cref="STS2RitsuLib.Utils.I18N" /> 字典。</para>
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
    ///     <para xml:lang="en">
    ///         Routes <c>LocTable.IsLocalKey</c> through <see cref="I18NLocTableBridge" /> for virtual
    ///         <c>MODID_I18N_STEM</c> tables.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         针对虚拟 <c>MODID_I18N_STEM</c> 表，将 <c>LocTable.IsLocalKey</c> 查询转交给
    ///         <see cref="I18NLocTableBridge" />。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Reports whether the backing I18N dictionary contains a key for the current locale, allowing
        ///         SmartFormat to select the appropriate culture.
        ///     </para>
        ///     <para xml:lang="zh-CN">报告背后的 I18N 字典是否包含当前区域设置下的键，以便 SmartFormat 选择适用的区域性。</para>
        /// </summary>
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocTable __instance, string key, ref bool __result)
        {
            if (!I18NLocTablePatchHelper.TryGetBackingI18N(__instance, out var i18N))
                return true;

            __result = i18N.ContainsLocalKey(key);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <c>LocTable.GetRawText</c> through <see cref="I18NLocTableBridge" /> for virtual
    ///         <c>MODID_I18N_STEM</c> tables.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         针对虚拟 <c>MODID_I18N_STEM</c> 表，将 <c>LocTable.GetRawText</c> 查询转交给
    ///         <see cref="I18NLocTableBridge" />。
    ///     </para>
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
        ///     <para xml:lang="en">Returns the raw template from the backing I18N dictionary when it contains the requested key.</para>
        ///     <para xml:lang="zh-CN">当背后的 I18N 字典包含请求的键时，返回其中的原始模板文本。</para>
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
    ///     <para xml:lang="en">
    ///         Routes <c>LocTable.GetLocString</c> through <see cref="I18NLocTableBridge" /> for virtual
    ///         <c>MODID_I18N_STEM</c> tables.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         针对虚拟 <c>MODID_I18N_STEM</c> 表，将 <c>LocTable.GetLocString</c> 查询转交给
    ///         <see cref="I18NLocTableBridge" />。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns a <see cref="LocString" /> that points to the virtual table ID when the backing I18N
        ///         dictionary contains the key.
        ///     </para>
        ///     <para xml:lang="zh-CN">当背后的 I18N 字典包含该键时，返回指向虚拟表 ID 的 <see cref="LocString" />。</para>
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
