using System.Reflection;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Per-mod registry for custom <c>EpochModel</c> and <c>StoryModel</c> types wired into the game's static
    ///     timeline dictionaries.
    /// </summary>
    public sealed class ModTimelineRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModTimelineRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<Type> RegisteredEpochTypes = [];

        private static readonly HashSet<Type> RegisteredStoryTypes = [];

        private readonly string _modId;
        private string? _freezeReason;

        private ModTimelineRegistry(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     True after the framework freezes further epoch/story registrations (e.g. at model init).
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Returns the timeline registry singleton for <paramref name="modId" />.
        /// </summary>
        public static ModTimelineRegistry For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);

            lock (SyncRoot)
            {
                if (Registries.TryGetValue(modId, out var registry))
                    return registry;

                registry = new(modId);
                Registries[modId] = registry;
                return registry;
            }
        }

        /// <summary>
        ///     Registers a concrete epoch type so its id appears in vanilla epoch discovery.
        /// </summary>
        public void RegisterEpoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            RegisterEpoch(typeof(TEpoch));
        }

        /// <summary>
        ///     Registers a concrete story type in the game's story type dictionary.
        /// </summary>
        public void RegisterStory<TStory>() where TStory : StoryModel, new()
        {
            RegisterStory(typeof(TStory));
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;
            }
        }

        private void RegisterEpoch(Type epochType)
        {
            EnsureMutable($"register epoch '{epochType.Name}'");
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            var epoch = (EpochModel)Activator.CreateInstance(epochType)!;
            var epochId = epoch.Id;

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfEpochIdConflicts(
                    epochId,
                    epochType,
                    GetKnownEpochTypes());

                if (!RegisteredEpochTypes.Add(epochType))
                    return;

                var epochTypeDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
                var typeToIdDictionary =
                    GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

                epochTypeDictionary[epochId] = epochType;
                typeToIdDictionary[epochType] = epochId;
                SetStaticField(typeof(EpochModel), "_allEpochIds",
                    epochTypeDictionary.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
            }
        }

        private void RegisterStory(Type storyType)
        {
            EnsureMutable($"register story '{storyType.Name}'");
            EnsureSubtype(storyType, typeof(StoryModel), nameof(storyType));

            var storyId = GetStoryId(storyType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfStoryIdConflicts(
                    storyId,
                    storyType,
                    GetKnownStoryTypes());

                if (!RegisteredStoryTypes.Add(storyType))
                    return;

                var storyDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
                storyDictionary[storyId] = storyType;
            }
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after timeline registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register custom stories and epochs from your mod initializer before model initialization.");
        }

        private static void EnsureSubtype(Type type, Type expectedBaseType, string paramName)
        {
            if (type.IsAbstract || type.IsInterface || !expectedBaseType.IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{expectedBaseType.FullName}'.",
                    paramName);
        }

        private static Type[] GetKnownEpochTypes()
        {
            var typeToIdDictionary =
                GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");
            return typeToIdDictionary.Keys.ToArray();
        }

        private static Type[] GetKnownStoryTypes()
        {
            var storyDictionary = GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
            return storyDictionary.Values.ToArray();
        }

        private static string GetStoryId(Type storyType)
        {
            var story = (StoryModel)Activator.CreateInstance(storyType)!;
            var property = storyType.GetProperty("Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (string)(property?.GetValue(story) ??
                            throw new InvalidOperationException(
                                $"Story type '{storyType.FullName}' does not expose an Id property."));
        }

        private static TField GetStaticField<TField>(Type ownerType, string fieldName) where TField : class
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(ownerType.FullName, fieldName);

            return (TField)(field.GetValue(null)
                            ?? throw new InvalidOperationException(
                                $"Static field '{ownerType.FullName}.{fieldName}' is null."));
        }

        private static void SetStaticField(Type ownerType, string fieldName, object? value)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(ownerType.FullName, fieldName);

            field.SetValue(null, value);
        }
    }
}
