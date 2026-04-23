using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Ancients.Options;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Declarative manifest entry that registers content with a <see cref="ModContentRegistry" /> when applied.
    /// </summary>
    public interface IContentRegistrationEntry
    {
        /// <summary>
        ///     Performs the registration for this entry against <paramref name="registry" />.
        /// </summary>
        void Register(ModContentRegistry registry);
    }

    /// <summary>
    ///     Registers a mod character model type.
    /// </summary>
    /// <typeparam name="TCharacter">Concrete <see cref="CharacterModel" /> to register.</typeparam>
    public sealed class CharacterRegistrationEntry<TCharacter> : IContentRegistrationEntry
        where TCharacter : CharacterModel
    {
        private readonly List<Action<ModContentRegistry>> _starterRegistrations = [];

        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacter<TCharacter>();

            foreach (var registration in _starterRegistrations)
                registration(registry);
        }

        /// <summary>
        ///     Appends starter-deck copies of <typeparamref name="TCard" /> when this character entry is registered.
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingCard<TCard>(int count = 1)
            where TCard : CardModel
        {
            _starterRegistrations.Add(registry => registry.RegisterCharacterStarterCard<TCharacter, TCard>(count));
            return this;
        }

        /// <summary>
        ///     Appends starting relic copies of <typeparamref name="TRelic" /> when this character entry is registered.
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingRelic<TRelic>(int count = 1)
            where TRelic : RelicModel
        {
            _starterRegistrations.Add(registry => registry.RegisterCharacterStarterRelic<TCharacter, TRelic>(count));
            return this;
        }

        /// <summary>
        ///     Appends starting potion copies of <typeparamref name="TPotion" /> when this character entry is registered.
        /// </summary>
        public CharacterRegistrationEntry<TCharacter> AddStartingPotion<TPotion>(int count = 1)
            where TPotion : PotionModel
        {
            _starterRegistrations.Add(registry => registry.RegisterCharacterStarterPotion<TCharacter, TPotion>(count));
            return this;
        }
    }

    /// <summary>
    ///     Registers additional starter-deck copies of a card for an already-known character type.
    /// </summary>
    public sealed class CharacterStarterCardRegistrationEntry<TCharacter, TCard>(int count = 1)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterCard<TCharacter, TCard>(count);
        }
    }

    /// <summary>
    ///     Registers additional starting relic copies for an already-known character type.
    /// </summary>
    public sealed class CharacterStarterRelicRegistrationEntry<TCharacter, TRelic>(int count = 1)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterRelic<TCharacter, TRelic>(count);
        }
    }

    /// <summary>
    ///     Registers additional starting potion copies for an already-known character type.
    /// </summary>
    public sealed class CharacterStarterPotionRegistrationEntry<TCharacter, TPotion>(int count = 1)
        : IContentRegistrationEntry
        where TCharacter : CharacterModel
        where TPotion : PotionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCharacterStarterPotion<TCharacter, TPotion>(count);
        }
    }

    /// <summary>
    ///     Registers a mod act model type.
    /// </summary>
    /// <typeparam name="TAct">Concrete <see cref="ActModel" /> to register.</typeparam>
    public sealed class ActRegistrationEntry<TAct> : IContentRegistrationEntry
        where TAct : ActModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAct<TAct>();
        }
    }

    /// <summary>
    ///     Registers a card type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Card pool model type.</typeparam>
    /// <typeparam name="TCard">Card model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class CardRegistrationEntry<TPool, TCard>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCard<TPool, TCard>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers <see cref="ModCardHandGlowRegistry" /> rules for a card type (gold/red hand highlights).
    /// </summary>
    /// <typeparam name="TCard">Concrete <see cref="CardModel" />.</typeparam>
    /// <param name="rules">Predicate rules; merged with <see cref="ModCardHandGlowRules.Or" /> if registered twice.</param>
    public sealed class CardHandGlowRegistrationEntry<TCard>(ModCardHandGlowRules rules) : IContentRegistrationEntry
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCardHandGlow<TCard>(rules);
        }
    }

    /// <summary>
    ///     Registers <see cref="ModCardHandOutlineRegistry" /> tint rules for a card type (arbitrary hand-highlight colors).
    /// </summary>
    public sealed class CardHandOutlineRegistrationEntry<TCard>(ModCardHandOutlineRule rule) : IContentRegistrationEntry
        where TCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterCardHandOutline<TCard>(rule);
        }
    }

    /// <summary>
    ///     Registers a relic type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Relic pool model type.</typeparam>
    /// <typeparam name="TRelic">Relic model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class RelicRegistrationEntry<TPool, TRelic>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : RelicPoolModel
        where TRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterRelic<TPool, TRelic>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a potion type with its pool and optional public entry options.
    /// </summary>
    /// <typeparam name="TPool">Potion pool model type.</typeparam>
    /// <typeparam name="TPotion">Potion model type.</typeparam>
    /// <param name="publicEntry">Optional stable entry / visibility options.</param>
    public sealed class PotionRegistrationEntry<TPool, TPotion>(ModelPublicEntryOptions publicEntry = default)
        : IContentRegistrationEntry
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPotion<TPool, TPotion>(publicEntry);
        }
    }

    /// <summary>
    ///     Registers a standalone power model type.
    /// </summary>
    /// <typeparam name="TPower">Concrete <see cref="PowerModel" />.</typeparam>
    public sealed class PowerRegistrationEntry<TPower> : IContentRegistrationEntry
        where TPower : PowerModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPower<TPower>();
        }
    }

    /// <summary>
    ///     Registers a non-power health bar forecast source type.
    /// </summary>
    /// <typeparam name="TSource">Concrete forecast source type.</typeparam>
    /// <param name="sourceId">Optional stable id; defaults to the source type name.</param>
    public sealed class HealthBarForecastRegistrationEntry<TSource>(string? sourceId = null)
        : IContentRegistrationEntry
        where TSource : IHealthBarForecastSource, new()
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterHealthBarForecast<TSource>(registry.ModId, sourceId);
        }
    }

    /// <summary>
    ///     Registers a shared card pool type (not tied to a single character registration here).
    /// </summary>
    /// <typeparam name="TPool">Concrete <see cref="CardPoolModel" />.</typeparam>
    public sealed class SharedCardPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedCardPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a mod orb model type.
    /// </summary>
    /// <typeparam name="TOrb">Concrete <see cref="OrbModel" />.</typeparam>
    public sealed class OrbRegistrationEntry<TOrb> : IContentRegistrationEntry
        where TOrb : OrbModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterOrb<TOrb>();
        }
    }

    /// <summary>
    ///     Registers a mod enchantment model type.
    /// </summary>
    /// <typeparam name="TEnchantment">Concrete <see cref="EnchantmentModel" />.</typeparam>
    public sealed class EnchantmentRegistrationEntry<TEnchantment> : IContentRegistrationEntry
        where TEnchantment : EnchantmentModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterEnchantment<TEnchantment>();
        }
    }

    /// <summary>
    ///     Registers a mod affliction model type.
    /// </summary>
    /// <typeparam name="TAffliction">Concrete <see cref="AfflictionModel" />.</typeparam>
    public sealed class AfflictionRegistrationEntry<TAffliction> : IContentRegistrationEntry
        where TAffliction : AfflictionModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAffliction<TAffliction>();
        }
    }

    /// <summary>
    ///     Registers a mod achievement model type.
    /// </summary>
    /// <typeparam name="TAchievement">Concrete <see cref="AchievementModel" />.</typeparam>
    public sealed class AchievementRegistrationEntry<TAchievement> : IContentRegistrationEntry
        where TAchievement : AchievementModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAchievement<TAchievement>();
        }
    }

    /// <summary>
    ///     Registers a mod singleton model type.
    /// </summary>
    /// <typeparam name="TSingleton">Concrete <see cref="SingletonModel" />.</typeparam>
    public sealed class SingletonRegistrationEntry<TSingleton> : IContentRegistrationEntry
        where TSingleton : SingletonModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSingleton<TSingleton>();
        }
    }

    /// <summary>
    ///     Registers a mod modifier as a good daily modifier.
    /// </summary>
    /// <typeparam name="TModifier">Concrete <see cref="ModifierModel" />.</typeparam>
    public sealed class GoodModifierRegistrationEntry<TModifier> : IContentRegistrationEntry
        where TModifier : ModifierModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterGoodModifier<TModifier>();
        }
    }

    /// <summary>
    ///     Registers a mod modifier as a bad daily modifier.
    /// </summary>
    /// <typeparam name="TModifier">Concrete <see cref="ModifierModel" />.</typeparam>
    public sealed class BadModifierRegistrationEntry<TModifier> : IContentRegistrationEntry
        where TModifier : ModifierModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterBadModifier<TModifier>();
        }
    }

    /// <summary>
    ///     Registers a shared relic pool model type.
    /// </summary>
    /// <typeparam name="TPool">Concrete <see cref="RelicPoolModel" />.</typeparam>
    public sealed class SharedRelicPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedRelicPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a shared potion pool model type.
    /// </summary>
    /// <typeparam name="TPool">Concrete <see cref="PotionPoolModel" />.</typeparam>
    public sealed class SharedPotionPoolRegistrationEntry<TPool> : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedPotionPool<TPool>();
        }
    }

    /// <summary>
    ///     Registers a mod monster model type.
    /// </summary>
    /// <typeparam name="TMonster">Concrete <see cref="MonsterModel" />.</typeparam>
    public sealed class MonsterRegistrationEntry<TMonster> : IContentRegistrationEntry
        where TMonster : MonsterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterMonster<TMonster>();
        }
    }

    /// <summary>
    ///     Registers a shared event model type.
    /// </summary>
    /// <typeparam name="TEvent">Concrete <see cref="EventModel" />.</typeparam>
    public sealed class SharedEventRegistrationEntry<TEvent> : IContentRegistrationEntry
        where TEvent : EventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedEvent<TEvent>();
        }
    }

    /// <summary>
    ///     Registers an encounter model scoped to <typeparamref name="TAct" />.
    /// </summary>
    public sealed class ActEncounterRegistrationEntry<TAct, TEncounter> : IContentRegistrationEntry
        where TAct : ActModel
        where TEncounter : EncounterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActEncounter<TAct, TEncounter>();
        }
    }

    /// <summary>
    ///     Registers an encounter model merged into every act’s encounter list (see
    ///     <c>ModContentRegistry.RegisterGlobalEncounter&lt;TEncounter&gt;()</c>).
    /// </summary>
    /// <typeparam name="TEncounter">Concrete <see cref="EncounterModel" />.</typeparam>
    public sealed class GlobalEncounterRegistrationEntry<TEncounter> : IContentRegistrationEntry
        where TEncounter : EncounterModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterGlobalEncounter<TEncounter>();
        }
    }

    /// <summary>
    ///     Registers an event model scoped to <typeparamref name="TAct" />.
    /// </summary>
    public sealed class ActEventRegistrationEntry<TAct, TEvent> : IContentRegistrationEntry
        where TAct : ActModel
        where TEvent : EventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActEvent<TAct, TEvent>();
        }
    }

    /// <summary>
    ///     Registers a shared ancient event model type.
    /// </summary>
    /// <typeparam name="TAncient">Concrete <see cref="AncientEventModel" />.</typeparam>
    public sealed class SharedAncientRegistrationEntry<TAncient> : IContentRegistrationEntry
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterSharedAncient<TAncient>();
        }
    }

    /// <summary>
    ///     Registers an ancient event model scoped to <typeparamref name="TAct" />.
    /// </summary>
    public sealed class ActAncientRegistrationEntry<TAct, TAncient> : IContentRegistrationEntry
        where TAct : ActModel
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterActAncient<TAct, TAncient>();
        }
    }

    /// <summary>
    ///     Registers extra initial-option injection rules for a specific ancient model type.
    /// </summary>
    public sealed class AncientOptionRegistrationEntry<TAncient>(ModAncientOptionRule rule) : IContentRegistrationEntry
        where TAncient : AncientEventModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterAncientOption<TAncient>(rule);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder card from a stable entry stem.
    /// </summary>
    public sealed class PlaceholderCardRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderCardDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderCard<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder card with explicit public entry options.
    /// </summary>
    public sealed class PlaceholderCardFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderCardDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : CardPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderCard<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder relic from a stable entry stem.
    /// </summary>
    public sealed class PlaceholderRelicRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderRelicDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderRelic<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder relic with explicit public entry options.
    /// </summary>
    public sealed class PlaceholderRelicFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderRelicDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : RelicPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderRelic<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder potion from a stable entry stem.
    /// </summary>
    public sealed class PlaceholderPotionRegistrationEntry<TPool>(
        string stableEntryStem,
        PlaceholderPotionDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderPotion<TPool>(stableEntryStem, descriptor);
        }
    }

    /// <summary>
    ///     Registers a generated placeholder potion with explicit public entry options.
    /// </summary>
    public sealed class PlaceholderPotionFromOptionsRegistrationEntry<TPool>(
        ModelPublicEntryOptions publicEntry,
        PlaceholderPotionDescriptor descriptor = default) : IContentRegistrationEntry
        where TPool : PotionPoolModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            registry.RegisterPlaceholderPotion<TPool>(publicEntry, descriptor);
        }
    }

    /// <summary>
    ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping (starter deck card → ancient transform target).
    /// </summary>
    /// <typeparam name="TStarterCard">Deck card id to match.</typeparam>
    /// <typeparam name="TAncientCard">Transform target prototype from <see cref="ModelDb.Card{T}" />.</typeparam>
    public sealed class
        ArchaicToothTranscendenceRegistrationEntry<TStarterCard, TAncientCard> : IContentRegistrationEntry
        where TStarterCard : CardModel
        where TAncientCard : CardModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(registry.ModId);
        }
    }

    /// <summary>
    ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping with explicit starter id and ancient card type.
    /// </summary>
    /// <param name="StarterCardId">Deck card model id to match.</param>
    /// <param name="AncientCardType">Concrete ancient card type (resolved via <see cref="ModelDb" /> at runtime).</param>
    public sealed record ArchaicToothTranscendenceByIdRegistrationEntry(
        ModelId StarterCardId,
        Type AncientCardType) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(
                StarterCardId,
                AncientCardType,
                registry.ModId);
        }
    }

    /// <summary>
    ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping (starter relic → upgraded relic).
    /// </summary>
    /// <typeparam name="TStarterRelic">Starter relic id to match.</typeparam>
    /// <typeparam name="TUpgradedRelic">Replacement relic prototype from <see cref="ModelDb.Relic{T}" />.</typeparam>
    public sealed class
        TouchOfOrobasRefinementRegistrationEntry<TStarterRelic, TUpgradedRelic> : IContentRegistrationEntry
        where TStarterRelic : RelicModel
        where TUpgradedRelic : RelicModel
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(registry.ModId);
        }
    }

    /// <summary>
    ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping with explicit starter id and upgraded relic type.
    /// </summary>
    /// <param name="StarterRelicId">Starter relic id to match.</param>
    /// <param name="UpgradedRelicType">Concrete upgraded relic type (resolved via <see cref="ModelDb" /> at runtime).</param>
    public sealed record TouchOfOrobasRefinementByIdRegistrationEntry(
        ModelId StarterRelicId,
        Type UpgradedRelicType) : IContentRegistrationEntry
    {
        /// <inheritdoc />
        public void Register(ModContentRegistry registry)
        {
            RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(
                StarterRelicId,
                UpgradedRelicType,
                registry.ModId);
        }
    }
}
