using System.Reflection;
using Godot;
using HarmonyLib;
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
#if STS2_AT_LEAST_0_109_0
using System.IO.Hashing;
using System.Text;
#endif
#if STS2_AT_LEAST_0_109_0
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using SavedPropertyCache = MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache;

#else
using MegaCrit.Sts2.Core.Localization;
using SavedPropertyCache = MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache;
#endif

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Finalizes RitsuLib saved-property registrations at a deterministic initialization point and integrates them
    ///     with the game's saved-property net-id table.
    ///     在确定性的初始化点完成 RitsuLib 保存属性注册，并将其集成到游戏的保存属性 net-id 表。
    /// </summary>
    internal sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static bool _completed;
#if STS2_AT_LEAST_0_109_0
        private static int _remainingGameplayTypes = -1;
#endif
        internal static bool UsesDeterministicNetIdTable { get; private set; }
        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        public static string Description =>
#if STS2_AT_LEAST_0_109_0
            "Collect synthetic SavedProperties names during native ModelIdSerializationCache initialization";
#else
            "Deterministic SavedPropertiesTypeCache injection for modded models with SavedProperty";
#endif

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
#if STS2_AT_LEAST_0_109_0
            return
            [
                new(typeof(SavedPropertyCache), "CachePropertiesForType",
                    [typeof(Type), typeof(XxHash32), typeof(byte[])]),
            ];
#else
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
#endif
        }

#if STS2_AT_LEAST_0_109_0
        public static void Postfix(XxHash32? __1, byte[]? __2)
        {
            if (__1 == null || __2 == null)
                return;

            IReadOnlyList<string> syntheticNames;
            int beforeCount;
            int afterCount;
            lock (Gate)
            {
                if (_completed)
                    return;

                if (_remainingGameplayTypes < 0)
                    _remainingGameplayTypes = ContentSorter<ModelId>.Sort(
                            ModelDb.All.Select(static model => model.GetType()),
                            ModelDb.GetId)
                        .Count(static item => item.mod?.manifest?.affectsGameplay ?? true);

                _remainingGameplayTypes--;
                if (_remainingGameplayTypes > 0)
                    return;

                _completed = true;
                ModelSavedDataRegistry.FinalizeRegistration();
                beforeCount = GetPropertyNameCount();
                syntheticNames = SavedAttachedStateRegistry.FinalizePropertyNameRegistration(false);
                foreach (var name in syntheticNames)
                    InjectSyntheticName(name, __1, __2);
                afterCount = GetPropertyNameCount();
                UsesDeterministicNetIdTable = true;
            }

            if (syntheticNames.Count > 0)
                RitsuLibFramework.Logger.Info(
                    $"[SavedProperties] Collected {syntheticNames.Count} synthetic property name(s) during native " +
                    $"cache initialization; property net IDs: {beforeCount} -> {afterCount}.");
        }

        private static void InjectSyntheticName(string name, XxHash32 hash, byte[] buffer)
        {
            var propertyNameToNetIdMap = GetPropertyNameToNetIdMap() ??
                                         throw new InvalidOperationException(
                                             "Native saved-property name-to-id map is unavailable.");
            var netIdToPropertyNameMap = GetNetIdToPropertyNameMap() ??
                                         throw new InvalidOperationException(
                                             "Native saved-property id-to-name map is unavailable.");
            if (propertyNameToNetIdMap.ContainsKey(name))
                throw new InvalidOperationException(
                    $"SavedAttachedState name is not unique in the native saved-property cache: {name}");

            propertyNameToNetIdMap[name] = netIdToPropertyNameMap.Count;
            netIdToPropertyNameMap.Add(name);
            var bytes = Encoding.UTF8.GetBytes(name, 0, name.Length, buffer, 0);
            hash.Append(buffer.AsSpan(0, bytes));
        }
#else
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
#endif

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
#if STS2_AT_LEAST_0_109_0
            AccessTools.Property(typeof(SavedPropertyCache), nameof(SavedPropertyCache.PropertyIdBitSize))
#else
            AccessTools.Property(typeof(SavedPropertyCache), nameof(SavedPropertyCache.NetIdBitSize))
#endif
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
            return AccessTools.DeclaredField(typeof(SavedPropertyCache), "_propertyNameToNetIdMap")
                ?.GetValue(null) as Dictionary<string, int>;
        }

        private static List<string>? GetNetIdToPropertyNameMap()
        {
            return AccessTools.DeclaredField(typeof(SavedPropertyCache), "_netIdToPropertyNameMap")
                ?.GetValue(null) as List<string>;
        }
    }
}
