using System.Buffers.Binary;
using System.Collections;
using System.IO.Hashing;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Data;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Timeline;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     Rebuilds <see cref="ModelIdSerializationCache" /> from the finalized <see cref="ModelDb" /> content after
    ///     the existing initialization path when registered extensions are present, using a strict deterministic order
    ///     so all network-relevant models share one stable net-id and hash source.
    ///     当存在注册扩展内容时，在既有初始化路径之后使用严格确定的顺序，从最终 <see cref="ModelDb" /> 内容重建
    ///     <see cref="ModelIdSerializationCache" />，让所有网络相关模型共用同一个稳定的 net-id 与 hash 来源。
    /// </summary>
    internal sealed class ModelIdSerializationCacheDynamicContentPatch : IPatchMethod
    {
        private static int _staleCacheWarningLogged;
        internal static bool UsesDeterministicCache { get; private set; }
        private static ModelDbCacheSignature? AppliedDeterministicCacheSignature { get; set; }

        private static IReadOnlyDictionary<ModelId, ModelSortIds> LocalOnlyModelSortIds { get; set; } =
            new Dictionary<ModelId, ModelSortIds>();

        // Setter invocation happens at init-time only; keep the simple reflection path to
        // avoid delegate-signature mismatches across different runtime property types.
        public static string PatchId => "model_id_serialization_cache_dynamic_content";

        public static string Description =>
            "Rebuild ModelIdSerializationCache maps and hash from deterministically sorted final ModelDb content";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))];
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix()
        {
            var mode = RitsuLibSettingsStore.GetModelDbDeterministicSortMode();
            if (mode == ModelDbDeterministicSortMode.Disabled)
            {
                UsesDeterministicCache = false;
                AppliedDeterministicCacheSignature = null;
                PatchLog.For<ModelIdSerializationCacheDynamicContentPatch>().Info(
                    "[ModelIdSerializationCache] Deterministic final-content cache disabled by settings.");
                return;
            }

            var result = RebuildDeterministicCache(mode == ModelDbDeterministicSortMode.Force);
            LogAutomaticResult(result);
        }

        internal static ModelDbDeterministicCacheRebuildResult RebuildDeterministicCacheForSettings()
        {
            return RebuildDeterministicCache(true);
        }

        internal static ModelDbDeterministicCacheStatus GetDeterministicCacheStatus()
        {
            if (!UsesDeterministicCache)
                return ModelDbDeterministicCacheStatus.Inactive("Stable sorting is not active.");

            var applied = AppliedDeterministicCacheSignature;
            if (applied == null)
                return ModelDbDeterministicCacheStatus.Inactive(
                    "Stable sorting is not verifiable because no RitsuLib cache signature is available.");

            var current = TryCaptureCurrentCacheSignature();
            if (current == null)
                return ModelDbDeterministicCacheStatus.Inactive(
                    "Stable sorting is not verifiable because ModelIdSerializationCache internals are not available.");

            if (current == applied)
                return ModelDbDeterministicCacheStatus.Active("Stable sorting");

            const string reason =
                "RitsuLib rebuilt the ModelIdSerializationCache, but the current cache no longer matches " +
                "RitsuLib's deterministic signature. Another patch likely rewrote the cache or hash after RitsuLib.";
            if (Interlocked.Exchange(ref _staleCacheWarningLogged, 1) == 0)
                PatchLog.For<ModelIdSerializationCacheDynamicContentPatch>().Warn(
                    $"[ModelIdSerializationCache] {reason} Expected {applied}; current {current}.");

            return ModelDbDeterministicCacheStatus.Inactive(reason);
        }

        private static ModelDbDeterministicCacheRebuildResult RebuildDeterministicCache(bool force)
        {
            UsesDeterministicCache = false;
            AppliedDeterministicCacheSignature = null;

            var contentById = GetModelDbContentById();
            if (contentById == null || contentById.Count == 0)
                return ModelDbDeterministicCacheRebuildResult.NotApplied("ModelDb content is not available.");

            if (!force && !HasRegisteredSerializationContent(contentById))
                return ModelDbDeterministicCacheRebuildResult.NotApplied("No matching content was detected.");

            var catMap =
                GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_categoryNameToNetIdMap");
            var catList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");
            var entMap =
                GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_entryNameToNetIdMap");
            var entList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");
            var epochMap =
                GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_epochNameToNetIdMap");
            var epochList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEpochNameMap");

            if (catMap == null || catList == null || entMap == null || entList == null ||
                epochMap == null || epochList == null)
                return ModelDbDeterministicCacheRebuildResult.NotApplied(
                    "ModelIdSerializationCache internals are not available.");

            var initialHash = ModelIdSerializationCache.Hash;

            var modelEntries = GetSortedModelEntries(contentById, false);
            var allModelEntries = GetSortedModelEntries(contentById, true);
            var epochIds = EpochModel.AllEpochIds
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray();

            catMap.Clear();
            catList.Clear();
            entMap.Clear();
            entList.Clear();
            epochMap.Clear();
            epochList.Clear();

            EnsureCategory(ModelId.none.Category, catMap, catList);
            EnsureEntry(ModelId.none.Entry, entMap, entList);

            foreach (var entry in modelEntries)
            {
                var id = entry.Id;
                EnsureCategory(id.Category, catMap, catList);
                EnsureEntry(id.Entry, entMap, entList);
            }

            foreach (var epochId in epochIds)
                EnsureEpoch(epochId, epochMap, epochList);

            var localOnlySortIds = BuildLocalOnlySortIds(
                allModelEntries,
                modelEntries,
                catMap,
                catList.Count,
                entMap,
                entList.Count);
            LocalOnlyModelSortIds = localOnlySortIds;

            if (localOnlySortIds.Count > 0)
                PatchLog.For<ModelIdSerializationCacheDynamicContentPatch>().Info(
                    "[ModelIdSerializationCache] Assigned local-only sort IDs for " +
                    $"{localOnlySortIds.Count} non-gameplay ModelDb model(s); they are excluded from network maps and hash.");

            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.CategoryIdBitSize),
                GetBitSize(catList.Count));
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EntryIdBitSize),
                GetBitSize(entList.Count));
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EpochIdBitSize),
                GetBitSize(epochList.Count));

            var newHash = ComputeHash(modelEntries, epochIds, catList.Count, entList.Count, epochList.Count);
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Hash), newHash);
            UsesDeterministicCache = true;
            AppliedDeterministicCacheSignature = TryCaptureCurrentCacheSignature();
            Interlocked.Exchange(ref _staleCacheWarningLogged, 0);

            return ModelDbDeterministicCacheRebuildResult.Rebuilt(initialHash, newHash);
        }

        private static void LogAutomaticResult(ModelDbDeterministicCacheRebuildResult result)
        {
            if (!result.Applied)
            {
                PatchLog.For<ModelIdSerializationCacheDynamicContentPatch>().Info(
                    "[ModelIdSerializationCache] Deterministic final-content cache not enabled.");
                return;
            }

            PatchLog.For<ModelIdSerializationCacheDynamicContentPatch>().Info(
                "[ModelIdSerializationCache] Deterministic final-content cache enabled. " +
                $"Hash: {result.InitialHash} -> {result.FinalHash}.");
        }

        private static IDictionary? GetModelDbContentById()
        {
            var field = AccessTools.DeclaredField(typeof(ModelDb), "_contentById");
            return field?.GetValue(null) as IDictionary;
        }

        internal static bool TryGetLocalOnlySortIds(ModelId id, out ModelSortIds sortIds)
        {
            return LocalOnlyModelSortIds.TryGetValue(id, out sortIds);
        }

        internal static bool IsLocalOnlyModelId(ModelId id)
        {
            return LocalOnlyModelSortIds.ContainsKey(id);
        }

        private static bool HasRegisteredSerializationContent(IDictionary contentById)
        {
            foreach (DictionaryEntry entry in contentById)
                if (entry.Value is AbstractModel model &&
                    ModContentRegistry.TryGetOwnerModId(model.GetType(), out _))
                    return true;

            return ModTimelineRegistry.RegisteredEpochCount() > 0;
        }

        private static ModelCacheEntry[] GetSortedModelEntries(IDictionary contentById, bool includeLocalOnly)
        {
            var entries = new List<ModelCacheEntry>(contentById.Count);
            foreach (DictionaryEntry entry in contentById)
            {
                if (entry.Key is not ModelId id || entry.Value is not AbstractModel model)
                    continue;

                var modelType = model.GetType();
                if (!includeLocalOnly && !Sts2ModManagerCompat.IsGameplayRelevantLoadedModType(modelType))
                    continue;

                entries.Add(new(modelType, id, ResolveOwnerModId(modelType)));
            }

            entries.Sort(CompareModelCacheEntries);
            return [.. entries];
        }

        private static IReadOnlyDictionary<ModelId, ModelSortIds> BuildLocalOnlySortIds(
            IReadOnlyList<ModelCacheEntry> allModelEntries,
            IReadOnlyList<ModelCacheEntry> networkModelEntries,
            IReadOnlyDictionary<string, int> networkCategoryMap,
            int networkCategoryCount,
            IReadOnlyDictionary<string, int> networkEntryMap,
            int networkEntryCount)
        {
            var networkIds = networkModelEntries
                .Select(static entry => entry.Id)
                .ToHashSet();
            var localCategoryIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var localEntryIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var result = new Dictionary<ModelId, ModelSortIds>();

            foreach (var entry in allModelEntries)
            {
                var id = entry.Id;
                if (networkIds.Contains(id))
                    continue;

                var categoryId = GetLocalSortId(
                    id.Category,
                    networkCategoryMap,
                    networkCategoryCount,
                    localCategoryIds);
                var entryId = GetLocalSortId(
                    id.Entry,
                    networkEntryMap,
                    networkEntryCount,
                    localEntryIds);

                result[id] = new(categoryId, entryId);
            }

            return result;
        }

        private static int GetLocalSortId(
            string name,
            IReadOnlyDictionary<string, int> networkMap,
            int networkCount,
            Dictionary<string, int> localIds)
        {
            if (networkMap.TryGetValue(name, out var networkId))
                return networkId;

            if (localIds.TryGetValue(name, out var localId))
                return localId;

            localId = networkCount + localIds.Count;
            localIds[name] = localId;
            return localId;
        }

        private static int CompareModelCacheEntries(ModelCacheEntry left, ModelCacheEntry right)
        {
            var c = string.CompareOrdinal(left.Id.Category, right.Id.Category);
            if (c != 0)
                return c;

            c = string.CompareOrdinal(left.Id.Entry, right.Id.Entry);
            if (c != 0)
                return c;

            c = string.CompareOrdinal(left.ModelType.Name, right.ModelType.Name);
            if (c != 0)
                return c;

            c = string.CompareOrdinal(left.OwnerModId, right.OwnerModId);
            if (c != 0)
                return c;

            c = string.CompareOrdinal(left.ModelType.FullName ?? left.ModelType.Name,
                right.ModelType.FullName ?? right.ModelType.Name);
            if (c != 0)
                return c;

            return string.CompareOrdinal(left.ModelType.Assembly.GetName().Name,
                right.ModelType.Assembly.GetName().Name);
        }

        private static ModelDbCacheSignature? TryCaptureCurrentCacheSignature()
        {
            var catList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");
            var entList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");
            var epochList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEpochNameMap");
            if (catList == null || entList == null || epochList == null)
                return null;

            return new(
                ModelIdSerializationCache.Hash,
                ModelIdSerializationCache.CategoryIdBitSize,
                ModelIdSerializationCache.EntryIdBitSize,
                ModelIdSerializationCache.EpochIdBitSize,
                catList.Count,
                entList.Count,
                epochList.Count,
                StableHash(catList),
                StableHash(entList),
                StableHash(epochList));
        }

        private static uint StableHash(IEnumerable<string> values)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var value in values)
                {
                    foreach (var ch in value)
                    {
                        hash ^= ch;
                        hash *= 16777619u;
                    }

                    hash ^= 0xffu;
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static string ResolveOwnerModId(Type modelType)
        {
            return ModContentRegistry.TryGetOwnerModId(modelType, out var ownerModId)
                ? ownerModId
                : string.Empty;
        }

        private static uint ComputeHash(
            IReadOnlyList<ModelCacheEntry> modelEntries,
            IReadOnlyList<string> epochIds,
            int maxCategory,
            int maxEntry,
            int maxEpoch)
        {
            var buffer = new byte[512];
            var xxHash = new XxHash32();

            foreach (var id in modelEntries.Select(static entry => entry.Id))
            {
                AppendUtf8(xxHash, id.Category, buffer);
                AppendUtf8(xxHash, id.Entry, buffer);
            }

            foreach (var epochId in epochIds)
                AppendUtf8(xxHash, epochId, buffer);

            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxCategory);
            xxHash.Append(buffer.AsSpan(0, 4));
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxEntry);
            xxHash.Append(buffer.AsSpan(0, 4));
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxEpoch);
            xxHash.Append(buffer.AsSpan(0, 4));

            return xxHash.GetCurrentHashAsUInt32();
        }

        private static void AppendUtf8(XxHash32 xxHash, string text, byte[] buffer)
        {
            var bytes = Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
            xxHash.Append(buffer.AsSpan(0, bytes));
        }

        private static void EnsureCategory(string category, Dictionary<string, int> map, List<string> list)
        {
            if (map.ContainsKey(category))
                return;

            map[category] = list.Count;
            list.Add(category);
        }

        private static void EnsureEntry(string entry, Dictionary<string, int> map, List<string> list)
        {
            if (map.ContainsKey(entry))
                return;

            map[entry] = list.Count;
            list.Add(entry);
        }

        private static void EnsureEpoch(string epochId, Dictionary<string, int> map, List<string> list)
        {
            if (map.ContainsKey(epochId))
                return;

            map[epochId] = list.Count;
            list.Add(epochId);
        }

        private static int GetBitSize(int count)
        {
            return count <= 1
                ? 0
                : Mathf.CeilToInt(Math.Log2(count));
        }

        private static T? GetStaticField<T>(Type declaringType, string name)
            where T : class
        {
            return AccessTools.DeclaredField(declaringType, name)?.GetValue(null) as T;
        }

        private static void SetStaticProperty(Type declaringType, string name, object value)
        {
            var prop = declaringType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            prop?.GetSetMethod(true)?.Invoke(null, [value]);
        }

        internal readonly record struct ModelSortIds(int CategorySortingId, int EntrySortingId);

        private readonly record struct ModelCacheEntry(Type ModelType, ModelId Id, string OwnerModId);

        private sealed record ModelDbCacheSignature(
            uint Hash,
            int CategoryIdBitSize,
            int EntryIdBitSize,
            int EpochIdBitSize,
            int CategoryCount,
            int EntryCount,
            int EpochCount,
            uint CategoryMapHash,
            uint EntryMapHash,
            uint EpochMapHash);

        internal readonly record struct ModelDbDeterministicCacheRebuildResult(
            bool Applied,
            uint InitialHash,
            uint FinalHash,
            string? Reason)
        {
            public static ModelDbDeterministicCacheRebuildResult Rebuilt(uint initialHash, uint finalHash)
            {
                return new(true, initialHash, finalHash, null);
            }

            public static ModelDbDeterministicCacheRebuildResult NotApplied(string reason)
            {
                return new(false, 0, 0, reason);
            }
        }

        internal readonly record struct ModelDbDeterministicCacheStatus(bool IsActive, string Detail)
        {
            public static ModelDbDeterministicCacheStatus Active(string detail)
            {
                return new(true, detail);
            }

            public static ModelDbDeterministicCacheStatus Inactive(string detail)
            {
                return new(false, detail);
            }
        }
    }

    internal sealed class LocalOnlyModelIdSortingPatch : IPatchMethod
    {
        private static readonly FieldInfo? CategorySortingIdField =
            AccessTools.DeclaredField(typeof(AbstractModel), "<CategorySortingId>k__BackingField");

        private static readonly FieldInfo? EntrySortingIdField =
            AccessTools.DeclaredField(typeof(AbstractModel), "<EntrySortingId>k__BackingField");

        public static string PatchId => "local_only_model_id_sorting";

        public static string Description =>
            "Allow non-gameplay local-only ModelDb models to initialize without entering network net-id maps";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(AbstractModel), nameof(AbstractModel.InitId), [typeof(ModelId)])];
        }

        public static Exception? Finalizer(Exception? __exception, AbstractModel __instance, ModelId id)
        {
            if (__exception is not ArgumentException ||
                CategorySortingIdField == null ||
                EntrySortingIdField == null ||
                !ModelIdSerializationCacheDynamicContentPatch.TryGetLocalOnlySortIds(id, out var sortIds))
                return __exception;

            CategorySortingIdField.SetValue(__instance, sortIds.CategorySortingId);
            EntrySortingIdField.SetValue(__instance, sortIds.EntrySortingId);
            return null;
        }
    }
}
