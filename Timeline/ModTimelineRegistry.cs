using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Per-mod registry for custom <c>EpochModel</c> and <c>StoryModel</c> types wired into the game's static
    ///     timeline dictionaries. Epochs are individual unlock slots; a <see cref="StoryModel" /> groups them into one
    ///     timeline column (story title + ordered epoch list in the progression UI).
    /// </summary>
    public sealed class ModTimelineRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModTimelineRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<Type> RegisteredEpochTypes = [];

        private static readonly HashSet<Type> RegisteredStoryTypes = [];
        private readonly Logger _logger;

        private readonly string _modId;
        private string? _freezeReason;

        private ModTimelineRegistry(string modId)
        {
            _modId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
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
        ///     Registers <paramref name="epochType" /> so its id appears in vanilla epoch discovery.
        /// </summary>
        public void RegisterEpoch(Type epochType)
        {
            EnsureMutable($"register epoch '{epochType.Name}'");
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            var epochId = GetEpochId(epochType);

            lock (SyncRoot)
            {
                RegistrationConflictDetector.ThrowIfEpochIdConflicts(
                    epochId,
                    epochType,
                    GetKnownEpochTypes());

                if (!RegisteredEpochTypes.Add(epochType))
                {
                    _logger.Debug($"[Timeline] Skipping duplicate epoch registration: {epochType.Name} (id={epochId})");
                    return;
                }

                var epochTypeDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
                var typeToIdDictionary =
                    GetStaticField<Dictionary<Type, string>>(typeof(EpochModel), "_typeToIdDictionary");

                epochTypeDictionary[epochId] = epochType;
                typeToIdDictionary[epochType] = epochId;
                RefreshAllEpochIdsSnapshotLocked();
            }

            _logger.Info($"[Timeline] Registered epoch: {epochType.Name} (id={epochId})");
        }

        /// <summary>
        ///     Registers a concrete story type in the game's story type dictionary.
        /// </summary>
        public void RegisterStory<TStory>() where TStory : StoryModel, new()
        {
            RegisterStory(typeof(TStory));
        }

        /// <summary>
        ///     Registers <paramref name="storyType" /> in the game's story type dictionary.
        /// </summary>
        public void RegisterStory(Type storyType)
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
                {
                    _logger.Debug($"[Timeline] Skipping duplicate story registration: {storyType.Name} (id={storyId})");
                    return;
                }

                var storyDictionary =
                    GetStaticField<Dictionary<string, Type>>(typeof(StoryModel), "_storyTypeDictionary");
                storyDictionary[storyId] = storyType;
            }

            _logger.Info($"[Timeline] Registered story: {storyType.Name} (id={storyId})");
        }

        /// <summary>
        ///     Registers <typeparamref name="TEpoch" /> with vanilla epoch discovery and appends it to
        ///     <typeparamref name="TStory" />'s ordered column via <see cref="ModStoryEpochBindings" /> (manifest / pack order).
        /// </summary>
        public void RegisterStoryEpoch<TStory, TEpoch>()
            where TStory : StoryModel, new()
            where TEpoch : EpochModel, new()
        {
            RegisterStoryEpoch(typeof(TStory), typeof(TEpoch));
        }

        /// <summary>
        ///     Registers <paramref name="epochType" /> and binds it to <paramref name="storyType" />'s story column.
        /// </summary>
        public void RegisterStoryEpoch(Type storyType, Type epochType)
        {
            ArgumentNullException.ThrowIfNull(storyType);
            ArgumentNullException.ThrowIfNull(epochType);
            EnsureMutable($"register story-epoch binding '{storyType.Name}' ← '{epochType.Name}'");
            EnsureSubtype(storyType, typeof(StoryModel), nameof(storyType));
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));

            RegisterEpoch(epochType);
            ModStoryEpochBindings.Append(storyType, epochType);
            _logger.Info($"[Timeline] Story-epoch binding: {storyType.Name} ← {epochType.Name}");
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (IsFrozen)
                    return;

                IsFrozen = true;
                ModStoryEpochBindings.Freeze();
                foreach (var registry in Registries.Values)
                    registry._freezeReason = reason;
            }

            ModTimelineLayoutRegistry.FreezeAndValidate();
        }

        /// <summary>
        ///     Rewrites <see cref="EpochModel.AllEpochIds" /> from the live <c>_epochTypeDictionary</c> so
        ///     <see cref="EpochModel.IsValid" /> matches <see cref="EpochModel.Get" /> after third-party dictionary edits or
        ///     early vanilla lazy-init of <c>_allEpochIds</c>.
        /// </summary>
        internal static void EnsureAllEpochIdsSyncedWithDictionary()
        {
            lock (SyncRoot)
            {
                RefreshAllEpochIdsSnapshotLocked();
            }
        }

        internal static string GetEpochId(Type epochType)
        {
            EnsureSubtype(epochType, typeof(EpochModel), nameof(epochType));
            var epoch = Activator.CreateInstance(epochType) as EpochModel
                        ?? throw new InvalidOperationException(
                            $"Could not construct epoch type '{epochType.FullName}'.");
            return string.IsNullOrWhiteSpace(epoch.Id)
                ? throw new InvalidOperationException($"Epoch type '{epochType.FullName}' returned an empty Id.")
                : epoch.Id.Trim();
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

        private static void RefreshAllEpochIdsSnapshotLocked()
        {
            var epochTypeDictionary =
                GetStaticField<Dictionary<string, Type>>(typeof(EpochModel), "_epochTypeDictionary");
            SetStaticField(typeof(EpochModel), "_allEpochIds",
                epochTypeDictionary.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        }
    }
}
