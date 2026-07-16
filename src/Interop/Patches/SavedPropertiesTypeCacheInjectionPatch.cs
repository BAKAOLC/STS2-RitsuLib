using System.Reflection;
#if STS2_AT_LEAST_0_109_0
using System.IO.Hashing;
using System.Text;
#endif
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
#if STS2_AT_LEAST_0_109_0
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Timeline;
using SavedPropertyCache = MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache;
#else
using SavedPropertyCache = MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache;
#endif
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
    ///     Finalizes RitsuLib saved-property registrations at a deterministic initialization point and integrates them
    ///     with the game's saved-property net-id table.
    ///     在确定性的初始化点完成 RitsuLib 保存属性注册，并将其集成到游戏的保存属性 net-id 表。
    /// </summary>
    internal sealed class SavedPropertiesTypeCacheInjectionPatch : IPatchMethod
    {
        private static readonly Lock Gate = new();
        private static bool _completed;
        internal static bool UsesDeterministicNetIdTable { get; private set; }
        public static string PatchId => "ritsulib_saved_properties_type_cache_injection";

        public static string Description =>
#if STS2_AT_LEAST_0_109_0
            "Integrate synthetic SavedProperties names with ModelIdSerializationCache and its multiplayer hash";
#else
            "Deterministic SavedPropertiesTypeCache injection for modded models with SavedProperty";
#endif

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
#if STS2_AT_LEAST_0_109_0
            return [new(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))];
#else
            return [new(typeof(LocManager), nameof(LocManager.Initialize))];
#endif
        }

#if STS2_AT_LEAST_0_109_0
        public static void Postfix()
        {
            lock (Gate)
            {
                if (_completed)
                    return;
                _completed = true;
            }

            ModelSavedDataRegistry.FinalizeRegistration();
            var beforeCount = GetPropertyNameCount();
            var nativeHash = ModelIdSerializationCache.Hash;
            var reproducedNativeHash = ComputeHash([]);
            if (reproducedNativeHash != nativeHash)
                throw new InvalidOperationException(
                    $"RitsuLib could not reproduce the native ModelIdSerializationCache hash. " +
                    $"Native: {nativeHash}; reproduced: {reproducedNativeHash}.");

            var syntheticNames = SavedAttachedStateRegistry.FinalizePropertyNameRegistration();
            var afterCount = GetPropertyNameCount();
            var finalHash = syntheticNames.Count == 0
                ? nativeHash
                : ComputeHash(syntheticNames);
            AccessTools.Property(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Hash))
                ?.SetValue(null, finalHash);
            UsesDeterministicNetIdTable = true;

            if (syntheticNames.Count > 0)
                RitsuLibFramework.Logger.Info(
                    $"[SavedProperties] Added {syntheticNames.Count} synthetic property name(s) to the native cache; " +
                    $"property net IDs: {beforeCount} -> {afterCount}, bit size " +
                    $"{ModelIdSerializationCache.PropertyIdBitSize}, hash: {nativeHash} -> {finalHash}.");
        }

        private static uint ComputeHash(IReadOnlyCollection<string> syntheticNames)
        {
            var buffer = new byte[512];
            var hash = new XxHash32();
            var modelItems = ContentSorter<ModelId>.Sort(
                ModelDb.All.Select(static model => model.GetType()),
                ModelDb.GetId);

            foreach (var item in modelItems)
            {
                if (!(item.mod?.manifest?.affectsGameplay ?? true))
                    continue;

                AppendUtf8(hash, item.id.Category, buffer);
                AppendUtf8(hash, item.id.Entry, buffer);
            }

            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in modelItems)
            {
                if (!(item.mod?.manifest?.affectsGameplay ?? true))
                    continue;

                var properties = item.type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(static property => new
                    {
                        Property = property,
                        Attribute = property.GetCustomAttribute<SavedPropertyAttribute>(),
                    })
                    .Where(static entry => entry.Attribute != null)
                    .OrderBy(static entry => entry.Attribute!.order)
                    .ThenBy(static entry => entry.Property.Name, StringComparer.Ordinal);
                foreach (var entry in properties)
                    if (propertyNames.Add(entry.Property.Name))
                        AppendUtf8(hash, entry.Property.Name, buffer);
            }

            foreach (var syntheticName in syntheticNames)
                AppendUtf8(hash, syntheticName, buffer);

            foreach (var item in ContentSorter<string>.Sort(EpochModel.AllEpochs, EpochModel.GetId))
                AppendUtf8(hash, item.id, buffer);

            return hash.GetCurrentHashAsUInt32();
        }

        private static void AppendUtf8(XxHash32 hash, string text, byte[] buffer)
        {
            var bytes = Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
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
