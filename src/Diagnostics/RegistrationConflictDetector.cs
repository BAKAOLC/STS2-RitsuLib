using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Diagnostics
{
    internal static class RegistrationConflictDetector
    {
        private static readonly Lock IndexGate = new();
        private static Dictionary<ModelId, List<Type>>? _gameModelIdIndex;
        private static bool _baseLibDynamicEnumCollisionCheckCompleted;

        private static Dictionary<ModelId, List<Type>> GetGameModelIdIndex()
        {
            lock (IndexGate)
            {
                if (_gameModelIdIndex != null)
                    return _gameModelIdIndex;

                var index = new Dictionary<ModelId, List<Type>>();
                foreach (var type in ReflectionHelper.GetSubtypes<AbstractModel>())
                {
                    var id = ModelDb.GetId(type);
                    if (!index.TryGetValue(id, out var bucket))
                    {
                        bucket = [];
                        index[id] = bucket;
                    }

                    bucket.Add(type);
                }

                _gameModelIdIndex = index;
                return index;
            }
        }

        internal static void InvalidateModelIdIndex()
        {
            lock (IndexGate)
            {
                _gameModelIdIndex = null;
            }
        }

        internal static void UpdateModelIdIndex(Type type, ModelId previousId, ModelId currentId)
        {
            if (previousId == currentId)
                return;

            lock (IndexGate)
            {
                if (_gameModelIdIndex == null)
                    return;

                if (_gameModelIdIndex.TryGetValue(previousId, out var previousBucket))
                {
                    previousBucket.Remove(type);
                    if (previousBucket.Count == 0)
                        _gameModelIdIndex.Remove(previousId);
                }

                if (!_gameModelIdIndex.TryGetValue(currentId, out var currentBucket))
                {
                    currentBucket = [];
                    _gameModelIdIndex[currentId] = currentBucket;
                }

                if (!currentBucket.Contains(type))
                    currentBucket.Add(type);
            }
        }

        internal static void ThrowIfModelIdConflicts(Type candidateType)
        {
            ArgumentNullException.ThrowIfNull(candidateType);

            var candidateId = ModelDb.GetId(candidateType);
            if (!GetGameModelIdIndex().TryGetValue(candidateId, out var bucket))
                return;

            var conflicts = bucket.Where(type => type != candidateType).ToArray();
            if (conflicts.Length == 0)
                return;

            throw new InvalidOperationException(
                $"ModelId collision detected for '{candidateId}'. Type '{candidateType.FullName}' conflicts with: " +
                string.Join(", ", conflicts.Select(type => type.FullName)) +
                ". RitsuLib now formats registered model ids as '<modid>_<category>_<typename>', so a remaining collision usually means two registered models in the same mod/category still share the same CLR type name.");
        }

        internal static void ValidateAndLogModelIdCollisions()
        {
            InvalidateModelIdIndex();
            var conflicts = GetGameModelIdIndex()
                .Where(pair => pair.Value.Count > 1)
                .ToArray();

            foreach (var group in conflicts)
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[Content] ModelId collision detected for '{group.Key}': " +
                    string.Join(", ", group.Value.Select(type => type.FullName)));

            if (conflicts.Length > 0)
                RitsuLibFramework.Logger.ErrorNoTrace(
                    "[Content] Duplicate patched ModelIds are unsafe. RitsuLib formats registered model ids as '<modid>_<category>_<typename>', so this usually indicates two registered models still share the same mod/category/type-name combination.");

            InvalidateModelIdIndex();
        }

        internal static void ValidateAndLogBaseLibDynamicEnumValueCollisions()
        {
            if (_baseLibDynamicEnumCollisionCheckCompleted)
                return;

            _baseLibDynamicEnumCollisionCheckCompleted = true;

            var baseLibEntries = GetBaseLibCustomEnumEntries();
            if (baseLibEntries.Length == 0)
                return;

            var ritsuEntries = GetRitsuDynamicEnumEntries()
                .GroupBy(static entry => (entry.EnumType, entry.Value))
                .ToDictionary(
                    static group => group.Key,
                    static group => group
                        .OrderBy(static entry => entry.Id, StringComparer.Ordinal)
                        .ToArray());

            var conflicts = baseLibEntries
                .SelectMany(entry => ritsuEntries.TryGetValue((entry.EnumType, entry.Value), out var ritsu)
                    ? ritsu.Select(ritsuEntry => new DynamicEnumValueCollision(entry, ritsuEntry))
                    : [])
                .OrderBy(static collision => collision.BaseLibEntry.EnumType.FullName, StringComparer.Ordinal)
                .ThenBy(static collision => collision.BaseLibEntry.Value)
                .ThenBy(static collision => collision.RitsuEntry.Id, StringComparer.Ordinal)
                .ToArray();

            foreach (var collision in conflicts)
                RitsuLibFramework.Logger.ErrorNoTrace(
                    "[DynamicEnum] BaseLib/RitsuLib value collision detected: "
                    + $"{collision.BaseLibEntry.EnumType.FullName} value 0x{collision.BaseLibEntry.Value:X8} "
                    + $"is used by BaseLib '{collision.BaseLibEntry.DisplayName}' and RitsuLib "
                    + $"'{collision.RitsuEntry.DisplayName}'.");

            if (conflicts.Length > 0)
                RitsuLibFramework.Logger.ErrorNoTrace(
                    "[DynamicEnum] BaseLib/RitsuLib dynamic enum value collisions are unsafe. "
                    + "RitsuLib does not remap minted values automatically; change the colliding RitsuLib id or "
                    + "the BaseLib CustomEnum field/name source.");
        }

        internal static void ThrowIfEpochIdConflicts(string epochId, Type candidateType,
            IEnumerable<Type> knownEpochTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);
            ArgumentNullException.ThrowIfNull(candidateType);
            ArgumentNullException.ThrowIfNull(knownEpochTypes);

            var conflicts = knownEpochTypes
                .Where(type => type != candidateType)
                .Where(type => CreateEpoch(type).Id.Equals(epochId, StringComparison.Ordinal))
                .ToArray();

            if (conflicts.Length == 0)
                return;

            throw new InvalidOperationException(
                $"Epoch id collision detected for '{epochId}'. Type '{candidateType.FullName}' conflicts with: " +
                string.Join(", ", conflicts.Select(type => type.FullName)));
        }

        internal static void ThrowIfStoryIdConflicts(string storyId, Type candidateType,
            IEnumerable<Type> knownStoryTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storyId);
            ArgumentNullException.ThrowIfNull(candidateType);
            ArgumentNullException.ThrowIfNull(knownStoryTypes);

            var conflicts = knownStoryTypes
                .Where(type => type != candidateType)
                .Where(type => GetStoryId(type).Equals(storyId, StringComparison.Ordinal))
                .ToArray();

            if (conflicts.Length == 0)
                return;

            throw new InvalidOperationException(
                $"Story id collision detected for '{storyId}'. Type '{candidateType.FullName}' conflicts with: " +
                string.Join(", ", conflicts.Select(type => type.FullName)));
        }

        private static EpochModel CreateEpoch(Type type)
        {
            return (EpochModel)Activator.CreateInstance(type)!;
        }

        private static string GetStoryId(Type type)
        {
            var story = (StoryModel)Activator.CreateInstance(type)!;
            var property = type.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (string)(property?.GetValue(story) ??
                            throw new InvalidOperationException(
                                $"Story type '{type.FullName}' does not expose an Id property."));
        }

        private static BaseLibCustomEnumEntry[] GetBaseLibCustomEnumEntries()
        {
            var customEnumsType = ResolveLoadedType("BaseLib.Patches.Content.CustomEnums");
            var generatedEntriesField = customEnumsType?.GetField(
                "GeneratedCustomEnumEntries",
                BindingFlags.Static | BindingFlags.Public);

            if (generatedEntriesField?.GetValue(null) is not IDictionary byEnumType)
                return [];

            var result = new List<BaseLibCustomEnumEntry>();
            foreach (DictionaryEntry enumGroup in byEnumType)
            {
                if (enumGroup.Key is not Type enumType || enumGroup.Value is not IDictionary entriesByValue)
                    continue;

                foreach (DictionaryEntry entry in entriesByValue)
                {
                    if (!TryConvertToInt32(entry.Key, out var value))
                        continue;

                    var (prefix, name) = ReadBaseLibGeneratedName(entry.Value);
                    result.Add(new(enumType, value, prefix, name));
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<RitsuDynamicEnumEntry> GetRitsuDynamicEnumEntries()
        {
            foreach (var entry in GetRitsuDynamicEnumEntries<CardKeyword>())
                yield return entry;
            foreach (var entry in GetRitsuDynamicEnumEntries<PileType>())
                yield return entry;
            foreach (var entry in GetRitsuDynamicEnumEntries<CardTag>())
                yield return entry;
            foreach (var entry in GetRitsuDynamicEnumEntries<RewardType>())
                yield return entry;
            foreach (var entry in GetRitsuDynamicEnumEntries<TargetType>())
                yield return entry;
        }

        private static IEnumerable<RitsuDynamicEnumEntry> GetRitsuDynamicEnumEntries<TEnum>()
            where TEnum : struct, Enum
        {
            var entriesByValue = new Dictionary<int, RitsuDynamicEnumEntry>();
            foreach (var definition in DynamicEnumValueRegistry<TEnum>.GetDefinitionsSnapshot())
            {
                var value = Convert.ToInt32(definition.Value);
                entriesByValue[value] = new(typeof(TEnum), value, definition.Id, definition.ModId, true);
            }

            foreach (var (id, value) in DynamicEnumValueRegistry<TEnum>.GetMintedValuesSnapshot())
            {
                var numericValue = Convert.ToInt32(value);
                entriesByValue.TryAdd(numericValue, new(typeof(TEnum), numericValue, id, null, false));
            }

            return entriesByValue.Values;
        }

        private static Type? ResolveLoadedType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = null;
                try
                {
                    type = assembly.GetType(fullTypeName, false);
                }
                catch
                {
                    // ignored
                }

                if (type != null)
                    return type;
            }

            return null;
        }

        private static (string Prefix, string Name) ReadBaseLibGeneratedName(object? generatedName)
        {
            if (generatedName == null)
                return ("", "<unknown>");

            var type = generatedName.GetType();
            var prefix = type.GetField("Item1")?.GetValue(generatedName) as string ?? "";
            var name = type.GetField("Item2")?.GetValue(generatedName) as string ?? "<unknown>";
            return (prefix, name);
        }

        private static bool TryConvertToInt32(object key, out int value)
        {
            try
            {
                value = Convert.ToInt32(key);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        private readonly record struct BaseLibCustomEnumEntry(
            Type EnumType,
            int Value,
            string Prefix,
            string Name)
        {
            public string DisplayName => $"{Prefix}{Name}";
        }

        private readonly record struct RitsuDynamicEnumEntry(
            Type EnumType,
            int Value,
            string Id,
            string? ModId,
            bool Registered)
        {
            public string DisplayName => Registered && !string.IsNullOrWhiteSpace(ModId)
                ? $"{Id} (mod {ModId})"
                : $"{Id} (minted lookup)";
        }

        private readonly record struct DynamicEnumValueCollision(
            BaseLibCustomEnumEntry BaseLibEntry,
            RitsuDynamicEnumEntry RitsuEntry);
    }
}
