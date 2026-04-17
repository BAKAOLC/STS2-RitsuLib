using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Base metadata for declarative registrations discovered by the ritsulib auto-registration pipeline.
    /// </summary>
    public abstract class AutoRegistrationAttribute : Attribute
    {
        /// <summary>
        ///     Local ordering within the same registration phase. Lower values run first.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    ///     Base metadata for content registrations dispatched through <c>ModContentRegistry</c>.
    /// </summary>
    public abstract class ContentRegistrationAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a character model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an act model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a monster model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterMonsterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a power model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPowerAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an orb model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOrbAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an enchantment model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEnchantmentAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an affliction model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAfflictionAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an achievement model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAchievementAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a singleton model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSingletonAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a good daily modifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGoodModifierAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a bad daily modifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterBadModifierAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared card pool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedCardPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared relic pool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedRelicPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared potion pool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedPotionPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedEventAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared ancient event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedAncientAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a global encounter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGlobalEncounterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Base metadata for pool-backed model registrations that can override fixed public entry generation.
    /// </summary>
    /// <param name="poolType">Target pool model type.</param>
    public abstract class ModelPublicEntryRegistrationAttributeBase(Type poolType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target pool model type.
        /// </summary>
        public Type PoolType { get; } = poolType;

        /// <summary>
        ///     Optional stable author-chosen type-name stem.
        /// </summary>
        public string? StableEntryStem { get; set; }

        /// <summary>
        ///     Optional full fixed public entry override.
        /// </summary>
        public string? FullPublicEntry { get; set; }
    }

    /// <summary>
    ///     Registers the annotated type as a card in the given pool.
    /// </summary>
    /// <param name="poolType">Target card pool type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCardAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Registers the annotated type as a relic in the given pool.
    /// </summary>
    /// <param name="poolType">Target relic pool type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterRelicAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Registers the annotated type as a potion in the given pool.
    /// </summary>
    /// <param name="poolType">Target potion pool type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPotionAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Base metadata for character starter-content registrations.
    /// </summary>
    /// <param name="characterType">Target character model type.</param>
    /// <param name="count">How many copies to register.</param>
    public abstract class CharacterStarterRegistrationAttributeBase(Type characterType, int count = 1)
        : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target character model type.
        /// </summary>
        public Type CharacterType { get; } = characterType;

        /// <summary>
        ///     How many copies to register.
        /// </summary>
        public int Count { get; } = count;
    }

    /// <summary>
    ///     Registers the annotated card type as starter content for a character.
    /// </summary>
    /// <param name="characterType">Target character model type.</param>
    /// <param name="count">How many copies to register.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterCardAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Registers the annotated relic type as starter content for a character.
    /// </summary>
    /// <param name="characterType">Target character model type.</param>
    /// <param name="count">How many copies to register.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterRelicAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Registers the annotated potion type as starter content for a character.
    /// </summary>
    /// <param name="characterType">Target character model type.</param>
    /// <param name="count">How many copies to register.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterPotionAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Base metadata for act-scoped registrations.
    /// </summary>
    /// <param name="actType">Target act model type.</param>
    public abstract class ActScopedRegistrationAttributeBase(Type actType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target act model type.
        /// </summary>
        public Type ActType { get; } = actType;
    }

    /// <summary>
    ///     Registers the annotated encounter type for the given act.
    /// </summary>
    /// <param name="actType">Target act model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEncounterAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Registers the annotated event type for the given act.
    /// </summary>
    /// <param name="actType">Target act model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEventAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Registers the annotated ancient type for the given act.
    /// </summary>
    /// <param name="actType">Target act model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAncientAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Base metadata for owned keyword registrations.
    /// </summary>
    /// <param name="localKeywordStem">Local mod-scoped keyword stem.</param>
    public abstract class KeywordRegistrationAttributeBase(string localKeywordStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local mod-scoped keyword stem.
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     Localization table containing the title key.
        /// </summary>
        public string TitleTable { get; set; } = "card_keywords";

        /// <summary>
        ///     Optional explicit localization key for the title.
        /// </summary>
        public string? TitleKey { get; set; }

        /// <summary>
        ///     Optional localization table containing the description key.
        /// </summary>
        public string? DescriptionTable { get; set; }

        /// <summary>
        ///     Optional explicit localization key for the description.
        /// </summary>
        public string? DescriptionKey { get; set; }

        /// <summary>
        ///     Optional icon path used by hover-tip rendering.
        /// </summary>
        public string? IconPath { get; set; }
    }

    /// <summary>
    ///     Registers an owned keyword definition.
    /// </summary>
    /// <param name="localKeywordStem">Local mod-scoped keyword stem.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedKeywordAttribute(string localKeywordStem)
        : KeywordRegistrationAttributeBase(localKeywordStem)
    {
        /// <summary>
        ///     Optional placement for inline card-description injection.
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether the keyword should appear in card hover tips.
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     Registers an owned card keyword definition using the card-keyword localization convention.
    /// </summary>
    /// <param name="localKeywordStem">Local mod-scoped keyword stem.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardKeywordAttribute(string localKeywordStem)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local mod-scoped keyword stem.
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     Optional explicit localization key prefix.
        /// </summary>
        public string? LocKeyPrefix { get; set; }

        /// <summary>
        ///     Optional icon path used by hover-tip rendering.
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Optional placement for inline card-description injection.
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether the keyword should appear in card hover tips.
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     Registers the annotated type as a timeline epoch.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a timeline story.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Binds the annotated epoch type into the given story column.
    /// </summary>
    /// <param name="storyType">Target story model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryEpochAttribute(Type storyType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target story model type.
        /// </summary>
        public Type StoryType { get; } = storyType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the first free slot in the given era column.
    /// </summary>
    /// <param name="era">Target era column.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAttribute(EpochEra era) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target era column.
        /// </summary>
        public EpochEra Era { get; } = era;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly before the given anchor era.
    /// </summary>
    /// <param name="anchorEra">Anchor era whose left side should receive the epoch.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose left side should receive the epoch.
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly before the column of the reference epoch.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column anchors the placement.
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly after the given anchor era.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose right side should receive the epoch.
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly after the column of the reference epoch.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column anchors the placement.
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the same era column as the given anchor era.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose column should be shared.
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the same era column as the reference epoch.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column should be shared.
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Registers an ArchaicTooth transcendence mapping from the annotated starter card type to the given ancient card.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterArchaicToothTranscendenceAttribute(Type ancientCardType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Ancient card type produced by transcendence.
        /// </summary>
        public Type AncientCardType { get; } = ancientCardType;
    }

    /// <summary>
    ///     Registers a TouchOfOrobas refinement mapping from the annotated starter relic type to the given upgraded relic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTouchOfOrobasRefinementAttribute(Type upgradedRelicType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Upgraded relic type produced by refinement.
        /// </summary>
        public Type UpgradedRelicType { get; } = upgradedRelicType;
    }

    /// <summary>
    ///     Registers explicit card unlock content for the annotated epoch and gates those cards behind it.
    /// </summary>
    /// <param name="cardTypes">Card model types revealed by the epoch.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochCardsAttribute(params Type[] cardTypes) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Card model types revealed by the epoch.
        /// </summary>
        public IReadOnlyList<Type> CardTypes { get; } = cardTypes;
    }

    /// <summary>
    ///     Gates every registered card in the given pool behind the annotated epoch.
    /// </summary>
    /// <param name="poolType">Card pool model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireAllCardsInPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Card pool model type.
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     Registers every relic in the given pool as unlock content for the annotated epoch and gates them behind it.
    /// </summary>
    /// <param name="poolType">Relic pool model type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochRelicsFromPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Relic pool model type.
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     Gates the annotated content type behind the given epoch.
    /// </summary>
    /// <param name="epochType">Required epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireEpochAttribute(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Required epoch type.
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     Base metadata for character-to-epoch unlock registrations.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    public abstract class CharacterEpochRegistrationAttributeBase(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target epoch type.
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     Unlocks an epoch after completing any run as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Unlocks an epoch after winning a run as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterWinAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Unlocks an epoch after winning at or above a given ascension as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    /// <param name="ascensionLevel">Minimum ascension level.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionWinAttribute(Type epochType, int ascensionLevel)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Minimum ascension level required for the unlock.
        /// </summary>
        public int AscensionLevel { get; } = ascensionLevel;
    }

    /// <summary>
    ///     Unlocks an epoch after defeating a number of elites as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    /// <param name="requiredEliteWins">Required elite victories.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterEliteVictoriesAttribute(Type epochType, int requiredEliteWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Required elite victories.
        /// </summary>
        public int RequiredEliteWins { get; } = requiredEliteWins;
    }

    /// <summary>
    ///     Unlocks an epoch after defeating a number of bosses as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    /// <param name="requiredBossWins">Required boss victories.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterBossVictoriesAttribute(Type epochType, int requiredBossWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Required boss victories.
        /// </summary>
        public int RequiredBossWins { get; } = requiredBossWins;
    }

    /// <summary>
    ///     Unlocks an epoch after an ascension-one win as the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionOneWinAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Reveals ascension UI for the annotated character after the given epoch is revealed.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RevealAscensionAfterEpochAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Grants the given epoch through the post-run character unlock flow for the annotated character.
    /// </summary>
    /// <param name="epochType">Target epoch type.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockCharacterAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);
}
