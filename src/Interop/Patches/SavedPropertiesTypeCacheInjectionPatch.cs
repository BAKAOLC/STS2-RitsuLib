using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Data;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Injects all loaded mod model types that declare <see cref="SavedPropertyAttribute" /> at a deterministic
    ///     initialization point so multiplayer peers build the same <see cref="SavedPropertiesTypeCache" /> net-id table.
    ///     在确定性的初始化点注入所有声明 <see cref="SavedPropertyAttribute" /> 的已加载 mod 模型类型，
    ///     使多人对等端构建相同的 <see cref="SavedPropertiesTypeCache" /> net-id 表。
    /// </summary>
    internal sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static bool _completed;
        internal static bool UsesDeterministicNetIdTable { get; private set; }
        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        public static string Description =>
            "Deterministic SavedPropertiesTypeCache injection for modded models with SavedProperty";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
        }

        /// <summary>
        ///     Injects cache entries after mod type-discovery contributors have had a chance to register content.
        ///     在 mod 类型发现贡献器有机会注册内容后注入缓存条目。
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix()
        {
            lock (Gate)
            {
                if (_completed)
                    return;
                _completed = true;
            }

            var modelTypes = GetModModelTypesWithSavedProperties().ToArray();
            var beforeCount = GetPropertyNameCount();
            var injectedTypes = 0;

            foreach (var modelType in modelTypes)
            {
#if STS2_AT_LEAST_0_108_0
                SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
                injectedTypes++;
#else
                if (SavedPropertiesTypeCache.GetJsonPropertiesForType(modelType) != null)
                    continue;

                SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
                injectedTypes++;
#endif
            }

            ModelSavedDataRegistry.FinalizeRegistration();
            SavedAttachedStateRegistry.FinalizePropertyNameRegistration();
            var afterCount = GetPropertyNameCount();
#if STS2_AT_LEAST_0_108_0
            const bool sorted = false;
            UsesDeterministicNetIdTable = false;
#else
            var sorted = SortNetIdTableIfEnabled(modelTypes.Length > 0 || afterCount != beforeCount);
            UsesDeterministicNetIdTable = sorted;
            RefreshNetIdBitSize();
#endif

            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (injectedTypes > 0 || sorted || afterCount != beforeCount)
                RitsuLibFramework.Logger.Info(
                    // ReSharper disable once HeuristicUnreachableCode
                    $"[SavedProperties] Injected {injectedTypes} mod model type(s); property net IDs: {beforeCount} -> {afterCount}, bit size {SavedPropertiesTypeCache.NetIdBitSize}, deterministic sort: {(sorted ? "applied" : "not applied")}.");
        }

        private static bool HasSavedProperty(Type modelType)
        {
            return modelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
        }

        private static IEnumerable<Type> GetModModelTypesWithSavedProperties()
        {
            return ModManager.GetLoadedMods()
                .SelectMany(static mod => Sts2ModManagerCompat.GetAssemblies(mod).Select(assembly => new
                {
                    ModId = mod.manifest?.id ?? assembly.GetName().Name ?? mod.path,
                    Assembly = assembly,
                }))
                .OrderBy(static mod => mod.ModId, StringComparer.Ordinal)
                .ThenBy(static mod => mod.Assembly.FullName, StringComparer.Ordinal)
                .SelectMany(static mod =>
                    AssemblyTypeScanHelper.GetLoadableTypes(mod.Assembly, RitsuLibFramework.Logger))
                .Where(static type =>
                    type is { IsAbstract: false, IsInterface: false } &&
                    typeof(AbstractModel).IsAssignableFrom(type) &&
                    HasSavedProperty(type))
                .Distinct()
                .OrderBy(static type => type.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(static type => type.Assembly.FullName, StringComparer.Ordinal)
                .ThenBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal);
        }

        private static int GetPropertyNameCount()
        {
            return GetNetIdToPropertyNameMap()?.Count ?? 0;
        }

        internal static bool RebuildDeterministicNetIdTableForSettings()
        {
            var sorted = SortNetIdTable(true);
            UsesDeterministicNetIdTable = sorted;
            RefreshNetIdBitSize();
            return sorted;
        }

        private static void RefreshNetIdBitSize()
        {
            var count = GetPropertyNameCount();

            var newBitSize = count <= 1
                ? 0
                : Mathf.CeilToInt(Mathf.Log(count) / Mathf.Log(2));
            AccessTools.Property(typeof(SavedPropertiesTypeCache), nameof(SavedPropertiesTypeCache.NetIdBitSize))
                ?.SetValue(null, newBitSize);
        }

        private static bool SortNetIdTableIfEnabled(bool autoDetectedSavedPropertyContent)
        {
            var mode = RitsuLibSettingsStore.GetModelDbDeterministicSortMode();
            if (mode == ModelDbDeterministicSortMode.Disabled ||
                (mode == ModelDbDeterministicSortMode.Auto && !autoDetectedSavedPropertyContent))
                return false;

            return SortNetIdTable(true);
        }

        private static bool SortNetIdTable(bool requireMultipleEntries)
        {
            var propertyNameToNetIdMap = GetPropertyNameToNetIdMap();
            var netIdToPropertyNameMap = GetNetIdToPropertyNameMap();
            if (propertyNameToNetIdMap == null || netIdToPropertyNameMap == null ||
                (requireMultipleEntries && netIdToPropertyNameMap.Count <= 1))
                return false;

            netIdToPropertyNameMap.Sort(StringComparer.Ordinal);

            propertyNameToNetIdMap.Clear();
            for (var i = 0; i < netIdToPropertyNameMap.Count; i++)
                propertyNameToNetIdMap[netIdToPropertyNameMap[i]] = i;

            return true;
        }

        private static Dictionary<string, int>? GetPropertyNameToNetIdMap()
        {
            return AccessTools.DeclaredField(typeof(SavedPropertiesTypeCache), "_propertyNameToNetIdMap")
                ?.GetValue(null) as Dictionary<string, int>;
        }

        private static List<string>? GetNetIdToPropertyNameMap()
        {
            return AccessTools.DeclaredField(typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap")
                ?.GetValue(null) as List<string>;
        }
    }
}
