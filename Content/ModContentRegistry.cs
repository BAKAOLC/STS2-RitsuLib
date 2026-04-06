using System.Reflection;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Whether <see cref="ModContentRegistry" /> still accepts new registrations from mods.
    /// </summary>
    public enum ContentRegistrationState
    {
        /// <summary>
        ///     Registrations are allowed until the framework freezes them.
        /// </summary>
        Open = 0,

        /// <summary>
        ///     Further registration throws; game content lists are considered sealed.
        /// </summary>
        Frozen = 1,
    }

    /// <summary>
    ///     Per-mod content registration surface: pool models, standalone models, act-scoped content, and stable public
    ///     entry overrides used by patched <see cref="ModelDb" /> identity.
    /// </summary>
    public sealed partial class ModContentRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, ModContentRegistry> Registries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<Type, string> FixedPublicEntryOverrides = [];

        private static readonly HashSet<(Type PoolType, Type ModelType)> RegisteredPoolContent = [];
        private static readonly HashSet<Type> RegisteredCharacters = [];
        private static readonly HashSet<Type> RegisteredActs = [];
        private static readonly HashSet<Type> RegisteredMonsters = [];
        private static readonly HashSet<Type> RegisteredPowers = [];
        private static readonly HashSet<Type> RegisteredOrbs = [];
        private static readonly HashSet<Type> RegisteredSharedCardPools = [];
        private static readonly HashSet<Type> RegisteredSharedEvents = [];
        private static readonly HashSet<Type> RegisteredSharedAncients = [];
        private static readonly Dictionary<Type, HashSet<Type>> RegisteredActEncounters = [];

        private static readonly HashSet<Type> RegisteredGlobalEncounters = [];
        private static readonly Dictionary<Type, HashSet<Type>> RegisteredActEvents = [];
        private static readonly Dictionary<Type, HashSet<Type>> RegisteredActAncients = [];
        private static readonly HashSet<Type> RegisteredEnchantments = [];
        private static readonly HashSet<Type> RegisteredAfflictions = [];
        private static readonly HashSet<Type> RegisteredAchievements = [];
        private static readonly HashSet<Type> RegisteredSingletons = [];
        private static readonly HashSet<Type> RegisteredSharedRelicPools = [];
        private static readonly HashSet<Type> RegisteredSharedPotionPools = [];
        private static readonly HashSet<Type> RegisteredGoodModifiers = [];
        private static readonly HashSet<Type> RegisteredBadModifiers = [];
        private static readonly Dictionary<Type, string> RegisteredTypeOwners = [];

        private readonly Logger _logger;
        private string? _freezeReason;

        private ModContentRegistry(string modId)
        {
            ModId = modId;
            _logger = RitsuLibFramework.CreateLogger(modId);
        }

        /// <summary>
        ///     Mod identifier this registry instance was created for (<see cref="For" />).
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     True after <c>FreezeRegistrations</c> has run globally.
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Convenience view of <see cref="IsFrozen" /> as <see cref="ContentRegistrationState" />.
        /// </summary>
        public static ContentRegistrationState State => IsFrozen
            ? ContentRegistrationState.Frozen
            : ContentRegistrationState.Open;

        /// <summary>
        ///     Resolves which mod registered <paramref name="modelType" />, if any.
        /// </summary>
        public static bool TryGetOwnerModId(Type modelType, out string modId)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            lock (SyncRoot)
            {
                return RegisteredTypeOwners.TryGetValue(modelType, out modId!);
            }
        }

        /// <summary>
        ///     Returns the stable public entry string for a RitsuLib-registered model type (override or generated).
        /// </summary>
        public static bool TryGetFixedPublicEntry(Type modelType, out string entry)
        {
            ArgumentNullException.ThrowIfNull(modelType);

            if (!TryGetOwnerModId(modelType, out var modId))
            {
                entry = string.Empty;
                return false;
            }

            lock (SyncRoot)
            {
                if (FixedPublicEntryOverrides.TryGetValue(modelType, out var over))
                {
                    entry = over;
                    return true;
                }
            }

            entry = GetFixedPublicEntry(modId, modelType);
            return true;
        }

        /// <summary>
        ///     Builds the default normalized entry <c>MOD_CATEGORY_TYPENAME</c> for a type owned by
        ///     <paramref name="modId" />.
        /// </summary>
        public static string GetFixedPublicEntry(string modId, Type modelType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentNullException.ThrowIfNull(modelType);

            var modStem = NormalizePublicStem(modId);
            var categoryStem = NormalizePublicStem(ModelDb.GetCategory(modelType));
            var typeStem = NormalizePublicStem(modelType.Name);
            return $"{modStem}_{categoryStem}_{typeStem}";
        }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" /> (created on first use).
        /// </summary>
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

        /// <summary>
        ///     Registers <typeparamref name="TCard" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        /// </summary>
        public void RegisterCard<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterCard<TPool, TCard>(default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TCard" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterCard<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TCard), "card", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        /// </summary>
        public void RegisterRelic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic<TPool, TRelic>(default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterRelic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TRelic), "relic", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        /// </summary>
        public void RegisterPotion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion<TPool, TPotion>(default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterPotion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPoolModel(typeof(TPool), typeof(TPotion), "potion", publicEntry);
        }

        /// <summary>
        ///     Registers a mod character model for inclusion in <see cref="ModelDb.AllCharacters" />.
        /// </summary>
        public void RegisterCharacter<TCharacter>() where TCharacter : CharacterModel
        {
            RegisterStandaloneModel(RegisteredCharacters, typeof(TCharacter), typeof(CharacterModel), "character");
        }

        /// <summary>
        ///     Registers a mod act model for inclusion in <see cref="ModelDb.Acts" />.
        /// </summary>
        public void RegisterAct<TAct>() where TAct : ActModel
        {
            RegisterStandaloneModel(RegisteredActs, typeof(TAct), typeof(ActModel), "act");
        }

        /// <summary>
        ///     Registers a mod monster model type for RitsuLib tracking, <see cref="ModelDb" /> identity, dynamic injection, and
        ///     patched merge into <c>ModelDb.Monsters</c>.
        /// </summary>
        public void RegisterMonster<TMonster>() where TMonster : MonsterModel
        {
            RegisterStandaloneModel(RegisteredMonsters, typeof(TMonster), typeof(MonsterModel), "monster");
        }

        /// <summary>
        ///     Registers a mod power model for inclusion in <see cref="ModelDb.AllPowers" />.
        /// </summary>
        public void RegisterPower<TPower>() where TPower : PowerModel
        {
            RegisterStandaloneModel(RegisteredPowers, typeof(TPower), typeof(PowerModel), "power");
        }

        /// <summary>
        ///     Registers a mod orb model for inclusion in <see cref="ModelDb.Orbs" />.
        /// </summary>
        public void RegisterOrb<TOrb>() where TOrb : OrbModel
        {
            RegisterStandaloneModel(RegisteredOrbs, typeof(TOrb), typeof(OrbModel), "orb");
        }

        /// <summary>
        ///     Registers a mod enchantment model for RitsuLib tracking, fixed <see cref="ModelDb" /> entry identity, dynamic
        ///     injection, and inclusion in patched <see cref="ModelDb.DebugEnchantments" />.
        /// </summary>
        public void RegisterEnchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            RegisterStandaloneModel(RegisteredEnchantments, typeof(TEnchantment), typeof(EnchantmentModel),
                "enchantment");
        }

        /// <summary>
        ///     Registers a mod affliction model for RitsuLib tracking, fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.DebugAfflictions" />.
        /// </summary>
        public void RegisterAffliction<TAffliction>() where TAffliction : AfflictionModel
        {
            RegisterStandaloneModel(RegisteredAfflictions, typeof(TAffliction), typeof(AfflictionModel), "affliction");
        }

        /// <summary>
        ///     Registers a mod achievement model for fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.Achievements" />.
        /// </summary>
        public void RegisterAchievement<TAchievement>() where TAchievement : AchievementModel
        {
            RegisterStandaloneModel(RegisteredAchievements, typeof(TAchievement), typeof(AchievementModel),
                "achievement");
        }

        /// <summary>
        ///     Registers a mod singleton model for fixed entry identity and dynamic injection (resolved via
        ///     <see cref="ModelDb.Singleton{T}" />).
        /// </summary>
        public void RegisterSingleton<TSingleton>() where TSingleton : SingletonModel
        {
            RegisterStandaloneModel(RegisteredSingletons, typeof(TSingleton), typeof(SingletonModel), "singleton");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;good&quot; daily modifier for patched <see cref="ModelDb.GoodModifiers" />.
        /// </summary>
        public void RegisterGoodModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterStandaloneModel(RegisteredGoodModifiers, typeof(TModifier), typeof(ModifierModel), "good modifier");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;bad&quot; daily modifier for patched <see cref="ModelDb.BadModifiers" />.
        /// </summary>
        public void RegisterBadModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterStandaloneModel(RegisteredBadModifiers, typeof(TModifier), typeof(ModifierModel), "bad modifier");
        }

        /// <summary>
        ///     Registers a shared card pool model for inclusion in <see cref="ModelDb.AllSharedCardPools" />.
        /// </summary>
        public void RegisterSharedCardPool<TPool>() where TPool : CardPoolModel
        {
            RegisterStandaloneModel(RegisteredSharedCardPools, typeof(TPool), typeof(CardPoolModel),
                "shared card pool");
        }

        /// <summary>
        ///     Registers a shared relic pool model for inclusion in patched <see cref="ModelDb.AllRelicPools" />.
        /// </summary>
        public void RegisterSharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            RegisterStandaloneModel(RegisteredSharedRelicPools, typeof(TPool), typeof(RelicPoolModel),
                "shared relic pool");
        }

        /// <summary>
        ///     Registers a shared potion pool model for inclusion in patched <see cref="ModelDb.AllPotionPools" />.
        /// </summary>
        public void RegisterSharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            RegisterStandaloneModel(RegisteredSharedPotionPools, typeof(TPool), typeof(PotionPoolModel),
                "shared potion pool");
        }

        /// <summary>
        ///     Registers a shared event model for inclusion in shared event enumerations.
        /// </summary>
        public void RegisterSharedEvent<TEvent>() where TEvent : EventModel
        {
            RegisterStandaloneModel(RegisteredSharedEvents, typeof(TEvent), typeof(EventModel), "shared event");
        }

        /// <summary>
        ///     Registers an encounter model scoped to <typeparamref name="TAct" />.
        /// </summary>
        public void RegisterActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            RegisterScopedModel(RegisteredActEncounters, typeof(TAct), typeof(TEncounter), typeof(ActModel),
                typeof(EncounterModel), "act encounter");
        }

        /// <summary>
        ///     Registers an encounter model appended to <strong>every</strong> act’s
        ///     <see cref="ActModel.GenerateAllEncounters" /> result (after vanilla and act-scoped mod encounters).
        ///     Use for elites / monsters / bosses that should appear in multiple acts; use
        ///     <see cref="RegisterActEncounter{TAct,TEncounter}" /> when the encounter belongs to one act only.
        /// </summary>
        public void RegisterGlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            RegisterStandaloneModel(RegisteredGlobalEncounters, typeof(TEncounter), typeof(EncounterModel),
                "global encounter");
        }

        /// <summary>
        ///     Registers an event model scoped to <typeparamref name="TAct" />.
        /// </summary>
        public void RegisterActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            RegisterScopedModel(RegisteredActEvents, typeof(TAct), typeof(TEvent), typeof(ActModel), typeof(EventModel),
                "act event");
        }

        /// <summary>
        ///     Registers a shared ancient event model for inclusion in ancient enumerations.
        /// </summary>
        public void RegisterSharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            RegisterStandaloneModel(RegisteredSharedAncients, typeof(TAncient), typeof(AncientEventModel),
                "shared ancient");
        }

        /// <summary>
        ///     Registers an ancient event model scoped to <typeparamref name="TAct" />.
        /// </summary>
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

            foreach (var registry in Registries.Values)
                registry._logger.Info($"[Content] Content registration is now frozen ({reason}).");

            RitsuLibFramework.PublishLifecycleEvent(
                new ContentRegistrationClosedEvent(reason, DateTimeOffset.UtcNow),
                nameof(ContentRegistrationClosedEvent)
            );
        }

        internal static IEnumerable<CharacterModel> AppendCharacters(IEnumerable<CharacterModel> source)
        {
            return AppendResolved(source, ResolveModels<CharacterModel>(RegisteredCharacters));
        }

        internal static IEnumerable<CharacterModel> GetModCharacters()
        {
            return ResolveModels<CharacterModel>(RegisteredCharacters);
        }

        internal static IEnumerable<EventModel> AppendSharedEvents(IEnumerable<EventModel> source)
        {
            return AppendResolved(source, ResolveModels<EventModel>(RegisteredSharedEvents));
        }

        internal static IEnumerable<ActModel> AppendActs(IEnumerable<ActModel> source)
        {
            return AppendResolved(source, ResolveModels<ActModel>(RegisteredActs));
        }

        internal static IEnumerable<PowerModel> AppendPowers(IEnumerable<PowerModel> source)
        {
            return AppendResolved(source, ResolveModels<PowerModel>(RegisteredPowers));
        }

        internal static IEnumerable<OrbModel> AppendOrbs(IEnumerable<OrbModel> source)
        {
            return AppendResolved(source, ResolveModels<OrbModel>(RegisteredOrbs));
        }

        internal static IEnumerable<EnchantmentModel> AppendEnchantments(IEnumerable<EnchantmentModel> source)
        {
            return AppendResolved(source, ResolveModels<EnchantmentModel>(RegisteredEnchantments));
        }

        internal static IEnumerable<AfflictionModel> AppendAfflictions(IEnumerable<AfflictionModel> source)
        {
            return AppendResolved(source, ResolveModels<AfflictionModel>(RegisteredAfflictions));
        }

        internal static IReadOnlyList<AchievementModel> AppendAchievements(IReadOnlyList<AchievementModel> source)
        {
            var additional = ResolveModels<AchievementModel>(RegisteredAchievements);
            return additional.Length == 0 ? source : MergeDistinctByModelId(source, additional);
        }

        internal static IReadOnlyList<ModifierModel> AppendGoodModifiers(IReadOnlyList<ModifierModel> source)
        {
            var additional = ResolveModels<ModifierModel>(RegisteredGoodModifiers);
            return additional.Length == 0 ? source : MergeDistinctByModelId(source, additional);
        }

        internal static IReadOnlyList<ModifierModel> AppendBadModifiers(IReadOnlyList<ModifierModel> source)
        {
            var additional = ResolveModels<ModifierModel>(RegisteredBadModifiers);
            return additional.Length == 0 ? source : MergeDistinctByModelId(source, additional);
        }

        internal static IEnumerable<RelicPoolModel> AppendSharedRelicPools(IEnumerable<RelicPoolModel> source)
        {
            return AppendResolved(source, ResolveModels<RelicPoolModel>(RegisteredSharedRelicPools));
        }

        internal static IEnumerable<PotionPoolModel> AppendSharedPotionPools(IEnumerable<PotionPoolModel> source)
        {
            return AppendResolved(source, ResolveModels<PotionPoolModel>(RegisteredSharedPotionPools));
        }

        internal static IEnumerable<CardPoolModel> AppendSharedCardPools(IEnumerable<CardPoolModel> source)
        {
            return AppendResolved(source, ResolveModels<CardPoolModel>(RegisteredSharedCardPools));
        }

        internal static IEnumerable<EventModel> AppendActEvents(ActModel act, IEnumerable<EventModel> source)
        {
            return AppendResolved(source, ResolveScopedModels<EventModel>(RegisteredActEvents, act.GetType()));
        }

        internal static IEnumerable<EncounterModel> AppendActEncounters(ActModel act,
            IEnumerable<EncounterModel> source)
        {
            return AppendResolved(source, ResolveScopedModels<EncounterModel>(RegisteredActEncounters, act.GetType()));
        }

        internal static IEnumerable<EncounterModel> AppendGlobalEncounters(IEnumerable<EncounterModel> source)
        {
            return AppendResolved(source, ResolveModels<EncounterModel>(RegisteredGlobalEncounters));
        }

        internal static IEnumerable<MonsterModel> AppendRegisteredMonsters(IEnumerable<MonsterModel> source)
        {
            var additional = ResolveModels<MonsterModel>(RegisteredMonsters);
            return MergeDistinctByModelId(source, additional);
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

        /// <summary>
        ///     Injects RitsuLib-registered types that live in <see cref="Assembly.IsDynamic" /> assemblies into
        ///     <see cref="ModelDb" /> before <c>Init</c> finishes populating <c>_contentById</c>. Static mod DLL types are
        ///     picked up by the game's subtype scan; Reflection.Emit placeholder types are not, so they must be injected here.
        /// </summary>
        internal static void InjectDynamicRegisteredModels()
        {
            Type[] typesToInject;

            lock (SyncRoot)
            {
                typesToInject = RegisteredPoolContent
                    .SelectMany(static entry => new[] { entry.PoolType, entry.ModelType })
                    .Concat(RegisteredCharacters)
                    .Concat(RegisteredActs)
                    .Concat(RegisteredMonsters)
                    .Concat(RegisteredPowers)
                    .Concat(RegisteredOrbs)
                    .Concat(RegisteredEnchantments)
                    .Concat(RegisteredAfflictions)
                    .Concat(RegisteredAchievements)
                    .Concat(RegisteredSingletons)
                    .Concat(RegisteredSharedCardPools)
                    .Concat(RegisteredSharedRelicPools)
                    .Concat(RegisteredSharedPotionPools)
                    .Concat(RegisteredGoodModifiers)
                    .Concat(RegisteredBadModifiers)
                    .Concat(RegisteredSharedEvents)
                    .Concat(RegisteredSharedAncients)
                    .Concat(RegisteredActEncounters.Values.SelectMany(static set => set))
                    .Concat(RegisteredGlobalEncounters)
                    .Concat(RegisteredActEvents.Values.SelectMany(static set => set))
                    .Concat(RegisteredActAncients.Values.SelectMany(static set => set))
                    .Distinct()
                    .Where(static t => t.Assembly.IsDynamic)
                    .ToArray();
            }

            foreach (var type in typesToInject)
                ModelDb.Inject(type);
        }

        private void RegisterPoolModel(Type poolType, Type modelType, string contentKind,
            ModelPublicEntryOptions publicEntry = default)
        {
            EnsureMutable($"register {contentKind} '{modelType.Name}' into pool '{poolType.Name}'");
            EnsureModelType(poolType, typeof(AbstractModel), nameof(poolType));
            EnsureModelType(modelType, typeof(AbstractModel), nameof(modelType));
            PrimeOwnedType(modelType);
            ApplyFixedPublicEntryForModel(modelType, publicEntry);
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

                RememberOwner(modelType);
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
            PrimeOwnedType(modelType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);

            lock (SyncRoot)
            {
                if (!registry.Add(modelType))
                {
                    _logger.Debug($"[Content] Skipping duplicate {contentKind} registration: {modelType.Name}");
                    return;
                }

                RememberOwner(modelType);
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
            PrimeOwnedType(modelType);
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

                RememberOwner(modelType);
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

        private static TModel[] ResolveModels<TModel>(IEnumerable<Type> modelTypes)
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

        private static TModel[] ResolveScopedModels<TModel>(Dictionary<Type, HashSet<Type>> registry,
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

        private static TModel[] AppendResolved<TModel>(IEnumerable<TModel> source,
            IEnumerable<TModel> additional)
            where TModel : AbstractModel
        {
            return source.Concat(additional).Distinct().ToArray();
        }

        private static List<TModel> MergeDistinctByModelId<TModel>(IEnumerable<TModel> first,
            IEnumerable<TModel> second)
            where TModel : AbstractModel
        {
            return first.Concat(second).DistinctBy(static m => m.Id).ToList();
        }

        private static string NormalizePublicStem(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var normalized = NonAlphaNumericRegex().Replace(value.Trim(), "_");
            normalized = AcronymBoundaryRegex().Replace(normalized, "$1_$2");
            normalized = CamelBoundaryRegex().Replace(normalized, "$1_$2");
            normalized = RepeatedUnderscoreRegex().Replace(normalized, "_");
            return normalized.Trim('_').ToUpperInvariant();
        }

        private static string NormalizeFullPublicEntry(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            var normalized = NonAlphaNumericRegex().Replace(value.Trim(), "_");
            normalized = AcronymBoundaryRegex().Replace(normalized, "$1_$2");
            normalized = CamelBoundaryRegex().Replace(normalized, "$1_$2");
            normalized = RepeatedUnderscoreRegex().Replace(normalized, "_");
            return normalized.Trim('_').ToUpperInvariant();
        }

        private void ApplyFixedPublicEntryForModel(Type modelType, ModelPublicEntryOptions options)
        {
            if (options.Kind == ModelPublicEntryKind.FromTypeName)
                return;

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var resolved = options.Kind switch
            {
                ModelPublicEntryKind.Stem =>
                    $"{NormalizePublicStem(ModId)}_{NormalizePublicStem(ModelDb.GetCategory(modelType))}_{NormalizePublicStem(options.Value!)}",
                ModelPublicEntryKind.FullEntry => NormalizeFullPublicEntry(options.Value!),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options.Kind, null),
            };

            lock (SyncRoot)
            {
                if (FixedPublicEntryOverrides.TryGetValue(modelType, out var existing))
                {
                    if (!string.Equals(existing, resolved, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"Cannot change fixed public entry for '{modelType.FullName}' from '{existing}' to '{resolved}'.");

                    return;
                }

                FixedPublicEntryOverrides[modelType] = resolved;
            }
        }

        [GeneratedRegex("[^A-Za-z0-9]+")]
        private static partial Regex NonAlphaNumericRegex();

        [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
        private static partial Regex AcronymBoundaryRegex();

        [GeneratedRegex("([a-z0-9])([A-Z])")]
        private static partial Regex CamelBoundaryRegex();

        [GeneratedRegex("_+")]
        private static partial Regex RepeatedUnderscoreRegex();

        private void RememberOwner(Type type)
        {
            RegisteredTypeOwners[type] = ModId;
        }

        private void PrimeOwnedType(Type type)
        {
            lock (SyncRoot)
            {
                RegisteredTypeOwners[type] = ModId;
            }
        }
    }
}
