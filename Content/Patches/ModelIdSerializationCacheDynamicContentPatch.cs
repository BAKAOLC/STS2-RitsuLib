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
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <see cref="ModelIdSerializationCache.Init" /> only walks <see cref="ModelDb.AllAbstractModelSubtypes" />, so
    ///     Reflection.Emit placeholder models (and any other injected types not returned by mod subtype scan) never receive
    ///     net
    ///     IDs. This postfix merges <see cref="ModelDb" /> content and recomputes bit sizes and hash like vanilla
    ///     <c>Init</c>.
    /// </summary>
    public class ModelIdSerializationCacheDynamicContentPatch : IPatchMethod
    {
        // Setter invocation happens at init-time only; keep the simple reflection path to
        // avoid delegate-signature mismatches across different runtime property types.

        /// <inheritdoc />
        public static string PatchId => "model_id_serialization_cache_dynamic_content";

        /// <inheritdoc />
        public static string Description =>
            "Include ModelDb-injected dynamic mod models in ModelIdSerializationCache maps and hash";

        /// <inheritdoc />
        public static bool IsCritical => true;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))];
        }

        /// <summary>
        ///     After vanilla <see cref="ModelIdSerializationCache.Init" />, merges injected <see cref="ModelDb" /> entries
        ///     into net ID maps and refreshes bit sizes and hash.
        /// </summary>
        public static void Postfix()
        {
            var contentById = GetModelDbContentById();
            if (contentById == null || contentById.Count == 0)
                return;

            var catMap =
                GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_categoryNameToNetIdMap");
            var catList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");
            var entMap =
                GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_entryNameToNetIdMap");
            var entList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");

            if (catMap == null || catList == null || entMap == null || entList == null)
                return;

            foreach (DictionaryEntry entry in contentById)
            {
                if (entry.Key is not ModelId id)
                    continue;

                EnsureCategory(id.Category, catMap, catList);
                EnsureEntry(id.Entry, entMap, entList);
            }

            var maxCategory = catList.Count;
            var maxEntry = entList.Count;
            var epochList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEpochNameMap");
            var maxEpoch = epochList?.Count ?? 0;

            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.CategoryIdBitSize),
                Mathf.CeilToInt(Math.Log2(maxCategory)));
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EntryIdBitSize),
                Mathf.CeilToInt(Math.Log2(maxEntry)));
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EpochIdBitSize),
                Mathf.CeilToInt(Math.Log2(maxEpoch)));

            var newHash = ComputeHashLikeVanilla(contentById, maxCategory, maxEntry, maxEpoch);
            SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Hash), newHash);
        }

        private static IDictionary? GetModelDbContentById()
        {
            var field = AccessTools.DeclaredField(typeof(ModelDb), "_contentById");
            return field?.GetValue(null) as IDictionary;
        }

        private static uint ComputeHashLikeVanilla(IDictionary contentById, int maxCategory, int maxEntry, int maxEpoch)
        {
            var buffer = new byte[512];
            var xxHash = new XxHash32();

            var types = new HashSet<Type>();
            foreach (var t in ModelDb.AllAbstractModelSubtypes)
                types.Add(t);

            foreach (DictionaryEntry entry in contentById)
                if (entry.Value is AbstractModel model)
                    types.Add(model.GetType());

            var sorted = types.ToList();
            sorted.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var id in sorted.Select(ModelDb.GetId))
            {
                AppendUtf8(xxHash, id.Category, buffer);
                AppendUtf8(xxHash, id.Entry, buffer);
            }

            foreach (var epochId in EpochModel.AllEpochIds)
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
    }
}
