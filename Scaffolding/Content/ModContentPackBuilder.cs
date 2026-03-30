using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Unlocks;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Immutable snapshot of registries and ids used while applying a content pack.
    /// </summary>
    /// <param name="ModId">Owning mod identifier string.</param>
    /// <param name="Content">Content registry for models and pools.</param>
    /// <param name="Keywords">Keyword registration surface.</param>
    /// <param name="Timeline">Epoch/story timeline registry.</param>
    /// <param name="Unlocks">Unlock rule registry.</param>
    public readonly record struct ModContentPackContext(
        string ModId,
        ModContentRegistry Content,
        ModKeywordRegistry Keywords,
        ModTimelineRegistry Timeline,
        ModUnlockRegistry Unlocks);

    /// <summary>
    ///     Fluent registration helper that batches common mod-author setup into a single readable flow.
    /// </summary>
    public sealed class ModContentPackBuilder
    {
        private readonly string _modId;
        private readonly List<Action<ModContentPackContext>> _steps = [];

        private ModContentPackBuilder(string modId)
        {
            _modId = modId;
        }

        /// <summary>
        ///     Starts a builder for the given <paramref name="modId" />.
        /// </summary>
        public static ModContentPackBuilder For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId);
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacter{TCharacter}" />.
        /// </summary>
        public ModContentPackBuilder Character<TCharacter>() where TCharacter : CharacterModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacter<TCharacter>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAct{TAct}" />.
        /// </summary>
        public ModContentPackBuilder Act<TAct>() where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterAct<TAct>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" />.
        /// </summary>
        public ModContentPackBuilder ActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEncounter<TAct, TEncounter>());
        }

        /// <summary>
        ///     Queues <c>RegisterCard&lt;TPool, TCard&gt;()</c> on the content registry with default public entry options.
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>());
        }

        /// <summary>
        ///     Queues <c>RegisterCard&lt;TPool, TCard&gt;(ModelPublicEntryOptions)</c> on the content registry.
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>(publicEntry));
        }

        /// <summary>
        ///     Registers a generated placeholder card (no custom CLR type). Prefer this for quick WIP flow.
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool>(string stableEntryStem,
            PlaceholderCardDescriptor descriptor = default)
            where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderCard<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a card with a stable public entry stem when you already have a concrete card type.
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool, TCard>(string stableEntryStem)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return Card<TPool, TCard>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <c>RegisterRelic&lt;TPool, TRelic&gt;()</c> with default public entry options.
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>());
        }

        /// <summary>
        ///     Queues <c>RegisterRelic&lt;TPool, TRelic&gt;(ModelPublicEntryOptions)</c>.
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>(publicEntry));
        }

        /// <summary>
        ///     Queues placeholder relic emission via <c>RegisterPlaceholderRelic&lt;TPool&gt;(...)</c>.
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool>(string stableEntryStem,
            PlaceholderRelicDescriptor descriptor = default)
            where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderRelic<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a relic type using a stable entry stem mapped through <see cref="ModelPublicEntryOptions.FromStem" />.
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool, TRelic>(string stableEntryStem)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return Relic<TPool, TRelic>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <c>RegisterPotion&lt;TPool, TPotion&gt;()</c> with default public entry options.
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>());
        }

        /// <summary>
        ///     Queues <c>RegisterPotion&lt;TPool, TPotion&gt;(ModelPublicEntryOptions)</c>.
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>(publicEntry));
        }

        /// <summary>
        ///     Queues placeholder potion emission via <c>RegisterPlaceholderPotion&lt;TPool&gt;(...)</c>.
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool>(string stableEntryStem,
            PlaceholderPotionDescriptor descriptor = default)
            where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderPotion<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a potion type using a stable entry stem mapped through <see cref="ModelPublicEntryOptions.FromStem" />.
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool, TPotion>(string stableEntryStem)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return Potion<TPool, TPotion>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterPower{TPower}" />.
        /// </summary>
        public ModContentPackBuilder Power<TPower>() where TPower : PowerModel
        {
            return AddStep(ctx => ctx.Content.RegisterPower<TPower>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterOrb{TOrb}" />.
        /// </summary>
        public ModContentPackBuilder Orb<TOrb>() where TOrb : OrbModel
        {
            return AddStep(ctx => ctx.Content.RegisterOrb<TOrb>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterEnchantment{TEnchantment}" />.
        /// </summary>
        public ModContentPackBuilder Enchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            return AddStep(ctx => ctx.Content.RegisterEnchantment<TEnchantment>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAffliction{TAffliction}" />.
        /// </summary>
        public ModContentPackBuilder Affliction<TAffliction>() where TAffliction : AfflictionModel
        {
            return AddStep(ctx => ctx.Content.RegisterAffliction<TAffliction>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAchievement{TAchievement}" />.
        /// </summary>
        public ModContentPackBuilder Achievement<TAchievement>() where TAchievement : AchievementModel
        {
            return AddStep(ctx => ctx.Content.RegisterAchievement<TAchievement>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSingleton{TSingleton}" />.
        /// </summary>
        public ModContentPackBuilder Singleton<TSingleton>() where TSingleton : SingletonModel
        {
            return AddStep(ctx => ctx.Content.RegisterSingleton<TSingleton>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterGoodModifier{TModifier}" />.
        /// </summary>
        public ModContentPackBuilder GoodModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterGoodModifier<TModifier>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterBadModifier{TModifier}" />.
        /// </summary>
        public ModContentPackBuilder BadModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterBadModifier<TModifier>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedCardPool{TPool}" />.
        /// </summary>
        public ModContentPackBuilder SharedCardPool<TPool>() where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedCardPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedRelicPool{TPool}" />.
        /// </summary>
        public ModContentPackBuilder SharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedRelicPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedPotionPool{TPool}" />.
        /// </summary>
        public ModContentPackBuilder SharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedPotionPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedEvent{TEvent}" />.
        /// </summary>
        public ModContentPackBuilder SharedEvent<TEvent>() where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedEvent<TEvent>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEvent{TAct,TEvent}" />.
        /// </summary>
        public ModContentPackBuilder ActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEvent<TAct, TEvent>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedAncient{TAncient}" />.
        /// </summary>
        public ModContentPackBuilder SharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedAncient<TAncient>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActAncient{TAct,TAncient}" />.
        /// </summary>
        public ModContentPackBuilder ActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActAncient<TAct, TAncient>());
        }

        /// <summary>
        ///     Queues <see cref="ModKeywordRegistry.RegisterCardKeyword" />.
        /// </summary>
        public ModContentPackBuilder CardKeyword(string id, string? locKeyPrefix = null, string? iconPath = null)
        {
            return AddStep(ctx => ctx.Keywords.RegisterCardKeyword(id, locKeyPrefix, iconPath));
        }

        /// <summary>
        ///     Queues a general keyword registration on <see cref="ModKeywordRegistry" />.
        /// </summary>
        public ModContentPackBuilder Keyword(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return AddStep(ctx =>
                ctx.Keywords.Register(id, titleTable, titleKey, descriptionTable, descriptionKey, iconPath));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterEpoch{TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder Epoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterEpoch<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterStory{TStory}" />.
        /// </summary>
        public ModContentPackBuilder Story<TStory>() where TStory : StoryModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStory<TStory>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.RequireEpoch{TModel,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RequireEpoch<TModel, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterWinAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(ascensionLevel));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(requiredEliteWins));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(requiredBossWins));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RevealAscensionAfterEpoch<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter,TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockCharacterAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Appends a manifest <see cref="IContentRegistrationEntry" /> step.
        /// </summary>
        public ModContentPackBuilder Entry(IContentRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Content));
        }

        /// <summary>
        ///     Appends each content registration entry in order.
        /// </summary>
        public ModContentPackBuilder Entries(IEnumerable<IContentRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                Entry(entry);

            return this;
        }

        /// <summary>
        ///     Appends a typed <see cref="KeywordRegistrationEntry" /> registration step.
        /// </summary>
        public ModContentPackBuilder Keyword(KeywordRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Keywords));
        }

        /// <summary>
        ///     Appends each keyword registration entry in order.
        /// </summary>
        public ModContentPackBuilder Keywords(IEnumerable<KeywordRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                Keyword(entry);

            return this;
        }

        /// <summary>
        ///     Convenience batch for optional content and keyword manifest enumerables.
        /// </summary>
        /// <remarks>
        ///     <see cref="IContentRegistrationEntry" /> may include
        ///     <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />,
        ///     <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />, and related Orobas
        ///     entries alongside cards/relics/etc.
        /// </remarks>
        public ModContentPackBuilder Manifest(
            IEnumerable<IContentRegistrationEntry>? contentEntries = null,
            IEnumerable<KeywordRegistrationEntry>? keywordEntries = null)
        {
            if (contentEntries != null)
                Entries(contentEntries);

            if (keywordEntries != null)
                Keywords(keywordEntries);

            return this;
        }

        /// <summary>
        ///     Queues <see cref="RitsuLibFramework.RegisterArchaicToothTranscendenceMapping{TStarterCard,TAncientCard}" />
        ///     using this pack’s <see cref="ModContentPackContext.ModId" />.
        /// </summary>
        public ModContentPackBuilder ArchaicToothTranscendence<TStarterCard, TAncientCard>()
            where TStarterCard : CardModel
            where TAncientCard : CardModel
        {
            return AddStep(ctx =>
                RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(ctx.ModId));
        }

        /// <summary>
        ///     Queues ArchaicTooth transcendence registration by starter card id and ancient card type, using this pack’s
        ///     mod id.
        /// </summary>
        public ModContentPackBuilder ArchaicToothTranscendence(ModelId starterCardId, Type ancientCardType)
        {
            ArgumentNullException.ThrowIfNull(ancientCardType);
            return AddStep(ctx =>
                RitsuLibFramework.RegisterArchaicToothTranscendenceMapping(
                    starterCardId,
                    ancientCardType,
                    ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping{TStarterRelic,TUpgradedRelic}" />
        ///     using this pack’s mod id.
        /// </summary>
        public ModContentPackBuilder TouchOfOrobasRefinement<TStarterRelic, TUpgradedRelic>()
            where TStarterRelic : RelicModel
            where TUpgradedRelic : RelicModel
        {
            return AddStep(ctx =>
                RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(ctx.ModId));
        }

        /// <summary>
        ///     Queues TouchOfOrobas refinement registration by starter relic id and upgraded relic type, using this pack’s
        ///     mod id.
        /// </summary>
        public ModContentPackBuilder TouchOfOrobasRefinement(ModelId starterRelicId, Type upgradedRelicType)
        {
            ArgumentNullException.ThrowIfNull(upgradedRelicType);
            return AddStep(ctx =>
                RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping(
                    starterRelicId,
                    upgradedRelicType,
                    ctx.ModId));
        }

        /// <summary>
        ///     Appends an arbitrary delegate executed during <see cref="Apply" />.
        /// </summary>
        public ModContentPackBuilder Custom(Action<ModContentPackContext> step)
        {
            return AddStep(step);
        }

        /// <summary>
        ///     Materializes registries for the builder’s mod id without running queued steps.
        /// </summary>
        public ModContentPackContext BuildContext()
        {
            return new(
                _modId,
                RitsuLibFramework.GetContentRegistry(_modId),
                RitsuLibFramework.GetKeywordRegistry(_modId),
                RitsuLibFramework.GetTimelineRegistry(_modId),
                RitsuLibFramework.GetUnlockRegistry(_modId));
        }

        /// <summary>
        ///     Builds context, runs all queued steps, logs a summary, and returns the context.
        /// </summary>
        public ModContentPackContext Apply()
        {
            var context = BuildContext();
            foreach (var step in _steps)
                step(context);

            RitsuLibFramework.CreateLogger(_modId)
                .Info($"[ContentPack] Applied {_steps.Count} registration step(s).");
            return context;
        }

        private ModContentPackBuilder AddStep(Action<ModContentPackContext> step)
        {
            ArgumentNullException.ThrowIfNull(step);
            _steps.Add(step);
            return this;
        }
    }
}
