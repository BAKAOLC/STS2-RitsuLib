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

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Immutable snapshot of registries and ids used while applying a content pack.
    ///     应用内容包时使用的注册表和 id 的不可变快照。
    /// </summary>
    /// <param name="ModId">
    ///     Owning mod identifier string.
    ///     所属 Mod 标识符字符串。
    /// </param>
    /// <param name="Content">
    ///     Content registry for models and pools.
    ///     模型和池使用的内容注册表。
    /// </param>
    /// <param name="Keywords">
    ///     Keyword registration surface.
    ///     关键词注册入口。
    /// </param>
    /// <param name="Timeline">
    ///     Epoch/story timeline registry.
    ///     纪元/故事时间线注册表。
    /// </param>
    /// <param name="Unlocks">
    ///     Unlock rule registry.
    ///     解锁规则注册表。
    /// </param>
    public readonly record struct ModContentPackContext(
        string ModId,
        ModContentRegistry Content,
        ModKeywordRegistry Keywords,
        ModTimelineRegistry Timeline,
        ModUnlockRegistry Unlocks)
    {
        /// <summary>
        ///     Same as the 5-parameter <see cref="ModContentPackContext" /> primary constructor. The
        ///     <paramref name="cardTagRegistry" /> argument is accepted so call sites can mirror
        ///     <see cref="RitsuLibFramework.GetContentRegistry" /> / <see cref="RitsuLibFramework.GetKeywordRegistry" />
        ///     / … style: pass <c>ModCardTagRegistry.For(<paramref name="modId" />)</c> or
        ///     <see cref="RitsuLibFramework.GetCardTagRegistry" />. The value is not read; <see cref="CardTags" /> is
        ///     always the per-mod singleton from <c>ModCardTagRegistry.For</c>.
        ///     <see cref="RitsuLibFramework.GetContentRegistry" /> / <see cref="RitsuLibFramework.GetKeywordRegistry" />
        ///     与 5 参数 <see cref="ModContentPackContext" /> 主构造函数相同。接受 <paramref name="cardTagRegistry" /> 参数是为了让调用点可以镜像
        ///     <see cref="RitsuLibFramework.GetContentRegistry" /> / <see cref="RitsuLibFramework.GetKeywordRegistry" /> / ...
        ///     风格：传入 <c>ModCardTagRegistry.For(<paramref name="modId" />)</c> 或
        ///     <see cref="RitsuLibFramework.GetCardTagRegistry" />。该值不会被读取；<see cref="CardTags" /> 始终是来自
        ///     <c>ModCardTagRegistry.For</c> 的每 mod singleton。
        ///     <see cref="RitsuLibFramework.GetContentRegistry" /> / <see cref="RitsuLibFramework.GetKeywordRegistry" />
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
        ///     Same as the 6-parameter compatibility constructor, accepting a card-pile registry so call sites can
        ///     mirror <see cref="RitsuLibFramework.GetCardPileRegistry" /> style alongside content / keywords /
        ///     card tags. The value is not read; <see cref="CardPiles" /> is always the per-mod singleton.
        ///     与 6 参数兼容构造函数相同，接受牌堆注册表，使调用点可以在 content / keywords / card tags 旁边
        ///     镜像 <see cref="RitsuLibFramework.GetCardPileRegistry" /> 风格。该值不会被读取；
        ///     <see cref="CardPiles" /> 始终是每 mod 单例。
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
        ///     Custom <see cref="CardTag" /> surface for <see cref="ModId" />; same singleton as
        ///     <c>ModCardTagRegistry.For(ModId)</c> and <c>RitsuLibFramework.GetCardTagRegistry</c>.
        ///     <see cref="CardTag" /> 的自定义 <see cref="ModId" /> 入口；与
        ///     <c>ModCardTagRegistry.For(ModId)</c> 和 <c>RitsuLibFramework.GetCardTagRegistry</c> 是同一个单例。
        /// </summary>
        public ModCardTagRegistry CardTags => ModCardTagRegistry.For(ModId);

        /// <summary>
        ///     Custom <see cref="CardPile" /> surface for <see cref="ModId" />; same singleton as
        ///     <c>ModCardPileRegistry.For(ModId)</c> and <c>RitsuLibFramework.GetCardPileRegistry</c>.
        ///     <see cref="CardPile" /> 的自定义 <see cref="ModId" /> 入口；与
        ///     <c>ModCardPileRegistry.For(ModId)</c> 和 <c>RitsuLibFramework.GetCardPileRegistry</c> 是同一个单例。
        /// </summary>
        public ModCardPileRegistry CardPiles => ModCardPileRegistry.For(ModId);

        /// <summary>
        ///     SmartFormat extension surface for <see cref="ModId" />; same singleton as
        ///     <c>ModSmartFormatExtensionRegistry.For(ModId)</c> and <c>RitsuLibFramework.GetSmartFormatRegistry</c>.
        ///     <see cref="ModId" /> 的 SmartFormat 扩展入口；与
        ///     <c>ModSmartFormatExtensionRegistry.For(ModId)</c> 和 <c>RitsuLibFramework.GetSmartFormatRegistry</c>
        ///     是同一个单例。
        /// </summary>
        public ModSmartFormatExtensionRegistry SmartFormat => ModSmartFormatExtensionRegistry.For(ModId);

        /// <summary>
        ///     Top-bar button surface for <see cref="ModId" />; same singleton as
        ///     <c>ModTopBarButtonRegistry.For(ModId)</c> and <c>RitsuLibFramework.GetTopBarButtonRegistry</c>.
        ///     <see cref="ModId" /> 的顶部栏按钮入口；与 <c>ModTopBarButtonRegistry.For(ModId)</c> 和
        ///     <c>RitsuLibFramework.GetTopBarButtonRegistry</c> 是同一个单例。
        /// </summary>
        public ModTopBarButtonRegistry TopBarButtons => ModTopBarButtonRegistry.For(ModId);
    }

    /// <summary>
    ///     Fluent registration helper that batches common mod-author setup into a single readable flow.
    ///     流式注册辅助类，将常见 Mod 作者设置批处理为一个可读流程。
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
        ///     为给定 <paramref name="modId" /> 启动构建器。
        /// </summary>
        public static ModContentPackBuilder For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            return new(modId);
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacter{TCharacter}" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacter{TCharacter}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Character<TCharacter>() where TCharacter : CharacterModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacter<TCharacter>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterBadge{TBadge}" />.
        ///     将 <see cref="ModContentRegistry.RegisterBadge{TBadge}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Badge<TBadge>() where TBadge : ModBadgeTemplate
        {
            return AddStep(ctx => ctx.Content.RegisterBadge<TBadge>());
        }

        /// <summary>
        ///     Queues character registration plus additive starter content configuration in one place.
        ///     在一个位置将角色注册和追加初始内容配置加入队列。
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
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterCard<TCharacter, TCard>(int count = 1)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            return CharacterStarterCard<TCharacter, TCard>(count, 0);
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int,int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterCard{TCharacter,TCard}(int,int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterCard<TCharacter, TCard>(int count, int order)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterCard<TCharacter, TCard>(count, order));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterRelic<TCharacter, TRelic>(int count = 1)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            return CharacterStarterRelic<TCharacter, TRelic>(count, 0);
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int,int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterRelic{TCharacter,TRelic}(int,int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterRelic<TCharacter, TRelic>(int count, int order)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterRelic<TCharacter, TRelic>(count, order));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterPotion<TCharacter, TPotion>(int count = 1)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            return CharacterStarterPotion<TCharacter, TPotion>(count, 0);
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int,int)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCharacterStarterPotion{TCharacter,TPotion}(int,int)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterStarterPotion<TCharacter, TPotion>(int count, int order)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterCharacterStarterPotion<TCharacter, TPotion>(count, order));
        }

        /// <summary>
        ///     Queues direct character asset replacement registration by character id.
        ///     按角色 id 将直接角色资源替换注册加入队列。
        /// </summary>
        public ModContentPackBuilder CharacterAssetReplacement(string characterEntry,
            CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentNullException.ThrowIfNull(assetProfile);
            return AddStep(ctx => ctx.Content.RegisterCharacterAssetReplacement(characterEntry, assetProfile));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAct{TAct}" />.
        ///     将 <see cref="ModContentRegistry.RegisterAct{TAct}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Act<TAct>() where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterAct<TAct>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterForce{TAct}" />.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterForce{TAct}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEnterForce<TAct>(int slotIndex, int priority,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterForce<TAct>(slotIndex, priority, eligibility));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterUniformPool" />.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterUniformPool" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPool(slotIndex));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}" />.
        ///     Queues <c>ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterUniformPoolCandidate{TAct}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEnterUniformPoolCandidate<TAct>(int slotIndex,
            Func<ActEnterResolveContext, bool> eligibility)
            where TAct : ActModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterUniformPoolCandidate<TAct>(slotIndex, eligibility));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPool" />.
        ///     Queues <c>ModContentRegistry.RegisterActEnterWeightedPool</c>.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterWeightedPool" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPool(int slotIndex)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPool(slotIndex));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}" />.
        ///     Queues <c>ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterWeightedPoolCandidate{TAct}" /> 加入队列。
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
        ///     Queues <c>ModContentRegistry.RegisterActEnterWeightedPoolBaseline</c>.
        ///     将 <see cref="ModContentRegistry.RegisterActEnterWeightedPoolBaseline" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEnterWeightedPoolBaseline(int slotIndex,
            Func<ActEnterResolveContext, double> weight)
        {
            return AddStep(ctx => ctx.Content.RegisterActEnterWeightedPoolBaseline(slotIndex, weight));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" /> (encounter only in that act).
        ///     将 <see cref="ModContentRegistry.RegisterActEncounter{TAct,TEncounter}" /> 加入队列（遭遇只出现在该章节中）。
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
        ///     将 <see cref="ModContentRegistry.RegisterGlobalEncounter{TEncounter}" /> 加入队列（遭遇会合并到每个章节的
        ///     遭遇池）。
        /// </summary>
        public ModContentPackBuilder GlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            return AddStep(ctx => ctx.Content.RegisterGlobalEncounter<TEncounter>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterMonster{TMonster}" /> (standalone monster type + patched
        ///     <c>ModelDb.Monsters</c> merge).
        ///     将 <see cref="ModContentRegistry.RegisterMonster{TMonster}" /> 加入队列（独立怪物类型 + 已修补的
        ///     <c>ModelDb.Monsters</c> 合并）。
        /// </summary>
        public ModContentPackBuilder Monster<TMonster>() where TMonster : MonsterModel
        {
            return AddStep(ctx => ctx.Content.RegisterMonster<TMonster>());
        }

        /// <summary>
        ///     Queues <c>RegisterCard&lt;TPool, TCard&gt;()</c> on the content registry with default public entry options.
        ///     将 <c>RegisterCard&lt;TPool, TCard&gt;()</c> 加入队列，在内容注册表上使用默认公共条目选项。
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>());
        }

        /// <summary>
        ///     Queues <c>RegisterCard&lt;TPool, TCard&gt;(ModelPublicEntryOptions)</c> on the content registry.
        ///     将内容注册表上的 <c>RegisterCard&lt;TPool, TCard&gt;(ModelPublicEntryOptions)</c> 加入队列。
        /// </summary>
        public ModContentPackBuilder Card<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCard<TPool, TCard>(publicEntry));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> for hand gold/red highlight rules.
        ///     将 <see cref="ModContentRegistry.RegisterCardHandGlow{TCard}" /> 加入队列，用于手牌金色/红色高亮规则。
        /// </summary>
        public ModContentPackBuilder CardHandGlow<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandGlow<TCard>(rules));
        }

        /// <summary>
        ///     Queues <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;(...)</c> for custom hand-highlight colors.
        ///     将 <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;(...)</c> 加入队列，用于自定义手牌高亮颜色。
        /// </summary>
        public ModContentPackBuilder CardHandOutline<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            return AddStep(ctx => ctx.Content.RegisterCardHandOutline<TCard>(rule));
        }

        /// <summary>
        ///     Registers a generated placeholder card (no custom CLR type). Prefer this for quick WIP flow.
        ///     注册生成的占位卡牌（无自定义 CLR 类型）。适合快速 WIP 流程。
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool>(string stableEntryStem,
            PlaceholderCardDescriptor descriptor = default)
            where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderCard<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a card with a stable public entry stem when you already have a concrete card type.
        ///     已有具体卡牌类型时，用稳定公共 entry stem 注册卡牌。
        /// </summary>
        public ModContentPackBuilder PlaceholderCard<TPool, TCard>(string stableEntryStem)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            return Card<TPool, TCard>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <c>RegisterRelic&lt;TPool, TRelic&gt;()</c> with default public entry options.
        ///     将 <c>RegisterRelic&lt;TPool, TRelic&gt;()</c> 加入队列，并使用默认公共条目选项。
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>());
        }

        /// <summary>
        ///     Queues <c>RegisterRelic&lt;TPool, TRelic&gt;(ModelPublicEntryOptions)</c>.
        ///     将 <c>RegisterRelic&lt;TPool, TRelic&gt;(ModelPublicEntryOptions)</c> 加入队列。
        /// </summary>
        public ModContentPackBuilder Relic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return AddStep(ctx => ctx.Content.RegisterRelic<TPool, TRelic>(publicEntry));
        }

        /// <summary>
        ///     Queues placeholder relic emission via <c>RegisterPlaceholderRelic&lt;TPool&gt;(...)</c>.
        ///     通过 <c>RegisterPlaceholderRelic&lt;TPool&gt;(...)</c> 将占位遗物生成加入队列。
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool>(string stableEntryStem,
            PlaceholderRelicDescriptor descriptor = default)
            where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderRelic<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a relic type using a stable entry stem mapped through <see cref="ModelPublicEntryOptions.FromStem" />.
        ///     使用通过 <see cref="ModelPublicEntryOptions.FromStem" /> 映射的稳定 entry stem 注册遗物类型。
        /// </summary>
        public ModContentPackBuilder PlaceholderRelic<TPool, TRelic>(string stableEntryStem)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            return Relic<TPool, TRelic>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <c>RegisterPotion&lt;TPool, TPotion&gt;()</c> with default public entry options.
        ///     将 <c>RegisterPotion&lt;TPool, TPotion&gt;()</c> 加入队列，并使用默认公共条目选项。
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>());
        }

        /// <summary>
        ///     Queues <c>RegisterPotion&lt;TPool, TPotion&gt;(ModelPublicEntryOptions)</c>.
        ///     将 <c>RegisterPotion&lt;TPool, TPotion&gt;(ModelPublicEntryOptions)</c> 加入队列。
        /// </summary>
        public ModContentPackBuilder Potion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return AddStep(ctx => ctx.Content.RegisterPotion<TPool, TPotion>(publicEntry));
        }

        /// <summary>
        ///     Queues placeholder potion emission via <c>RegisterPlaceholderPotion&lt;TPool&gt;(...)</c>.
        ///     通过 <c>RegisterPlaceholderPotion&lt;TPool&gt;(...)</c> 将占位药水生成加入队列。
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool>(string stableEntryStem,
            PlaceholderPotionDescriptor descriptor = default)
            where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterPlaceholderPotion<TPool>(stableEntryStem, descriptor));
        }

        /// <summary>
        ///     Registers a potion type using a stable entry stem mapped through <see cref="ModelPublicEntryOptions.FromStem" />.
        ///     使用通过 <see cref="ModelPublicEntryOptions.FromStem" /> 映射的稳定 entry stem 注册药水类型。
        /// </summary>
        public ModContentPackBuilder PlaceholderPotion<TPool, TPotion>(string stableEntryStem)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            return Potion<TPool, TPotion>(ModelPublicEntryOptions.FromStem(stableEntryStem));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterPower{TPower}" />.
        ///     将 <see cref="ModContentRegistry.RegisterPower{TPower}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Power<TPower>() where TPower : PowerModel
        {
            return AddStep(ctx => ctx.Content.RegisterPower<TPower>());
        }

        /// <summary>
        ///     Queues <see cref="RitsuLibFramework.RegisterHealthBarForecast{TSource}" /> for a non-power forecast source.
        ///     将 <see cref="RitsuLibFramework.RegisterHealthBarForecast{TSource}" /> 加入队列，用于非能力的预测来源。
        /// </summary>
        public ModContentPackBuilder HealthBarForecast<TSource>(string? sourceId = null)
            where TSource : IHealthBarForecastSource, new()
        {
            return AddStep(ctx => RitsuLibFramework.RegisterHealthBarForecast<TSource>(ctx.ModId, sourceId));
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterOrb{TOrb}" />.
        ///     将 <see cref="ModContentRegistry.RegisterOrb{TOrb}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Orb<TOrb>() where TOrb : OrbModel
        {
            return AddStep(ctx => ctx.Content.RegisterOrb<TOrb>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterEnchantment{TEnchantment}" />.
        ///     Queues <c>ModContentRegistry.RegisterEnchantment{TEnchantment}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterEnchantment{TEnchantment}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Enchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            return AddStep(ctx => ctx.Content.RegisterEnchantment<TEnchantment>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAffliction{TAffliction}" />.
        ///     Queues <c>ModContentRegistry.RegisterAffliction{TAffliction}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterAffliction{TAffliction}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Affliction<TAffliction>() where TAffliction : AfflictionModel
        {
            return AddStep(ctx => ctx.Content.RegisterAffliction<TAffliction>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAchievement{TAchievement}" />.
        ///     Queues <c>ModContentRegistry.RegisterAchievement{TAchievement}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterAchievement{TAchievement}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Achievement<TAchievement>() where TAchievement : AchievementModel
        {
            return AddStep(ctx => ctx.Content.RegisterAchievement<TAchievement>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSingleton{TSingleton}" />.
        ///     Queues <c>ModContentRegistry.RegisterSingleton{TSingleton}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterSingleton{TSingleton}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Singleton<TSingleton>() where TSingleton : SingletonModel
        {
            return AddStep(ctx => ctx.Content.RegisterSingleton<TSingleton>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterGoodModifier{TModifier}" />.
        ///     Queues <c>ModContentRegistry.RegisterGoodModifier{TModifier}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterGoodModifier{TModifier}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder GoodModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterGoodModifier<TModifier>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterBadModifier{TModifier}" />.
        ///     Queues <c>ModContentRegistry.RegisterBadModifier{TModifier}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterBadModifier{TModifier}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder BadModifier<TModifier>() where TModifier : ModifierModel
        {
            return AddStep(ctx => ctx.Content.RegisterBadModifier<TModifier>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedCardPool{TPool}" />.
        ///     Queues <c>ModContentRegistry.RegisterSharedCardPool{TPool}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterSharedCardPool{TPool}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SharedCardPool<TPool>() where TPool : CardPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedCardPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter{TPool}(string,string)" />.
        ///     将 <see cref="ModContentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter{TPool}(string,string)" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder CardLibraryCompendiumSharedPoolFilter<TPool>(string stableId,
            string iconTexturePath)
            where TPool : CardPoolModel
        {
            return AddStep(ctx =>
                ctx.Content.RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(stableId, iconTexturePath));
        }

        /// <summary>
        ///     Queues the <c>RegisterCardLibraryCompendiumSharedPoolFilter&lt;TPool&gt;</c> overload that accepts
        ///     placement rules.
        ///     将带 placementRules 的 <c>RegisterCardLibraryCompendiumSharedPoolFilter&lt;TPool&gt;</c> 加入队列。
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
        ///     Queues <see cref="ModContentRegistry.RegisterSharedRelicPool{TPool}" />.
        ///     将 <see cref="ModContentRegistry.RegisterSharedRelicPool{TPool}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedRelicPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedPotionPool{TPool}" />.
        ///     Queues <c>ModContentRegistry.RegisterSharedPotionPool{TPool}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterSharedPotionPool{TPool}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedPotionPool<TPool>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedEvent{TEvent}" />.
        ///     将 <see cref="ModContentRegistry.RegisterSharedEvent{TEvent}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SharedEvent<TEvent>() where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedEvent<TEvent>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActEvent{TAct,TEvent}" />.
        ///     将 <see cref="ModContentRegistry.RegisterActEvent{TAct,TEvent}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActEvent<TAct, TEvent>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterSharedAncient{TAncient}" />.
        ///     Queues <c>ModContentRegistry.RegisterSharedAncient{TAncient}</c>.
        ///     将 <see cref="ModContentRegistry.RegisterSharedAncient{TAncient}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterSharedAncient<TAncient>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterActAncient{TAct,TAncient}" />.
        ///     将 <see cref="ModContentRegistry.RegisterActAncient{TAct,TAncient}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterActAncient<TAct, TAncient>());
        }

        /// <summary>
        ///     Queues <see cref="ModContentRegistry.RegisterAncientOption{TAncient}" /> for injecting extra initial options.
        ///     将 <see cref="ModContentRegistry.RegisterAncientOption{TAncient}" /> 加入队列，用于注入额外初始选项。
        /// </summary>
        public ModContentPackBuilder AncientOption<TAncient>(ModAncientOptionRule rule)
            where TAncient : AncientEventModel
        {
            return AddStep(ctx => ctx.Content.RegisterAncientOption<TAncient>(rule));
        }

        /// <summary>
        ///     Queues <see cref="ModSmartFormatExtensionRegistry.Register{TFormatter}" />.
        ///     将 <see cref="ModSmartFormatExtensionRegistry.Register{TFormatter}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SmartFormatter<TFormatter>(int order = 0)
            where TFormatter : IFormatter, new()
        {
            return AddStep(ctx => ctx.SmartFormat.Register<TFormatter>(order));
        }

        /// <summary>
        ///     Queues <see cref="ModSmartFormatExtensionRegistry.RegisterFormatterType" />.
        ///     将 <see cref="ModSmartFormatExtensionRegistry.RegisterFormatterType" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SmartFormatter(Type formatterType, int order = 0)
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterFormatterType(formatterType, order));
        }

        /// <summary>
        ///     Queues <see cref="ModSmartFormatExtensionRegistry.RegisterSource{TSource}" />.
        ///     将 <see cref="ModSmartFormatExtensionRegistry.RegisterSource{TSource}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SmartFormatSource<TSource>(int order = 0)
            where TSource : ISource, new()
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterSource<TSource>(order));
        }

        /// <summary>
        ///     Queues <see cref="ModSmartFormatExtensionRegistry.RegisterSourceType" />.
        ///     将 <see cref="ModSmartFormatExtensionRegistry.RegisterSourceType" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder SmartFormatSource(Type sourceType, int order = 0)
        {
            return AddStep(ctx => ctx.SmartFormat.RegisterSourceType(sourceType, order));
        }

        /// <summary>
        ///     Queues <c>ModKeywordRegistry.RegisterCardKeywordOwnedByLocNamespace</c> (qualified id for both
        ///     keyword id and <c>card_keywords</c> <c>{id}.title</c> / <c>.description</c> keys).
        ///     将 <c>ModKeywordRegistry.RegisterCardKeywordOwnedByLocNamespace</c> 加入队列（用于关键字 id 和
        ///     <c>card_keywords</c> 的 <c>{id}.title</c> / <c>.description</c> 键的限定 id）。
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
        ///     Queues <c>ModKeywordRegistry.RegisterCardKeywordOwnedByLocNamespace</c> with legacy hover defaults.
        ///     将 <c>ModKeywordRegistry.RegisterCardKeywordOwnedByLocNamespace</c> 加入队列，并使用旧版悬停默认值。
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
        ///     Queues extended <see cref="ModKeywordRegistry" /> card-keyword registration (placement + hover-tip flags).
        ///     将扩展的 <see cref="ModKeywordRegistry" /> 卡牌关键字注册加入队列（位置 + 悬停提示标志）。
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
        ///     Legacy <c>CardKeyword</c> signature preserved for older mods; forwards with prior hover-tip behavior.
        ///     保留旧版 <c>CardKeyword</c> 签名以兼容旧 mod；按先前的悬停提示行为转发。
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
        ///     Queues <c>ModKeywordRegistry.RegisterOwned</c> (mod-local stem → qualified id).
        ///     将 <c>ModKeywordRegistry.RegisterOwned</c> (mod-local stem → qualified id) 加入队列。
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
        ///     将 <c>ModKeywordRegistry.RegisterOwned</c> 加入队列，并使用旧版悬停默认值。
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
        ///     将 extended <see cref="ModKeywordRegistry" /> keyword registration (placement + hover-tip flags) 加入队列。
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
        ///     保留旧版 <c>Keyword</c> 签名以兼容旧 mod；按先前的悬停提示行为转发。
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
        ///     将 <see cref="ModTimelineRegistry.RegisterEpoch{TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Epoch<TEpoch>() where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterEpoch<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" /> (epoch + story column order).
        ///     将 <see cref="ModTimelineRegistry.RegisterStoryEpoch{TStory, TEpoch}" /> (epoch + story column order) 加入队列。
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
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterTimelineSlot" /> 加入队列，用于不使用
        ///     <see cref="TimelineColumnPackEntry{TStory}" /> 的 <see cref="ModEpochTemplate" />。
        /// </summary>
        public ModContentPackBuilder ModEpochTimelineSlot<TEpoch>(EpochEra era, int eraPosition)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterTimelineSlot(typeof(TEpoch), era, eraPosition, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlot" /> for a <see cref="ModEpochTemplate" />.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlot" /> 加入队列，用于 <see cref="ModEpochTemplate" />。
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlot<TEpoch>(EpochEra era)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx => ModTimelineLayoutRegistry.RegisterAutoTimelineSlot(typeof(TEpoch), era, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn" />.
        ///     Queues <c>ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn</c>.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEraColumn" /> 加入队列。
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
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotAfterColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEraColumn(typeof(TEpoch), anchorEra, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn" />.
        ///     Queues <c>ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn</c>.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder ModEpochAutoTimelineSlotInColumn<TEpoch>(EpochEra anchorEra)
            where TEpoch : ModEpochTemplate
        {
            return AddStep(ctx =>
                ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEraColumn(typeof(TEpoch), anchorEra, ctx.ModId));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn" />.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotBeforeEpochColumn" /> 加入队列。
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
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn" />.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotAfterEpochColumn" /> 加入队列。
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
        ///     Queues <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn" />.
        ///     Queues <c>ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn</c>.
        ///     将 <see cref="ModTimelineLayoutRegistry.RegisterAutoTimelineSlotInEpochColumn" /> 加入队列。
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
        ///     Queues <see cref="TimelineColumnPackEntry{TStory}" /> — one fluent block for column order + per-epoch unlock
        ///     bindings (recommended over many separate pack entry types).
        ///     将 <see cref="TimelineColumnPackEntry{TStory}" /> 加入队列：用一个流式块处理列顺序 + 逐纪元解锁
        ///     绑定（推荐替代多个单独的包条目类型）。
        /// </summary>
        public ModContentPackBuilder TimelineColumn<TStory>(Action<TimelineColumnBuilder<TStory>> configure)
            where TStory : StoryModel, new()
        {
            ArgumentNullException.ThrowIfNull(configure);
            return PackEntry(new TimelineColumnPackEntry<TStory>(configure));
        }

        /// <summary>
        ///     Queues <see cref="ModTimelineRegistry.RegisterStory{TStory}" />.
        ///     将 <see cref="ModTimelineRegistry.RegisterStory{TStory}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder Story<TStory>() where TStory : StoryModel, new()
        {
            return AddStep(ctx => ctx.Timeline.RegisterStory<TStory>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.RequireEpoch{TModel,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.RequireEpoch{TModel,TEpoch}" /> 加入队列。
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
        ///     将 <see cref="BindCardUnlockEpochPackEntry{TEpoch}" /> 加入队列 - <typeparamref name="TEpoch" /> 上列出的每张卡牌出现在牌池前都需要该纪元。
        /// </summary>
        public ModContentPackBuilder BindCardUnlockEpoch<TEpoch>()
            where TEpoch : CardUnlockEpochTemplate, new()
        {
            return PackEntry(new BindCardUnlockEpochPackEntry<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="BindRelicUnlockEpochPackEntry{TEpoch}" /> — each relic listed on
        ///     <typeparamref name="TEpoch" /> requires that epoch before appearing in pools.
        ///     将 <see cref="BindRelicUnlockEpochPackEntry{TEpoch}" /> 加入队列 - <typeparamref name="TEpoch" /> 上列出的每个遗物出现在池中前都需要该纪元。
        /// </summary>
        public ModContentPackBuilder BindRelicUnlockEpoch<TEpoch>()
            where TEpoch : RelicUnlockEpochTemplate, new()
        {
            return PackEntry(new BindRelicUnlockEpochPackEntry<TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterRunAs{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterWinAs{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterWinAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterWinAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionWin{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(int ascensionLevel)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(ascensionLevel));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterRunCount{TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterRunCount<TEpoch>(int requiredRuns, bool requireVictory = false)
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterEliteVictories{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(int requiredEliteWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(requiredEliteWins));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterBossVictories{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterBossVictories<TCharacter, TEpoch>(int requiredBossWins = 15)
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(requiredBossWins));
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockEpochAfterAscensionOneWin{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.RevealAscensionAfterEpoch{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder RevealAscensionAfterEpoch<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.RevealAscensionAfterEpoch<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Queues <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter,TEpoch}" />.
        ///     将 <see cref="ModUnlockRegistry.UnlockCharacterAfterRunAs{TCharacter,TEpoch}" /> 加入队列。
        /// </summary>
        public ModContentPackBuilder UnlockCharacterAfterRunAs<TCharacter, TEpoch>()
            where TCharacter : CharacterModel
            where TEpoch : EpochModel, new()
        {
            return AddStep(ctx => ctx.Unlocks.UnlockCharacterAfterRunAs<TCharacter, TEpoch>());
        }

        /// <summary>
        ///     Appends a manifest <see cref="IContentRegistrationEntry" /> step.
        ///     追加 manifest <see cref="IContentRegistrationEntry" /> 步骤。
        /// </summary>
        public ModContentPackBuilder Entry(IContentRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Content));
        }

        /// <summary>
        ///     Appends each content registration entry in order.
        ///     按顺序追加每个内容注册条目。
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
        ///     追加类型化 <see cref="KeywordRegistrationEntry" /> 注册步骤。
        /// </summary>
        public ModContentPackBuilder Keyword(KeywordRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.Keywords));
        }

        /// <summary>
        ///     Appends each keyword registration entry in order.
        ///     按顺序追加每个关键词注册条目。
        /// </summary>
        public ModContentPackBuilder Keywords(IEnumerable<KeywordRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                Keyword(entry);

            return this;
        }

        /// <summary>
        ///     Queues <see cref="ModCardTagRegistry.RegisterOwned" /> for a local stem under this pack’s mod id.
        ///     将 <see cref="ModCardTagRegistry.RegisterOwned" /> 加入队列，用于此包 mod id 下的本地 stem。
        /// </summary>
        public ModContentPackBuilder CardTagOwned(string localTagStem)
        {
            return AddStep(ctx => ctx.CardTags.RegisterOwned(localTagStem));
        }

        /// <summary>
        ///     Appends a <see cref="CardTagRegistrationEntry" /> registration step.
        ///     追加 <see cref="CardTagRegistrationEntry" /> 注册步骤。
        /// </summary>
        public ModContentPackBuilder CardTag(CardTagRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.CardTags));
        }

        /// <summary>
        ///     Appends each card-tag registration entry in order.
        ///     按顺序追加每个卡牌标签注册条目。
        /// </summary>
        public ModContentPackBuilder CardTags(IEnumerable<CardTagRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                CardTag(entry);

            return this;
        }

        /// <summary>
        ///     Queues <see cref="ModCardPileRegistry.RegisterOwned" /> for a local stem under this pack’s mod id.
        ///     将 <see cref="ModCardPileRegistry.RegisterOwned" /> 加入队列，用于此包 mod id 下的本地 stem。
        /// </summary>
        public ModContentPackBuilder CardPileOwned(string localPileStem, ModCardPileSpec? spec = null)
        {
            return AddStep(ctx => ctx.CardPiles.RegisterOwned(localPileStem, spec ?? new ModCardPileSpec()));
        }

        /// <summary>
        ///     Appends a <see cref="CardPileRegistrationEntry" /> registration step.
        ///     追加 <see cref="CardPileRegistrationEntry" /> 注册步骤。
        /// </summary>
        public ModContentPackBuilder CardPile(CardPileRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.CardPiles));
        }

        /// <summary>
        ///     Queues <see cref="ModCardPileRegistry.Register" /> for a raw global id.
        ///     将 <see cref="ModCardPileRegistry.Register" /> 加入队列，用于原始全局 id。
        /// </summary>
        public ModContentPackBuilder CardPile(string id, ModCardPileSpec spec)
        {
            return AddStep(ctx => ctx.CardPiles.Register(id, spec));
        }

        /// <summary>
        ///     Appends each card-pile registration entry in order.
        ///     按顺序追加每个卡牌牌堆注册条目。
        /// </summary>
        public ModContentPackBuilder CardPiles(IEnumerable<CardPileRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                CardPile(entry);

            return this;
        }

        /// <summary>
        ///     Queues <see cref="ModTopBarButtonRegistry.RegisterOwned" /> for a local stem under this pack’s mod id.
        ///     将 <see cref="ModTopBarButtonRegistry.RegisterOwned" /> 加入队列，用于此包 mod id 下的本地 stem。
        /// </summary>
        public ModContentPackBuilder TopBarButtonOwned(string localButtonStem, ModTopBarButtonSpec spec)
        {
            return AddStep(ctx => ctx.TopBarButtons.RegisterOwned(localButtonStem, spec));
        }

        /// <summary>
        ///     Appends a <see cref="TopBarButtonRegistrationEntry" /> registration step.
        ///     追加 <see cref="TopBarButtonRegistrationEntry" /> 注册步骤。
        /// </summary>
        public ModContentPackBuilder TopBarButton(TopBarButtonRegistrationEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(ctx => entry.Register(ctx.TopBarButtons));
        }

        /// <summary>
        ///     Queues <see cref="ModTopBarButtonRegistry.Register" /> for a raw global id.
        ///     将 <see cref="ModTopBarButtonRegistry.Register" /> 加入队列，用于原始全局 id。
        /// </summary>
        public ModContentPackBuilder TopBarButton(string id, ModTopBarButtonSpec spec)
        {
            return AddStep(ctx => ctx.TopBarButtons.Register(id, spec));
        }

        /// <summary>
        ///     Appends each top-bar-button registration entry in order.
        ///     按顺序追加每个顶部栏按钮注册条目。
        /// </summary>
        public ModContentPackBuilder TopBarButtons(IEnumerable<TopBarButtonRegistrationEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var entry in entries)
                TopBarButton(entry);

            return this;
        }

        /// <summary>
        ///     Registers <see cref="ModContentRegistry" /> entries (character, cards, relics, powers, …).
        ///     注册 <see cref="ModContentRegistry" /> 条目（角色、卡牌、遗物、能力等）。
        /// </summary>
        public ModContentPackBuilder ContentManifest(IEnumerable<IContentRegistrationEntry>? entries)
        {
            return entries != null ? Entries(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModKeywordRegistry" /> entries (separate from ModelDb content).
        ///     注册 <see cref="ModKeywordRegistry" /> 条目（与 ModelDb 内容分开）。
        /// </summary>
        public ModContentPackBuilder KeywordManifest(IEnumerable<KeywordRegistrationEntry>? entries)
        {
            return entries != null ? Keywords(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModCardTagRegistry" /> entries (custom <c>CardTag</c> ids separate from ModelDb).
        ///     注册 <see cref="ModCardTagRegistry" /> 条目（与 ModelDb 分开的自定义 <c>CardTag</c> id）。
        /// </summary>
        public ModContentPackBuilder CardTagManifest(IEnumerable<CardTagRegistrationEntry>? entries)
        {
            return entries != null ? CardTags(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModCardPileRegistry" /> entries (custom <c>CardPile</c> ids separate from ModelDb).
        ///     注册 <see cref="ModCardPileRegistry" /> 条目（与 ModelDb 分开的自定义 <c>CardPile</c> id）。
        /// </summary>
        public ModContentPackBuilder CardPileManifest(IEnumerable<CardPileRegistrationEntry>? entries)
        {
            return entries != null ? CardPiles(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModTopBarButtonRegistry" /> entries.
        ///     注册 <see cref="ModTopBarButtonRegistry" /> 条目。
        /// </summary>
        public ModContentPackBuilder TopBarButtonManifest(IEnumerable<TopBarButtonRegistrationEntry>? entries)
        {
            return entries != null ? TopBarButtons(entries) : this;
        }

        /// <summary>
        ///     Registers <see cref="ModTimelineRegistry" /> / <see cref="ModUnlockRegistry" /> via
        ///     <see cref="IModContentPackEntry" /> (story–epoch bindings, unlock rules). Usually applied after content so
        ///     <c>RequireEpoch</c> can resolve character ids.
        ///     通过 <see cref="IModContentPackEntry" /> 注册 <see cref="ModTimelineRegistry" /> / <see cref="ModUnlockRegistry" />
        ///     （story-纪元绑定、解锁规则）。通常在内容之后应用，使 <c>RequireEpoch</c> 可以解析角色 id。
        /// </summary>
        public ModContentPackBuilder PackManifest(IEnumerable<IModContentPackEntry>? entries)
        {
            return PackEntries(entries);
        }

        /// <summary>
        ///     Convenience batch for optional content and keyword manifest enumerables.
        ///     用于可选内容和关键词 manifest 可枚举项的便捷批处理。
        /// </summary>
        /// <remarks>
        ///     <see cref="IContentRegistrationEntry" /> may include
        ///     <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />,
        ///     <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />, and related Orobas
        ///     entries alongside cards/relics/etc. Keywords use a different registry; prefer
        ///     <see cref="ContentManifest" /> / <see cref="KeywordManifest" /> / <see cref="PackManifest" /> when you want
        ///     that split to be explicit.
        ///     <see cref="IContentRegistrationEntry" /> 可以包含
        ///     <see cref="ArchaicToothTranscendenceRegistrationEntry{TStarterCard,TAncientCard}" />、
        ///     <see cref="TouchOfOrobasRefinementRegistrationEntry{TStarterRelic,TUpgradedRelic}" />，以及与卡牌 / 遗物等并列的相关 Orobas
        ///     条目。关键词使用不同的注册表；当你希望这种拆分显式可见时，优先使用 <see cref="ContentManifest" /> / <see cref="KeywordManifest" /> /
        ///     <see cref="PackManifest" />。
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
        ///     包含可选 <see cref="IModContentPackEntry" /> 步骤（时间线绑定、解锁）的便捷批处理。
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
        ///     追加 <see cref="IModContentPackEntry" />（时间线 / 解锁 / 其它包表面）。
        /// </summary>
        public ModContentPackBuilder PackEntry(IModContentPackEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return AddStep(entry.Apply);
        }

        /// <summary>
        ///     Appends each <see cref="IModContentPackEntry" /> in order.
        ///     按顺序追加每个 <see cref="IModContentPackEntry" />。
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
        ///     将 <see cref="RitsuLibFramework.RegisterArchaicToothTranscendenceMapping{TStarterCard,TAncientCard}" /> 加入队列，使用此包的
        ///     <see cref="ModContentPackContext.ModId" />。
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
        ///     按初始卡牌 id 和远古卡牌类型将 ArchaicTooth 超越注册加入队列，使用此包的 mod id。
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
        ///     将 <see cref="RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping{TStarterRelic,TUpgradedRelic}" /> 加入队列，使用此包的
        ///     mod id。
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
        ///     按初始遗物 id 和升级遗物类型将 TouchOfOrobas 精炼注册加入队列，使用此包的 mod id。
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
        ///     追加一个在 <see cref="Apply" /> 期间执行的任意 delegate。
        /// </summary>
        public ModContentPackBuilder Custom(Action<ModContentPackContext> step)
        {
            return AddStep(step);
        }

        /// <summary>
        ///     Materializes registries for the builder’s mod id without running queued steps.
        ///     为构建器的 mod id 实例化注册表，但不运行已排队步骤。
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
        ///     Schedules all queued registration steps to apply during the framework discovery window and returns the
        ///     materialized context for this mod id.
        ///     安排所有已排队注册步骤在框架发现窗口期间应用，并返回此 mod id 的实体化上下文。
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
