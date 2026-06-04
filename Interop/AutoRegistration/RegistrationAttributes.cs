using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Base metadata for declarative registrations discovered by the ritsulib auto-registration pipeline.
    ///     ritsulib 自动注册管线发现的声明式注册基础元数据。
    /// </summary>
    public abstract class AutoRegistrationAttribute : Attribute
    {
        /// <summary>
        ///     Local ordering within the same registration phase. Lower values run first.
        ///     同一注册阶段内的局部排序。数值越小越先运行。
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        ///     When true on an attribute declared on a base type, the same registration is applied to concrete
        ///     derived types (duplicate signatures from direct declarations on the derived type are skipped).
        ///     当基类型上声明的 attribute 将此项设为 true 时，同一注册会应用到具体
        ///     派生类型（派生类型直接声明产生的重复签名会被跳过）。
        /// </summary>
        public bool Inherit { get; set; }
    }

    /// <summary>
    ///     Base metadata for content registrations dispatched through <c>ModContentRegistry</c>.
    ///     通过 <c>ModContentRegistry</c> 分发的内容注册基础元数据。
    /// </summary>
    public abstract class ContentRegistrationAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a character model.
    ///     将带注解的类型注册为char章节er模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an act model.
    ///     将带注解的类型注册为章节模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a monster model.
    ///     将带注解的类型注册为怪物模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterMonsterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a power model.
    ///     将带注解的类型注册为能力模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPowerAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an orb model.
    ///     将带注解的类型注册为充能球模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOrbAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an enchantment model.
    ///     将带注解的类型注册为附魔模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEnchantmentAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an affliction model.
    ///     将带注解的类型注册为苦痛模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAfflictionAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as an achievement model.
    ///     将带注解的类型注册为成就模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAchievementAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a singleton model.
    ///     将带注解的类型注册为单例模型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSingletonAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a model capability.
    ///     将带注解的类型注册为模型能力。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterModelCapabilityAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Optional stable author-chosen capability id and public-entry stem.
        ///     可选的、由作者选择的稳定能力 ID 与 public-entry stem。
        /// </summary>
        public string? StableEntryStem { get; set; }

        /// <summary>
        ///     Optional full fixed public entry and capability id override.
        ///     可选的完整固定 public-entry 与能力 ID 覆盖。
        /// </summary>
        public string? FullPublicEntry { get; set; }
    }

    /// <summary>
    ///     Adds the annotated capability type to the default capability set for matching model instances.
    ///     将带注解的能力类型添加到匹配模型实例的默认能力集合。
    /// </summary>
    /// <param name="targetModelType">
    ///     Target model type.
    ///     目标模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterDefaultModelCapabilityAttribute(Type targetModelType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target model type.
        ///     目标模型类型。
        /// </summary>
        public Type TargetModelType { get; } = targetModelType;

        /// <summary>
        ///     Optional stable modifier id. Defaults to a mod-scoped id derived from the capability and target type.
        ///     可选的稳定 modifier ID。默认根据能力与目标类型派生 mod 作用域 ID。
        /// </summary>
        public string? ModifierId { get; set; }
    }

    /// <summary>
    ///     Registers the annotated type as a good daily modifier.
    ///     将带注解的类型注册为正面每日修饰符。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGoodModifierAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Negative values insert before the current good-modifier list segment; non-negative values insert after.
        ///     负值插入当前正面修饰符列表段之前；非负值插入之后。
        /// </summary>
        public int ModifierListSortOrder { get; set; }
    }

    /// <summary>
    ///     Registers the annotated type as a bad daily modifier.
    ///     将带注解的类型注册为负面每日修饰符。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterBadModifierAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Negative values insert before the current bad-modifier list segment; non-negative values insert after.
        ///     负值插入当前负面修饰符列表段之前；非负值插入之后。
        /// </summary>
        public int ModifierListSortOrder { get; set; }
    }

    /// <summary>
    ///     Registers a mutually exclusive modifier group for the custom run and daily-run roller.
    ///     为自定义 run 与每日挑战 roll 注册互斥修饰符组。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterMutuallyExclusiveModifierGroupAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Creates an exclusivity group from the annotated modifier type plus <paramref name="memberTypes" />.
        ///     从带注解的修饰符类型与 <paramref name="memberTypes" /> 创建互斥组。
        /// </summary>
        public RegisterMutuallyExclusiveModifierGroupAttribute(params Type[] memberTypes)
        {
            MemberTypes = memberTypes ?? throw new ArgumentNullException(nameof(memberTypes));
        }

        /// <summary>
        ///     Additional modifier types in the same exclusivity group.
        ///     同一互斥组中的其它修饰符类型。
        /// </summary>
        public Type[] MemberTypes { get; }
    }

    /// <summary>
    ///     Registers the annotated type as a shared card pool.
    ///     将带注解的类型注册为共享卡牌池。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedCardPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared relic pool.
    ///     将带注解的类型注册为共享遗物池。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedRelicPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared potion pool.
    ///     将带注解的类型注册为共享药水池。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedPotionPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared event.
    ///     将带注解的类型注册为共享事件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedEventAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a shared ancient event.
    ///     将带注解的类型注册为共享 ancient 事件。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedAncientAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a global encounter.
    ///     将带注解的类型注册为全局遭遇。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGlobalEncounterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     Base metadata for pool-backed model registrations that can override fixed public entry generation.
    ///     可覆盖固定公共条目生成的池支持模型注册基础元数据。
    /// </summary>
    /// <param name="poolType">
    ///     Target pool model type.
    ///     目标牌池模型类型。
    /// </param>
    public abstract class ModelPublicEntryRegistrationAttributeBase(Type poolType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target pool model type.
        ///     目标牌池模型类型。
        /// </summary>
        public Type PoolType { get; } = poolType;

        /// <summary>
        ///     Optional stable author-chosen type-name stem.
        ///     可选的、由作者选择的稳定类型名 stem。
        /// </summary>
        public string? StableEntryStem { get; set; }

        /// <summary>
        ///     Optional full fixed public entry override.
        ///     可选的完整固定公共条目覆盖。
        /// </summary>
        public string? FullPublicEntry { get; set; }
    }

    /// <summary>
    ///     Registers the annotated type as a card in the given pool.
    ///     将带注解的类型注册为给定牌池中的卡牌。
    /// </summary>
    /// <param name="poolType">
    ///     Target card pool type.
    ///     目标卡牌池类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCardAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Registers the annotated type as a relic in the given pool.
    ///     将带注解的类型注册为给定牌池中的遗物。
    /// </summary>
    /// <param name="poolType">
    ///     Target relic pool type.
    ///     目标遗物池类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterRelicAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Registers the annotated type as a potion in the given pool.
    ///     将带注解的类型注册为给定牌池中的药水。
    /// </summary>
    /// <param name="poolType">
    ///     Target potion pool type.
    ///     目标药水池类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPotionAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     Base metadata for character starter-content registrations.
    ///     角色初始内容注册的基础元数据。
    /// </summary>
    /// <param name="characterType">
    ///     Target character model type.
    ///     目标角色模型类型。
    /// </param>
    /// <param name="count">
    ///     How many copies to register.
    ///     要注册的份数。
    /// </param>
    public abstract class CharacterStarterRegistrationAttributeBase(Type characterType, int count = 1)
        : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target character model type.
        ///     目标角色模型类型。
        /// </summary>
        public Type CharacterType { get; } = characterType;

        /// <summary>
        ///     How many copies to register.
        ///     要注册的份数。
        /// </summary>
        public int Count { get; } = count;
    }

    /// <summary>
    ///     Registers the annotated card type as starter content for a character.
    ///     将带注解的卡牌类型注册为某个角色的初始内容。
    /// </summary>
    /// <param name="characterType">
    ///     Target character model type.
    ///     目标角色模型类型。
    /// </param>
    /// <param name="count">
    ///     How many copies to register.
    ///     要注册的份数。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterCardAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Registers the annotated relic type as starter content for a character.
    ///     将带注解的遗物类型注册为某个角色的初始内容。
    /// </summary>
    /// <param name="characterType">
    ///     Target character model type.
    ///     目标角色模型类型。
    /// </param>
    /// <param name="count">
    ///     How many copies to register.
    ///     要注册的份数。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterRelicAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Registers the annotated potion type as starter content for a character.
    ///     将带注解的药水类型注册为某个角色的初始内容。
    /// </summary>
    /// <param name="characterType">
    ///     Target character model type.
    ///     目标角色模型类型。
    /// </param>
    /// <param name="count">
    ///     How many copies to register.
    ///     要注册的份数。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterPotionAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     Base metadata for act-scoped registrations.
    ///     act 范围注册的基础元数据。
    /// </summary>
    /// <param name="actType">
    ///     Target act model type.
    ///     目标章节模型类型。
    /// </param>
    public abstract class ActScopedRegistrationAttributeBase(Type actType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     Target act model type.
        ///     目标章节模型类型。
        /// </summary>
        public Type ActType { get; } = actType;
    }

    /// <summary>
    ///     Registers the annotated encounter type for the given act.
    ///     将带注解的遭遇类型注册到给定章节。
    /// </summary>
    /// <param name="actType">
    ///     Target act model type.
    ///     目标章节模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEncounterAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Registers the annotated event type for the given act.
    ///     将带注解的事件类型注册到给定章节。
    /// </summary>
    /// <param name="actType">
    ///     Target act model type.
    ///     目标章节模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEventAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Registers the annotated ancient type for the given act.
    ///     将带注解的古代类型注册到给定章节。
    /// </summary>
    /// <param name="actType">
    ///     Target act model type.
    ///     目标章节模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAncientAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     Base metadata for owned keyword registrations.
    ///     owned keyword 注册的基础元数据。
    /// </summary>
    /// <param name="localKeywordStem">
    ///     Local mod-scoped keyword stem.
    ///     mod 作用域内的本地 keyword stem。
    /// </param>
    public abstract class KeywordRegistrationAttributeBase(string localKeywordStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local mod-scoped keyword stem.
        ///     mod 作用域内的本地 keyword stem。
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     Localization table containing the title key.
        ///     包含标题键的本地化表。
        /// </summary>
        public string TitleTable { get; set; } = "card_keywords";

        /// <summary>
        ///     Optional explicit localization key for the title.
        ///     标题使用的可选显式本地化键。
        /// </summary>
        public string? TitleKey { get; set; }

        /// <summary>
        ///     Optional localization table containing the description key.
        ///     包含描述键的可选本地化表。
        /// </summary>
        public string? DescriptionTable { get; set; }

        /// <summary>
        ///     Optional explicit localization key for the description.
        ///     描述使用的可选显式本地化键。
        /// </summary>
        public string? DescriptionKey { get; set; }

        /// <summary>
        ///     Optional icon path used by hover-tip rendering.
        ///     悬停提示渲染使用的可选图标路径。
        /// </summary>
        public string? IconPath { get; set; }
    }

    /// <summary>
    ///     Registers an owned keyword definition.
    ///     注册一个 owned keyword 定义。
    /// </summary>
    /// <param name="localKeywordStem">
    ///     Local mod-scoped keyword stem.
    ///     mod 作用域内的本地 keyword stem。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedKeywordAttribute(string localKeywordStem)
        : KeywordRegistrationAttributeBase(localKeywordStem)
    {
        /// <summary>
        ///     Optional placement for inline card-description injection.
        ///     内联卡牌描述注入使用的可选位置。
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether the keyword should appear in card hover tips.
        ///     此 keyword 是否应显示在卡牌悬停提示中。
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     Registers an owned card keyword definition using the card-keyword localization convention.
    ///     使用 card-keyword 本地化约定注册一个归属当前 mod 的卡牌关键词定义。
    /// </summary>
    /// <param name="localKeywordStem">
    ///     Local mod-scoped keyword stem.
    ///     mod 作用域内的本地 keyword stem。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardKeywordAttribute(string localKeywordStem)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local mod-scoped keyword stem.
        ///     mod 作用域内的本地 keyword stem。
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     Optional icon path used by hover-tip rendering.
        ///     悬停提示渲染使用的可选图标路径。
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Optional placement for inline card-description injection.
        ///     内联卡牌描述注入使用的可选位置。
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     Whether the keyword should appear in card hover tips.
        ///     此 keyword 是否应显示在卡牌悬停提示中。
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     Registers an owned custom <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> id for this mod assembly.
    ///     为此 mod 程序集注册一个归属当前 mod 的自定义 <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> id。
    /// </summary>
    /// <param name="localCardTagStem">
    ///     Local stem; combined with the mod id via <c>GetQualifiedCardTagId</c>.
    ///     本地 stem；会通过 <c>GetQualifiedCardTagId</c> 与 mod id 组合。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardTagAttribute(string localCardTagStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local mod-scoped card-tag stem.
        ///     mod 作用域内的本地 card-tag 词干。
        /// </summary>
        public string LocalCardTagStem { get; } = localCardTagStem;
    }

    /// <summary>
    ///     Registers the annotated type as a timeline epoch.
    ///     将带注解的类型注册为时间线纪元。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Registers the annotated type as a timeline story.
    ///     将带注解的类型注册为时间线 story。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     Binds the annotated epoch type into the given story column.
    ///     将带注解的纪元类型绑定到给定 story column。
    /// </summary>
    /// <param name="storyType">
    ///     Target story model type.
    ///     目标 story 模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryEpochAttribute(Type storyType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target story model type.
        ///     目标 story 模型类型。
        /// </summary>
        public Type StoryType { get; } = storyType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the first free slot in the given era column.
    ///     将带注解的 mod 纪元放入给定 era column 中第一个空 slot。
    /// </summary>
    /// <param name="era">
    ///     Target era column.
    ///     目标 era column。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAttribute(EpochEra era) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target era column.
        ///     目标 era column。
        /// </summary>
        public EpochEra Era { get; } = era;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly before the given anchor era.
    ///     将带注解的 mod 纪元放入严格位于给定锚点 era 之前的最近空列。
    /// </summary>
    /// <param name="anchorEra">
    ///     Anchor era whose left side should receive the epoch.
    ///     其左侧应接收该纪元的锚点 era。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose left side should receive the epoch.
        ///     其左侧应接收该纪元的锚点 era。
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly before the column of the reference epoch.
    ///     将带注解的 mod 纪元放入严格位于参考纪元所在列之前的最近空列。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column anchors the placement.
        ///     其列用于锚定位置的参考纪元。
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly after the given anchor era.
    ///     将带注解的 mod 纪元放入严格位于给定锚点 era 之后的最近空列。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose right side should receive the epoch.
        ///     其右侧应接收该纪元的锚点 era。
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the nearest free column strictly after the column of the reference epoch.
    ///     将带注解的 mod 纪元放入严格位于参考纪元所在列之后的最近空列。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column anchors the placement.
        ///     其列用于锚定位置的参考纪元。
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the same era column as the given anchor era.
    ///     将带注解的 mod 纪元放入与给定锚点 era 相同的 era column。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Anchor era whose column should be shared.
        ///     应共享其列的锚点 era。
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     Places the annotated mod epoch into the same era column as the reference epoch.
    ///     将带注解的 mod 纪元放入与参考纪元相同的 era column。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Reference epoch whose column should be shared.
        ///     应共享其列的参考纪元。
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     Registers an ArchaicTooth transcendence mapping from the annotated starter card type to the given ancient card.
    ///     注册从带注解的初始卡牌类型到给定古代卡牌的 ArchaicTooth 超越映射。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterArchaicToothTranscendenceAttribute(Type ancientCardType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Ancient card type produced by transcendence.
        ///     超越产生的古代卡牌类型。
        /// </summary>
        public Type AncientCardType { get; } = ancientCardType;
    }

    /// <summary>
    ///     Registers a TouchOfOrobas refinement mapping from the annotated starter relic type to the given upgraded relic.
    ///     注册从带注解的初始遗物类型到给定升级遗物的 TouchOfOrobas 精炼映射。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTouchOfOrobasRefinementAttribute(Type upgradedRelicType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Upgraded relic type produced by refinement.
        ///     精炼产生的升级遗物类型。
        /// </summary>
        public Type UpgradedRelicType { get; } = upgradedRelicType;
    }

    /// <summary>
    ///     Registers explicit card unlock content for the annotated epoch and gates those cards behind it.
    ///     为带注解的纪元注册显式卡牌解锁内容，并将这些卡牌 gated 在其之后。
    /// </summary>
    /// <param name="cardTypes">
    ///     Card model types revealed by the epoch.
    ///     该纪元揭示的卡牌模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochCardsAttribute(params Type[] cardTypes) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Card model types revealed by the epoch.
        ///     该纪元揭示的卡牌模型类型。
        /// </summary>
        public IReadOnlyList<Type> CardTypes { get; } = cardTypes;
    }

    /// <summary>
    ///     Gates every registered card in the given pool behind the annotated epoch.
    ///     将给定牌池中每张已注册卡牌 gated 在带注解的纪元之后。
    /// </summary>
    /// <param name="poolType">
    ///     Card pool model type.
    ///     卡牌池模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireAllCardsInPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Card pool model type.
        ///     卡牌池模型类型。
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     Registers every relic in the given pool as unlock content for the annotated epoch and gates them behind it.
    ///     将给定牌池中的每个遗物注册为带注解纪元的解锁内容，并将它们 gated 在其之后。
    /// </summary>
    /// <param name="poolType">
    ///     Relic pool model type.
    ///     遗物池模型类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochRelicsFromPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Relic pool model type.
        ///     遗物池模型类型。
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     Gates the annotated content type behind the given epoch.
    ///     将带注解的内容类型 gated 在给定纪元之后。
    /// </summary>
    /// <param name="epochType">
    ///     Required epoch type.
    ///     所需纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireEpochAttribute(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Required epoch type.
        ///     所需纪元类型。
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     Base metadata for character-to-epoch unlock registrations.
    ///     角色到纪元解锁注册的基础元数据。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    public abstract class CharacterEpochRegistrationAttributeBase(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Target epoch type.
        ///     目标纪元类型。
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     Unlocks an epoch after completing any run as the annotated character.
    ///     使用带注解的角色完成任意跑局后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Unlocks an epoch after winning a run as the annotated character.
    ///     使用带注解的角色赢得一次跑局后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterWinAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Unlocks an epoch after winning at or above a given ascension as the annotated character.
    ///     使用带注解的角色在给定进阶等级或更高等级获胜后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    /// <param name="ascensionLevel">
    ///     Minimum ascension level.
    ///     最低进阶等级。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionWinAttribute(Type epochType, int ascensionLevel)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Minimum ascension level required for the unlock.
        ///     解锁所需的最低进阶等级。
        /// </summary>
        public int AscensionLevel { get; } = ascensionLevel;
    }

    /// <summary>
    ///     Unlocks an epoch after defeating a number of elites as the annotated character.
    ///     使用带注解的角色击败指定数量的精英后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    /// <param name="requiredEliteWins">
    ///     Required elite victories.
    ///     所需精英胜利次数。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterEliteVictoriesAttribute(Type epochType, int requiredEliteWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Required elite victories.
        ///     所需精英胜利次数。
        /// </summary>
        public int RequiredEliteWins { get; } = requiredEliteWins;
    }

    /// <summary>
    ///     Unlocks an epoch after defeating a number of bosses as the annotated character.
    ///     使用带注解的角色击败指定数量的 boss 后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    /// <param name="requiredBossWins">
    ///     Required boss victories.
    ///     所需 boss 胜利次数。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterBossVictoriesAttribute(Type epochType, int requiredBossWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     Required boss victories.
        ///     所需 boss 胜利次数。
        /// </summary>
        public int RequiredBossWins { get; } = requiredBossWins;
    }

    /// <summary>
    ///     Unlocks an epoch after an ascension-one win as the annotated character.
    ///     使用带注解的角色取得进阶一胜利后解锁纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionOneWinAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Reveals ascension UI for the annotated character after the given epoch is revealed.
    ///     给定纪元 reveal 后，为带注解的角色显示进阶 UI。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RevealAscensionAfterEpochAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     Grants the given epoch through the post-run character unlock flow for the annotated character.
    ///     通过带注解角色的跑局后角色解锁流程授予给定纪元。
    /// </summary>
    /// <param name="epochType">
    ///     Target epoch type.
    ///     目标纪元类型。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockCharacterAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);
}
