using System.Reflection;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Content
{
    /// <summary>
    ///     Whether <see cref="ModContentRegistry" /> still accepts new registrations from mods.
    ///     <see cref="ModContentRegistry" /> 是否仍接受来自 mod 的新注册。
    /// </summary>
    public enum ContentRegistrationState
    {
        /// <summary>
        ///     Registrations are allowed until the framework freezes them.
        ///     在框架冻结注册之前允许继续注册。
        /// </summary>
        Open = 0,

        /// <summary>
        ///     Further registration throws; game content lists are considered sealed.
        ///     继续注册会抛出异常；游戏内容列表视为已封闭。
        /// </summary>
        Frozen = 1,
    }

    /// <summary>
    ///     Per-mod content registration surface: pool models, standalone models, act-scoped content, and stable public
    ///     entry overrides used by patched <see cref="ModelDb" /> identity.
    ///     每个 mod 的内容注册表面：池模型、独立模型、章节作用域内容，以及已修补 <see cref="ModelDb" />
    ///     身份使用的稳定公共条目覆盖。
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
        private static readonly HashSet<Type> RegisteredModelCapabilities = [];
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
        private static readonly HashSet<Type> RegisteredBadges = [];
        private static readonly HashSet<Type> RegisteredSharedRelicPools = [];
        private static readonly HashSet<Type> RegisteredSharedPotionPools = [];
        private static readonly List<ModifierRegistration> RegisteredGoodModifiers = [];
        private static readonly List<ModifierRegistration> RegisteredBadModifiers = [];
        private static readonly List<HashSet<Type>> RegisteredMutuallyExclusiveModifierGroups = [];
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
        ///     创建此注册表实例时使用的 mod 标识符（<see cref="For" />）。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     True after <c>FreezeRegistrations</c> has run globally.
        ///     <c>FreezeRegistrations</c> 全局运行后为 true。
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     Convenience view of <see cref="IsFrozen" /> as <see cref="ContentRegistrationState" />.
        ///     将 <see cref="IsFrozen" /> 以 <see cref="ContentRegistrationState" /> 形式查看的便捷视图。
        /// </summary>
        public static ContentRegistrationState State => IsFrozen
            ? ContentRegistrationState.Frozen
            : ContentRegistrationState.Open;

        /// <summary>
        ///     Resolves which mod registered <paramref name="modelType" />, if any.
        ///     解析注册 <paramref name="modelType" /> 的 mod（如果有）。
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
        ///     返回 RitsuLib 注册模型类型的稳定公共条目字符串（覆盖或生成值）。
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
        ///     为 <paramref name="modId" /> 拥有的类型构建默认规范化条目
        ///     <c>MOD_CATEGORY_TYPENAME</c>。
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
        ///     Builds a stable three-segment compound id: <c>{normalizedModId}_{TYPE}_{normalizedName}</c>
        ///     (underscore-separated). Mod and name use <see cref="NormalizePublicStem" />; the type segment is only
        ///     trimmed then uppercased with <c>ToUpperInvariant</c> (no stem normalization).
        ///     构建稳定的三段复合 id：<c>{normalizedModId}_{TYPE}_{normalizedName}</c>
        ///     （以下划线分隔）。mod 和 name 使用 <see cref="NormalizePublicStem" />；type 段只
        ///     去除首尾空白后用 <c>ToUpperInvariant</c> 转大写（不做词干规范化）。
        /// </summary>
        public static string GetCompoundId(string modId, string typeStem, string nameStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(nameStem);
            ArgumentNullException.ThrowIfNull(typeStem);

            var trimmedType = typeStem.Trim();
            if (trimmedType.Length == 0)
                throw new ArgumentException("Type segment cannot be empty or whitespace.", nameof(typeStem));

            var mod = NormalizePublicStem(modId);
            var type = trimmedType.ToUpperInvariant();
            var name = NormalizePublicStem(nameStem);
            return $"{mod}_{type}_{name}";
        }

        /// <summary>
        ///     Builds a mod-scoped keyword id: <c>{normalizedModId}_KEYWORD_{normalizedStem}</c>, matching the
        ///     three-segment convention used by <see cref="GetQualifiedCardPileId" /> and
        ///     <see cref="GetQualifiedTopBarButtonId" /> (all uppercase). Other mods can reference a provider’s keyword
        ///     by passing the same <paramref name="modId" /> and <paramref name="localKeywordStem" />.
        ///     构建 mod 作用域的关键词 id：<c>{normalizedModId}_KEYWORD_{normalizedStem}</c>，匹配
        ///     <see cref="GetQualifiedCardPileId" /> 和
        ///     <see cref="GetQualifiedTopBarButtonId" /> 使用的三段约定（全部大写）。其他 mod 可通过传入相同的
        ///     <paramref name="modId" /> 和 <paramref name="localKeywordStem" /> 引用提供者的关键词。
        /// </summary>
        public static string GetQualifiedKeywordId(string modId, string localKeywordStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            return GetCompoundId(modId, "KEYWORD", localKeywordStem);
        }

        /// <summary>
        ///     Builds a mod-scoped card-pile id using the ritsulib <c>MODID_CATEGORY_TYPENAME</c> public-entry
        ///     convention — three uppercase segments separated by underscores, aligning with
        ///     <see cref="GetFixedPublicEntry(string, Type)" /> and the vanilla <c>static_hover_tips</c> key
        ///     style (<c>DRAW_PILE</c>, <c>EXHAUST_PILE</c>, ...).
        ///     使用 ritsulib <c>MODID_CATEGORY_TYPENAME</c> 公共条目约定构建 mod 作用域的牌堆 id
        ///     -- 三个大写段以下划线分隔，与
        ///     <see cref="GetFixedPublicEntry(string, Type)" /> 和原版 <c>static_hover_tips</c> 键
        ///     风格（<c>DRAW_PILE</c>、<c>EXHAUST_PILE</c> 等）对齐。
        /// </summary>
        /// <remarks>
        ///     The returned string is the stem for <c>static_hover_tips.json</c> keys, so a pile registered by
        ///     mod <c>com.example.my-mod</c> with local stem <c>overflow_pile</c> uses id
        ///     <c>MYMOD_CARDPILE_OVERFLOW_PILE</c> and loc keys <c>MYMOD_CARDPILE_OVERFLOW_PILE.title</c> /
        ///     <c>.description</c> / <c>.empty</c>.
        ///     <c>.description</c> / <c>.empty</c>。
        ///     返回的字符串是 <c>static_hover_tips.json</c> 键的词干，因此由
        ///     mod <c>com.example.my-mod</c> 以本地词干 <c>overflow_pile</c> 注册的牌堆会使用 id
        ///     <c>MYMOD_CARDPILE_OVERFLOW_PILE</c>，并使用本地化键 <c>MYMOD_CARDPILE_OVERFLOW_PILE.title</c>、
        ///     <c>.description</c>、<c>.empty</c>。
        ///     <c>.description</c>、<c>.empty</c>。
        /// </remarks>
        public static string GetQualifiedCardPileId(string modId, string localPileStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localPileStem);

            return GetCompoundId(modId, "CARDPILE", localPileStem);
        }

        /// <summary>
        ///     Builds a mod-scoped <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> id using the ritsulib
        ///     <c>MODID_CATEGORY_TYPENAME</c> convention with middle segment <c>CARDTAG</c>, aligned with
        ///     <see cref="GetQualifiedKeywordId" /> and <see cref="GetQualifiedCardPileId" />.
        ///     使用 ritsulib <c>MODID_CATEGORY_TYPENAME</c> 约定构建 mod 作用域的 <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" />
        ///     id，
        ///     中间段为 <c>CARDTAG</c>，并与
        ///     <see cref="GetQualifiedKeywordId" /> 和 <see cref="GetQualifiedCardPileId" /> 对齐。
        /// </summary>
        public static string GetQualifiedCardTagId(string modId, string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            return GetCompoundId(modId, "CARDTAG", localTagStem);
        }

        /// <summary>
        ///     Builds a mod-scoped reward id using the ritsulib <c>MODID_CATEGORY_TYPENAME</c> convention
        ///     with middle segment <c>REWARD</c>.
        ///     使用 ritsulib <c>MODID_CATEGORY_TYPENAME</c> 约定构建 mod 作用域奖励 id，
        ///     中间段为 <c>REWARD</c>。
        /// </summary>
        public static string GetQualifiedRewardId(string modId, string localRewardStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localRewardStem);

            return GetCompoundId(modId, "REWARD", localRewardStem);
        }

        /// <summary>
        ///     Builds a mod-scoped <see cref="MegaCrit.Sts2.Core.Entities.Cards.TargetType" /> id using the ritsulib
        ///     three-segment convention with middle segment <c>TARGETTYPE</c>.
        ///     使用 ritsulib 三段式约定构建 mod 作用域的
        ///     <see cref="MegaCrit.Sts2.Core.Entities.Cards.TargetType" /> ID，中间段为 <c>TARGETTYPE</c>。
        /// </summary>
        public static string GetQualifiedTargetTypeId(string modId, string localTargetTypeStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTargetTypeStem);

            return GetCompoundId(modId, "TARGETTYPE", localTargetTypeStem);
        }

        /// <summary>
        ///     Builds a mod-scoped model-capability id using the ritsulib three-segment convention with middle segment
        ///     <c>MODELCAPABILITY</c>.
        ///     使用 ritsulib 三段式约定构建 mod 作用域的模型能力 ID，中间段为 <c>MODELCAPABILITY</c>。
        /// </summary>
        public static string GetQualifiedModelCapabilityId(string modId, string localCapabilityStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localCapabilityStem);

            return GetCompoundId(modId, "MODELCAPABILITY", localCapabilityStem);
        }

        /// <summary>
        ///     Builds a mod-scoped top-bar-button id in the ritsulib <c>MODID_CATEGORY_TYPENAME</c> public
        ///     entry style (uppercase, three segments, underscore-separated, middle segment fixed to
        ///     <c>TOPBARBUTTON</c>). Used by <see cref="STS2RitsuLib.TopBar.ModTopBarButtonRegistry" />; the
        ///     returned string is the stem for <c>static_hover_tips.json</c> title / description keys.
        ///     以 ritsulib <c>MODID_CATEGORY_TYPENAME</c> 公共条目风格构建 mod 作用域的顶部栏按钮 id
        ///     （大写、三段、以下划线分隔，中间段固定为
        ///     <c>TOPBARBUTTON</c>）。由 <see cref="STS2RitsuLib.TopBar.ModTopBarButtonRegistry" /> 使用；
        ///     返回的字符串是 <c>static_hover_tips.json</c> 标题/描述键的词干。
        /// </summary>
        public static string GetQualifiedTopBarButtonId(string modId, string localButtonStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localButtonStem);

            return GetCompoundId(modId, "TOPBARBUTTON", localButtonStem);
        }

        /// <summary>
        ///     Builds a mod-scoped right-click binding id using the ritsulib <c>MODID_CATEGORY_TYPENAME</c>
        ///     convention with middle segment <c>RIGHTCLICK</c>.
        ///     使用 ritsulib <c>MODID_CATEGORY_TYPENAME</c> 约定构建 mod 作用域右键绑定 id，
        ///     中间段为 <c>RIGHTCLICK</c>。
        /// </summary>
        public static string GetQualifiedRightClickId(string modId, string localRightClickStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localRightClickStem);

            return GetCompoundId(modId, "RIGHTCLICK", localRightClickStem);
        }

        /// <summary>
        ///     Returns the singleton registry for <paramref name="modId" /> (created on first use).
        ///     返回 <paramref name="modId" /> 的单例注册表（首次使用时创建）。
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
        ///     使用默认公共条目命名，将 <typeparamref name="TCard" /> 注册到 <typeparamref name="TPool" />。
        /// </summary>
        public void RegisterCard<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterCard(typeof(TPool), typeof(TCard));
        }

        /// <summary>
        ///     Registers <paramref name="cardType" /> into <paramref name="poolType" /> with default public entry naming.
        ///     使用默认公共条目命名，将 <paramref name="cardType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType)
        {
            RegisterCard(poolType, cardType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TCard" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        ///     使用 <paramref name="publicEntry" /> 规则，将 <typeparamref name="TCard" /> 注册到 <typeparamref name="TPool" />。
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
        ///     使用 <paramref name="publicEntry" /> 规则，将 <paramref name="cardType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, cardType, "card", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        ///     使用默认公共条目命名，将 <typeparamref name="TRelic" /> 注册到 <typeparamref name="TPool" />。
        /// </summary>
        public void RegisterRelic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic(typeof(TPool), typeof(TRelic));
        }

        /// <summary>
        ///     Registers <paramref name="relicType" /> into <paramref name="poolType" /> with default public entry naming.
        ///     使用默认公共条目命名，将 <paramref name="relicType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType)
        {
            RegisterRelic(poolType, relicType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TRelic" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        ///     使用 <paramref name="publicEntry" /> 规则，将 <typeparamref name="TRelic" /> 注册到 <typeparamref name="TPool" />。
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
        ///     使用 <paramref name="publicEntry" /> 规则，将 <paramref name="relicType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, relicType, "relic", publicEntry);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> with default public entry
        ///     naming.
        ///     使用默认公共条目命名，将 <typeparamref name="TPotion" /> 注册到 <typeparamref name="TPool" />。
        /// </summary>
        public void RegisterPotion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion(typeof(TPool), typeof(TPotion));
        }

        /// <summary>
        ///     Registers <paramref name="potionType" /> into <paramref name="poolType" /> with default public entry naming.
        ///     使用默认公共条目命名，将 <paramref name="potionType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType)
        {
            RegisterPotion(poolType, potionType, default);
        }

        /// <summary>
        ///     Registers <typeparamref name="TPotion" /> into <typeparamref name="TPool" /> using
        ///     <paramref name="publicEntry" /> rules.
        ///     使用 <paramref name="publicEntry" /> 规则，将 <typeparamref name="TPotion" /> 注册到 <typeparamref name="TPool" />。
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
        ///     使用 <paramref name="publicEntry" /> 规则，将 <paramref name="potionType" /> 注册到 <paramref name="poolType" />。
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, potionType, "potion", publicEntry);
        }

        /// <summary>
        ///     Registers a mod character model for inclusion in <see cref="ModelDb.AllCharacters" />.
        ///     注册 mod 角色模型，以纳入 <see cref="ModelDb.AllCharacters" />。
        /// </summary>
        public void RegisterCharacter<TCharacter>() where TCharacter : CharacterModel
        {
            RegisterCharacter(typeof(TCharacter));
        }

        /// <summary>
        ///     Registers <paramref name="characterType" /> for inclusion in <see cref="ModelDb.AllCharacters" />.
        ///     注册 <paramref name="characterType" />，以纳入 <see cref="ModelDb.AllCharacters" />。
        /// </summary>
        public void RegisterCharacter(Type characterType)
        {
            RegisterStandaloneModel(RegisteredCharacters, characterType, typeof(CharacterModel), "character");
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <typeparamref name="TCard" /> for <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TCard" /> 初始牌组副本。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterCard<TCharacter, TCard>(int count = 1)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterStarterCard<TCharacter, TCard>(count, 0);
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <typeparamref name="TCard" /> for <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TCard" /> 初始牌组副本。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterCard<TCharacter, TCard>(int count, int order)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterStarterCard(typeof(TCharacter), typeof(TCard), count, order);
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <paramref name="cardType" /> for <paramref name="characterType" />.
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="cardType" /> 初始牌组副本。
        /// </summary>
        public void RegisterCharacterStarterCard(Type characterType, Type cardType, int count = 1)
        {
            RegisterCharacterStarterCard(characterType, cardType, count, 0);
        }

        /// <summary>
        ///     Registers additional starter-deck copies of <paramref name="cardType" /> for <paramref name="characterType" />.
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="cardType" /> 初始牌组副本。
        /// </summary>
        public void RegisterCharacterStarterCard(Type characterType, Type cardType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, cardType, typeof(CardModel),
                CharacterStarterContentKind.Card,
                count, order);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <typeparamref name="TRelic" /> for <typeparamref name="TCharacter" />
        ///     .
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TRelic" /> 初始遗物副本
        ///     。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterRelic<TCharacter, TRelic>(int count = 1)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterStarterRelic<TCharacter, TRelic>(count, 0);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <typeparamref name="TRelic" /> for <typeparamref name="TCharacter" />
        ///     .
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TRelic" /> 初始遗物副本
        ///     。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterRelic<TCharacter, TRelic>(int count, int order)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterStarterRelic(typeof(TCharacter), typeof(TRelic), count, order);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <paramref name="relicType" /> for <paramref name="characterType" />.
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="relicType" /> 初始遗物副本
        ///     。
        /// </summary>
        public void RegisterCharacterStarterRelic(Type characterType, Type relicType, int count = 1)
        {
            RegisterCharacterStarterRelic(characterType, relicType, count, 0);
        }

        /// <summary>
        ///     Registers additional starting relic copies of <paramref name="relicType" /> for <paramref name="characterType" />.
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="relicType" /> 初始遗物副本
        ///     。
        /// </summary>
        public void RegisterCharacterStarterRelic(Type characterType, Type relicType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, relicType, typeof(RelicModel),
                CharacterStarterContentKind.Relic, count, order);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <typeparamref name="TPotion" /> for
        ///     <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TPotion" /> 初始药水副本。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterPotion<TCharacter, TPotion>(int count = 1)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterStarterPotion<TCharacter, TPotion>(count, 0);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <typeparamref name="TPotion" /> for
        ///     <typeparamref name="TCharacter" />.
        ///     The target character may be registered before or after this call; resolution happens when the character model is
        ///     queried. Matching uses the live instance CLR type; registrations against an assignable ancestor type also apply,
        ///     except a registration keyed only to <see cref="CharacterModel" /> itself.
        ///     为 <typeparamref name="TCharacter" /> 注册额外的 <typeparamref name="TPotion" /> 初始药水副本。
        ///     目标角色可以在此调用之前或之后注册；解析会在查询角色模型时发生。
        ///     匹配使用实时实例 CLR 类型；针对可赋值祖先类型的注册也会应用，
        ///     但仅以 <see cref="CharacterModel" /> 本身为键的注册除外。
        /// </summary>
        public void RegisterCharacterStarterPotion<TCharacter, TPotion>(int count, int order)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterStarterPotion(typeof(TCharacter), typeof(TPotion), count, order);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <paramref name="potionType" /> for <paramref name="characterType" />
        ///     .
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="potionType" /> 初始药水副本
        ///     。
        /// </summary>
        public void RegisterCharacterStarterPotion(Type characterType, Type potionType, int count = 1)
        {
            RegisterCharacterStarterPotion(characterType, potionType, count, 0);
        }

        /// <summary>
        ///     Registers additional starting potion copies of <paramref name="potionType" /> for <paramref name="characterType" />
        ///     .
        ///     为 <paramref name="characterType" /> 注册额外的 <paramref name="potionType" /> 初始药水副本
        ///     。
        /// </summary>
        public void RegisterCharacterStarterPotion(Type characterType, Type potionType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, potionType, typeof(PotionModel),
                CharacterStarterContentKind.Potion, count, order);
        }

        /// <summary>
        ///     Registers a mod act model for inclusion in <see cref="ModelDb.Acts" />.
        ///     注册 mod 章节模型，以纳入 <see cref="ModelDb.Acts" />。
        /// </summary>
        public void RegisterAct<TAct>() where TAct : ActModel
        {
            RegisterAct(typeof(TAct));
        }

        /// <summary>
        ///     Registers <paramref name="actType" /> for inclusion in <see cref="ModelDb.Acts" />.
        ///     注册 <paramref name="actType" />，以纳入 <see cref="ModelDb.Acts" />。
        /// </summary>
        public void RegisterAct(Type actType)
        {
            RegisterStandaloneModel(RegisteredActs, actType, typeof(ActModel), "act");
        }

        /// <summary>
        ///     Registers a mod monster model type for RitsuLib tracking, <see cref="ModelDb" /> identity, dynamic injection, and
        ///     patched merge into <c>ModelDb.Monsters</c>.
        ///     注册 mod 怪物模型类型，用于 RitsuLib 跟踪、<see cref="ModelDb" /> 身份、动态注入，以及
        ///     修补后合并到 <c>ModelDb.Monsters</c>。
        ///     修补后合并到 <c>ModelDb.Monsters</c>。
        /// </summary>
        public void RegisterMonster<TMonster>() where TMonster : MonsterModel
        {
            RegisterMonster(typeof(TMonster));
        }

        /// <summary>
        ///     Registers <paramref name="monsterType" /> for RitsuLib tracking and patched monster injection.
        ///     注册 <paramref name="monsterType" />，用于 RitsuLib 跟踪和修补后的怪物注入。
        /// </summary>
        public void RegisterMonster(Type monsterType)
        {
            RegisterStandaloneModel(RegisteredMonsters, monsterType, typeof(MonsterModel), "monster");
        }

        /// <summary>
        ///     Registers a mod power model for inclusion in <see cref="ModelDb.AllPowers" />.
        ///     注册 mod 能力模型，以纳入 <see cref="ModelDb.AllPowers" />。
        /// </summary>
        public void RegisterPower<TPower>() where TPower : PowerModel
        {
            RegisterPower(typeof(TPower));
        }

        /// <summary>
        ///     Registers <paramref name="powerType" /> for inclusion in <see cref="ModelDb.AllPowers" />.
        ///     注册 <paramref name="powerType" />，以纳入 <see cref="ModelDb.AllPowers" />。
        /// </summary>
        public void RegisterPower(Type powerType)
        {
            RegisterStandaloneModel(RegisteredPowers, powerType, typeof(PowerModel), "power");
        }

        /// <summary>
        ///     Registers a mod orb model for inclusion in <see cref="ModelDb.Orbs" />.
        ///     注册 mod 充能球模型，以纳入 <see cref="ModelDb.Orbs" />。
        /// </summary>
        public void RegisterOrb<TOrb>() where TOrb : OrbModel
        {
            RegisterOrb(typeof(TOrb));
        }

        /// <summary>
        ///     Registers <paramref name="orbType" /> for inclusion in <see cref="ModelDb.Orbs" />.
        ///     注册 <paramref name="orbType" />，以纳入 <see cref="ModelDb.Orbs" />。
        /// </summary>
        public void RegisterOrb(Type orbType)
        {
            RegisterStandaloneModel(RegisteredOrbs, orbType, typeof(OrbModel), "orb");
        }

        /// <summary>
        ///     Registers a model-backed component for use with <see cref="ModelCapabilities" />.
        ///     注册一个基于模型的组件，供 <see cref="ModelCapabilities" /> 使用。
        /// </summary>
        public void RegisterModelCapability<TCapability>() where TCapability : ModelCapability
        {
            RegisterModelCapability<TCapability>(default);
        }

        /// <summary>
        ///     Registers a model-backed component using <paramref name="publicEntry" /> rules.
        ///     使用 <paramref name="publicEntry" /> 规则注册一个基于模型的组件。
        /// </summary>
        public void RegisterModelCapability<TCapability>(ModelPublicEntryOptions publicEntry)
            where TCapability : ModelCapability
        {
            RegisterModelCapability(typeof(TCapability), publicEntry);
        }

        /// <summary>
        ///     Registers <paramref name="capabilityType" /> as a model-backed component.
        ///     将 <paramref name="capabilityType" /> 注册为基于模型的组件。
        /// </summary>
        public void RegisterModelCapability(Type capabilityType)
        {
            RegisterModelCapability(capabilityType, default);
        }

        /// <summary>
        ///     Registers <paramref name="capabilityType" /> as a model-backed component using
        ///     <paramref name="publicEntry" /> rules.
        ///     使用 <paramref name="publicEntry" /> 规则将 <paramref name="capabilityType" /> 注册为基于模型的组件。
        /// </summary>
        public void RegisterModelCapability(Type capabilityType, ModelPublicEntryOptions publicEntry)
        {
            EnsureMutable($"register model capability '{capabilityType.Name}'");
            EnsureModelType(capabilityType, typeof(ModelCapability), nameof(capabilityType));
            PrimeOwnedType(capabilityType);
            ApplyFixedPublicEntryForModel(capabilityType, publicEntry);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(capabilityType);

            lock (SyncRoot)
            {
                if (!RegisteredModelCapabilities.Add(capabilityType))
                {
                    _logger.Debug(
                        $"[Content] Skipping duplicate model capability registration: {capabilityType.Name}");
                    return;
                }

                RememberOwner(capabilityType);
            }

            var capabilityId = ResolveModelCapabilityId(capabilityType, publicEntry);
            ModelCapabilityRegistry.RegisterModelCapability(capabilityType, capabilityId);
            _logger.Info($"[Content] Registered model capability: {capabilityType.Name} (id={capabilityId})");
        }

        /// <summary>
        ///     Configures the default capability set for matching <paramref name="modelType" /> instances.
        ///     配置匹配的 <paramref name="modelType" /> 实例的默认能力集合。
        /// </summary>
        public void ConfigureDefaultModelCapabilities(
            Type modelType,
            string modifierId,
            Action<AbstractModel, ModelCapabilityList> modifier,
            int order = 0)
        {
            EnsureMutable($"configure default model capabilities '{modelType.Name}/{modifierId}'");
            EnsureModelFamilyType(modelType, nameof(modelType));
            ModelCapabilityDefaults.Modify(ModId, modifierId, modelType, modifier, order);
            _logger.Info($"[Content] Registered default model capability modifier: {modelType.Name}/{modifierId}");
        }

        /// <summary>
        ///     Configures the default capability set for matching <typeparamref name="TModel" /> instances.
        ///     配置匹配的 <typeparamref name="TModel" /> 实例的默认能力集合。
        /// </summary>
        public void ConfigureDefaultModelCapabilities<TModel>(
            string modifierId,
            Action<TModel, ModelCapabilityList> modifier,
            int order = 0)
            where TModel : AbstractModel
        {
            EnsureMutable($"configure default model capabilities '{typeof(TModel).Name}/{modifierId}'");
            ModelCapabilityDefaults.Modify(ModId, modifierId, modifier, order);
            _logger.Info(
                $"[Content] Registered default model capability modifier: {typeof(TModel).Name}/{modifierId}");
        }

        /// <summary>
        ///     Registers a mod enchantment model for RitsuLib tracking, fixed <see cref="ModelDb" /> entry identity, dynamic
        ///     injection, and inclusion in patched <see cref="ModelDb.DebugEnchantments" />.
        ///     注册 mod 附魔模型，用于 RitsuLib 跟踪、固定 <see cref="ModelDb" /> 条目身份、动态
        ///     注入，并纳入修补后的 <see cref="ModelDb.DebugEnchantments" />。
        /// </summary>
        public void RegisterEnchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            RegisterEnchantment(typeof(TEnchantment));
        }

        /// <summary>
        ///     Registers <paramref name="enchantmentType" /> for patched enchantment injection.
        ///     注册 <paramref name="enchantmentType" />，用于修补后的附魔注入。
        /// </summary>
        public void RegisterEnchantment(Type enchantmentType)
        {
            RegisterStandaloneModel(RegisteredEnchantments, enchantmentType, typeof(EnchantmentModel),
                "enchantment");
        }

        /// <summary>
        ///     Registers a mod affliction model for RitsuLib tracking, fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.DebugAfflictions" />.
        ///     注册 mod 苦痛模型，用于 RitsuLib 跟踪、固定条目身份、动态注入，以及修补后的
        ///     <see cref="ModelDb.DebugAfflictions" />。
        /// </summary>
        public void RegisterAffliction<TAffliction>() where TAffliction : AfflictionModel
        {
            RegisterAffliction(typeof(TAffliction));
        }

        /// <summary>
        ///     Registers <paramref name="afflictionType" /> for patched affliction injection.
        ///     注册 <paramref name="afflictionType" />，用于修补后的苦痛注入。
        /// </summary>
        public void RegisterAffliction(Type afflictionType)
        {
            RegisterStandaloneModel(RegisteredAfflictions, afflictionType, typeof(AfflictionModel), "affliction");
        }

        /// <summary>
        ///     Registers a mod achievement model for fixed entry identity, dynamic injection, and patched
        ///     <see cref="ModelDb.Achievements" />.
        ///     注册 mod 成就模型，用于固定条目身份、动态注入，以及修补后的
        ///     <see cref="ModelDb.Achievements" />。
        /// </summary>
        public void RegisterAchievement<TAchievement>() where TAchievement : AchievementModel
        {
            RegisterAchievement(typeof(TAchievement));
        }

        /// <summary>
        ///     Registers <paramref name="achievementType" /> for patched achievement injection.
        ///     注册 <paramref name="achievementType" />，用于修补后的成就注入。
        /// </summary>
        public void RegisterAchievement(Type achievementType)
        {
            RegisterStandaloneModel(RegisteredAchievements, achievementType, typeof(AchievementModel),
                "achievement");
        }

        /// <summary>
        ///     Registers a mod singleton model for fixed entry identity and dynamic injection (resolved via
        ///     <see cref="ModelDb.Singleton{T}" />).
        ///     注册 mod 单例模型，用于固定条目身份和动态注入（通过
        ///     <see cref="ModelDb.Singleton{T}" /> 解析）。
        /// </summary>
        public void RegisterSingleton<TSingleton>() where TSingleton : SingletonModel
        {
            RegisterSingleton(typeof(TSingleton));
        }

        /// <summary>
        ///     Registers <paramref name="singletonType" /> for dynamic singleton injection.
        ///     注册 <paramref name="singletonType" />，用于动态单例注入。
        /// </summary>
        public void RegisterSingleton(Type singletonType)
        {
            RegisterStandaloneModel(RegisteredSingletons, singletonType, typeof(SingletonModel), "singleton");
        }

        /// <summary>
        ///     Registers a custom badge template type.
        ///     注册自定义徽章模板类型。
        /// </summary>
        public void RegisterBadge<TBadge>() where TBadge : ModBadgeTemplate
        {
            RegisterBadge(typeof(TBadge));
        }

        /// <summary>
        ///     Registers a custom badge template type.
        ///     注册自定义徽章模板类型。
        /// </summary>
        public void RegisterBadge(Type badgeType)
        {
            EnsureMutable($"register badge '{badgeType.Name}'");
            EnsureBadgeType(badgeType, nameof(badgeType));

            lock (SyncRoot)
            {
                if (!RegisteredBadges.Add(badgeType))
                {
                    _logger.Debug($"[Content] Skipping duplicate badge registration: {badgeType.Name}");
                    return;
                }

                RememberOwner(badgeType);
            }

            _logger.Info($"[Content] Registered badge: {badgeType.Name}");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;good&quot; daily modifier for patched <see cref="ModelDb.GoodModifiers" />.
        ///     将 mod 修饰符注册为已修补 <see cref="ModelDb.GoodModifiers" /> 的正面每日修饰符。
        /// </summary>
        public void RegisterGoodModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterGoodModifier(typeof(TModifier));
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a good daily modifier.
        ///     将 <paramref name="modifierType" /> 注册为正面每日修饰符。
        /// </summary>
        public void RegisterGoodModifier(Type modifierType)
        {
            RegisterGoodModifier(modifierType, 0);
        }

        /// <summary>
        ///     Registers a mod modifier as a good daily modifier with list placement relative to the current segment.
        ///     将 mod 修饰符注册为正面每日修饰符，并指定相对于当前列表段的插入位置。
        /// </summary>
        public void RegisterGoodModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            RegisterGoodModifier(typeof(TModifier), modifierListSortOrder);
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a good daily modifier with list placement.
        ///     将 <paramref name="modifierType" /> 注册为正面每日修饰符，并指定列表插入位置。
        /// </summary>
        public void RegisterGoodModifier(Type modifierType, int modifierListSortOrder)
        {
            RegisterModifier(RegisteredGoodModifiers, modifierType, modifierListSortOrder, "good modifier");
        }

        /// <summary>
        ///     Registers a mod modifier as a &quot;bad&quot; daily modifier for patched <see cref="ModelDb.BadModifiers" />.
        ///     将 mod 修饰符注册为已修补 <see cref="ModelDb.BadModifiers" /> 的负面每日修饰符。
        /// </summary>
        public void RegisterBadModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterBadModifier(typeof(TModifier));
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a bad daily modifier.
        ///     将 <paramref name="modifierType" /> 注册为负面每日修饰符。
        /// </summary>
        public void RegisterBadModifier(Type modifierType)
        {
            RegisterBadModifier(modifierType, 0);
        }

        /// <summary>
        ///     Registers a mod modifier as a bad daily modifier with list placement relative to the current segment.
        ///     将 mod 修饰符注册为负面每日修饰符，并指定相对于当前列表段的插入位置。
        /// </summary>
        public void RegisterBadModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            RegisterBadModifier(typeof(TModifier), modifierListSortOrder);
        }

        /// <summary>
        ///     Registers <paramref name="modifierType" /> as a bad daily modifier with list placement.
        ///     将 <paramref name="modifierType" /> 注册为负面每日修饰符，并指定列表插入位置。
        /// </summary>
        public void RegisterBadModifier(Type modifierType, int modifierListSortOrder)
        {
            RegisterModifier(RegisteredBadModifiers, modifierType, modifierListSortOrder, "bad modifier");
        }

        /// <summary>
        ///     Registers a mutually exclusive modifier group for patched <see cref="ModelDb.MutuallyExclusiveModifiers" />.
        ///     注册互斥修饰符组，用于修补后的 <see cref="ModelDb.MutuallyExclusiveModifiers" />。
        /// </summary>
        public void RegisterMutuallyExclusiveModifierGroup(params Type[] modifierTypes)
        {
            RegisterMutuallyExclusiveModifierGroup((IReadOnlyList<Type>)modifierTypes);
        }

        /// <summary>
        ///     Registers a mutually exclusive modifier group for patched <see cref="ModelDb.MutuallyExclusiveModifiers" />.
        ///     注册互斥修饰符组，用于修补后的 <see cref="ModelDb.MutuallyExclusiveModifiers" />。
        /// </summary>
        public void RegisterMutuallyExclusiveModifierGroup(IReadOnlyList<Type> modifierTypes)
        {
            ArgumentNullException.ThrowIfNull(modifierTypes);

            EnsureMutable("register mutually exclusive modifier group");
            if (modifierTypes.Count < 2)
                throw new ArgumentException(
                    "At least two modifier types are required for a mutually exclusive group.",
                    nameof(modifierTypes));

            var members = new HashSet<Type>();
            foreach (var modifierType in modifierTypes)
            {
                EnsureModelType(modifierType, typeof(ModifierModel), nameof(modifierTypes));
                if (!members.Add(modifierType))
                    continue;

                PrimeOwnedType(modifierType);
                RegistrationConflictDetector.ThrowIfModelIdConflicts(modifierType);
            }

            lock (SyncRoot)
            {
                RegisteredMutuallyExclusiveModifierGroups.Add(members);
            }

            _logger.Info(
                $"[Content] Registered mutually exclusive modifier group: {string.Join(", ", members.Select(static t => t.Name))}");
        }

        /// <summary>
        ///     Registers a shared card pool model for inclusion in <see cref="ModelDb.AllSharedCardPools" />.
        ///     注册共享卡牌池模型，以纳入 <see cref="ModelDb.AllSharedCardPools" />。
        /// </summary>
        public void RegisterSharedCardPool<TPool>() where TPool : CardPoolModel
        {
            RegisterSharedCardPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in <see cref="ModelDb.AllSharedCardPools" />.
        ///     注册 <paramref name="poolType" />，以纳入 <see cref="ModelDb.AllSharedCardPools" />。
        /// </summary>
        public void RegisterSharedCardPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedCardPools, poolType, typeof(CardPoolModel),
                "shared card pool");
        }

        /// <summary>
        ///     Registers a shared relic pool model for inclusion in patched <see cref="ModelDb.AllRelicPools" />.
        ///     注册共享遗物池模型，以纳入修补后的 <see cref="ModelDb.AllRelicPools" />。
        /// </summary>
        public void RegisterSharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            RegisterSharedRelicPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in patched <see cref="ModelDb.AllRelicPools" />.
        ///     注册 <paramref name="poolType" />，以纳入修补后的 <see cref="ModelDb.AllRelicPools" />。
        /// </summary>
        public void RegisterSharedRelicPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedRelicPools, poolType, typeof(RelicPoolModel),
                "shared relic pool");
        }

        /// <summary>
        ///     Registers a shared potion pool model for inclusion in patched <see cref="ModelDb.AllPotionPools" />.
        ///     注册共享药水池模型，以纳入修补后的 <see cref="ModelDb.AllPotionPools" />。
        /// </summary>
        public void RegisterSharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            RegisterSharedPotionPool(typeof(TPool));
        }

        /// <summary>
        ///     Registers <paramref name="poolType" /> for inclusion in patched <see cref="ModelDb.AllPotionPools" />.
        ///     注册 <paramref name="poolType" />，以纳入修补后的 <see cref="ModelDb.AllPotionPools" />。
        /// </summary>
        public void RegisterSharedPotionPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedPotionPools, poolType, typeof(PotionPoolModel),
                "shared potion pool");
        }

        /// <summary>
        ///     Registers a shared event model for inclusion in shared event enumerations.
        ///     注册一个共享事件模型，使其纳入共享事件枚举。
        /// </summary>
        public void RegisterSharedEvent<TEvent>() where TEvent : EventModel
        {
            RegisterSharedEvent(typeof(TEvent));
        }

        /// <summary>
        ///     Registers <paramref name="eventType" /> for inclusion in shared event enumerations.
        ///     注册 <paramref name="eventType" />，以纳入共享事件枚举。
        /// </summary>
        public void RegisterSharedEvent(Type eventType)
        {
            RegisterStandaloneModel(RegisteredSharedEvents, eventType, typeof(EventModel), "shared event");
        }

        /// <summary>
        ///     Registers an encounter model scoped to <typeparamref name="TAct" />.
        ///     注册作用域限定为 <typeparamref name="TAct" /> 的遭遇模型。
        /// </summary>
        public void RegisterActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            RegisterActEncounter(typeof(TAct), typeof(TEncounter));
        }

        /// <summary>
        ///     Registers <paramref name="encounterType" /> scoped to <paramref name="actType" />.
        ///     注册作用域限定为 <paramref name="actType" /> 的 <paramref name="encounterType" />。
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
        ///     注册会追加到 <strong>每个</strong>章节的
        ///     <see cref="ActModel.GenerateAllEncounters" /> 结果中的遭遇模型（位于原版和章节作用域 mod 遭遇之后）。
        ///     用于应出现在多个章节中的精英/怪物/首领；当遭遇只属于一个章节时，请使用
        ///     <see cref="RegisterActEncounter{TAct,TEncounter}" />。
        /// </summary>
        public void RegisterGlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            RegisterGlobalEncounter(typeof(TEncounter));
        }

        /// <summary>
        ///     Registers <paramref name="encounterType" /> as a global encounter.
        ///     将 <paramref name="encounterType" /> 注册为全局遭遇。
        /// </summary>
        public void RegisterGlobalEncounter(Type encounterType)
        {
            RegisterStandaloneModel(RegisteredGlobalEncounters, encounterType, typeof(EncounterModel),
                "global encounter");
        }

        /// <summary>
        ///     Registers an event model scoped to <typeparamref name="TAct" />.
        ///     注册作用域限定为 <typeparamref name="TAct" /> 的事件模型。
        /// </summary>
        public void RegisterActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            RegisterActEvent(typeof(TAct), typeof(TEvent));
        }

        /// <summary>
        ///     Registers <paramref name="eventType" /> scoped to <paramref name="actType" />.
        ///     注册作用域限定为 <paramref name="actType" /> 的 <paramref name="eventType" />。
        /// </summary>
        public void RegisterActEvent(Type actType, Type eventType)
        {
            RegisterScopedModel(RegisteredActEvents, actType, eventType, typeof(ActModel), typeof(EventModel),
                "act event");
        }

        /// <summary>
        ///     Registers a shared ancient event model for inclusion in ancient enumerations.
        ///     注册一个共享远古事件模型，使其纳入远古事件枚举。
        /// </summary>
        public void RegisterSharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            RegisterSharedAncient(typeof(TAncient));
        }

        /// <summary>
        ///     Registers <paramref name="ancientType" /> for inclusion in ancient enumerations.
        ///     注册 <paramref name="ancientType" />，以纳入 ancient 枚举。
        /// </summary>
        public void RegisterSharedAncient(Type ancientType)
        {
            RegisterStandaloneModel(RegisteredSharedAncients, ancientType, typeof(AncientEventModel),
                "shared ancient");
        }

        /// <summary>
        ///     Registers an ancient event model scoped to <typeparamref name="TAct" />.
        ///     注册作用域限定为 <typeparamref name="TAct" /> 的 ancient 事件模型。
        /// </summary>
        public void RegisterActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            RegisterActAncient(typeof(TAct), typeof(TAncient));
        }

        /// <summary>
        ///     Registers <paramref name="ancientType" /> scoped to <paramref name="actType" />.
        ///     注册作用域限定为 <paramref name="actType" /> 的 <paramref name="ancientType" />。
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

            ResolvedModelCache.MarkFrozen();

            foreach (var registry in Registries.Values)
                registry._logger.Info($"[Content] Content registration is now frozen ({reason}).");

            RitsuLibFramework.PublishLifecycleEvent(
                new ContentRegistrationClosedEvent(reason, DateTimeOffset.UtcNow),
                nameof(ContentRegistrationClosedEvent)
            );
        }

        internal static void ValidateFrozenModelReferences()
        {
            ContentModelReference[] references;
            lock (SyncRoot)
            {
                var list = new List<ContentModelReference>();
                AddMany(list, RegisteredPoolContent.SelectMany(static entry => new[]
                {
                    new ContentModelReference(entry.PoolType, typeof(AbstractModel), "registered pool"),
                    new ContentModelReference(entry.ModelType, typeof(AbstractModel), "registered pool content"),
                }));
                AddMany(list, RegisteredCharacterStarterContent.SelectMany(static entry => new[]
                {
                    new ContentModelReference(entry.CharacterType, typeof(CharacterModel),
                        "registered starter character"),
                    new ContentModelReference(entry.ModelType, typeof(AbstractModel),
                        $"registered starter {entry.Kind}"),
                }));
                AddMany(list, RegisteredCharacters.Select(static type =>
                    new ContentModelReference(type, typeof(CharacterModel), "registered character")));
                AddMany(list, RegisteredActs.Select(static type =>
                    new ContentModelReference(type, typeof(ActModel), "registered act")));
                AddMany(list, RegisteredMonsters.Select(static type =>
                    new ContentModelReference(type, typeof(MonsterModel), "registered monster")));
                AddMany(list, RegisteredPowers.Select(static type =>
                    new ContentModelReference(type, typeof(PowerModel), "registered power")));
                AddMany(list, RegisteredOrbs.Select(static type =>
                    new ContentModelReference(type, typeof(OrbModel), "registered orb")));
                AddMany(list, RegisteredModelCapabilities.Select(static type =>
                    new ContentModelReference(type, typeof(ModelCapability), "registered model capability")));
                AddMany(list, RegisteredEnchantments.Select(static type =>
                    new ContentModelReference(type, typeof(EnchantmentModel), "registered enchantment")));
                AddMany(list, RegisteredAfflictions.Select(static type =>
                    new ContentModelReference(type, typeof(AfflictionModel), "registered affliction")));
                AddMany(list, RegisteredAchievements.Select(static type =>
                    new ContentModelReference(type, typeof(AchievementModel), "registered achievement")));
                AddMany(list, RegisteredSingletons.Select(static type =>
                    new ContentModelReference(type, typeof(SingletonModel), "registered singleton")));
                AddMany(list, RegisteredSharedCardPools.Select(static type =>
                    new ContentModelReference(type, typeof(CardPoolModel), "registered shared card pool")));
                AddMany(list, RegisteredSharedRelicPools.Select(static type =>
                    new ContentModelReference(type, typeof(RelicPoolModel), "registered shared relic pool")));
                AddMany(list, RegisteredSharedPotionPools.Select(static type =>
                    new ContentModelReference(type, typeof(PotionPoolModel), "registered shared potion pool")));
                AddMany(list, RegisteredGoodModifiers.Select(static registration =>
                    new ContentModelReference(registration.ModifierType, typeof(ModifierModel),
                        "registered good modifier")));
                AddMany(list, RegisteredBadModifiers.Select(static registration =>
                    new ContentModelReference(registration.ModifierType, typeof(ModifierModel),
                        "registered bad modifier")));
                AddMany(list, RegisteredSharedEvents.Select(static type =>
                    new ContentModelReference(type, typeof(EventModel), "registered shared event")));
                AddMany(list, RegisteredSharedAncients.Select(static type =>
                    new ContentModelReference(type, typeof(AncientEventModel), "registered shared ancient")));
                AddScoped(list, RegisteredActEncounters, typeof(ActModel), typeof(EncounterModel),
                    "registered act encounter");
                AddMany(list, RegisteredGlobalEncounters.Select(static type =>
                    new ContentModelReference(type, typeof(EncounterModel), "registered global encounter")));
                AddScoped(list, RegisteredActEvents, typeof(ActModel), typeof(EventModel),
                    "registered act event");
                AddScoped(list, RegisteredActAncients, typeof(ActModel), typeof(AncientEventModel),
                    "registered act ancient");

                references = list
                    .DistinctBy(static reference => (reference.ModelType, reference.ExpectedBaseType,
                        reference.Description))
                    .ToArray();
            }

            foreach (var reference in references)
            {
                TryGetOwnerModId(reference.ModelType, out var owner);
                RegistrationFreezeDiagnostics.WarnMissingModelType(
                    "Content",
                    owner,
                    reference.Description,
                    reference.ModelType,
                    reference.ExpectedBaseType);
            }

            return;

            static void AddMany(List<ContentModelReference> list, IEnumerable<ContentModelReference> values)
            {
                list.AddRange(values);
            }

            static void AddScoped(List<ContentModelReference> list, Dictionary<Type, HashSet<Type>> registry,
                Type expectedScopeType, Type expectedModelType, string description)
            {
                foreach (var (scopeType, modelTypes) in registry)
                {
                    list.Add(new(scopeType, expectedScopeType, $"{description} scope"));
                    list.AddRange(modelTypes.Select(modelType =>
                        new ContentModelReference(modelType, expectedModelType, description)));
                }
            }
        }

        internal static IEnumerable<CharacterModel> AppendCharacters(IEnumerable<CharacterModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Characters, source);
        }

        internal static IEnumerable<CharacterModel> GetModCharacters()
        {
            return ResolvedModelCache.GetGlobal<CharacterModel>(ContentCatalogId.Characters);
        }

        /// <summary>
        ///     Snapshot of registered model types with owner and resolved/public-entry diagnostics.
        ///     已注册模型类型的快照，包含所有者和已解析/公共条目诊断信息。
        /// </summary>
        public static ModContentRegisteredTypeSnapshot[] GetRegisteredTypeSnapshots()
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
            return MergeGlobalCatalog(ContentCatalogId.SharedEvents, source);
        }

        internal static IEnumerable<EventModel> AppendAllEvents(IEnumerable<EventModel> source)
        {
            var merged = AppendSharedEvents(source);
            var catalog = GetCatalog(ContentCatalogId.ActEvents);
            var actTypes = GetRegisteredActEventScopeTypes();
            var additional = actTypes
                .SelectMany(static actType =>
                    ResolvedModelCache.GetScoped<EventModel>(ContentCatalogId.ActEvents, actType))
                .ToArray();
            return ContentMergeStrategies.GetEnumerable<EventModel>(catalog.MergeMode).Merge(merged, additional);
        }

        internal static IEnumerable<ActModel> AppendActs(IEnumerable<ActModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Acts, source);
        }

        internal static Type[] GetRegisteredActTypes()
        {
            lock (SyncRoot)
            {
                return RegisteredActs.ToArray();
            }
        }

        private static Type[] GetRegisteredActEventScopeTypes()
        {
            lock (SyncRoot)
            {
                return RegisteredActEvents.Keys.ToArray();
            }
        }

        internal static IEnumerable<PowerModel> AppendPowers(IEnumerable<PowerModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Powers, source);
        }

        internal static IEnumerable<OrbModel> AppendOrbs(IEnumerable<OrbModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Orbs, source);
        }

        internal static IEnumerable<EnchantmentModel> AppendEnchantments(IEnumerable<EnchantmentModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Enchantments, source);
        }

        internal static IEnumerable<AfflictionModel> AppendAfflictions(IEnumerable<AfflictionModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Afflictions, source);
        }

        internal static IReadOnlyList<AchievementModel> AppendAchievements(IReadOnlyList<AchievementModel> source)
        {
            return MergeGlobalCatalogList(ContentCatalogId.Achievements, source);
        }

        internal static IReadOnlyList<ModifierModel> AppendGoodModifiers(IReadOnlyList<ModifierModel> source)
        {
            lock (SyncRoot)
            {
                return ModifierContentMerge.InsertModifiers(source, RegisteredGoodModifiers);
            }
        }

        internal static IReadOnlyList<ModifierModel> AppendBadModifiers(IReadOnlyList<ModifierModel> source)
        {
            lock (SyncRoot)
            {
                return ModifierContentMerge.InsertModifiers(source, RegisteredBadModifiers);
            }
        }

        internal static IReadOnlyList<IReadOnlySet<ModifierModel>> AppendMutuallyExclusiveModifiers(
            IReadOnlyList<IReadOnlySet<ModifierModel>> source)
        {
            lock (SyncRoot)
            {
                return ModifierContentMerge.MergeMutuallyExclusiveModifiers(source,
                    RegisteredMutuallyExclusiveModifierGroups);
            }
        }

        internal static IEnumerable<RelicPoolModel> AppendSharedRelicPools(IEnumerable<RelicPoolModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.SharedRelicPools, source);
        }

        internal static IEnumerable<PotionPoolModel> AppendSharedPotionPools(IEnumerable<PotionPoolModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.SharedPotionPools, source);
        }

        internal static IEnumerable<CardPoolModel> AppendSharedCardPools(IEnumerable<CardPoolModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.SharedCardPools, source);
        }

        internal static IEnumerable<EventModel> AppendActEvents(ActModel act, IEnumerable<EventModel> source)
        {
            return MergeScopedCatalog(ContentCatalogId.ActEvents, act.GetType(), source);
        }

        internal static IEnumerable<EncounterModel> AppendActEncounters(ActModel act,
            IEnumerable<EncounterModel> source)
        {
            return MergeScopedCatalog(ContentCatalogId.ActEncounters, act.GetType(), source);
        }

        internal static IEnumerable<EncounterModel> AppendGlobalEncounters(IEnumerable<EncounterModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.GlobalEncounters, source);
        }

        internal static IEnumerable<MonsterModel> AppendRegisteredMonsters(IEnumerable<MonsterModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.Monsters, source);
        }

        internal static IEnumerable<AncientEventModel> AppendSharedAncients(IEnumerable<AncientEventModel> source)
        {
            return MergeGlobalCatalog(ContentCatalogId.SharedAncients, source);
        }

        internal static IEnumerable<AncientEventModel> AppendActAncients(ActModel act,
            IEnumerable<AncientEventModel> source)
        {
            return MergeScopedCatalog(ContentCatalogId.ActAncients, act.GetType(), source);
        }

        internal static Type[] GetRegisteredBadgeTypes()
        {
            lock (SyncRoot)
            {
                return RegisteredBadges
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Injects RitsuLib-registered types that live in <see cref="Assembly.IsDynamic" /> assemblies into
        ///     <see cref="ModelDb" /> before <c>Init</c> finishes populating <c>_contentById</c>. Static mod DLL types are
        ///     picked up by the game's subtype scan; Reflection.Emit placeholder types are not, so they must be injected here.
        ///     将位于 <see cref="Assembly.IsDynamic" /> 程序集中的 RitsuLib 注册类型注入
        ///     <see cref="ModelDb" />，时机是在 <c>Init</c> 完成填充 <c>_contentById</c> 之前。静态 mod DLL 类型会
        ///     被游戏的子类型扫描拾取；Reflection.Emit 占位类型不会，因此必须在此处注入。
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
                    .Concat(RegisteredModelCapabilities)
                    .Concat(RegisteredEnchantments)
                    .Concat(RegisteredAfflictions)
                    .Concat(RegisteredAchievements)
                    .Concat(RegisteredSingletons)
                    .Concat(RegisteredSharedCardPools)
                    .Concat(RegisteredSharedRelicPools)
                    .Concat(RegisteredSharedPotionPools)
                    .Concat(RegisteredGoodModifiers.Select(static registration => registration.ModifierType))
                    .Concat(RegisteredBadModifiers.Select(static registration => registration.ModifierType))
                    .Concat(RegisteredMutuallyExclusiveModifierGroups.SelectMany(static group => group))
                    .Concat(RegisteredSharedEvents)
                    .Concat(RegisteredSharedAncients)
                    .Concat(RegisteredActEncounters.Values.SelectMany(static set => set))
                    .Concat(RegisteredGlobalEncounters)
                    .Concat(RegisteredActEvents.Values.SelectMany(static set => set))
                    .Concat(RegisteredActAncients.Values.SelectMany(static set => set))
                    .Distinct()
                    .Where(static t => t.Assembly.IsDynamic)
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
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
            var modelLabel = FormatModelForLog(modelType);

            lock (SyncRoot)
            {
                if (!RegisteredPoolContent.Add((poolType, modelType)))
                {
                    _logger.Debug(
                        $"[Content] Skipping duplicate {contentKind} registration: {modelLabel} -> {poolType.Name}");
                    return;
                }

                RememberOwner(modelType);
            }

            ModHelper.AddModelToPool(poolType, modelType);
            _logger.Info($"[Content] Registered {contentKind}: {modelLabel} -> {poolType.Name}");
        }

        private void RegisterCharacterStarterModel(Type characterType, Type modelType, Type expectedModelBaseType,
            CharacterStarterContentKind kind, int count, int order)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Starter content count must be positive.");

            EnsureMutable(
                $"register starter {kind.ToString().ToLowerInvariant()} '{modelType.Name}' for '{characterType.Name}'");
            EnsureModelType(characterType, typeof(CharacterModel), nameof(characterType));
            EnsureModelType(modelType, expectedModelBaseType, nameof(modelType));
            RegistrationConflictDetector.ThrowIfModelIdConflicts(characterType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modelType);
            var modelLabel = FormatModelForLog(modelType);

            lock (SyncRoot)
            {
                RegisteredCharacterStarterContent.Add(new(characterType, modelType, kind, count, order));
            }

            _logger.Info(
                $"[Content] Registered starter {kind.ToString().ToLowerInvariant()}: {modelLabel} x{count} -> {characterType.Name}");
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
            var modelLabel = FormatModelForLog(modelType);

            lock (SyncRoot)
            {
                if (!registry.Add(modelType))
                {
                    _logger.Debug($"[Content] Skipping duplicate {contentKind} registration: {modelLabel}");
                    return;
                }

                RememberOwner(modelType);
            }

            _logger.Info($"[Content] Registered {contentKind}: {modelLabel}");
        }

        private void RegisterModifier(
            List<ModifierRegistration> registry,
            Type modifierType,
            int modifierListSortOrder,
            string contentKind)
        {
            EnsureMutable($"register {contentKind} '{modifierType.Name}'");
            EnsureModelType(modifierType, typeof(ModifierModel), nameof(modifierType));
            PrimeOwnedType(modifierType);
            RegistrationConflictDetector.ThrowIfModelIdConflicts(modifierType);
            var modifierLabel = FormatModelForLog(modifierType);

            lock (SyncRoot)
            {
                if (registry.Any(entry => entry.ModifierType == modifierType))
                {
                    _logger.Debug($"[Content] Skipping duplicate {contentKind} registration: {modifierLabel}");
                    return;
                }

                registry.Add(new(modifierType, modifierListSortOrder));
                RememberOwner(modifierType);
            }

            _logger.Info($"[Content] Registered {contentKind}: {modifierLabel}");
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
            var modelLabel = FormatModelForLog(modelType);

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
                        $"[Content] Skipping duplicate {contentKind} registration: {modelLabel} -> {scopeType.Name}");
                    return;
                }

                RememberOwner(modelType);
            }

            _logger.Info($"[Content] Registered {contentKind}: {modelLabel} -> {scopeType.Name}");
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

        private static void EnsureModelFamilyType(Type type, string paramName)
        {
            if (type.IsInterface || type.ContainsGenericParameters || !typeof(AbstractModel).IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be an abstract model type or a concrete model type.",
                    paramName
                );
        }

        private static void EnsureBadgeType(Type type, string paramName)
        {
            if (type.IsAbstract || type.IsInterface || !typeof(ModBadgeTemplate).IsAssignableFrom(type))
                throw new ArgumentException(
                    $"Type '{type.FullName}' must be a concrete subtype of '{typeof(ModBadgeTemplate).FullName}'.",
                    paramName
                );
        }

        private static bool MatchesRegisteredStarterCharacter(Type registeredCharacterType, Type runtimeCharacterType)
        {
            if (registeredCharacterType == runtimeCharacterType)
                return true;

            if (!registeredCharacterType.IsAssignableFrom(runtimeCharacterType))
                return false;

            return registeredCharacterType != typeof(CharacterModel);
        }

        private static string FormatModelForLog(Type modelType)
        {
            return TryGetFixedPublicEntry(modelType, out var entry)
                ? $"{modelType.Name} (id={entry})"
                : modelType.Name;
        }

        private static Type[] GetRegisteredCharacterStarterTypes(Type characterType, CharacterStarterContentKind kind)
        {
            ArgumentNullException.ThrowIfNull(characterType);

            lock (SyncRoot)
            {
                return RegisteredCharacterStarterContent
                    .Select(static (entry, index) => new { entry, index })
                    .OrderBy(static x => x.entry.Order)
                    .ThenBy(static x => x.index)
                    .Where(x => x.entry.Kind == kind && MatchesRegisteredStarterCharacter(x.entry.CharacterType,
                        characterType))
                    .SelectMany(static x => Enumerable.Repeat(x.entry.ModelType, x.entry.Count))
                    .ToArray();
            }
        }

        /// <summary>
        ///     Normalizes a public id segment: non-alphanumeric collapsed to underscores, acronym/camel boundaries
        ///     split, repeated underscores merged, and final uppercase.
        ///     split, repeated underscores merged, 和 final uppercase.
        /// </summary>
        public static string NormalizePublicStem(string value)
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

        private string ResolveModelCapabilityId(Type capabilityType, ModelPublicEntryOptions options)
        {
            return options.Kind switch
            {
                ModelPublicEntryKind.FromTypeName => GetQualifiedModelCapabilityId(ModId, capabilityType.Name),
                ModelPublicEntryKind.Stem => GetQualifiedModelCapabilityId(ModId, options.Value!),
                ModelPublicEntryKind.FullEntry => NormalizeFullPublicEntry(options.Value!),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options.Kind, null),
            };
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

        private readonly record struct ContentModelReference(
            Type ModelType,
            Type ExpectedBaseType,
            string Description);

        /// <summary>
        ///     Immutable snapshot row describing one registered model type and its identity metadata.
        ///     描述一个已注册模型类型及其身份元数据的不可变快照行。
        /// </summary>
        public readonly record struct ModContentRegisteredTypeSnapshot
        {
            /// <summary>
            ///     Creates a registered-type snapshot row.
            ///     创建已注册类型快照行。
            /// </summary>
            public ModContentRegisteredTypeSnapshot(
                string modId,
                Type modelType,
                ModelId? modelDbId,
                string? expectedPublicEntry,
                bool hasExplicitPublicEntryOverride,
                string? typeNamePublicEntry)
            {
                ModId = modId;
                ModelType = modelType;
                ModelDbId = modelDbId;
                ExpectedPublicEntry = expectedPublicEntry;
                HasExplicitPublicEntryOverride = hasExplicitPublicEntryOverride;
                TypeNamePublicEntry = typeNamePublicEntry;
            }

            /// <summary>
            ///     Owning mod id recorded at registration time.
            ///     Owning mod id recorded at 注册 time.
            /// </summary>
            public string ModId { get; }

            /// <summary>
            ///     Registered model CLR type.
            ///     已注册模型的 CLR 类型。
            /// </summary>
            public Type ModelType { get; }

            /// <summary>
            ///     Resolved runtime <c>ModelDb</c> id, if currently available.
            ///     resolved runtime <c>ModelDb</c> id, 如果 currently 可用.
            /// </summary>
            public ModelId? ModelDbId { get; }

            /// <summary>
            ///     Expected fixed public entry for this model under current registry rules.
            ///     在当前注册表规则下，此模型预期使用的固定公共条目。
            /// </summary>
            public string? ExpectedPublicEntry { get; }

            /// <summary>
            ///     Whether the expected entry comes from an explicit override.
            ///     表示预期条目是否来自显式覆盖。
            /// </summary>
            public bool HasExplicitPublicEntryOverride { get; }

            /// <summary>
            ///     Type-name-derived public entry (<c>CATEGORY_TYPENAME</c>) when resolvable.
            ///     可解析时，由类型名派生的公共条目（<c>CATEGORY_TYPENAME</c>）。
            /// </summary>
            public string? TypeNamePublicEntry { get; }
        }

        private readonly record struct CharacterStarterRegistration(
            Type CharacterType,
            Type ModelType,
            CharacterStarterContentKind Kind,
            int Count,
            int Order);
    }
}
