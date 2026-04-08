using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;
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
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterForce{TAct}" />.
        /// </summary>
        public ModContentPackBuilder ActEnterForce<TAct>(int slotIndex, int priority,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterForce<TAct>(slotIndex, priority, eligibility));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterUniformPool" />.
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPool(slotIndex));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}" />.
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPoolCandidate<TAct>(slotIndex, eligibility));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPool" />.
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPool(slotIndex));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}" />.
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility, Func<ActEnterResolveContext, double> weight)
            where TAct : ActModel
        {
            return AddStep(ctx =>
                ctx.Content.RegisterActEnterWeightedPoolCandidate<TAct>(slotIndex, eligibility, weight));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" />.
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPoolBaseline(int slotIndex,
            Func<ActEnterResolveContext, double> weight)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPoolBaseline(slotIndex, weight));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" /> (encounter only in that act).
        /// </summary>
        public ModContentPackBuilder ActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEncounter<TAct, TEncounter>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterGlobalEncounter{TEncounter}" /> (encounter merged into every act’s
        ///     encounter pool).
        /// </summary>
        public ModContentPackBuilder GlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterGlobalEncounter<TEncounter>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterMonster{TMonster}" /> (standalone monster type + patched
        ///     <c>ModelDb.Monsters</c> merge).
        /// </summary>
        public ModContentPackBuilder Monster<TMonster>() where TMonster : MonsterModel
        {
            return AddStep(ctx => ctx.Content.RegisterMonster<TMonster>());
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
        ///     Queues <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> for hand gold/red highlight rules.
        /// </summary>
        public ModContentPackBuilder CardHandGlow<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandGlow<TCard>(rules));
        }

        /// <summary>
        ///     Queues <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;(...)</c> for custom hand-highlight colors.
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandOutline<TCard>(rule));
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
        ///     Queues <see cref="RitsuLibFramework.RegisterHealthBarForecast{TSource}" /> for a non-power forecast source.
        /// </summary>
        public ModContentPackBuilder HealthBarForecast<TSource>(string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            return AddStep(ctx => RitsuLibFramework.RegisterHealthBarForecast<TSource>(ctx.ModId, sourceId));
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
        ///     Queues <c>ModKeywordRegistry.RegisterCardKeywordOwned</c> (mod-local stem → qualified id).
        /// </summary>
        public ModContentPackBuilder CardKeywordOwned(
            string localKeywordStem,
            string? locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
                ctx.Keywords.RegisterCardKeywordOwned(localKeywordStem, locKeyPrefix, iconPath,
                    cardDescriptionPlacement, includeInCardHoverTip));
        }

        /// <summary>
        ///     Queues <c>ModKeywordRegistry.RegisterCardKeywordOwned</c> with legacy hover defaults.
        /// </summary>
        public ModContentPackBuilder CardKeywordOwned(
            string localKeywordStem,
            string? locKeyPrefix = null,
            string? iconPath = null)
        {
            return CardKeywordOwned(
                localKeywordStem,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Queues extended <see cref="ModKeywordRegistry" /> card-keyword registration (placement + hover-tip flags).
        /// </summary>
        [Obsolete(
            "Prefer CardKeywordOwned(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder CardKeyword(
            string id,
            string? locKeyPrefix,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id);
                var prefix = string.IsNullOrWhiteSpace(locKeyPrefix)
                    ? StringHelper.Slugify(id)
                    : locKeyPrefix.Trim();

                ctx.Keywords.RegisterCore(
                    id,
                    "card_keywords",
                    $"{prefix}.title",
                    "card_keywords",
                    $"{prefix}.description",
                    iconPath,
                    cardDescriptionPlacement,
                    includeInCardHoverTip);
            });
        }

        /// <summary>
        ///     Legacy <c>CardKeyword</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        /// </summary>
        [Obsolete(
            "Prefer CardKeywordOwned(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder CardKeyword(string id, string? locKeyPrefix = null, string? iconPath = null)
        {
            return CardKeyword(
                id,
                locKeyPrefix,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Queues <c>ModKeywordRegistry.RegisterOwned</c> (mod-local stem → qualified id).
        /// </summary>
        public ModContentPackBuilder KeywordOwned(
            string localKeywordStem,
            string titleTable,
            string? titleKey,
            string? descriptionTable,
            string? descriptionKey,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
                ctx.Keywords.RegisterOwned(localKeywordStem, titleTable, titleKey, descriptionTable, descriptionKey,
                    iconPath, cardDescriptionPlacement, includeInCardHoverTip));
        }

        /// <summary>
        ///     Queues <c>ModKeywordRegistry.RegisterOwned</c> with legacy hover defaults.
        /// </summary>
        public ModContentPackBuilder KeywordOwned(
            string localKeywordStem,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return KeywordOwned(
                localKeywordStem,
                titleTable,
                titleKey,
                descriptionTable,
                descriptionKey,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Queues extended <see cref="ModKeywordRegistry" /> keyword registration (placement + hover-tip flags).
        /// </summary>
        [Obsolete(
            "Prefer KeywordOwned(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder Keyword(
            string id,
            string titleTable,
            string? titleKey,
            string? descriptionTable,
            string? descriptionKey,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
                ctx.Keywords.RegisterCore(id, titleTable, titleKey, descriptionTable, descriptionKey, iconPath,
                    cardDescriptionPlacement, includeInCardHoverTip));
        }

        /// <summary>
        ///     Legacy <c>Keyword</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        /// </summary>
        [Obsolete(
            "Prefer KeywordOwned(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder Keyword(
            string id,
            string titleTable = "card_keywords",
            string? titleKey = null,
            string? descriptionTable = null,
            string? descriptionKey = null,
            string? iconPath = null)
        {
            return Keyword(
                id,
                titleTable,
                titleKey,
                descriptionTable,
                descriptionKey,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterEpoch{TEpoch}" />.
        /// </summary>
        public ModContentPackBuilder Epoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterEpoch<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" /> (epoch + story column order).
        /// </summary>
        public ModContentPackBuilder StoryEpoch<TStory, TEpoch>()
            where TStory : StoryModel, new()
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStoryEpoch<TStory, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterTimelineSlot" /> for a <see cref="ModEpochTemplate" />
        ///     when not using <see cref="TimelineColumnPackEntry{TStory}" />.
        /// </summary>
        public ModContentPackBuilder ModEpochTimelineSlot<TEpoch>(EpochEra era, int eraPosition)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(TEpoch), era, eraPosition, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlot" /> for a <see cref="ModEpochTemplate" />.
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlot<TEpoch>(EpochEra era)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx => ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(typeof(TEpoch), era, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn" />.
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotBeforeColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn(typeof(TEpoch), anchorEra,
                    ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn" />.
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotAfterColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(typeof(TEpoch), anchorEra, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="TimelineColumnPackEntry{TStory}" /> — one fluent block for column order + per-epoch unlock
        ///     bindings (recommended over many separate pack entry types).
        /// </summary>
        public ModContentPackBuilder TimelineColumn<TStory>(Action<TimelineColumnBuilder<TStory>> configure)
            where TStory : StoryModel, new()
        {
            ArgumentNullException.ThrowIfNull(configure);
            return PackEntry(new TimelineColumnPackEntry<TStory>(configure));
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
        ///     Queues <see cref="BindCardUnlockEpochPackEntry{TEpoch}" /> — each card listed on
        ///     <typeparamref name="TEpoch" /> requires that epoch before appearing in pools.
        /// </summary>
        public ModContentPackBuilder BindCardUnlockEpoch<TEpoch>()
            where TEpoch : CardUnlockEpochTemplate, new()
        {
            return PackEntry(new BindCardUnlockEpochPackEntry<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="BindRelicUnlockEpochPackEntry{TEpoch}" /> — each relic listed on
        ///     <typeparamref name="TEpoch" /> requires that epoch before appearing in pools.
        /// </summary>
        public ModContentPackBuilder BindRelicUnlockEpoch<TEpoch>()
            where TEpoch : RelicUnlockEpochTemplate, new()
        {
            return PackEntry(new BindRelicUnlockEpochPackEntry<TEpoch>());
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
        ///     Registers <see cref="ModContentRegistry" /> entries (character, cards, relics, powers, …).
        /// </summary>
        public ModContentPackBuilder ContentManifest(IEnumerable<IContentRegistrationEntry>? entries)
        {
            return entries != null ? Entries(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModKeywordRegistry" /> entries (separate from ModelDb content).
        /// </summary>
        public ModContentPackBuilder KeywordManifest(IEnumerable<KeywordRegistrationEntry>? entries)
        {
            return entries != null ? Keywords(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModTimelineRegistry" /> / <see cref="ModUnlockRegistry" /> via
        ///     <see cref="IModContentPackEntry" /> (story–epoch bindings, unlock rules). Usually applied after content so
        ///     <c>RequireEpoch</c> can resolve character ids.
        /// </summary>
        public ModContentPackBuilder PackManifest(IEnumerable<IModContentPackEntry>? entries)
        {
            return PackEntries(entries);
        }

        /// <summary>
        ///     Convenience batch for optional content and keyword manifest enumerables.
        /// </summary>
        /// <remarks>
        ///     <see cref="IContentRegistrationEntry" /> may include
        ///     <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />,
        ///     <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />, and related Orobas
        ///     entries alongside cards/relics/etc. Keywords use a different registry; prefer
        ///     <see cref="ContentManifest" /> / <see cref="KeywordManifest" /> / <see cref="PackManifest" /> when you want
        ///     that split to be explicit.
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
        ///     Convenience batch including optional <see cref="IModContentPackEntry" /> steps (timeline bindings, unlocks).
        /// </summary>
        public ModContentPackBuilder Manifest(
            IEnumerable<IContentRegistrationEntry>? contentEntries,
            IEnumerable<KeywordRegistrationEntry>? keywordEntries,
            IEnumerable<IModContentPackEntry>? packEntries)
        {
            Manifest(contentEntries, keywordEntries);
            if (packEntries != null)
                PackEntries(packEntries);

            return this;
        }

        /// <summary>
        ///     Appends a <see cref="IModContentPackEntry" /> (timeline / unlock / other pack surface).
        /// </summary>
        public ModContentPackBuilder PackEntry(IModContentPackEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(entry.Apply);
        }

        /// <summary>
        ///     Appends each <see cref="IModContentPackEntry" /> in order.
        /// </summary>
        public ModContentPackBuilder PackEntries(IEnumerable<IModContentPackEntry>? entries)
        {
            if (entries == null)
                return this;

            foreach (var entry in entries)
                PackEntry(entry);

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
        ///     Builds context, runs all queued registration steps, and returns the context.
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
