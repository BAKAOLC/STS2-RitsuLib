using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace STS2RitsuLib.Diagnostics
{
    internal static class RegistrationConflictDetector
    {
        private static readonly Lock IndexGate = new();
        private static Dictionary<ModelId, List<Type>>? _gameModelIdIndex;

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
            var conflicts = GetGameModelIdIndex()
                .Where(pair => pair.Value.Count > 1)
                .ToArray();

            foreach (var group in conflicts)
                RitsuLibFramework.Logger.Error(
                    $"[Content] ModelId collision detected for '{group.Key}': " +
                    string.Join(", ", group.Value.Select(type => type.FullName)));

            if (conflicts.Length > 0)
                RitsuLibFramework.Logger.Error(
                    "[Content] Duplicate patched ModelIds are unsafe. RitsuLib formats registered model ids as '<modid>_<category>_<typename>', so this usually indicates two registered models still share the same mod/category/type-name combination.");

            InvalidateModelIdIndex();
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
    }
}
