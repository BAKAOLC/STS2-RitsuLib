using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Localization.Patches
{
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
    ///         Resolves virtual I18N entries at the <c>LocString.GetRawText</c> boundary so an inlined
    ///         <c>LocManager.GetTable</c> cannot bypass the bridge.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <c>LocString.GetRawText</c> 边界解析虚拟 I18N 条目，防止内联的 <c>LocManager.GetTable</c> 绕过桥接。
    ///     </para>
    /// </summary>
    internal class LocStringGetRawTextI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_string_get_raw_text_i18n_bridge";
        public static string Description => "Resolve virtual I18N entries from LocString.GetRawText";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocString), nameof(LocString.GetRawText), Type.EmptyTypes),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocString __instance, ref string __result)
        {
            if (!I18NLocTableBridge.TryGetLocTable(__instance.LocTable, out var locTable))
                return true;

            __result = locTable.GetRawText(__instance.LocEntryKey);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Resolves static <c>LocString.Exists</c> queries for virtual I18N tables.</para>
    ///     <para xml:lang="zh-CN">解析虚拟 I18N 表的静态 <c>LocString.Exists</c> 查询。</para>
    /// </summary>
    internal class LocStringStaticExistsI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_string_static_exists_i18n_bridge";
        public static string Description => "Resolve static LocString.Exists via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocString), nameof(LocString.Exists), [typeof(string), typeof(string)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string table, string key, ref bool __result)
        {
            if (!I18NLocTableBridge.TryGetLocTable(table, out var locTable))
                return true;

            __result = locTable.HasEntry(key);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Resolves instance <c>LocString.Exists</c> queries for virtual I18N tables.</para>
    ///     <para xml:lang="zh-CN">解析虚拟 I18N 表的实例 <c>LocString.Exists</c> 查询。</para>
    /// </summary>
    internal class LocStringExistsI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_string_exists_i18n_bridge";
        public static string Description => "Resolve instance LocString.Exists via registered I18N virtual tables";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocString), nameof(LocString.Exists), Type.EmptyTypes),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(LocString __instance, ref bool __result)
        {
            if (!I18NLocTableBridge.TryGetLocTable(__instance.LocTable, out var locTable))
                return true;

            __result = locTable.HasEntry(__instance.LocEntryKey);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Resolves random prefix selection for virtual I18N tables.</para>
    ///     <para xml:lang="zh-CN">解析虚拟 I18N 表的随机前缀选择。</para>
    /// </summary>
    internal class LocStringGetRandomWithPrefixI18NBridgePatch : IPatchMethod
    {
        public static string PatchId => "loc_string_get_random_with_prefix_i18n_bridge";

        public static string Description =>
            "Resolve LocString random prefix selection via registered I18N virtual tables";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(LocString), nameof(LocString.GetRandomWithPrefix),
                    [typeof(string), typeof(string), typeof(Rng)]),
            ];
        }

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string table, string keyPrefix, Rng? rng, ref LocString __result)
        {
            if (!I18NLocTableBridge.TryGetLocTable(table, out var locTable))
                return true;

            rng ??= Rng.Chaotic;
            __result = rng.NextItem(locTable.GetLocStringsWithPrefix(keyPrefix))!;
            return false;
        }
    }
}
