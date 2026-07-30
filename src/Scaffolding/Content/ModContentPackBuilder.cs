using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;
using SmartFormat.Core.Extensions;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Localization.SmartFormat;
using STS2RitsuLib.Scaffolding.Ancients.Options;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Timeline;
using STS2RitsuLib.Timeline.Scaffolding;
using STS2RitsuLib.TopBar;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     <para xml:lang="en">Provides an immutable snapshot of the registries used to apply a content pack.</para>
    ///     <para xml:lang="zh-CN">提供应用内容包时所用注册表的不可变快照。</para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">The owning mod ID.</para>
    ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
    /// </param>
    /// <param name="Content">
    ///     <para xml:lang="en">The content registry for models and pools.</para>
    ///     <para xml:lang="zh-CN">模型和内容池的内容注册表。</para>
    /// </param>
    /// <param name="Keywords">
    ///     <para xml:lang="en">The keyword registry.</para>
    ///     <para xml:lang="zh-CN">关键词注册表。</para>
    /// </param>
    /// <param name="Timeline">
    ///     <para xml:lang="en">The epoch and story timeline registry.</para>
    ///     <para xml:lang="zh-CN">时代和故事时间线注册表。</para>
    /// </param>
    /// <param name="Unlocks">
    ///     <para xml:lang="en">The unlock-rule registry.</para>
    ///     <para xml:lang="zh-CN">解锁规则注册表。</para>
    /// </param>
    public readonly record struct ModContentPackContext(
        string ModId,
        ModContentRegistry Content,
        ModKeywordRegistry Keywords,
        ModTimelineRegistry Timeline,
        ModUnlockRegistry Unlocks)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a context while accepting a card-tag registry for call-site symmetry. The supplied registry
        ///         is not stored; <see cref="CardTags" /> always resolves the per-mod singleton.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建上下文，并为保持调用形式一致而接受卡牌标签注册表。传入的注册表不会被保存；
        ///         <see cref="CardTags" /> 始终解析为该模组的单例。
        ///     </para>
        /// </summary>
        public ModContentPackContext(
            string modId,
            ModContentRegistry content,
            ModKeywordRegistry keywords,
            ModTimelineRegistry timeline,
            ModUnlockRegistry unlocks,
            ModCardTagRegistry cardTagRegistry) : this(modId, content, keywords, timeline, unlocks)
        {
            _ = cardTagRegistry;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a context while accepting card-tag and card-pile registries for call-site symmetry.
        ///         The supplied card-pile registry is not stored; <see cref="CardPiles" /> always resolves the
        ///         per-mod singleton.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建上下文，并为保持调用形式一致而接受卡牌标签和牌堆注册表。传入的牌堆注册表不会被保存；
        ///         <see cref="CardPiles" /> 始终解析为该模组的单例。
        ///     </para>
        /// </summary>
        public ModContentPackContext(
            string modId,
            ModContentRegistry content,
            ModKeywordRegistry keywords,
            ModTimelineRegistry timeline,
            ModUnlockRegistry unlocks,
            ModCardTagRegistry cardTagRegistry,
            ModCardPileRegistry cardPileRegistry) : this(modId, content, keywords, timeline, unlocks,
            cardTagRegistry)
        {
            _ = cardPileRegistry;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the custom <see cref="CardTag" /> registry for <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModId" /> 的自定义 <see cref="CardTag" /> 注册表。</para>
        /// </summary>
        public ModCardTagRegistry CardTags => ModCardTagRegistry.For(ModId);

        /// <summary>
        ///     <para xml:lang="en">Gets the custom <see cref="CardPile" /> registry for <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModId" /> 的自定义 <see cref="CardPile" /> 注册表。</para>
        /// </summary>
        public ModCardPileRegistry CardPiles => ModCardPileRegistry.For(ModId);

        /// <summary>
        ///     <para xml:lang="en">Gets the SmartFormat extension registry for <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModId" /> 的 SmartFormat 扩展注册表。</para>
        /// </summary>
        public ModSmartFormatExtensionRegistry SmartFormat => ModSmartFormatExtensionRegistry.For(ModId);

        /// <summary>
        ///     <para xml:lang="en">Gets the top-bar button registry for <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModId" /> 的顶部栏按钮注册表。</para>
        /// </summary>
        public ModTopBarButtonRegistry TopBarButtons => ModTopBarButtonRegistry.For(ModId);

        /// <summary>
        ///     <para xml:lang="en">Gets the dynamic-enum-value registry for <see cref="ModId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <see cref="ModId" /> 的动态枚举值注册表。</para>
        /// </summary>
        public ModDynamicEnumValueRegistry<TEnum> DynamicEnumValues<TEnum>() where TEnum : struct, Enum
        {
            return DynamicEnumValueRegistry<TEnum>.For(ModId);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Provides a fluent API for collecting and applying common mod registrations.</para>
    ///     <para xml:lang="zh-CN">提供流式 API，用于收集和应用常见的模组注册操作。</para>
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
        ///     <para xml:lang="en">Creates a builder for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="modId" /> 创建构建器。</para>
        /// </summary>
        public static ModContentPackBuilder For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId.Trim());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterCharacter{TCharacter}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterCharacter{TCharacter}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Character<TCharacter>() where TCharacter : CharacterModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacter<TCharacter>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterBadge{TBadge}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterBadge{TBadge}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Badge<TBadge>() where TBadge : ModBadgeTemplate
        {
            return AddStep(ctx => ctx.Content.RegisterBadge<TBadge>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues character registration and additive starter-content configuration.</para>
        ///     <para xml:lang="zh-CN">将角色注册和追加式初始内容配置一并加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Character<TCharacter>(Action<CharacterRegistrationEntry<TCharacter>> configure)
            where TCharacter : CharacterModel
        {
            ArgumentNullException.ThrowIfNull(configure);

            var entry = new CharacterRegistrationEntry<TCharacter>();
            configure(entry);
            return AddStep(ctx => entry.Register(ctx.Content));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterCard<TCharacter, TCard>(int count = 1)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            return CharacterStarterCard<TCharacter, TCard>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int,int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int,int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterCard<TCharacter, TCard>(int count, int order)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterCard<TCharacter, TCard>(count, order));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterRelic<TCharacter, TRelic>(int count = 1)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            return CharacterStarterRelic<TCharacter, TRelic>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int,int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int,int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterRelic<TCharacter, TRelic>(int count, int order)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterRelic<TCharacter, TRelic>(count, order));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterPotion<TCharacter, TPotion>(int count = 1)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            return CharacterStarterPotion<TCharacter, TPotion>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int,int)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int,int)" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CharacterStarterPotion<TCharacter, TPotion>(int count, int order)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterPotion<TCharacter, TPotion>(count, order));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a direct character-asset replacement by character ID.</para>
        ///     <para xml:lang="zh-CN">按角色 ID 将直接角色资源替换注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CharacterAssetReplacement(string characterEntry,
            CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentNullException.ThrowIfNull(assetProfile);
            return AddStep(ctx => ctx.Content.RegisterCharacterAssetReplacement(characterEntry, assetProfile));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterAct{TAct}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterAct{TAct}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Act<TAct>() where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterAct<TAct>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterActEnterForce{TAct}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterActEnterForce{TAct}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ActEnterForce<TAct>(int slotIndex, int priority,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterForce<TAct>(slotIndex, priority, eligibility));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterActEnterUniformPool" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterActEnterUniformPool" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPool(slotIndex));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPoolCandidate<TAct>(slotIndex, eligibility));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPool" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterActEnterWeightedPool" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPool(slotIndex));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility, Func<ActEnterResolveContext, double> weight)
            where TAct : ActModel
        {
            return AddStep(ctx =>
                ctx.Content.RegisterActEnterWeightedPoolCandidate<TAct>(slotIndex, eligibility, weight));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPoolBaseline(int slotIndex,
            Func<ActEnterResolveContext, double> weight)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPoolBaseline(slotIndex, weight));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" /> so the encounter
        ///         appears only in that act.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" /> 加入队列，
        ///         使该遭遇仅出现在指定章节。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder ActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEncounter<TAct, TEncounter>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterGlobalEncounter{TEncounter}" /> so the encounter is
        ///         merged into every act's encounter pool.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterGlobalEncounter{TEncounter}" /> 加入队列，
        ///         使该遭遇合并到每个章节的遭遇池。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder GlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterGlobalEncounter<TEncounter>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues registration of a standalone monster type.</para>
        ///     <para xml:lang="zh-CN">将独立怪物类型的注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Monster<TMonster>() where TMonster : MonsterModel
        {
            return AddStep(ctx => ctx.Content.RegisterMonster<TMonster>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues card registration with the default public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用默认公开条目选项将卡牌注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues card registration with the specified public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用指定的公开条目选项将卡牌注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>(publicEntry));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> for gold and red hand-glow rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> 加入队列，用于手牌的金色和红色发光规则。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder CardHandGlow<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandGlow<TCard>(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues typed custom hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">将类型化的自定义手牌描边规则加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineRules<TCard> rules)
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandOutline(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues one typed custom hand-outline rule.</para>
        ///     <para xml:lang="zh-CN">将一条类型化的自定义手牌描边规则加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard> rule)
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandOutline(rule));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues multiple typed custom hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">将多条类型化的自定义手牌描边规则加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[] rules)
            where TCard : CardModel
        {
            return CardHandOutline(ModCardHandOutlineRules<TCard>.Of(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues obsolete type-erased custom hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">将已过时的类型擦除自定义手牌描边规则加入队列。</para>
        /// </summary>
        [Obsolete("Use CardHandOutline<TCard>(ModCardHandOutlineRules<TCard>).")]
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineRules rules) where TCard : CardModel
        {
            return AddStep(_ => ModCardHandOutlineRegistry.Register<TCard>(rules));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues one obsolete type-erased custom hand-outline rule.</para>
        ///     <para xml:lang="zh-CN">将一条已过时的类型擦除自定义手牌描边规则加入队列。</para>
        /// </summary>
        [Obsolete("Use CardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineSwitchRule rule) where TCard : CardModel
        {
            return AddStep(_ => ModCardHandOutlineRegistry.Register<TCard>(rule));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues multiple obsolete type-erased custom hand-outline rules.</para>
        ///     <para xml:lang="zh-CN">将多条已过时的类型擦除自定义手牌描边规则加入队列。</para>
        /// </summary>
        [Obsolete("Use CardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[]).")]
        public ModContentPackBuilder CardHandOutline<TCard>(params ModCardHandOutlineSwitchRule[] rules)
            where TCard : CardModel
        {
            return AddStep(_ => ModCardHandOutlineRegistry.Register<TCard>(ModCardHandOutlineRules.Of(rules)));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a delegate-based custom hand-outline rule.</para>
        ///     <para xml:lang="zh-CN">将基于委托的自定义手牌描边规则加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandOutline(
                colorWhen,
                priority,
                visibleWhenUnplayable,
                refreshEveryFrame));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an obsolete custom hand-outline rule.</para>
        ///     <para xml:lang="zh-CN">将已过时的自定义手牌描边规则加入队列。</para>
        /// </summary>
        [Obsolete(
            "Use CardHandOutline<TCard>(ModCardHandOutlineRules<TCard>), CardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>), or CardHandOutline<TCard>(Func<TCard, Color?>).")]
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            return AddStep(_ => ModCardHandOutlineRegistry.Register<TCard>(rule.ToSwitchRule()));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a generated placeholder card that has no custom CLR type.</para>
        ///     <para xml:lang="zh-CN">将没有自定义 CLR 类型的生成式占位卡牌加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool>(string stableEntryStem,
            PlaceholderCardDescriptor descriptor = default)
            where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderCard<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a concrete card type with a stable public-entry stem.</para>
        ///     <para xml:lang="zh-CN">使用稳定的公开条目词干将具体卡牌类型加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool, TCard>(string stableEntryStem)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return Card<TPool, TCard>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues relic registration with the default public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用默认公开条目选项将遗物注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues relic registration with the specified public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用指定的公开条目选项将遗物注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>(publicEntry));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a generated placeholder relic that has no custom CLR type.</para>
        ///     <para xml:lang="zh-CN">将没有自定义 CLR 类型的生成式占位遗物加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool>(string stableEntryStem,
            PlaceholderRelicDescriptor descriptor = default)
            where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderRelic<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a concrete relic type with a stable public-entry stem.</para>
        ///     <para xml:lang="zh-CN">使用稳定的公开条目词干将具体遗物类型加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool, TRelic>(string stableEntryStem)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return Relic<TPool, TRelic>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues potion registration with the default public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用默认公开条目选项将药水注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues potion registration with the specified public-entry options.</para>
        ///     <para xml:lang="zh-CN">使用指定的公开条目选项将药水注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>(publicEntry));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a generated placeholder potion that has no custom CLR type.</para>
        ///     <para xml:lang="zh-CN">将没有自定义 CLR 类型的生成式占位药水加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool>(string stableEntryStem,
            PlaceholderPotionDescriptor descriptor = default)
            where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderPotion<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a concrete potion type with a stable public-entry stem.</para>
        ///     <para xml:lang="zh-CN">使用稳定的公开条目词干将具体药水类型加入队列。</para>
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool, TPotion>(string stableEntryStem)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return Potion<TPool, TPotion>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterPower{TPower}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterPower{TPower}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Power<TPower>() where TPower : PowerModel
        {
            return AddStep(ctx => ctx.Content.RegisterPower<TPower>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="RitsuLibFramework.RegisterHealthBarForecast{TSource}" /> for a forecast source
        ///         that is not a power.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="RitsuLibFramework.RegisterHealthBarForecast{TSource}" /> 加入队列，
        ///         用于不属于能力的生命条预测来源。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder HealthBarForecast<TSource>(string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            return AddStep(ctx => RitsuLibFramework.RegisterHealthBarForecast<TSource>(ctx.ModId, sourceId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterOrb{TOrb}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterOrb{TOrb}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Orb<TOrb>() where TOrb : OrbModel
        {
            return AddStep(ctx => ctx.Content.RegisterOrb<TOrb>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterEnchantment{TEnchantment}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterEnchantment{TEnchantment}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Enchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            return AddStep(ctx => ctx.Content.RegisterEnchantment<TEnchantment>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterAffliction{TAffliction}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterAffliction{TAffliction}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Affliction<TAffliction>() where TAffliction : AfflictionModel
        {
            return AddStep(ctx => ctx.Content.RegisterAffliction<TAffliction>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterAchievement{TAchievement}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterAchievement{TAchievement}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Achievement<TAchievement>() where TAchievement : AchievementModel
        {
            return AddStep(ctx => ctx.Content.RegisterAchievement<TAchievement>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSingleton{TSingleton}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSingleton{TSingleton}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Singleton<TSingleton>() where TSingleton : SingletonModel
        {
            return AddStep(ctx => ctx.Content.RegisterSingleton<TSingleton>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a beneficial modifier with the default list placement.</para>
        ///     <para xml:lang="zh-CN">使用默认列表位置将正面修饰符注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder GoodModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterGoodModifier<TModifier>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a beneficial modifier with an explicit list sort order.</para>
        ///     <para xml:lang="zh-CN">使用明确的列表排序顺序将正面修饰符注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder GoodModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterGoodModifier<TModifier>(modifierListSortOrder));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a detrimental modifier with the default list placement.</para>
        ///     <para xml:lang="zh-CN">使用默认列表位置将负面修饰符注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder BadModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterBadModifier<TModifier>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a detrimental modifier with an explicit list sort order.</para>
        ///     <para xml:lang="zh-CN">使用明确的列表排序顺序将负面修饰符注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder BadModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterBadModifier<TModifier>(modifierListSortOrder));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterMutuallyExclusiveModifierGroup(Type[])" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterMutuallyExclusiveModifierGroup(Type[])" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder MutuallyExclusiveModifierGroup(params Type[] modifierTypes)
        {
            return AddStep(ctx => ctx.Content.RegisterMutuallyExclusiveModifierGroup(modifierTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSharedCardPool{TPool}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSharedCardPool{TPool}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SharedCardPool<TPool>() where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedCardPool<TPool>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a shared-pool filter for the card library compendium.
        ///     </para>
        ///     <para xml:lang="zh-CN">将卡牌总览的共享卡池筛选项加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardLibraryCompendiumSharedPoolFilter<TPool>(string stableId,
            string iconTexturePath)
            where TPool : CardPoolModel
        {
            return AddStep(ctx =>
                ctx.Content.RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(stableId, iconTexturePath));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a shared-pool filter with placement rules for the card library compendium.
        ///     </para>
        ///     <para xml:lang="zh-CN">将带有位置规则的卡牌总览共享卡池筛选项加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardLibraryCompendiumSharedPoolFilter<TPool>(
            string stableId,
            string iconTexturePath,
            IReadOnlyList<CardLibraryCompendiumPlacementRule>? placementRules)
            where TPool : CardPoolModel
        {
            return AddStep(ctx =>
                ctx.Content.RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(stableId, iconTexturePath,
                    placementRules));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSharedRelicPool{TPool}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSharedRelicPool{TPool}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedRelicPool<TPool>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSharedPotionPool{TPool}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSharedPotionPool{TPool}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedPotionPool<TPool>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSharedEvent{TEvent}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSharedEvent{TEvent}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SharedEvent<TEvent>() where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedEvent<TEvent>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterActEvent{TAct,TEvent}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterActEvent{TAct,TEvent}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEvent<TAct, TEvent>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterSharedAncient{TAncient}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterSharedAncient{TAncient}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedAncient<TAncient>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModContentRegistry.RegisterActAncient{TAct,TAncient}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModContentRegistry.RegisterActAncient{TAct,TAncient}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActAncient<TAct, TAncient>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModContentRegistry.RegisterAncientOption{TAncient}" /> to add an initial option.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModContentRegistry.RegisterAncientOption{TAncient}" /> 加入队列，以添加初始选项。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder AncientOption<TAncient>(ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterAncientOption<TAncient>(rule));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a card for the Trash Heap's Grab pool.</para>
        ///     <para xml:lang="zh-CN">将卡牌加入垃圾堆的“拿取”卡牌池注册队列。</para>
        /// </summary>
        public ModContentPackBuilder TrashHeapCard<TCard>()
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterTrashHeapCard<TCard>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a relic for the Trash Heap's Dive In pool.</para>
        ///     <para xml:lang="zh-CN">将遗物加入垃圾堆的“深入翻找”遗物池注册队列。</para>
        /// </summary>
        public ModContentPackBuilder TrashHeapRelic<TRelic>()
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterTrashHeapRelic<TRelic>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModSmartFormatExtensionRegistry.Register{TFormatter}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModSmartFormatExtensionRegistry.Register{TFormatter}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SmartFormatter<TFormatter>(int order = 0)
            where TFormatter : IFormatter, new()
        {
            return AddStep(ctx => ctx.SmartFormat.Register<TFormatter>(order));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModSmartFormatExtensionRegistry.RegisterFormatterType" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModSmartFormatExtensionRegistry.RegisterFormatterType" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SmartFormatter(Type formatterType, int order = 0)
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterFormatterType(formatterType, order));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModSmartFormatExtensionRegistry.RegisterSource{TSource}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModSmartFormatExtensionRegistry.RegisterSource{TSource}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SmartFormatSource<TSource>(int order = 0)
            where TSource : ISource, new()
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterSource<TSource>(order));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModSmartFormatExtensionRegistry.RegisterSourceType" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModSmartFormatExtensionRegistry.RegisterSourceType" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder SmartFormatSource(Type sourceType, int order = 0)
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterSourceType(sourceType, order));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a card keyword whose qualified ID also identifies its localization entries.
        ///     </para>
        ///     <para xml:lang="zh-CN">将限定 ID 同时用作本地化条目标识的卡牌关键词注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
                ctx.Keywords.RegisterCardKeywordOwnedByLocNamespace(localKeywordStem, iconPath,
                    cardDescriptionPlacement, includeInCardHoverTip));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a mod-qualified card keyword with the legacy hover-tip defaults.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用旧版悬停提示默认值将模组限定的卡牌关键词注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardKeywordOwnedByLocNamespace(
            string localKeywordStem,
            string? iconPath = null)
        {
            return CardKeywordOwnedByLocNamespace(
                localKeywordStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues obsolete flat-ID card-keyword registration with placement and hover-tip options.
        ///     </para>
        ///     <para xml:lang="zh-CN">将已过时的扁平 ID 卡牌关键词注册及其位置和悬停提示选项加入队列。</para>
        /// </summary>
        [Obsolete(
            "Prefer CardKeywordOwnedByLocNamespace(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder CardKeyword(
            string id,
            string? entryStem,
            string? iconPath,
            ModKeywordCardDescriptionPlacement cardDescriptionPlacement,
            bool includeInCardHoverTip)
        {
            return AddStep(ctx =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(id);
                var prefix = string.IsNullOrWhiteSpace(entryStem)
                    ? StringHelper.Slugify(id)
                    : entryStem.Trim();

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
        ///     <para xml:lang="en">
        ///         Provides the obsolete flat-ID <c>CardKeyword</c> signature with its legacy hover-tip behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         提供已过时的扁平 ID <c>CardKeyword</c> 签名，并保留其旧版悬停提示行为。
        ///     </para>
        /// </summary>
        [Obsolete(
            "Prefer CardKeywordOwnedByLocNamespace(localKeywordStem, ...) so the keyword id is mod-qualified; flat ids collide globally.")]
        public ModContentPackBuilder CardKeyword(string id, string? entryStem = null, string? iconPath = null)
        {
            return CardKeyword(
                id,
                entryStem,
                iconPath,
                ModKeywordCardDescriptionPlacement.None,
                true);
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a mod-owned keyword using a local stem.</para>
        ///     <para xml:lang="zh-CN">使用本地词干将模组所属关键词注册加入队列。</para>
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
        ///     <para xml:lang="en">Queues a mod-owned keyword with the legacy hover-tip defaults.</para>
        ///     <para xml:lang="zh-CN">使用旧版悬停提示默认值将模组所属关键词注册加入队列。</para>
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
        ///     <para xml:lang="en">
        ///         Queues obsolete flat-ID keyword registration with placement and hover-tip options.
        ///     </para>
        ///     <para xml:lang="zh-CN">将已过时的扁平 ID 关键词注册及其位置和悬停提示选项加入队列。</para>
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
        ///     <para xml:lang="en">
        ///         Provides the obsolete flat-ID <c>Keyword</c> signature with its legacy hover-tip behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         提供已过时的扁平 ID <c>Keyword</c> 签名，并保留其旧版悬停提示行为。
        ///     </para>
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
        ///     <para xml:lang="en">Queues <see cref="ModTimelineRegistry.RegisterEpoch{TEpoch}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModTimelineRegistry.RegisterEpoch{TEpoch}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Epoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterEpoch<TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch registration and its position in a story column.
        ///     </para>
        ///     <para xml:lang="zh-CN">将时代注册及其在故事列中的顺序加入队列。</para>
        /// </summary>
        public ModContentPackBuilder StoryEpoch<TStory, TEpoch>()
            where TStory : StoryModel, new()
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStoryEpoch<TStory, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an explicit timeline slot for a <see cref="ModEpochTemplate" />.</para>
        ///     <para xml:lang="zh-CN">为 <see cref="ModEpochTemplate" /> 将明确的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochTimelineSlot<TEpoch>(EpochEra era, int eraPosition)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(TEpoch), era, eraPosition, ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatically positioned timeline slot in the specified era.</para>
        ///     <para xml:lang="zh-CN">将指定时代中自动定位的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlot<TEpoch>(EpochEra era)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx => ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(typeof(TEpoch), era, ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot before an era column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在时代列之前的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotBeforeColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn(typeof(TEpoch), anchorEra,
                    ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot after an era column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在时代列之后的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotAfterColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(typeof(TEpoch), anchorEra, ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot within an era column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在时代列内的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotInColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn(typeof(TEpoch), anchorEra, ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot before a reference epoch's column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在参考时代所在列之前的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotBeforeEpochColumn<TEpoch, TReferenceEpoch>()
            where TEpoch : ModEpochTemplate
            where TReferenceEpoch : EpochModel, new()
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot after a reference epoch's column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在参考时代所在列之后的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotAfterEpochColumn<TEpoch, TReferenceEpoch>()
            where TEpoch : ModEpochTemplate
            where TReferenceEpoch : EpochModel, new()
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues an automatic timeline slot in a reference epoch's column.</para>
        ///     <para xml:lang="zh-CN">将自动定位在参考时代所在列内的时间线槽位加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotInEpochColumn<TEpoch, TReferenceEpoch>()
            where TEpoch : ModEpochTemplate
            where TReferenceEpoch : EpochModel, new()
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn(typeof(TEpoch),
                    typeof(TReferenceEpoch), ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a <see cref="TimelineColumnPackEntry{TStory}" /> that defines column order and per-epoch
        ///         unlock bindings in one fluent block.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="TimelineColumnPackEntry{TStory}" /> 加入队列，以一个流式配置块定义列顺序和各时代的解锁绑定。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder TimelineColumn<TStory>(Action<TimelineColumnBuilder<TStory>> configure)
            where TStory : StoryModel, new()
        {
            ArgumentNullException.ThrowIfNull(configure);
            return PackEntry(new TimelineColumnPackEntry<TStory>(configure));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModTimelineRegistry.RegisterStory{TStory}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModTimelineRegistry.RegisterStory{TStory}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder Story<TStory>() where TStory : StoryModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStory<TStory>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModUnlockRegistry.RequireEpoch{TModel,TEpoch}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModUnlockRegistry.RequireEpoch{TModel,TEpoch}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder RequireEpoch<TModel, TEpoch>()
            where TModel : AbstractModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RequireEpoch<TModel, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a binding that requires <typeparamref name="TEpoch" /> for each card declared by that epoch.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将绑定加入队列，使该时代声明的每张卡牌均需先解锁 <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder BindCardUnlockEpoch<TEpoch>()
            where TEpoch : CardUnlockEpochTemplate, new()
        {
            return PackEntry(new BindCardUnlockEpochPackEntry<TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a binding that requires <typeparamref name="TEpoch" /> for each relic declared by that epoch.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将绑定加入队列，使该时代声明的每件遗物均需先解锁 <typeparamref name="TEpoch" />。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder BindRelicUnlockEpoch<TEpoch>()
            where TEpoch : RelicUnlockEpochTemplate, new()
        {
            return PackEntry(new BindRelicUnlockEpochPackEntry<TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit card unlock content for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的卡牌解锁内容加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochCards<TEpoch>(IReadOnlyList<Type> cardTypes)
            where TEpoch : EpochModel
        {
            ArgumentNullException.ThrowIfNull(cardTypes);
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes(typeof(TEpoch), ctx, cardTypes, []));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit card unlock content for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的卡牌解锁内容加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochCards<TEpoch>(params Type[] cardTypes)
            where TEpoch : EpochModel
        {
            return EpochCards<TEpoch>((IReadOnlyList<Type>)cardTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues all cards registered in <typeparamref name="TPool" /> as unlock content for
        ///         <typeparamref name="TEpoch" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TPool" /> 中注册的所有卡牌作为 <typeparamref name="TEpoch" /> 的解锁内容加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder EpochCardsFromPool<TEpoch, TPool>()
            where TEpoch : EpochModel
            where TPool : CardPoolModel
        {
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyCardsFromPool(typeof(TEpoch), typeof(TPool), ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit relic unlock content for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的遗物解锁内容加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochRelics<TEpoch>(IReadOnlyList<Type> relicTypes)
            where TEpoch : EpochModel
        {
            ArgumentNullException.ThrowIfNull(relicTypes);
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyExplicitTypes(typeof(TEpoch), ctx, [], relicTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit relic unlock content for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的遗物解锁内容加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochRelics<TEpoch>(params Type[] relicTypes)
            where TEpoch : EpochModel
        {
            return EpochRelics<TEpoch>((IReadOnlyList<Type>)relicTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit potion requirements for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的药水解锁要求加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochPotions<TEpoch>(IReadOnlyList<Type> potionTypes)
            where TEpoch : EpochModel
        {
            ArgumentNullException.ThrowIfNull(potionTypes);
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyExplicitPotions(typeof(TEpoch), ctx, potionTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues explicit potion requirements for <typeparamref name="TEpoch" />.</para>
        ///     <para xml:lang="zh-CN">为 <typeparamref name="TEpoch" /> 将明确的药水解锁要求加入队列。</para>
        /// </summary>
        public ModContentPackBuilder EpochPotions<TEpoch>(params Type[] potionTypes)
            where TEpoch : EpochModel
        {
            return EpochPotions<TEpoch>((IReadOnlyList<Type>)potionTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement, when not already set, for every registered card in
        ///         <typeparamref name="TPool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TPool" /> 中每张尚未设置要求的已注册卡牌，将时代解锁要求加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder RequireAllCardsInPool<TEpoch, TPool>()
            where TEpoch : EpochModel
            where TPool : CardPoolModel
        {
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyRequireAllPoolCards(typeof(TEpoch), typeof(TPool), ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement, when not already set, for every registered relic in
        ///         <typeparamref name="TPool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TPool" /> 中每件尚未设置要求的已注册遗物，将时代解锁要求加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder RequireAllRelicsInPool<TEpoch, TPool>()
            where TEpoch : EpochModel
            where TPool : RelicPoolModel
        {
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyRequireAllPoolRelics(typeof(TEpoch), typeof(TPool), ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an epoch requirement, when not already set, for every registered potion in
        ///         <typeparamref name="TPool" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TPool" /> 中每瓶尚未设置要求的已注册药水，将时代解锁要求加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder RequireAllPotionsInPool<TEpoch, TPool>()
            where TEpoch : EpochModel
            where TPool : PotionPoolModel
        {
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyRequireAllPoolPotions(typeof(TEpoch), typeof(TPool), ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues every relic registered in <typeparamref name="TPool" /> as unlock content for
        ///         <typeparamref name="TEpoch" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <typeparamref name="TPool" /> 中注册的所有遗物作为 <typeparamref name="TEpoch" /> 的解锁内容加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder EpochRelicsFromPool<TEpoch, TPool>()
            where TEpoch : EpochModel
            where TPool : RelicPoolModel
        {
            return AddStep(ctx =>
                ModEpochGatedContentPackHelper.ApplyRelicsFromPool(typeof(TEpoch), typeof(TPool), ctx));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterWinAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(ascensionLevel));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" />.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" /> 加入队列。</para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(requiredEliteWins));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(requiredBossWins));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RevealAscensionAfterEpoch<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter,TEpoch}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将 <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter,TEpoch}" /> 加入队列。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockCharacterAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     <para xml:lang="en">Appends an <see cref="IContentRegistrationEntry" /> step.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="IContentRegistrationEntry" /> 步骤。</para>
        /// </summary>
        public ModContentPackBuilder Entry(IContentRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Content));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the content registration entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各内容注册条目。</para>
        /// </summary>
        public ModContentPackBuilder Entries(IEnumerable<IContentRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                Entry(entry);

            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a <see cref="KeywordRegistrationEntry" /> step.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="KeywordRegistrationEntry" /> 步骤。</para>
        /// </summary>
        public ModContentPackBuilder Keyword(KeywordRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Keywords));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the keyword registration entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各关键词注册条目。</para>
        /// </summary>
        public ModContentPackBuilder Keywords(IEnumerable<KeywordRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                Keyword(entry);

            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a mod-owned card tag using a local stem.</para>
        ///     <para xml:lang="zh-CN">使用本地词干将模组所属卡牌标签注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardTagOwned(string localTagStem)
        {
            return AddStep(ctx => ctx.CardTags.RegisterOwned(localTagStem));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a <see cref="CardTagRegistrationEntry" /> step.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="CardTagRegistrationEntry" /> 步骤。</para>
        /// </summary>
        public ModContentPackBuilder CardTag(CardTagRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.CardTags));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the card-tag registration entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各卡牌标签注册条目。</para>
        /// </summary>
        public ModContentPackBuilder CardTags(IEnumerable<CardTagRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                CardTag(entry);

            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a mod-owned dynamic enum value using a local stem.</para>
        ///     <para xml:lang="zh-CN">使用本地词干将模组所属动态枚举值注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder DynamicEnumValue<TEnum>(string localStem) where TEnum : struct, Enum
        {
            return AddStep(ctx => ctx.DynamicEnumValues<TEnum>().RegisterOwned(localStem));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a mod-owned card pile using a local stem.</para>
        ///     <para xml:lang="zh-CN">使用本地词干将模组所属牌堆注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardPileOwned(string localPileStem, ModCardPileSpec? spec = null)
        {
            return AddStep(ctx => ctx.CardPiles.RegisterOwned(localPileStem, spec ?? new ModCardPileSpec()));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a <see cref="CardPileRegistrationEntry" /> step.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="CardPileRegistrationEntry" /> 步骤。</para>
        /// </summary>
        public ModContentPackBuilder CardPile(CardPileRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.CardPiles));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a card-pile registration using a raw global ID.</para>
        ///     <para xml:lang="zh-CN">使用原始全局 ID 将牌堆注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardPile(string id, ModCardPileSpec spec)
        {
            return AddStep(ctx => ctx.CardPiles.Register(id, spec));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the card-pile registration entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各牌堆注册条目。</para>
        /// </summary>
        public ModContentPackBuilder CardPiles(IEnumerable<CardPileRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                CardPile(entry);

            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a mod-owned top-bar button using a local stem.</para>
        ///     <para xml:lang="zh-CN">使用本地词干将模组所属顶部栏按钮注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder TopBarButtonOwned(string localButtonStem, ModTopBarButtonSpec spec)
        {
            return AddStep(ctx => ctx.TopBarButtons.RegisterOwned(localButtonStem, spec));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends a <see cref="TopBarButtonRegistrationEntry" /> step.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="TopBarButtonRegistrationEntry" /> 步骤。</para>
        /// </summary>
        public ModContentPackBuilder TopBarButton(TopBarButtonRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.TopBarButtons));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a top-bar button registration using a raw global ID.</para>
        ///     <para xml:lang="zh-CN">使用原始全局 ID 将顶部栏按钮注册加入队列。</para>
        /// </summary>
        public ModContentPackBuilder TopBarButton(string id, ModTopBarButtonSpec spec)
        {
            return AddStep(ctx => ctx.TopBarButtons.Register(id, spec));
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the top-bar button registration entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各顶部栏按钮注册条目。</para>
        /// </summary>
        public ModContentPackBuilder TopBarButtons(IEnumerable<TopBarButtonRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                TopBarButton(entry);

            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the supplied <see cref="ModContentRegistry" /> entries.</para>
        ///     <para xml:lang="zh-CN">将提供的 <see cref="ModContentRegistry" /> 条目加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ContentManifest(IEnumerable<IContentRegistrationEntry>? entries)
        {
            return entries != null ? Entries(entries) : this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the supplied <see cref="ModKeywordRegistry" /> entries.</para>
        ///     <para xml:lang="zh-CN">将提供的 <see cref="ModKeywordRegistry" /> 条目加入队列。</para>
        /// </summary>
        public ModContentPackBuilder KeywordManifest(IEnumerable<KeywordRegistrationEntry>? entries)
        {
            return entries != null ? Keywords(entries) : this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the supplied <see cref="ModCardTagRegistry" /> entries.</para>
        ///     <para xml:lang="zh-CN">将提供的 <see cref="ModCardTagRegistry" /> 条目加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardTagManifest(IEnumerable<CardTagRegistrationEntry>? entries)
        {
            return entries != null ? CardTags(entries) : this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the supplied <see cref="ModCardPileRegistry" /> entries.</para>
        ///     <para xml:lang="zh-CN">将提供的 <see cref="ModCardPileRegistry" /> 条目加入队列。</para>
        /// </summary>
        public ModContentPackBuilder CardPileManifest(IEnumerable<CardPileRegistrationEntry>? entries)
        {
            return entries != null ? CardPiles(entries) : this;
        }

        /// <summary>
        ///     <para xml:lang="en">Queues the supplied <see cref="ModTopBarButtonRegistry" /> entries.</para>
        ///     <para xml:lang="zh-CN">将提供的 <see cref="ModTopBarButtonRegistry" /> 条目加入队列。</para>
        /// </summary>
        public ModContentPackBuilder TopBarButtonManifest(IEnumerable<TopBarButtonRegistrationEntry>? entries)
        {
            return entries != null ? TopBarButtons(entries) : this;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues timeline and unlock entries. These entries normally follow content registration so
        ///         <c>RequireEpoch</c> can resolve character IDs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将时间线和解锁条目加入队列。这些条目通常在内容注册之后应用，以便 <c>RequireEpoch</c> 解析角色 ID。
        ///     </para>
        /// </summary>
        public ModContentPackBuilder PackManifest(IEnumerable<IModContentPackEntry>? entries)
        {
            return PackEntries(entries);
        }

        /// <summary>
        ///     <para xml:lang="en">Queues optional content and keyword manifests.</para>
        ///     <para xml:lang="zh-CN">将可选的内容和关键词清单加入队列。</para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         <see cref="IContentRegistrationEntry" /> may include
        ///         <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />,
        ///         <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />, and related Orobas
        ///         entries alongside cards/relics/etc. Keywords use a different registry; prefer
        ///         <see cref="ContentManifest" /> / <see cref="KeywordManifest" /> / <see cref="PackManifest" /> when you want
        ///         that split to be explicit.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="IContentRegistrationEntry" /> 可以包含
        ///         <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />、
        ///         <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />，以及与卡牌 / 遗物等并列的相关 Orobas
        ///         条目。关键词使用不同的注册表；当你希望这种拆分显式可见时，优先使用 <see cref="ContentManifest" /> / <see cref="KeywordManifest" /> /
        ///         <see cref="PackManifest" />。
        ///     </para>
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
        ///     <para xml:lang="en">Queues optional content, keyword, and content-pack manifests.</para>
        ///     <para xml:lang="zh-CN">将可选的内容、关键词和内容包清单加入队列。</para>
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
        ///     <para xml:lang="en">Appends an <see cref="IModContentPackEntry" />.</para>
        ///     <para xml:lang="zh-CN">追加一个 <see cref="IModContentPackEntry" />。</para>
        /// </summary>
        public ModContentPackBuilder PackEntry(IModContentPackEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(entry.Apply);
        }

        /// <summary>
        ///     <para xml:lang="en">Appends the content-pack entries in enumeration order.</para>
        ///     <para xml:lang="zh-CN">按枚举顺序追加各内容包条目。</para>
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
        ///     <para xml:lang="en">Queues an Archaic Tooth transcendence mapping for this mod.</para>
        ///     <para xml:lang="zh-CN">将此模组的“古旧尖牙”超越映射加入队列。</para>
        /// </summary>
        public ModContentPackBuilder ArchaicToothTranscendence<TStarterCard, TAncientCard>()
            where TStarterCard : CardModel
            where TAncientCard : CardModel
        {
            return AddStep(ctx =>
                RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues an Archaic Tooth transcendence mapping by starter-card ID and Ancient-card type.
        ///     </para>
        ///     <para xml:lang="zh-CN">按初始卡牌 ID 和先古卡牌类型将“古旧尖牙”超越映射加入队列。</para>
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
        ///     <para xml:lang="en">Queues a Dusty Tome card mapping for this mod.</para>
        ///     <para xml:lang="zh-CN">将此模组的“尘封典籍”卡牌映射加入队列。</para>
        /// </summary>
        public ModContentPackBuilder DustyTomeCard<TCharacter, TAncientCard>()
            where TCharacter : CharacterModel
            where TAncientCard : CardModel
        {
            return AddStep(ctx =>
                RitsuLibFramework.RegisterDustyTomeCard<TCharacter, TAncientCard>(ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a Dusty Tome mapping by character ID and Ancient-card type.</para>
        ///     <para xml:lang="zh-CN">按角色 ID 和先古卡牌类型将“尘封典籍”映射加入队列。</para>
        /// </summary>
        public ModContentPackBuilder DustyTomeCard(ModelId characterId, Type ancientCardType)
        {
            ArgumentNullException.ThrowIfNull(ancientCardType);
            return AddStep(ctx =>
                RitsuLibFramework.RegisterDustyTomeCard(characterId, ancientCardType, ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">Queues a Touch of Orobas refinement mapping for this mod.</para>
        ///     <para xml:lang="zh-CN">将此模组的“衔尾蛇之触”精炼映射加入队列。</para>
        /// </summary>
        public ModContentPackBuilder TouchOfOrobasRefinement<TStarterRelic, TUpgradedRelic>()
            where TStarterRelic : RelicModel
            where TUpgradedRelic : RelicModel
        {
            return AddStep(ctx =>
                RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(ctx.ModId));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Queues a Touch of Orobas refinement mapping by starter-relic ID and upgraded-relic type.
        ///     </para>
        ///     <para xml:lang="zh-CN">按初始遗物 ID 和升级遗物类型将“衔尾蛇之触”精炼映射加入队列。</para>
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
        ///     <para xml:lang="en">Appends a custom delegate to execute during <see cref="Apply" />.</para>
        ///     <para xml:lang="zh-CN">追加一个在 <see cref="Apply" /> 期间执行的自定义委托。</para>
        /// </summary>
        public ModContentPackBuilder Custom(Action<ModContentPackContext> step)
        {
            return AddStep(step);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates the registry context without running queued steps.</para>
        ///     <para xml:lang="zh-CN">创建注册表上下文，但不执行队列中的步骤。</para>
        /// </summary>
        public ModContentPackContext BuildContext()
        {
            return new(
                _modId,
                RitsuLibFramework.GetContentRegistry(_modId),
                RitsuLibFramework.GetKeywordRegistry(_modId),
                RitsuLibFramework.GetTimelineRegistry(_modId),
                RitsuLibFramework.GetUnlockRegistry(_modId),
                RitsuLibFramework.GetCardTagRegistry(_modId),
                RitsuLibFramework.GetCardPileRegistry(_modId));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Schedules all queued registration steps for the framework discovery window and returns the
        ///         materialized context.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         安排所有队列中的注册步骤在框架发现窗口执行，并返回已创建的上下文。
        ///     </para>
        /// </summary>
        public ModContentPackContext Apply()
        {
            var context = BuildContext();
            var steps = _steps.ToArray();
            RitsuLibFramework.EnqueueDeferredContentPack(
                _modId,
                ctx =>
                {
                    foreach (var step in steps)
                        step(ctx);

                    RitsuLibFramework.CreateLogger(_modId)
                        .Info($"[ContentPack] Applied {steps.Length} deferred registration step(s).");
                },
                $"{_modId}:{steps.Length} step(s)");
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
