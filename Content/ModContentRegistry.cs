using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Content
{
    public enum ContentRegistrationState
    {
        Open = 0,
        Frozen = 1,
    }

    public sealed class ModContentRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModContentRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<(Type PoolType, Type ModelType)> RegisteredPoolContent = [];
        private static readonly HashSet<Type> RegisteredCharacters = [];
        private static readonly HashSet<Type> RegisteredPowers = [];
        private static readonly HashSet<Type> RegisteredOrbs = [];
        private static readonly HashSet<Type> RegisteredSharedEvents = [];
        private static readonly HashSet<Type> RegisteredSharedAncients = [];
        private static readonly Dictionary<Type, HashSet<Type>> RegisteredActEvents = [];
        private static readonly Dictionary<Type, HashSet<Type>> RegisteredActAncients = [];

        private readonly Logger _logger;
        private string? _freezeReason;

        private ModContentRegistry(string modId)
        {
            ModId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        public string ModId { get; }
        public static bool IsFrozen { get; private set; }

        public static ContentRegistrationState State => IsFrozen
            ? ContentRegistrationState.Frozen
            : ContentRegistrationState.Open;

        public static ModContentRegistry For(string modId)
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

        public void RegisterCard<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TCard), "card");
        }

        public void RegisterRelic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TRelic), "relic");
        }

        public void RegisterPotion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TPotion), "potion");
        }

        public void RegisterCharacter<TCharacter>() where TCharacter : CharacterModel
        {
            RegisterStandaloneModel(RegisteredCharacters, typeof(TCharacter), typeof(CharacterModel), "character");
        }

        public void RegisterPower<TPower>() where TPower : PowerModel
        {
            RegisterStandaloneModel(RegisteredPowers, typeof(TPower), typeof(PowerModel), "power");
        }

        public void RegisterOrb<TOrb>() where TOrb : OrbModel
        {
            RegisterStandaloneModel(RegisteredOrbs, typeof(TOrb), typeof(OrbModel), "orb");
        }

        public void RegisterSharedEvent<TEvent>() where TEvent : EventModel
        {
            RegisterStandaloneModel(RegisteredSharedEvents, typeof(TEvent), typeof(EventModel), "shared event");
        }

        public void RegisterActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            RegisterScopedModel(RegisteredActEvents, typeof(TAct), typeof(TEvent), typeof(ActModel), typeof(EventModel),
                "act event");
        }

        public void RegisterSharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            RegisterStandaloneModel(RegisteredSharedAncients, typeof(TAncient), typeof(AncientEventModel),
                "shared ancient");
        }

        public void RegisterActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            RegisterScopedModel(RegisteredActAncients, typeof(TAct), typeof(TAncient), typeof(ActModel),
                typeof(AncientEventModel), "act ancient");
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

            RitsuLibFramework.Logger.Info($"[Content] Content registration is now frozen ({reason}).");
            RitsuLibFramework.PublishLifecycleEvent(
                new ContentRegistrationClosedEvent(reason, DateTimeOffset.UtcNow),
                nameof(ContentRegistrationClosedEvent)
            );
        }

        internal static IEnumerable<CharacterModel> AppendCharacters(IEnumerable<CharacterModel> source)
        {
            return AppendResolved(source, ResolveModels<CharacterModel>(RegisteredCharacters));
        }

        internal static IEnumerable<EventModel> AppendSharedEvents(IEnumerable<EventModel> source)
        {
            return AppendResolved(source, ResolveModels<EventModel>(RegisteredSharedEvents));
        }

        internal static IEnumerable<PowerModel> AppendPowers(IEnumerable<PowerModel> source)
        {
            return AppendResolved(source, ResolveModels<PowerModel>(RegisteredPowers));
        }

        internal static IEnumerable<OrbModel> AppendOrbs(IEnumerable<OrbModel> source)
        {
            return AppendResolved(source, ResolveModels<OrbModel>(RegisteredOrbs));
        }

        internal static IEnumerable<EventModel> AppendActEvents(ActModel act, IEnumerable<EventModel> source)
        {
            return AppendResolved(source, ResolveScopedModels<EventModel>(RegisteredActEvents, act.GetType()));
        }

        internal static IEnumerable<AncientEventModel> AppendSharedAncients(IEnumerable<AncientEventModel> source)
        {
            return AppendResolved(source, ResolveModels<AncientEventModel>(RegisteredSharedAncients));
        }

        internal static IEnumerable<AncientEventModel> AppendActAncients(ActModel act,
            IEnumerable<AncientEventModel> source)
        {
            return AppendResolved(source, ResolveScopedModels<AncientEventModel>(RegisteredActAncients, act.GetType()));
        }

        internal static void InjectRegisteredModels()
        {
            Type[] typesToInject;

            lock (SyncRoot)
            {
                typesToInject = RegisteredPoolContent
                    .SelectMany(static entry => new[] { entry.PoolType, entry.ModelType })
                    .Concat(RegisteredCharacters)
                    .Concat(RegisteredPowers)
                    .Concat(RegisteredOrbs)
                    .Concat(RegisteredSharedEvents)
                    .Concat(RegisteredSharedAncients)
                    .Concat(RegisteredActEvents.Values.SelectMany(static set => set))
                    .Concat(RegisteredActAncients.Values.SelectMany(static set => set))
                    .Distinct()
                    .ToArray();
            }

            foreach (var type in typesToInject)
                ModelDb.Inject(type);
        }

        private void RegisterPoolModel(Type poolType, Type modelType, string contentKind)
        {
            EnsureMutable($"register {contentKind} '{modelType.Name}' into pool '{poolType.Name}'");
            EnsureModelType(poolType, typeof(AbstractModel), nameof(poolType));
            EnsureModelType(modelType, typeof(AbstractModel), nameof(modelType));
            RegistrationConflictDetector.ThrowIfModelIdConflicts(poolType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);

            lock (SyncRoot)
            {
                if (!RegisteredPoolContent.Add((poolType, modelType)))
                {
                    _logger.Debug(
                        $"[Content] Skipping duplicate {contentKind} registration: {modelType.Name} -> {poolType.Name}");
                    return;
                }
            }

            ModHelper.AddModelToPool(poolType, modelType);
            _logger.Info($"[Content] Registered {contentKind}: {modelType.Name} -> {poolType.Name}");
        }

        private void RegisterStandaloneModel(
            HashSet<Type> registry,
            Type modelType,
            Type expectedBaseType,
            string contentKind)
        {
            EnsureMutable($"register {contentKind} '{modelType.Name}'");
            EnsureModelType(modelType, expectedBaseType, nameof(modelType));
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);

            lock (SyncRoot)
            {
                if (!registry.Add(modelType))
                {
                    _logger.Debug($"[Content] Skipping duplicate {contentKind} registration: {modelType.Name}");
                    return;
                }
            }

            _logger.Info($"[Content] Registered {contentKind}: {modelType.Name}");
        }

        private void RegisterScopedModel(
            Dictionary<Type, HashSet<Type>> registry,
            Type scopeType,
            Type modelType,
            Type expectedScopeType,
            Type expectedModelBaseType,
            string contentKind)
        {
            EnsureMutable($"register {contentKind} '{modelType.Name}' for '{scopeType.Name}'");
            EnsureModelType(scopeType, expectedScopeType, nameof(scopeType));
            EnsureModelType(modelType, expectedModelBaseType, nameof(modelType));
            RegistrationConflictDetector.ThrowIfModelIdConflicts(scopeType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);

            lock (SyncRoot)
            {
                if (!registry.TryGetValue(scopeType, out var entries))
                {
                    entries = [];
                    registry[scopeType] = entries;
                }

                if (!entries.Add(modelType))
                {
                    _logger.Debug(
                        $"[Content] Skipping duplicate {contentKind} registration: {modelType.Name} -> {scopeType.Name}");
                    return;
                }
            }

            _logger.Info($"[Content] Registered {contentKind}: {modelType.Name} -> {scopeType.Name}");
        }

        private void EnsureMutable(string operation)
        {
            if (!IsFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after content registration has been frozen ({_freezeReason ?? "unknown"}). " +
                "Register content from your mod initializer before the game initializes ModelDb.");
        }

        private static void EnsureModelType(Type type, Type expectedBaseType, string paramName)
        {
            if (type.IsAbstract || type.IsInterface || !expectedBaseType.IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{expectedBaseType.FullName}'.",
                    paramName
                );
        }

        private static IEnumerable<TModel> ResolveModels<TModel>(IEnumerable<Type> modelTypes)
            where TModel : AbstractModel
        {
            lock (SyncRoot)
            {
                return modelTypes
                    .Select(ModelDb.GetId)
                    .Select(ModelDb.GetById<TModel>)
                    .ToArray();
            }
        }

        private static IEnumerable<TModel> ResolveScopedModels<TModel>(Dictionary<Type, HashSet<Type>> registry,
            Type scopeType)
            where TModel : AbstractModel
        {
            lock (SyncRoot)
            {
                return !registry.TryGetValue(scopeType, out var modelTypes)
                    ? []
                    : modelTypes
                        .Select(ModelDb.GetId)
                        .Select(ModelDb.GetById<TModel>)
                        .ToArray();
            }
        }

        private static IEnumerable<TModel> AppendResolved<TModel>(IEnumerable<TModel> source,
            IEnumerable<TModel> additional)
            where TModel : AbstractModel
        {
            return source.Concat(additional).Distinct().ToArray();
        }
    }
}
