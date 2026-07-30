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
    ///     <para xml:lang="en">Indicates whether <see cref="ModContentRegistry" /> still accepts registrations.</para>
    ///     <para xml:lang="zh-CN">指示 <see cref="ModContentRegistry" /> 是否仍接受注册。</para>
    /// </summary>
    public enum ContentRegistrationState
    {
        /// <summary>
        ///     <para xml:lang="en">Registrations are still accepted.</para>
        ///     <para xml:lang="zh-CN">仍可进行注册。</para>
        /// </summary>
        Open = 0,

        /// <summary>
        ///     <para xml:lang="en">Registrations are frozen, and further registration attempts throw.</para>
        ///     <para xml:lang="zh-CN">注册已冻结，继续尝试注册将抛出异常。</para>
        /// </summary>
        Frozen = 1,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a per-mod registry for pool models, standalone models, act-scoped content, and stable public-entry
    ///         overrides used by the patched <see cref="ModelDb" /> identity system.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供按模组隔离的注册表，用于注册池模型、独立模型、章节作用域内容，以及供修补后的
    ///         <see cref="ModelDb" /> 身份系统使用的稳定公共条目覆盖。
    ///     </para>
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
        ///     <para xml:lang="en">Gets the mod ID associated with this registry instance.</para>
        ///     <para xml:lang="zh-CN">获取与此注册表实例关联的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether registrations have been frozen globally.</para>
        ///     <para xml:lang="zh-CN">获取注册是否已在全局冻结。</para>
        /// </summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Gets the current <see cref="ContentRegistrationState" />.</para>
        ///     <para xml:lang="zh-CN">获取当前的 <see cref="ContentRegistrationState" />。</para>
        /// </summary>
        public static ContentRegistrationState State => IsFrozen
            ? ContentRegistrationState.Frozen
            : ContentRegistrationState.Open;

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that registered <paramref name="modelType" />, if any.</para>
        ///     <para xml:lang="zh-CN">获取注册 <paramref name="modelType" /> 的模组 ID（如有）。</para>
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
        ///     <para xml:lang="en">
        ///         Gets the stable public entry for a RitsuLib-registered model type, using either its explicit override or
        ///         the generated default.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取由 RitsuLib 注册的模型类型所对应的稳定公共条目；该值可以是显式覆盖值或自动生成的默认值。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Builds the default normalized <c>MOD_CATEGORY_TYPENAME</c> entry for a type owned by
        ///         <paramref name="modId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <paramref name="modId" /> 所属的类型构建默认的规范化
        ///         <c>MOD_CATEGORY_TYPENAME</c> 条目。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Builds a stable, underscore-delimited compound ID in the form
        ///         <c>{normalizedModId}_{TYPE}_{normalizedName}</c>. The mod ID and name are normalized with
        ///         <see cref="NormalizePublicStem" />; the type segment is only trimmed and converted with
        ///         <c>ToUpperInvariant</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建格式为 <c>{normalizedModId}_{TYPE}_{normalizedName}</c>、以下划线分隔的稳定复合 ID。
        ///         模组 ID 和名称通过 <see cref="NormalizePublicStem" /> 规范化；类型段仅去除首尾空白并通过
        ///         <c>ToUpperInvariant</c> 转换为大写。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped keyword ID in the form
        ///         <c>{normalizedModId}_KEYWORD_{normalizedStem}</c>. Other mods can reference the same keyword by
        ///         supplying the provider's <paramref name="modId" /> and <paramref name="localKeywordStem" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         构建格式为 <c>{normalizedModId}_KEYWORD_{normalizedStem}</c> 的模组作用域关键词 ID。
        ///         其他模组可通过提供注册方的 <paramref name="modId" /> 和
        ///         <paramref name="localKeywordStem" /> 引用同一关键词。
        ///     </para>
        /// </summary>
        public static string GetQualifiedKeywordId(string modId, string localKeywordStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localKeywordStem);

            return GetCompoundId(modId, "KEYWORD", localKeywordStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped card-pile ID using RitsuLib's <c>MODID_CATEGORY_TYPENAME</c> public-entry
        ///         convention: three uppercase segments separated by underscores.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 RitsuLib 的 <c>MODID_CATEGORY_TYPENAME</c> 公共条目约定，构建由三个大写段以下划线
        ///         分隔的模组作用域牌组 ID。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         The result is the stem for <c>static_hover_tips.json</c> keys. For example,
        ///         <c>com.example.my-mod</c> and <c>overflow_pile</c> produce
        ///         <c>MYMOD_CARDPILE_OVERFLOW_PILE</c>, with <c>.title</c>, <c>.description</c>, and <c>.empty</c>
        ///         localization keys.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回值是 <c>static_hover_tips.json</c> 键的词干。例如，<c>com.example.my-mod</c> 与
        ///         <c>overflow_pile</c> 会生成 <c>MYMOD_CARDPILE_OVERFLOW_PILE</c>，其本地化键分别使用
        ///         <c>.title</c>、<c>.description</c> 和 <c>.empty</c> 后缀。
        ///     </para>
        /// </remarks>
        public static string GetQualifiedCardPileId(string modId, string localPileStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localPileStem);

            return GetCompoundId(modId, "CARDPILE", localPileStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> ID using the
        ///         <c>MODID_CARDTAG_TYPENAME</c> convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <c>MODID_CARDTAG_TYPENAME</c> 约定构建模组作用域的
        ///         <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> ID。
        ///     </para>
        /// </summary>
        public static string GetQualifiedCardTagId(string modId, string localTagStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTagStem);

            return GetCompoundId(modId, "CARDTAG", localTagStem);
        }

        /// <summary>
        ///     <para xml:lang="en">Builds a mod-scoped reward ID using the <c>MODID_REWARD_TYPENAME</c> convention.</para>
        ///     <para xml:lang="zh-CN">按照 <c>MODID_REWARD_TYPENAME</c> 约定构建模组作用域的奖励 ID。</para>
        /// </summary>
        public static string GetQualifiedRewardId(string modId, string localRewardStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localRewardStem);

            return GetCompoundId(modId, "REWARD", localRewardStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped <see cref="MegaCrit.Sts2.Core.Entities.Cards.TargetType" /> ID using the
        ///         <c>MODID_TARGETTYPE_TYPENAME</c> convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <c>MODID_TARGETTYPE_TYPENAME</c> 约定构建模组作用域的
        ///         <see cref="MegaCrit.Sts2.Core.Entities.Cards.TargetType" /> ID。
        ///     </para>
        /// </summary>
        public static string GetQualifiedTargetTypeId(string modId, string localTargetTypeStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localTargetTypeStem);

            return GetCompoundId(modId, "TARGETTYPE", localTargetTypeStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped model-capability ID using the <c>MODID_MODELCAPABILITY_TYPENAME</c> convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <c>MODID_MODELCAPABILITY_TYPENAME</c> 约定构建模组作用域的模型能力 ID。
        ///     </para>
        /// </summary>
        public static string GetQualifiedModelCapabilityId(string modId, string localCapabilityStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localCapabilityStem);

            return GetCompoundId(modId, "MODELCAPABILITY", localCapabilityStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped top-bar-button ID using the <c>MODID_TOPBARBUTTON_TYPENAME</c> convention.
        ///         The result is used as the stem for <c>static_hover_tips.json</c> title and description keys.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <c>MODID_TOPBARBUTTON_TYPENAME</c> 约定构建模组作用域的顶部栏按钮 ID。
        ///         返回值用作 <c>static_hover_tips.json</c> 中标题和描述键的词干。
        ///     </para>
        /// </summary>
        public static string GetQualifiedTopBarButtonId(string modId, string localButtonStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localButtonStem);

            return GetCompoundId(modId, "TOPBARBUTTON", localButtonStem);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds a mod-scoped right-click binding ID using the <c>MODID_RIGHTCLICK_TYPENAME</c> convention.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照 <c>MODID_RIGHTCLICK_TYPENAME</c> 约定构建模组作用域的右键绑定 ID。
        ///     </para>
        /// </summary>
        public static string GetQualifiedRightClickId(string modId, string localRightClickStem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(localRightClickStem);

            return GetCompoundId(modId, "RIGHTCLICK", localRightClickStem);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registry for <paramref name="modId" />, creating it on first use.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的注册表；首次使用时会创建该实例。</para>
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
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TCard" /> with <typeparamref name="TPool" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <typeparamref name="TCard" /> 注册到 <typeparamref name="TPool" />。</para>
        /// </summary>
        public void RegisterCard<TPool, TCard>()
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterCard(typeof(TPool), typeof(TCard));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="cardType" /> with <paramref name="poolType" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <paramref name="cardType" /> 注册到 <paramref name="poolType" />。</para>
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType)
        {
            RegisterCard(poolType, cardType, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TCard" /> with <typeparamref name="TPool" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <typeparamref name="TCard" /> 注册到
        ///         <typeparamref name="TPool" />。
        ///     </para>
        /// </summary>
        public void RegisterCard<TPool, TCard>(ModelPublicEntryOptions publicEntry)
            where TPool : CardPoolModel
            where TCard : CardModel
        {
            RegisterCard(typeof(TPool), typeof(TCard), publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="cardType" /> with <paramref name="poolType" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <paramref name="cardType" /> 注册到
        ///         <paramref name="poolType" />。
        ///     </para>
        /// </summary>
        public void RegisterCard(Type poolType, Type cardType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, cardType, "card", publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TRelic" /> with <typeparamref name="TPool" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <typeparamref name="TRelic" /> 注册到 <typeparamref name="TPool" />。</para>
        /// </summary>
        public void RegisterRelic<TPool, TRelic>()
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic(typeof(TPool), typeof(TRelic));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="relicType" /> with <paramref name="poolType" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <paramref name="relicType" /> 注册到 <paramref name="poolType" />。</para>
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType)
        {
            RegisterRelic(poolType, relicType, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TRelic" /> with <typeparamref name="TPool" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <typeparamref name="TRelic" /> 注册到
        ///         <typeparamref name="TPool" />。
        ///     </para>
        /// </summary>
        public void RegisterRelic<TPool, TRelic>(ModelPublicEntryOptions publicEntry)
            where TPool : RelicPoolModel
            where TRelic : RelicModel
        {
            RegisterRelic(typeof(TPool), typeof(TRelic), publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="relicType" /> with <paramref name="poolType" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <paramref name="relicType" /> 注册到
        ///         <paramref name="poolType" />。
        ///     </para>
        /// </summary>
        public void RegisterRelic(Type poolType, Type relicType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, relicType, "relic", publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TPotion" /> with <typeparamref name="TPool" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <typeparamref name="TPotion" /> 注册到 <typeparamref name="TPool" />。</para>
        /// </summary>
        public void RegisterPotion<TPool, TPotion>()
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion(typeof(TPool), typeof(TPotion));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="potionType" /> with <paramref name="poolType" /> using the default
        ///         public entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用默认公共条目，将 <paramref name="potionType" /> 注册到 <paramref name="poolType" />。</para>
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType)
        {
            RegisterPotion(poolType, potionType, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <typeparamref name="TPotion" /> with <typeparamref name="TPool" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <typeparamref name="TPotion" /> 注册到
        ///         <typeparamref name="TPool" />。
        ///     </para>
        /// </summary>
        public void RegisterPotion<TPool, TPotion>(ModelPublicEntryOptions publicEntry)
            where TPool : PotionPoolModel
            where TPotion : PotionModel
        {
            RegisterPotion(typeof(TPool), typeof(TPotion), publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="potionType" /> with <paramref name="poolType" /> using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用 <paramref name="publicEntry" />，将 <paramref name="potionType" /> 注册到
        ///         <paramref name="poolType" />。
        ///     </para>
        /// </summary>
        public void RegisterPotion(Type poolType, Type potionType, ModelPublicEntryOptions publicEntry)
        {
            RegisterPoolModel(poolType, potionType, "potion", publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod character model for inclusion in <see cref="ModelDb.AllCharacters" />.</para>
        ///     <para xml:lang="zh-CN">注册模组角色模型，使其纳入 <see cref="ModelDb.AllCharacters" />。</para>
        /// </summary>
        public void RegisterCharacter<TCharacter>() where TCharacter : CharacterModel
        {
            RegisterCharacter(typeof(TCharacter));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="characterType" /> for inclusion in
        ///         <see cref="ModelDb.AllCharacters" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="characterType" />，使其纳入 <see cref="ModelDb.AllCharacters" />。</para>
        /// </summary>
        public void RegisterCharacter(Type characterType)
        {
            RegisterStandaloneModel(RegisteredCharacters, characterType, typeof(CharacterModel), "character");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TCard" /> to the starting deck of <typeparamref name="TCharacter" />.
        ///         Matching uses the live character's CLR type and applicable ancestor registrations, except registrations
        ///         keyed only to <see cref="CharacterModel" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向 <typeparamref name="TCharacter" /> 的初始牌组添加指定数量的 <typeparamref name="TCard" />。
        ///         匹配依据角色实例的 CLR 类型以及适用的祖先类型注册，但不包括仅以
        ///         <see cref="CharacterModel" /> 为键的注册。
        ///     </para>
        /// </summary>
        public void RegisterCharacterStarterCard<TCharacter, TCard>(int count = 1)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterStarterCard<TCharacter, TCard>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TCard" /> to the starting deck of <typeparamref name="TCharacter" />
        ///         using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照指定的公共条目规则，向 <typeparamref name="TCharacter" /> 的初始牌组添加指定数量的
        ///         <typeparamref name="TCard" />。
        ///     </para>
        /// </summary>
        public void RegisterCharacterStarterCard<TCharacter, TCard>(int count, int order)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterStarterCard(typeof(TCharacter), typeof(TCard), count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="cardType" /> to the starting deck of
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">向 <paramref name="characterType" /> 的初始牌组添加指定数量的 <paramref name="cardType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterCard(Type characterType, Type cardType, int count = 1)
        {
            RegisterCharacterStarterCard(characterType, cardType, count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="cardType" /> to the starting deck of
        ///         <paramref name="characterType" /> using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">按照指定的公共条目规则，向 <paramref name="characterType" /> 的初始牌组添加指定数量的 <paramref name="cardType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterCard(Type characterType, Type cardType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, cardType, typeof(CardModel),
                CharacterStarterContentKind.Card,
                count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TRelic" /> to the starting relics of
        ///         <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">向 <typeparamref name="TCharacter" /> 的初始遗物添加指定数量的 <typeparamref name="TRelic" />。</para>
        /// </summary>
        public void RegisterCharacterStarterRelic<TCharacter, TRelic>(int count = 1)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterStarterRelic<TCharacter, TRelic>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TRelic" /> to the starting relics of
        ///         <typeparamref name="TCharacter" /> using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照指定的公共条目规则，向 <typeparamref name="TCharacter" /> 的初始遗物添加指定数量的 <typeparamref name="TRelic" />
        ///         。
        ///     </para>
        /// </summary>
        public void RegisterCharacterStarterRelic<TCharacter, TRelic>(int count, int order)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterStarterRelic(typeof(TCharacter), typeof(TRelic), count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="relicType" /> to the starting relics of
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">向 <paramref name="characterType" /> 的初始遗物添加指定数量的 <paramref name="relicType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterRelic(Type characterType, Type relicType, int count = 1)
        {
            RegisterCharacterStarterRelic(characterType, relicType, count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="relicType" /> to the starting relics of
        ///         <paramref name="characterType" /> using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">按照指定的公共条目规则，向 <paramref name="characterType" /> 的初始遗物添加指定数量的 <paramref name="relicType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterRelic(Type characterType, Type relicType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, relicType, typeof(RelicModel),
                CharacterStarterContentKind.Relic, count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TPotion" /> to the starting potions of
        ///         <typeparamref name="TCharacter" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">向 <typeparamref name="TCharacter" /> 的初始药水添加指定数量的 <typeparamref name="TPotion" />。</para>
        /// </summary>
        public void RegisterCharacterStarterPotion<TCharacter, TPotion>(int count = 1)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterStarterPotion<TCharacter, TPotion>(count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <typeparamref name="TPotion" /> to the starting potions of
        ///         <typeparamref name="TCharacter" /> using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按照指定的公共条目规则，向 <typeparamref name="TCharacter" /> 的初始药水添加指定数量的
        ///         <typeparamref name="TPotion" />。
        ///     </para>
        /// </summary>
        public void RegisterCharacterStarterPotion<TCharacter, TPotion>(int count, int order)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterStarterPotion(typeof(TCharacter), typeof(TPotion), count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="potionType" /> to the starting potions of
        ///         <paramref name="characterType" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">向 <paramref name="characterType" /> 的初始药水添加指定数量的 <paramref name="potionType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterPotion(Type characterType, Type potionType, int count = 1)
        {
            RegisterCharacterStarterPotion(characterType, potionType, count, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds copies of <paramref name="potionType" /> to the starting potions of
        ///         <paramref name="characterType" /> using the specified public-entry rules.
        ///     </para>
        ///     <para xml:lang="zh-CN">按照指定的公共条目规则，向 <paramref name="characterType" /> 的初始药水添加指定数量的 <paramref name="potionType" />。</para>
        /// </summary>
        public void RegisterCharacterStarterPotion(Type characterType, Type potionType, int count, int order)
        {
            RegisterCharacterStarterModel(characterType, potionType, typeof(PotionModel),
                CharacterStarterContentKind.Potion, count, order);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod act model for inclusion in <see cref="ModelDb.Acts" />. This does not add it to the
        ///         vanilla randomized act list; implement <see cref="IModActRandomListPolicy" /> to opt in.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组章节模型，使其纳入 <see cref="ModelDb.Acts" />。此操作不会将该章节加入原版随机章节列表；
        ///         如需加入，请实现 <see cref="IModActRandomListPolicy" />。
        ///     </para>
        /// </summary>
        public void RegisterAct<TAct>() where TAct : ActModel
        {
            RegisterAct(typeof(TAct));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="actType" /> for inclusion in <see cref="ModelDb.Acts" />. This does not add it
        ///         to the vanilla randomized act list; implement <see cref="IModActRandomListPolicy" /> to opt in.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="actType" />，使其纳入 <see cref="ModelDb.Acts" />。此操作不会将该章节加入
        ///         原版随机章节列表；如需加入，请实现 <see cref="IModActRandomListPolicy" />。
        ///     </para>
        /// </summary>
        public void RegisterAct(Type actType)
        {
            RegisterStandaloneModel(RegisteredActs, actType, typeof(ActModel), "act");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod monster model for identity tracking, dynamic injection, and inclusion in the patched
        ///         <c>ModelDb.Monsters</c> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组怪物模型，用于身份跟踪、动态注入，并将其纳入修补后的 <c>ModelDb.Monsters</c> 列表。
        ///     </para>
        /// </summary>
        public void RegisterMonster<TMonster>() where TMonster : MonsterModel
        {
            RegisterMonster(typeof(TMonster));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="monsterType" /> for identity tracking and patched monster injection.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="monsterType" />，用于身份跟踪和修补后的怪物注入。</para>
        /// </summary>
        public void RegisterMonster(Type monsterType)
        {
            RegisterStandaloneModel(RegisteredMonsters, monsterType, typeof(MonsterModel), "monster");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod power model for inclusion in <see cref="ModelDb.AllPowers" />.</para>
        ///     <para xml:lang="zh-CN">注册模组能力模型，使其纳入 <see cref="ModelDb.AllPowers" />。</para>
        /// </summary>
        public void RegisterPower<TPower>() where TPower : PowerModel
        {
            RegisterPower(typeof(TPower));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="powerType" /> for inclusion in <see cref="ModelDb.AllPowers" />.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="powerType" />，使其纳入 <see cref="ModelDb.AllPowers" />。</para>
        /// </summary>
        public void RegisterPower(Type powerType)
        {
            RegisterStandaloneModel(RegisteredPowers, powerType, typeof(PowerModel), "power");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod orb model for inclusion in <see cref="ModelDb.Orbs" />.</para>
        ///     <para xml:lang="zh-CN">注册模组充能球模型，使其纳入 <see cref="ModelDb.Orbs" />。</para>
        /// </summary>
        public void RegisterOrb<TOrb>() where TOrb : OrbModel
        {
            RegisterOrb(typeof(TOrb));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="orbType" /> for inclusion in <see cref="ModelDb.Orbs" />.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="orbType" />，使其纳入 <see cref="ModelDb.Orbs" />。</para>
        /// </summary>
        public void RegisterOrb(Type orbType)
        {
            RegisterStandaloneModel(RegisteredOrbs, orbType, typeof(OrbModel), "orb");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a model-backed capability for use with <see cref="ModelCapabilities" />.</para>
        ///     <para xml:lang="zh-CN">注册基于模型的能力，供 <see cref="ModelCapabilities" /> 使用。</para>
        /// </summary>
        public void RegisterModelCapability<TCapability>() where TCapability : ModelCapability
        {
            RegisterModelCapability<TCapability>(default);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a model-backed capability using <paramref name="publicEntry" />.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="publicEntry" /> 注册基于模型的能力。</para>
        /// </summary>
        public void RegisterModelCapability<TCapability>(ModelPublicEntryOptions publicEntry)
            where TCapability : ModelCapability
        {
            RegisterModelCapability(typeof(TCapability), publicEntry);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="capabilityType" /> as a model-backed capability.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="capabilityType" /> 注册为基于模型的能力。</para>
        /// </summary>
        public void RegisterModelCapability(Type capabilityType)
        {
            RegisterModelCapability(capabilityType, default);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="capabilityType" /> as a model-backed capability using
        ///         <paramref name="publicEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="publicEntry" /> 将 <paramref name="capabilityType" /> 注册为基于模型的能力。</para>
        /// </summary>
        public void RegisterModelCapability(Type capabilityType, ModelPublicEntryOptions publicEntry)
        {
            EnsureMutable($"register model capability '{capabilityType.Name}'");
            EnsureModelType(capabilityType, typeof(ModelCapability), nameof(capabilityType));
            ModelCapabilities.EnsureInitialized();
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
        ///     <para xml:lang="en">Configures the default capabilities for matching <paramref name="modelType" /> instances.</para>
        ///     <para xml:lang="zh-CN">配置匹配 <paramref name="modelType" /> 实例的默认能力集合。</para>
        /// </summary>
        public void ConfigureDefaultModelCapabilities(
            Type modelType,
            string modifierId,
            Action<AbstractModel, ModelCapabilityList> modifier,
            int order = 0)
        {
            EnsureMutable($"configure default model capabilities '{modelType.Name}/{modifierId}'");
            EnsureModelFamilyType(modelType, nameof(modelType));
            ModelCapabilities.EnsureInitialized();
            ModelCapabilityDefaults.Modify(ModId, modifierId, modelType, modifier, order);
            _logger.Info($"[Content] Registered default model capability modifier: {modelType.Name}/{modifierId}");
        }

        /// <summary>
        ///     <para xml:lang="en">Configures the default capabilities for matching <typeparamref name="TModel" /> instances.</para>
        ///     <para xml:lang="zh-CN">配置匹配 <typeparamref name="TModel" /> 实例的默认能力集合。</para>
        /// </summary>
        public void ConfigureDefaultModelCapabilities<TModel>(
            string modifierId,
            Action<TModel, ModelCapabilityList> modifier,
            int order = 0)
            where TModel : AbstractModel
        {
            EnsureMutable($"configure default model capabilities '{typeof(TModel).Name}/{modifierId}'");
            ModelCapabilities.EnsureInitialized();
            ModelCapabilityDefaults.Modify(ModId, modifierId, modifier, order);
            _logger.Info(
                $"[Content] Registered default model capability modifier: {typeof(TModel).Name}/{modifierId}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod enchantment model for fixed identity, dynamic injection, and inclusion in the patched
        ///         <see cref="ModelDb.DebugEnchantments" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组附魔模型，用于固定身份、动态注入，并将其纳入修补后的
        ///         <see cref="ModelDb.DebugEnchantments" /> 列表。
        ///     </para>
        /// </summary>
        public void RegisterEnchantment<TEnchantment>() where TEnchantment : EnchantmentModel
        {
            RegisterEnchantment(typeof(TEnchantment));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="enchantmentType" /> for patched enchantment injection.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="enchantmentType" />，用于修补后的附魔注入。</para>
        /// </summary>
        public void RegisterEnchantment(Type enchantmentType)
        {
            RegisterStandaloneModel(RegisteredEnchantments, enchantmentType, typeof(EnchantmentModel),
                "enchantment");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod affliction model for fixed identity, dynamic injection, and inclusion in the patched
        ///         <see cref="ModelDb.DebugAfflictions" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组苦痛模型，用于固定身份、动态注入，并将其纳入修补后的
        ///         <see cref="ModelDb.DebugAfflictions" /> 列表。
        ///     </para>
        /// </summary>
        public void RegisterAffliction<TAffliction>() where TAffliction : AfflictionModel
        {
            RegisterAffliction(typeof(TAffliction));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="afflictionType" /> for patched affliction injection.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="afflictionType" />，用于修补后的苦痛注入。</para>
        /// </summary>
        public void RegisterAffliction(Type afflictionType)
        {
            RegisterStandaloneModel(RegisteredAfflictions, afflictionType, typeof(AfflictionModel), "affliction");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod achievement model for fixed identity, dynamic injection, and inclusion in the patched
        ///         <see cref="ModelDb.Achievements" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组成就模型，用于固定身份、动态注入，并将其纳入修补后的
        ///         <see cref="ModelDb.Achievements" /> 列表。
        ///     </para>
        /// </summary>
        public void RegisterAchievement<TAchievement>() where TAchievement : AchievementModel
        {
            RegisterAchievement(typeof(TAchievement));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="achievementType" /> for patched achievement injection.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="achievementType" />，用于修补后的成就注入。</para>
        /// </summary>
        public void RegisterAchievement(Type achievementType)
        {
            RegisterStandaloneModel(RegisteredAchievements, achievementType, typeof(AchievementModel),
                "achievement");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod singleton model for fixed identity and dynamic injection through
        ///         <see cref="ModelDb.Singleton{T}" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册模组单例模型，用于固定身份，并通过 <see cref="ModelDb.Singleton{T}" /> 动态注入。
        ///     </para>
        /// </summary>
        public void RegisterSingleton<TSingleton>() where TSingleton : SingletonModel
        {
            RegisterSingleton(typeof(TSingleton));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="singletonType" /> for dynamic singleton injection.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="singletonType" />，用于动态单例注入。</para>
        /// </summary>
        public void RegisterSingleton(Type singletonType)
        {
            RegisterStandaloneModel(RegisteredSingletons, singletonType, typeof(SingletonModel), "singleton");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a custom badge template type.</para>
        ///     <para xml:lang="zh-CN">注册自定义徽章模板类型。</para>
        /// </summary>
        public void RegisterBadge<TBadge>() where TBadge : ModBadgeTemplate
        {
            RegisterBadge(typeof(TBadge));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a custom badge template type.</para>
        ///     <para xml:lang="zh-CN">注册自定义徽章模板类型。</para>
        /// </summary>
        public void RegisterBadge(Type badgeType)
        {
            EnsureMutable($"register badge '{badgeType.Name}'");
            EnsureBadgeType(badgeType, nameof(badgeType));
            PrimeOwnedType(badgeType);

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
        ///     <para xml:lang="en">
        ///         Registers a mod modifier as a good daily modifier in the patched
        ///         <see cref="ModelDb.GoodModifiers" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">将模组修正项注册为正面每日修正项，使其纳入修补后的 <see cref="ModelDb.GoodModifiers" /> 列表。</para>
        /// </summary>
        public void RegisterGoodModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterGoodModifier(typeof(TModifier));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="modifierType" /> as a good daily modifier.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="modifierType" /> 注册为正面每日修正项。</para>
        /// </summary>
        public void RegisterGoodModifier(Type modifierType)
        {
            RegisterGoodModifier(modifierType, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod modifier as a good daily modifier with explicit list placement.</para>
        ///     <para xml:lang="zh-CN">将模组修正项注册为正面每日修正项，并指定其列表位置。</para>
        /// </summary>
        public void RegisterGoodModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            RegisterGoodModifier(typeof(TModifier), modifierListSortOrder);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="modifierType" /> as a good daily modifier with explicit list
        ///         placement.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <paramref name="modifierType" /> 注册为正面每日修正项，并指定其列表位置。</para>
        /// </summary>
        public void RegisterGoodModifier(Type modifierType, int modifierListSortOrder)
        {
            RegisterModifier(RegisteredGoodModifiers, modifierType, modifierListSortOrder, "good modifier");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mod modifier as a bad daily modifier in the patched
        ///         <see cref="ModelDb.BadModifiers" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">将模组修正项注册为负面每日修正项，使其纳入修补后的 <see cref="ModelDb.BadModifiers" /> 列表。</para>
        /// </summary>
        public void RegisterBadModifier<TModifier>() where TModifier : ModifierModel
        {
            RegisterBadModifier(typeof(TModifier));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="modifierType" /> as a bad daily modifier.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="modifierType" /> 注册为负面每日修正项。</para>
        /// </summary>
        public void RegisterBadModifier(Type modifierType)
        {
            RegisterBadModifier(modifierType, 0);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a mod modifier as a bad daily modifier with explicit list placement.</para>
        ///     <para xml:lang="zh-CN">将模组修正项注册为负面每日修正项，并指定其列表位置。</para>
        /// </summary>
        public void RegisterBadModifier<TModifier>(int modifierListSortOrder) where TModifier : ModifierModel
        {
            RegisterBadModifier(typeof(TModifier), modifierListSortOrder);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="modifierType" /> as a bad daily modifier with explicit list
        ///         placement.
        ///     </para>
        ///     <para xml:lang="zh-CN">将 <paramref name="modifierType" /> 注册为负面每日修正项，并指定其列表位置。</para>
        /// </summary>
        public void RegisterBadModifier(Type modifierType, int modifierListSortOrder)
        {
            RegisterModifier(RegisteredBadModifiers, modifierType, modifierListSortOrder, "bad modifier");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mutually exclusive modifier group in the patched
        ///         <see cref="ModelDb.MutuallyExclusiveModifiers" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册互斥修正项组，使其纳入修补后的 <see cref="ModelDb.MutuallyExclusiveModifiers" /> 列表。</para>
        /// </summary>
        public void RegisterMutuallyExclusiveModifierGroup(params Type[] modifierTypes)
        {
            RegisterMutuallyExclusiveModifierGroup((IReadOnlyList<Type>)modifierTypes);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a mutually exclusive modifier group in the patched
        ///         <see cref="ModelDb.MutuallyExclusiveModifiers" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册互斥修正项组，使其纳入修补后的 <see cref="ModelDb.MutuallyExclusiveModifiers" /> 列表。</para>
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
                members.Add(modifierType);
            }

            if (members.Count < 2)
                throw new ArgumentException(
                    "At least two distinct modifier types are required for a mutually exclusive group.",
                    nameof(modifierTypes));

            foreach (var modifierType in members)
            {
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
        ///     <para xml:lang="en">Registers a shared card-pool model for inclusion in <see cref="ModelDb.AllSharedCardPools" />.</para>
        ///     <para xml:lang="zh-CN">注册共享卡牌池模型，使其纳入 <see cref="ModelDb.AllSharedCardPools" />。</para>
        /// </summary>
        public void RegisterSharedCardPool<TPool>() where TPool : CardPoolModel
        {
            RegisterSharedCardPool(typeof(TPool));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="poolType" /> for inclusion in
        ///         <see cref="ModelDb.AllSharedCardPools" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="poolType" />，使其纳入 <see cref="ModelDb.AllSharedCardPools" />。</para>
        /// </summary>
        public void RegisterSharedCardPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedCardPools, poolType, typeof(CardPoolModel),
                "shared card pool");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a shared relic-pool model for inclusion in the patched
        ///         <see cref="ModelDb.AllRelicPools" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册共享遗物池模型，使其纳入修补后的 <see cref="ModelDb.AllRelicPools" /> 列表。</para>
        /// </summary>
        public void RegisterSharedRelicPool<TPool>() where TPool : RelicPoolModel
        {
            RegisterSharedRelicPool(typeof(TPool));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="poolType" /> for inclusion in the patched
        ///         <see cref="ModelDb.AllRelicPools" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="poolType" />，使其纳入修补后的 <see cref="ModelDb.AllRelicPools" /> 列表。</para>
        /// </summary>
        public void RegisterSharedRelicPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedRelicPools, poolType, typeof(RelicPoolModel),
                "shared relic pool");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a shared potion-pool model for inclusion in the patched
        ///         <see cref="ModelDb.AllPotionPools" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册共享药水池模型，使其纳入修补后的 <see cref="ModelDb.AllPotionPools" /> 列表。</para>
        /// </summary>
        public void RegisterSharedPotionPool<TPool>() where TPool : PotionPoolModel
        {
            RegisterSharedPotionPool(typeof(TPool));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers <paramref name="poolType" /> for inclusion in the patched
        ///         <see cref="ModelDb.AllPotionPools" /> list.
        ///     </para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="poolType" />，使其纳入修补后的 <see cref="ModelDb.AllPotionPools" /> 列表。</para>
        /// </summary>
        public void RegisterSharedPotionPool(Type poolType)
        {
            RegisterStandaloneModel(RegisteredSharedPotionPools, poolType, typeof(PotionPoolModel),
                "shared potion pool");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a shared event model for inclusion in shared-event enumerations.</para>
        ///     <para xml:lang="zh-CN">注册共享事件模型，使其纳入共享事件枚举。</para>
        /// </summary>
        public void RegisterSharedEvent<TEvent>() where TEvent : EventModel
        {
            RegisterSharedEvent(typeof(TEvent));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="eventType" /> for inclusion in shared-event enumerations.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="eventType" />，使其纳入共享事件枚举。</para>
        /// </summary>
        public void RegisterSharedEvent(Type eventType)
        {
            RegisterStandaloneModel(RegisteredSharedEvents, eventType, typeof(EventModel), "shared event");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an encounter model scoped to <typeparamref name="TAct" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <typeparamref name="TAct" /> 的遭遇模型。</para>
        /// </summary>
        public void RegisterActEncounter<TAct, TEncounter>()
            where TAct : ActModel
            where TEncounter : EncounterModel
        {
            RegisterActEncounter(typeof(TAct), typeof(TEncounter));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="encounterType" /> scoped to <paramref name="actType" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <paramref name="actType" /> 的 <paramref name="encounterType" />。</para>
        /// </summary>
        public void RegisterActEncounter(Type actType, Type encounterType)
        {
            RegisterScopedModel(RegisteredActEncounters, actType, encounterType, typeof(ActModel),
                typeof(EncounterModel), "act encounter");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a global encounter appended to every act's <see cref="ActModel.GenerateAllEncounters" />
        ///         result, after vanilla and act-scoped mod encounters. Use
        ///         <see cref="RegisterActEncounter{TAct,TEncounter}" /> for an encounter belonging to only one act.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册全局遭遇，并将其追加到每个章节的 <see cref="ActModel.GenerateAllEncounters" /> 结果中，
        ///         位于原版和章节作用域模组遭遇之后。若遭遇仅属于一个章节，请使用
        ///         <see cref="RegisterActEncounter{TAct,TEncounter}" />。
        ///     </para>
        /// </summary>
        public void RegisterGlobalEncounter<TEncounter>() where TEncounter : EncounterModel
        {
            RegisterGlobalEncounter(typeof(TEncounter));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="encounterType" /> as a global encounter.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="encounterType" /> 注册为全局遭遇。</para>
        /// </summary>
        public void RegisterGlobalEncounter(Type encounterType)
        {
            RegisterStandaloneModel(RegisteredGlobalEncounters, encounterType, typeof(EncounterModel),
                "global encounter");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an event model scoped to <typeparamref name="TAct" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <typeparamref name="TAct" /> 的事件模型。</para>
        /// </summary>
        public void RegisterActEvent<TAct, TEvent>()
            where TAct : ActModel
            where TEvent : EventModel
        {
            RegisterActEvent(typeof(TAct), typeof(TEvent));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="eventType" /> scoped to <paramref name="actType" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <paramref name="actType" /> 的 <paramref name="eventType" />。</para>
        /// </summary>
        public void RegisterActEvent(Type actType, Type eventType)
        {
            RegisterScopedModel(RegisteredActEvents, actType, eventType, typeof(ActModel), typeof(EventModel),
                "act event");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers a shared ancient event model for inclusion in ancient-event enumerations.</para>
        ///     <para xml:lang="zh-CN">注册共享先古之民事件模型，使其纳入先古之民事件枚举。</para>
        /// </summary>
        public void RegisterSharedAncient<TAncient>() where TAncient : AncientEventModel
        {
            RegisterSharedAncient(typeof(TAncient));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="ancientType" /> for inclusion in ancient-event enumerations.</para>
        ///     <para xml:lang="zh-CN">注册 <paramref name="ancientType" />，使其纳入先古之民事件枚举。</para>
        /// </summary>
        public void RegisterSharedAncient(Type ancientType)
        {
            RegisterStandaloneModel(RegisteredSharedAncients, ancientType, typeof(AncientEventModel),
                "shared ancient");
        }

        /// <summary>
        ///     <para xml:lang="en">Registers an ancient event model scoped to <typeparamref name="TAct" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <typeparamref name="TAct" /> 的先古之民事件模型。</para>
        /// </summary>
        public void RegisterActAncient<TAct, TAncient>()
            where TAct : ActModel
            where TAncient : AncientEventModel
        {
            RegisterActAncient(typeof(TAct), typeof(TAncient));
        }

        /// <summary>
        ///     <para xml:lang="en">Registers <paramref name="ancientType" /> scoped to <paramref name="actType" />.</para>
        ///     <para xml:lang="zh-CN">注册作用域限定为 <paramref name="actType" /> 的 <paramref name="ancientType" />。</para>
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

                references =
                [
                    .. list
                        .DistinctBy(static reference => (reference.ModelType, reference.ExpectedBaseType,
                            reference.Description)),
                ];
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
        ///     <para xml:lang="en">
        ///         Gets a snapshot of registered model types with ownership, resolved ID, and public-entry diagnostics.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模型类型的快照，其中包含所有者、已解析 ID 和公共条目诊断信息。
        ///     </para>
        /// </summary>
        public static ModContentRegisteredTypeSnapshot[] GetRegisteredTypeSnapshots()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. RegisteredTypeOwners
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
                        }),
                ];
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
                return [.. RegisteredActs];
            }
        }

        private static Type[] GetRegisteredActEventScopeTypes()
        {
            lock (SyncRoot)
            {
                return [.. RegisteredActEvents.Keys];
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

        internal static IEnumerable<RelicModel> AppendRegisteredRelics(IEnumerable<RelicModel> source)
        {
            Type[] relicTypes;
            lock (SyncRoot)
            {
                relicTypes =
                [
                    .. RegisteredPoolContent
                        .Select(static entry => entry.ModelType)
                        .Where(static type => typeof(RelicModel).IsAssignableFrom(type))
                        .Distinct(),
                ];
            }

            var additional = ResolveExistingModels<RelicModel>(relicTypes);
            return ContentMergeStrategies.GetEnumerable<RelicModel>(ContentMergeMode.AppendDistinctById)
                .Merge(source, additional);
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

        internal static IEnumerable<AncientEventModel> AppendAllAncients(IEnumerable<AncientEventModel> source)
        {
            var merged = AppendSharedAncients(source);
            var catalog = GetCatalog(ContentCatalogId.ActAncients);
            Type[] ancientTypes;
            lock (SyncRoot)
            {
                ancientTypes =
                [
                    .. RegisteredActAncients.Values
                        .SelectMany(static set => set)
                        .Distinct(),
                ];
            }

            var additional = ResolveExistingModels<AncientEventModel>(ancientTypes);
            return ContentMergeStrategies.GetEnumerable<AncientEventModel>(catalog.MergeMode).Merge(merged, additional);
        }

        internal static IEnumerable<AncientEventModel> AppendActAncients(ActModel act,
            IEnumerable<AncientEventModel> source)
        {
            return MergeScopedCatalog(ContentCatalogId.ActAncients, act.GetType(), source);
        }

        private static TModel[] ResolveExistingModels<TModel>(IEnumerable<Type> modelTypes)
            where TModel : AbstractModel
        {
            return
            [
                .. modelTypes
                    .OrderBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal)
                    .Select(static type => ModelDb.GetByIdOrNull<TModel>(ModelDb.GetId(type)))
                    .OfType<TModel>(),
            ];
        }

        internal static Type[] GetRegisteredBadgeTypes()
        {
            lock (SyncRoot)
            {
                return
                [
                    .. RegisteredBadges
                        .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal),
                ];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Injects RitsuLib-registered types from <see cref="Assembly.IsDynamic" /> assemblies into
        ///         <see cref="ModelDb" /> before <c>Init</c> finishes populating <c>_contentById</c>. The game's subtype
        ///         scan discovers static mod DLL types, but not Reflection.Emit placeholder types.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在 <c>Init</c> 完成填充 <c>_contentById</c> 前，将位于 <see cref="Assembly.IsDynamic" />
        ///         程序集中的 RitsuLib 注册类型注入 <see cref="ModelDb" />。游戏的子类型扫描能够发现静态模组
        ///         DLL 中的类型，但无法发现通过 Reflection.Emit 生成的占位类型。
        ///     </para>
        /// </summary>
        internal static void InjectDynamicRegisteredModels()
        {
            Type[] typesToInject;

            lock (SyncRoot)
            {
                typesToInject =
                [
                    .. RegisteredPoolContent
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
                        .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal),
                ];
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
                return
                [
                    .. RegisteredCharacterStarterContent
                        .Select(static (entry, index) => new { entry, index })
                        .OrderBy(static x => x.entry.Order)
                        .ThenBy(static x => x.index)
                        .Where(x => x.entry.Kind == kind && MatchesRegisteredStarterCharacter(x.entry.CharacterType,
                            characterType))
                        .SelectMany(static x => Enumerable.Repeat(x.entry.ModelType, x.entry.Count)),
                ];
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Normalizes a public ID segment by replacing non-alphanumeric runs with underscores, separating acronym
        ///         and camel-case boundaries, merging repeated underscores, and converting the result to uppercase.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         规范化公共 ID 段：将连续的非字母数字字符替换为下划线，拆分缩写词与驼峰命名边界，
        ///         合并连续下划线，并将结果转换为大写。
        ///     </para>
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

            var previousId = ModelDb.GetId(modelType);

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

            RegistrationConflictDetector.UpdateModelIdIndex(modelType, previousId, ModelDb.GetId(modelType));
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
            if (RegisteredTypeOwners.TryGetValue(type, out var existingOwner) &&
                !string.Equals(existingOwner, ModId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Model type '{type.FullName}' is already owned by mod '{existingOwner}' and cannot be registered by '{ModId}'.");

            RegisteredTypeOwners[type] = ModId;
        }

        private void PrimeOwnedType(Type type)
        {
            var previousId = ModelDb.GetId(type);

            lock (SyncRoot)
            {
                RememberOwner(type);
            }

            RegistrationConflictDetector.UpdateModelIdIndex(type, previousId, ModelDb.GetId(type));
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
        ///     <para xml:lang="en">Represents an immutable snapshot of a registered model type and its identity metadata.</para>
        ///     <para xml:lang="zh-CN">表示已注册模型类型及其身份元数据的不可变快照。</para>
        /// </summary>
        public readonly record struct ModContentRegisteredTypeSnapshot
        {
            /// <summary>
            ///     <para xml:lang="en">Creates a registered-model-type snapshot.</para>
            ///     <para xml:lang="zh-CN">创建已注册模型类型的快照。</para>
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
            ///     <para xml:lang="en">Gets the owning mod ID recorded at registration time.</para>
            ///     <para xml:lang="zh-CN">获取注册时记录的所属模组 ID。</para>
            /// </summary>
            public string ModId { get; }

            /// <summary>
            ///     <para xml:lang="en">Gets the registered model's CLR type.</para>
            ///     <para xml:lang="zh-CN">获取已注册模型的 CLR 类型。</para>
            /// </summary>
            public Type ModelType { get; }

            /// <summary>
            ///     <para xml:lang="en">Gets the resolved runtime <c>ModelDb</c> ID, if currently available.</para>
            ///     <para xml:lang="zh-CN">获取运行时解析的 <c>ModelDb</c> ID（如当前可用）。</para>
            /// </summary>
            public ModelId? ModelDbId { get; }

            /// <summary>
            ///     <para xml:lang="en">Gets the fixed public entry expected under the current registry rules.</para>
            ///     <para xml:lang="zh-CN">获取按当前注册表规则确定的预期固定公共条目。</para>
            /// </summary>
            public string? ExpectedPublicEntry { get; }

            /// <summary>
            ///     <para xml:lang="en">Gets whether the expected entry comes from an explicit override.</para>
            ///     <para xml:lang="zh-CN">获取预期条目是否来自显式覆盖。</para>
            /// </summary>
            public bool HasExplicitPublicEntryOverride { get; }

            /// <summary>
            ///     <para xml:lang="en">Gets the type-name-derived <c>CATEGORY_TYPENAME</c> public entry, if resolvable.</para>
            ///     <para xml:lang="zh-CN">获取由类型名派生的 <c>CATEGORY_TYPENAME</c> 公共条目（如可解析）。</para>
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
