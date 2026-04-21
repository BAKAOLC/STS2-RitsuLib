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
        private static readonly List<CharacterStarterRegistration> RegisteredCharacterStarterContent = [];
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
        ///     Builds a mod-scoped keyword id using the same stem normalization as fixed public model entries, then
        ///     lowercases the result for keyword registry storage. Other mods can
        ///     reference a provider’s keyword by passing the same <paramref name="modId" /> and
        ///     <paramref name="localKeywordStem" />.
        /// </summary>
        public static string GetQualifiedKeywordId(string modId, string localKeywordStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            var modStem = NormalizePublicStem(modId);
            var keyStem = NormalizePublicStem(localKeywordStem);
            return $"{modStem}_{keyStem}".ToLowerInvariant();
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
            RegisterCard(typeof(TPool), typeof(TCard));
        }

        /// <summary>
        ///     Registers <paramref name="cardType" /> into <paramref name="poolType" /> with default public entry naming.
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType)
        {
            RegisterCard(poolType, cardType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TCard" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterCard<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterCard(typeof(TPool), typeof(TCard), publicEntry);
        }

        /// <summary>
        ///     Registers <paramref name="cardType" /> into <paramref name="poolType" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, cardType, "card", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        /// </summary>
        public void RegisterRelic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic(typeof(TPool), typeof(TRelic));
        }

        /// <summary>
        ///     Registers <paramref name="relicType" /> into <paramref name="poolType" /> with default public entry naming.
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType)
        {
            RegisterRelic(poolType, relicType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterRelic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic(typeof(TPool), typeof(TRelic), publicEntry);
        }

        /// <summary>
        ///     Registers <paramref name="relicType" /> into <paramref name="poolType" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, relicType, "relic", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        /// </summary>
        public void RegisterPotion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion(typeof(TPool), typeof(TPotion));
        }

        /// <summary>
        ///     Registers <paramref name="potionType" /> into <paramref name="poolType" /> with default public entry naming.
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType)
        {
            RegisterPotion(poolType, potionType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterPotion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion(typeof(TPool), typeof(TPotion), publicEntry);
        }

        /// <summary>
        ///     Registers <paramref name="potionType" /> into <paramref name="poolType" /> using
        ///     <paramref name="publicEntry" /> rules.
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, potionType, "potion", publicEntry);
        }

        /// <summary>
        ///     Registers a mod character model for inclusion in <see cref="ModelDb.AllCharacters" />.
        /// </summary>
        public void RegisterCharacter<TCharacter>() where TCharacter : CharacterModel
        {
            RegisterCharacter(typeof(TCharacter));
        }

        /// <summary>
        ///     Registers <paramref name="characterType" /> for inclusion in <see cref="ModelDb.AllCharacters" />.
        /// </summary>
        public void RegisterCharacter(Type characterType)
        {
            RegisterStandaloneModel(RegisteredCharacters, characterType, typeof(CharacterModel), "character");
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <typeparamref name="TCard" /> for <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried.
        /// </summary>
        public void RegisterCharacterStarterCard<TCharacter, TCard>(int count = 1)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterStarterCard(typeof(TCharacter), typeof(TCard), count);
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <paramref name="cardType" /> for <paramref name="characterType" />.
        /// </summary>
        public void RegisterCharacterStarterCard(Type characterType, Type cardType, int count = 1)
        {
            RegisterCharacterStarterModel(characterType, cardType, typeof(CardModel),
                CharacterStarterContentKind.Card,
                count);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <typeparamref name="TRelic" /> for <typeparamref name="TCharacter" />
        ///     .
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried.
        /// </summary>
        public void RegisterCharacterStarterRelic<TCharacter, TRelic>(int count = 1)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterStarterRelic(typeof(TCharacter), typeof(TRelic), count);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <paramref name="relicType" /> for <paramref name="characterType" />.
        /// </summary>
        public void RegisterCharacterStarterRelic(Type characterType, Type relicType, int count = 1)
        {
            RegisterCharacterStarterModel(characterType, relicType, typeof(RelicModel),
                CharacterStarterContentKind.Relic, count);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <typeparamref name="TPotion" /> for
        ///     <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried.
        /// </summary>
        public void RegisterCharacterStarterPotion<TCharacter, TPotion>(int count = 1)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterStarterPotion(typeof(TCharacter), typeof(TPotion), count);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <paramref name="potionType" /> for <paramref name="characterType" />
        ///     .
        /// </summary>
        public void RegisterCharacterStarterPotion(Type characterType, Type potionType, int count = 1)
        {
            RegisterCharacterStarterModel(characterType, potionType, typeof(PotionModel),
                CharacterStarterContentKind.Potion, count);
        }

        /// <summary>
        ///     Registers a mod act model for inclusion in <see cref="ModelDb.Acts" />.
        /// </summary>
        public void RegisterAct<TAct>() where TAct : ActModel
        {
            RegisterAct(typeof(TAct));
        }

        /// <summary>
        ///     Registers <paramref name="actType" /> for inclusion in <see cref="ModelDb.Acts" />.
        /// </summary>
        public void RegisterAct(Type actType)
        {
            RegisterStandaloneModel(RegisteredActs, actType, typeof(ActModel), "act");
        }

        /// <summary>
        ///     Registers a mod monster model type for RitsuLib tracking, <see cref="ModelDb" /> identity, dynamic injection, and
        ///     patched merge into <c>ModelDb.Monsters</c>.
        /// </summary>
        public void RegisterMonster<TMonster>() where TMonster : MonsterModel
        {
            RegisterMonster(typeof(TMonster));
        }

        /// <summary>
        ///     Registers <paramref name="monsterType" /> for RitsuLib tracking and patched monster injection.
        /// </summary>
        public void RegisterMonster(Type monsterType)
        {
            RegisterStandaloneModel(RegisteredMonsters, monsterType, typeof(MonsterModel), "monster");
        }

        /// <summary>
        ///     Registers a mod power model for inclusion in <see cref="ModelDb.AllPowers" />.
        /// </summary>
        public void RegisterPower<TPower>() where TPower : PowerModel
        {
            RegisterPower(typeof(TPower));
        }

        /// <summary>
        ///     Registers <paramref name="powerType" /> for inclusion in <see cref="ModelDb.AllPowers" />.
        /// </summary>
        public void RegisterPower(Type powerType)
        {
            RegisterStandaloneModel(RegisteredPowers, powerType, typeof(PowerModel), "power");
        }

        /// <summary>
        ///     Registers a mod orb model for inclusion in <see cref="ModelDb.Orbs" />.
        /// </summary>
        public void RegisterOrb<TOrb>() where TOrb : OrbModel
        {
            RegisterOrb(typeof(TOrb));
        }

        /// <summary>
        ///     Registers <paramref name="orbType" /> for inclusion in <see cref="ModelDb.Orbs" />.
        /// </summary>
        public void RegisterOrb(Type orbType)
        {
            RegisterStandaloneModel(RegisteredOrbs, orbType, typeof(OrbModel), "orb");
        }

        /// <summary>
        ///     Registers a mod enchantment model for RitsuLib tracking, fixed <see cref="ModelDb" /> entry identity, dynamic
        ///     injection, and inclusion in patched <see cref="ModelDb.DebugEnchantments" />.
        /// </summary>
        public void RegisterEnchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            RegisterEnchantment(typeof(TEnchantment));
        }

        /// <summary>
        ///     Registers <paramref name="enchantmentType" /> for patched enchantment injection.
        /// </summary>
        public void RegisterEnchantment(Type enchantmentType)
        {
            RegisterStandaloneModel(RegisteredEnchantments, enchantmentType, typeof(EnchantmentModel),
                "enchantment");
        }

        /// <summary>
        ///     Registers a mod affliction model for RitsuLib tracking, fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.DebugAfflictions" />.
        /// </summary>
        public void RegisterAffliction<TAffliction>() where TAffliction : AfflictionModel
        {
            RegisterAffliction(typeof(TAffliction));
        }

        /// <summary>
        ///     Registers <paramref name="afflictionType" /> for patched affliction injection.
        /// </summary>
        public void RegisterAffliction(Type afflictionType)
        {
            RegisterStandaloneModel(RegisteredAfflictions, afflictionType, typeof(AfflictionModel), "affliction");
        }

        /// <summary>
        ///     Registers a mod achievement model for fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.Achievements" />.
        /// </summary>
        public void RegisterAchievement<TAchievement>() where TAchievement : AchievementModel
        {
            RegisterAchievement(typeof(TAchievement));
        }

        /// <summary>
        ///     Registers <paramref name="achievementType" /> for patched achievement injection.
        /// </summary>
        public void RegisterAchievement(Type achievementType)
        {
            RegisterStandaloneModel(RegisteredAchievements, achievementType, typeof(AchievementModel),
                "achievement");
        }

        /// <summary>
        ///     Registers a mod singleton model for fixed entry identity and dynamic injection (resolved via
        ///     <see cref="ModelDb.Singleton{T}" />).
        /// </summary>
        public void RegisterSingleton<TSingleton>() where TSingleton : SingletonModel
        {
            RegisterSingleton(typeof(TSingleton));
        }

        /// <summary>
        ///     Registers <paramref name="singletonType" /> for dynamic singleton injection.
        /// </summary>
        public void RegisterSingleton(Type singletonType)
        {
            RegisterStandaloneModel(RegisteredSingletons, singletonType, typeof(SingletonModel), "singleton");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;good&quot; daily modifier for patched <see cref="ModelDb.GoodModifiers" />.
        /// </summary>
        public void RegisterGoodModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterGoodModifier(typeof(TModifier));
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a good daily modifier.
        /// </summary>
        public void RegisterGoodModifier(Type modifierType)
        {
            RegisterStandaloneModel(RegisteredGoodModifiers, modifierType, typeof(ModifierModel), "good modifier");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;bad&quot; daily modifier for patched <see cref="ModelDb.BadModifiers" />.
        /// </summary>
        public void RegisterBadModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterBadModifier(typeof(TModifier));
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a bad daily modifier.
        /// </summary>
        public void RegisterBadModifier(Type modifierType)
        {
            RegisterStandaloneModel(RegisteredBadModifiers, modifierType, typeof(ModifierModel), "bad modifier");
        }

        /// <summary>
        ///     Registers a shared card pool model for inclusion in <see cref="ModelDb.AllSharedCardPools" />.
        /// </summary>
        public void RegisterSharedCardPool<TPool>() where TPool : CardPoolModel
        {
            RegisterSharedCardPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in <see cref="ModelDb.AllSharedCardPools" />.
        /// </summary>
        public void RegisterSharedCardPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedCardPools, poolType, typeof(CardPoolModel),
                "shared card pool");
        }

        /// <summary>
        ///     Registers a shared relic pool model for inclusion in patched <see cref="ModelDb.AllRelicPools" />.
        /// </summary>
        public void RegisterSharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            RegisterSharedRelicPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in patched <see cref="ModelDb.AllRelicPools" />.
        /// </summary>
        public void RegisterSharedRelicPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedRelicPools, poolType, typeof(RelicPoolModel),
                "shared relic pool");
        }

        /// <summary>
        ///     Registers a shared potion pool model for inclusion in patched <see cref="ModelDb.AllPotionPools" />.
        /// </summary>
        public void RegisterSharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            RegisterSharedPotionPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in patched <see cref="ModelDb.AllPotionPools" />.
        /// </summary>
        public void RegisterSharedPotionPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedPotionPools, poolType, typeof(PotionPoolModel),
                "shared potion pool");
        }

        /// <summary>
        ///     Registers a shared event model for inclusion in shared event enumerations.
        /// </summary>
        public void RegisterSharedEvent<TEvent>() where TEvent : EventModel
        {
            RegisterSharedEvent(typeof(TEvent));
        }

        /// <summary>
        ///     Registers <paramref name="eventType" /> for inclusion in shared event enumerations.
        /// </summary>
        public void RegisterSharedEvent(Type eventType)
        {
            RegisterStandaloneModel(RegisteredSharedEvents, eventType, typeof(EventModel), "shared event");
        }

        /// <summary>
        ///     Registers an encounter model scoped to <typeparamref name="TAct" />.
        /// </summary>
        public void RegisterActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            RegisterActEncounter(typeof(TAct), typeof(TEncounter));
        }

        /// <summary>
        ///     Registers <paramref name="encounterType" /> scoped to <paramref name="actType" />.
        /// </summary>
        public void RegisterActEncounter(Type actType, Type encounterType)
        {
            RegisterScopedModel(RegisteredActEncounters, actType, encounterType, typeof(ActModel),
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
            RegisterGlobalEncounter(typeof(TEncounter));
        }

        /// <summary>
        ///     Registers <paramref name="encounterType" /> as a global encounter.
        /// </summary>
        public void RegisterGlobalEncounter(Type encounterType)
        {
            RegisterStandaloneModel(RegisteredGlobalEncounters, encounterType, typeof(EncounterModel),
                "global encounter");
        }

        /// <summary>
        ///     Registers an event model scoped to <typeparamref name="TAct" />.
        /// </summary>
        public void RegisterActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            RegisterActEvent(typeof(TAct), typeof(TEvent));
        }

        /// <summary>
        ///     Registers <paramref name="eventType" /> scoped to <paramref name="actType" />.
        /// </summary>
        public void RegisterActEvent(Type actType, Type eventType)
        {
            RegisterScopedModel(RegisteredActEvents, actType, eventType, typeof(ActModel), typeof(EventModel),
                "act event");
        }

        /// <summary>
        ///     Registers a shared ancient event model for inclusion in ancient enumerations.
        /// </summary>
        public void RegisterSharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            RegisterSharedAncient(typeof(TAncient));
        }

        /// <summary>
        ///     Registers <paramref name="ancientType" /> for inclusion in ancient enumerations.
        /// </summary>
        public void RegisterSharedAncient(Type ancientType)
        {
            RegisterStandaloneModel(RegisteredSharedAncients, ancientType, typeof(AncientEventModel),
                "shared ancient");
        }

        /// <summary>
        ///     Registers an ancient event model scoped to <typeparamref name="TAct" />.
        /// </summary>
        public void RegisterActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            RegisterActAncient(typeof(TAct), typeof(TAncient));
        }

        /// <summary>
        ///     Registers <paramref name="ancientType" /> scoped to <paramref name="actType" />.
        /// </summary>
        public void RegisterActAncient(Type actType, Type ancientType)
        {
            RegisterScopedModel(RegisteredActAncients, actType, ancientType, typeof(ActModel),
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

        internal static ModContentRegisteredTypeSnapshot[] GetRegisteredTypeSnapshots()
        {
            lock (SyncRoot)
            {
                return RegisteredTypeOwners
                    .OrderBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(kvp => kvp.Key.FullName, StringComparer.Ordinal)
                    .Select(kvp =>
                    {
                        var modelType = kvp.Key;
                        var modId = kvp.Value;
                        var modelDbId = TryGetModelDbId(modelType);
                        var expectedPublicEntry =
                            TryGetExpectedPublicEntry(modelType, modId, out var hasExplicitOverride);
                        var typeNamePublicEntry = TryGetTypeNamePublicEntry(modelType);
                        return new ModContentRegisteredTypeSnapshot(
                            modId,
                            modelType,
                            modelDbId,
                            expectedPublicEntry,
                            hasExplicitOverride,
                            typeNamePublicEntry);
                    })
                    .ToArray();
            }

            static ModelId? TryGetModelDbId(Type modelType)
            {
                try
                {
                    return ModelDb.GetId(modelType);
                }
                catch
                {
                    return null;
                }
            }

            static string? TryGetExpectedPublicEntry(Type modelType, string modId, out bool hasExplicitOverride)
            {
                if (FixedPublicEntryOverrides.TryGetValue(modelType, out var entry))
                {
                    hasExplicitOverride = true;
                    return entry;
                }

                try
                {
                    hasExplicitOverride = false;
                    return GetFixedPublicEntry(modId, modelType);
                }
                catch
                {
                    hasExplicitOverride = false;
                    return null;
                }
            }

            static string? TryGetTypeNamePublicEntry(Type modelType)
            {
                try
                {
                    var typeStem = NormalizePublicStem(modelType.Name);
                    var categoryStem = NormalizePublicStem(ModelDb.GetCategory(modelType));
                    return $"{categoryStem}_{typeStem}";
                }
                catch
                {
                    return null;
                }
            }
        }

        internal static Type[] GetRegisteredCharacterStarterCards(Type characterType)
        {
            return GetRegisteredCharacterStarterTypes(characterType, CharacterStarterContentKind.Card);
        }

        internal static Type[] GetRegisteredCharacterStarterRelics(Type characterType)
        {
            return GetRegisteredCharacterStarterTypes(characterType, CharacterStarterContentKind.Relic);
        }

        internal static Type[] GetRegisteredCharacterStarterPotions(Type characterType)
        {
            return GetRegisteredCharacterStarterTypes(characterType, CharacterStarterContentKind.Potion);
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

        private void RegisterCharacterStarterModel(Type characterType, Type modelType, Type expectedModelBaseType,
            CharacterStarterContentKind kind, int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Starter content count must be positive.");

            EnsureMutable(
                $"register starter {kind.ToString().ToLowerInvariant()} '{modelType.Name}' for '{characterType.Name}'");
            EnsureModelType(characterType, typeof(CharacterModel), nameof(characterType));
            EnsureModelType(modelType, expectedModelBaseType, nameof(modelType));
            RegistrationConflictDetector.ThrowIfModelIdConflicts(characterType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);

            lock (SyncRoot)
            {
                RegisteredCharacterStarterContent.Add(new(characterType, modelType, kind, count));
            }

            _logger.Info(
                $"[Content] Registered starter {kind.ToString().ToLowerInvariant()}: {modelType.Name} x{count} -> {characterType.Name}");
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

        private static Type[] GetRegisteredCharacterStarterTypes(Type characterType, CharacterStarterContentKind kind)
        {
            ArgumentNullException.ThrowIfNull(characterType);

            lock (SyncRoot)
            {
                return RegisteredCharacterStarterContent
                    .Where(entry => entry.CharacterType == characterType && entry.Kind == kind)
                    .SelectMany(static entry => Enumerable.Repeat(entry.ModelType, entry.Count))
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

        private enum CharacterStarterContentKind
        {
            Card,
            Relic,
            Potion,
        }

        internal readonly record struct ModContentRegisteredTypeSnapshot(
            string ModId,
            Type ModelType,
            ModelId? ModelDbId,
            string? ExpectedPublicEntry,
            bool HasExplicitPublicEntryOverride,
            string? TypeNamePublicEntry);

        private readonly record struct CharacterStarterRegistration(
            Type CharacterType,
            Type ModelType,
            CharacterStarterContentKind Kind,
            int Count);
    }
}
