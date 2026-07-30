using MegaCrit.Sts2.Core.Timeline;
using STS2RitsuLib.Keywords;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">Base metadata for declarations processed by the RitsuLib auto-registration pipeline.</para>
    ///     <para xml:lang="zh-CN">由 RitsuLib 自动注册管线处理的声明式注册基础元数据。</para>
    /// </summary>
    public abstract class AutoRegistrationAttribute : Attribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local ordering within the same registration phase. Lower values run first.</para>
        ///     <para xml:lang="zh-CN">同一注册阶段内的局部排序。数值越小越先运行。</para>
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         When <see langword="true" /> on an attribute declared on a base type, the registration is available to concrete
        ///         derived
        ///         types. The nearest declaration wins per logical registration slot: a direct declaration can replace
        ///         inherited configuration such as a pool, count, path, placement, or threshold, while registrations for
        ///         distinct target IDs or scopes remain additive.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当基类型上声明的特性将此项设为 <see langword="true" /> 时，该注册可应用到具体派生类型。
        ///         每个逻辑注册槽位采用距离派生类型最近的声明：
        ///         直接声明可以替换继承的卡池、数量、路径、位置或阈值等配置，而不同目标 ID 或作用域的注册仍会累加。
        ///     </para>
        /// </summary>
        public bool Inherit { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Base metadata for content registrations dispatched through <c>ModContentRegistry</c>.</para>
    ///     <para xml:lang="zh-CN">通过 <c>ModContentRegistry</c> 分发的内容注册基础元数据。</para>
    /// </summary>
    public abstract class ContentRegistrationAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a character model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为角色模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as an act model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为阶段模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a monster model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为怪物模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterMonsterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a power model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为能力模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPowerAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as an orb model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为充能球模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOrbAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as an enchantment model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为附魔模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEnchantmentAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as an affliction model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为侵蚀模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAfflictionAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as an achievement model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为成就模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAchievementAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a singleton model.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为单例模型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSingletonAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a model capability.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为模型能力。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterModelCapabilityAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Optional stable, author-chosen capability ID and public-entry stem.</para>
        ///     <para xml:lang="zh-CN">可选的、由作者指定的稳定能力 ID 与公共条目词干。</para>
        /// </summary>
        public string? StableEntryStem { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional complete fixed public-entry and capability-ID override.</para>
        ///     <para xml:lang="zh-CN">可选的完整固定公共条目及能力 ID 覆盖值。</para>
        /// </summary>
        public string? FullPublicEntry { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Adds the annotated capability type to the default capability set for instances of a model type.</para>
    ///     <para xml:lang="zh-CN">将标注的能力类型添加到目标模型类型实例的默认能力集合。</para>
    /// </summary>
    /// <param name="targetModelType">
    ///     <para xml:lang="en">Target model type.</para>
    ///     <para xml:lang="zh-CN">目标模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterDefaultModelCapabilityAttribute(Type targetModelType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target model type.</para>
        ///     <para xml:lang="zh-CN">目标模型类型。</para>
        /// </summary>
        public Type TargetModelType { get; } = targetModelType;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional stable modifier ID. By default, RitsuLib derives a mod-scoped ID from the capability
        ///         and target types.
        ///     </para>
        ///     <para xml:lang="zh-CN">可选的稳定特效 ID。默认由 RitsuLib 根据能力类型和目标类型派生模组作用域 ID。</para>
        /// </summary>
        public string? ModifierId { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a good daily modifier.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为正面每日特效。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGoodModifierAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Negative values insert before the current good-modifier list segment; non-negative values
        ///         insert after.
        ///     </para>
        ///     <para xml:lang="zh-CN">负值插入当前正面特效列表区段之前；非负值插入之后。</para>
        /// </summary>
        public int ModifierListSortOrder { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a bad daily modifier.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为负面每日特效。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterBadModifierAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Negative values insert before the current bad-modifier list segment; non-negative values insert
        ///         after.
        ///     </para>
        ///     <para xml:lang="zh-CN">负值插入当前负面特效列表区段之前；非负值插入之后。</para>
        /// </summary>
        public int ModifierListSortOrder { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a mutually exclusive modifier group for custom and daily run selection.</para>
    ///     <para xml:lang="zh-CN">为自定模式和每日挑战的随机特效选择注册互斥组。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterMutuallyExclusiveModifierGroupAttribute : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a group containing the annotated modifier type and <paramref name="memberTypes" />.</para>
        ///     <para xml:lang="zh-CN">创建包含标注特效类型及 <paramref name="memberTypes" /> 的互斥组。</para>
        /// </summary>
        public RegisterMutuallyExclusiveModifierGroupAttribute(params Type[] memberTypes)
        {
            MemberTypes = memberTypes ?? throw new ArgumentNullException(nameof(memberTypes));
        }

        /// <summary>
        ///     <para xml:lang="en">Additional modifier types in the same exclusivity group.</para>
        ///     <para xml:lang="zh-CN">同一互斥组中的其他特效类型。</para>
        /// </summary>
        public Type[] MemberTypes { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a shared card pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为共享卡牌池。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedCardPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a shared relic pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为共享遗物池。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedRelicPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a shared potion pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为共享药水池。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedPotionPoolAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a shared event.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为共享事件。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedEventAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a shared Ancients event.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为共享先古之民事件。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSharedAncientAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a global encounter.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为全局遭遇。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterGlobalEncounterAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Base metadata for pool-backed model registrations with configurable public-entry naming.</para>
    ///     <para xml:lang="zh-CN">可配置公共条目命名方式的池类模型注册基础元数据。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Target pool model type.</para>
    ///     <para xml:lang="zh-CN">目标池模型类型。</para>
    /// </param>
    public abstract class ModelPublicEntryRegistrationAttributeBase(Type poolType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target pool model type.</para>
        ///     <para xml:lang="zh-CN">目标池模型类型。</para>
        /// </summary>
        public Type PoolType { get; } = poolType;

        /// <summary>
        ///     <para xml:lang="en">Optional stable, author-chosen type-name stem.</para>
        ///     <para xml:lang="zh-CN">可选的、由作者指定的稳定类型名词干。</para>
        /// </summary>
        public string? StableEntryStem { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional full fixed public entry override.</para>
        ///     <para xml:lang="zh-CN">可选的完整固定公共条目覆盖。</para>
        /// </summary>
        public string? FullPublicEntry { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a card in the given pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为指定卡牌池中的卡牌。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Target card pool type.</para>
    ///     <para xml:lang="zh-CN">目标卡牌池类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCardAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a relic in the given pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为指定遗物池中的遗物。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Target relic pool type.</para>
    ///     <para xml:lang="zh-CN">目标遗物池类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterRelicAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated card type as a candidate for the Trash Heap event's Grab option.</para>
    ///     <para xml:lang="zh-CN">将标注卡牌类型注册为“垃圾堆”事件“随便拿点垃圾”选项的候选。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTrashHeapCardAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated relic type as a candidate for the Trash Heap event's Dive In option.</para>
    ///     <para xml:lang="zh-CN">将标注遗物类型注册为“垃圾堆”事件“扎进垃圾堆”选项的候选。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTrashHeapRelicAttribute : ContentRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a potion in the given pool.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为指定药水池中的药水。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Target potion pool type.</para>
    ///     <para xml:lang="zh-CN">目标药水池类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterPotionAttribute(Type poolType) : ModelPublicEntryRegistrationAttributeBase(poolType);

    /// <summary>
    ///     <para xml:lang="en">Base metadata for character starter-content registrations.</para>
    ///     <para xml:lang="zh-CN">角色初始内容注册的基础元数据。</para>
    /// </summary>
    /// <param name="characterType">
    ///     <para xml:lang="en">Target character model type.</para>
    ///     <para xml:lang="zh-CN">目标角色模型类型。</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">How many copies to register.</para>
    ///     <para xml:lang="zh-CN">要注册的份数。</para>
    /// </param>
    public abstract class CharacterStarterRegistrationAttributeBase(Type characterType, int count = 1)
        : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target character model type.</para>
        ///     <para xml:lang="zh-CN">目标角色模型类型。</para>
        /// </summary>
        public Type CharacterType { get; } = characterType;

        /// <summary>
        ///     <para xml:lang="en">How many copies to register.</para>
        ///     <para xml:lang="zh-CN">要注册的份数。</para>
        /// </summary>
        public int Count { get; } = count;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated card type as starter content for a character.</para>
    ///     <para xml:lang="zh-CN">将标注卡牌类型注册为指定角色的初始内容。</para>
    /// </summary>
    /// <param name="characterType">
    ///     <para xml:lang="en">Target character model type.</para>
    ///     <para xml:lang="zh-CN">目标角色模型类型。</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">How many copies to register.</para>
    ///     <para xml:lang="zh-CN">要注册的份数。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterCardAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated relic type as starter content for a character.</para>
    ///     <para xml:lang="zh-CN">将标注遗物类型注册为指定角色的初始内容。</para>
    /// </summary>
    /// <param name="characterType">
    ///     <para xml:lang="en">Target character model type.</para>
    ///     <para xml:lang="zh-CN">目标角色模型类型。</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">How many copies to register.</para>
    ///     <para xml:lang="zh-CN">要注册的份数。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterRelicAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated potion type as starter content for a character.</para>
    ///     <para xml:lang="zh-CN">将标注药水类型注册为指定角色的初始内容。</para>
    /// </summary>
    /// <param name="characterType">
    ///     <para xml:lang="en">Target character model type.</para>
    ///     <para xml:lang="zh-CN">目标角色模型类型。</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">How many copies to register.</para>
    ///     <para xml:lang="zh-CN">要注册的份数。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterCharacterStarterPotionAttribute(Type characterType, int count = 1)
        : CharacterStarterRegistrationAttributeBase(characterType, count);

    /// <summary>
    ///     <para xml:lang="en">Base metadata for act-scoped registrations.</para>
    ///     <para xml:lang="zh-CN">按阶段限定内容注册范围的基础元数据。</para>
    /// </summary>
    /// <param name="actType">
    ///     <para xml:lang="en">Target act model type.</para>
    ///     <para xml:lang="zh-CN">目标阶段模型类型。</para>
    /// </param>
    public abstract class ActScopedRegistrationAttributeBase(Type actType) : ContentRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target act model type.</para>
        ///     <para xml:lang="zh-CN">目标阶段模型类型。</para>
        /// </summary>
        public Type ActType { get; } = actType;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated encounter type for the given act.</para>
    ///     <para xml:lang="zh-CN">将标注遭遇类型注册到指定阶段。</para>
    /// </summary>
    /// <param name="actType">
    ///     <para xml:lang="en">Target act model type.</para>
    ///     <para xml:lang="zh-CN">目标阶段模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEncounterAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated event type for the given act.</para>
    ///     <para xml:lang="zh-CN">将标注事件类型注册到指定阶段。</para>
    /// </summary>
    /// <param name="actType">
    ///     <para xml:lang="en">Target act model type.</para>
    ///     <para xml:lang="zh-CN">目标阶段模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActEventAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated ancient-event type for the given act.</para>
    ///     <para xml:lang="zh-CN">将标注先古之民事件类型注册到指定阶段。</para>
    /// </summary>
    /// <param name="actType">
    ///     <para xml:lang="en">Target act model type.</para>
    ///     <para xml:lang="zh-CN">目标阶段模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterActAncientAttribute(Type actType) : ActScopedRegistrationAttributeBase(actType);

    /// <summary>
    ///     <para xml:lang="en">Base metadata for mod-owned keyword registrations.</para>
    ///     <para xml:lang="zh-CN">归属于模组的关键词注册基础元数据。</para>
    /// </summary>
    /// <param name="localKeywordStem">
    ///     <para xml:lang="en">Local keyword stem within the owning mod's namespace.</para>
    ///     <para xml:lang="zh-CN">归属模组命名空间内的本地关键词词干。</para>
    /// </param>
    public abstract class KeywordRegistrationAttributeBase(string localKeywordStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local keyword stem within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地关键词词干。</para>
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     <para xml:lang="en">Localization table containing the title key.</para>
        ///     <para xml:lang="zh-CN">包含标题键的本地化表。</para>
        /// </summary>
        public string TitleTable { get; set; } = "card_keywords";

        /// <summary>
        ///     <para xml:lang="en">Optional explicit localization key for the title.</para>
        ///     <para xml:lang="zh-CN">标题使用的可选显式本地化键。</para>
        /// </summary>
        public string? TitleKey { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional localization table containing the description key.</para>
        ///     <para xml:lang="zh-CN">包含描述键的可选本地化表。</para>
        /// </summary>
        public string? DescriptionTable { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional explicit localization key for the description.</para>
        ///     <para xml:lang="zh-CN">描述使用的可选显式本地化键。</para>
        /// </summary>
        public string? DescriptionKey { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional icon path used by hover-tip rendering.</para>
        ///     <para xml:lang="zh-CN">悬停提示渲染使用的可选图标路径。</para>
        /// </summary>
        public string? IconPath { get; set; }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a mod-owned keyword definition.</para>
    ///     <para xml:lang="zh-CN">注册归属于模组的关键词定义。</para>
    /// </summary>
    /// <param name="localKeywordStem">
    ///     <para xml:lang="en">Local keyword stem within the owning mod's namespace.</para>
    ///     <para xml:lang="zh-CN">归属模组命名空间内的本地关键词词干。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedKeywordAttribute(string localKeywordStem)
        : KeywordRegistrationAttributeBase(localKeywordStem)
    {
        /// <summary>
        ///     <para xml:lang="en">Optional placement for inline card-description injection.</para>
        ///     <para xml:lang="zh-CN">内联卡牌描述注入使用的可选位置。</para>
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     <para xml:lang="en">Whether the keyword appears in card hover tips. Defaults to <see langword="true" />.</para>
        ///     <para xml:lang="zh-CN">此关键词是否显示在卡牌悬停提示中。默认为 <see langword="true" />。</para>
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a mod-owned keyword using the game's card-keyword localization convention.</para>
    ///     <para xml:lang="zh-CN">按照游戏的卡牌关键词本地化约定，注册归属于模组的关键词。</para>
    /// </summary>
    /// <param name="localKeywordStem">
    ///     <para xml:lang="en">Local keyword stem within the owning mod's namespace.</para>
    ///     <para xml:lang="zh-CN">归属模组命名空间内的本地关键词词干。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardKeywordAttribute(string localKeywordStem)
        : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local keyword stem within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地关键词词干。</para>
        /// </summary>
        public string LocalKeywordStem { get; } = localKeywordStem;

        /// <summary>
        ///     <para xml:lang="en">Optional icon path used by hover-tip rendering.</para>
        ///     <para xml:lang="zh-CN">悬停提示渲染使用的可选图标路径。</para>
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Optional placement for inline card-description injection.</para>
        ///     <para xml:lang="zh-CN">内联卡牌描述注入使用的可选位置。</para>
        /// </summary>
        public ModKeywordCardDescriptionPlacement CardDescriptionPlacement { get; set; } =
            ModKeywordCardDescriptionPlacement.None;

        /// <summary>
        ///     <para xml:lang="en">Whether the keyword appears in card hover tips. Defaults to <see langword="true" />.</para>
        ///     <para xml:lang="zh-CN">此关键词是否显示在卡牌悬停提示中。默认为 <see langword="true" />。</para>
        /// </summary>
        public bool IncludeInCardHoverTip { get; set; } = true;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a mod-owned custom <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> ID.</para>
    ///     <para xml:lang="zh-CN">注册归属于模组的自定义 <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardTag" /> ID。</para>
    /// </summary>
    /// <param name="localCardTagStem">
    ///     <para xml:lang="en">Local stem combined with the mod ID through <c>GetQualifiedCardTagId</c>.</para>
    ///     <para xml:lang="zh-CN">通过 <c>GetQualifiedCardTagId</c> 与模组 ID 组合的本地词干。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedCardTagAttribute(string localCardTagStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local card-tag stem within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地卡牌标签词干。</para>
        /// </summary>
        public string LocalCardTagStem { get; } = localCardTagStem;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a timeline epoch.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为时间线历史节点。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated type as a timeline story.</para>
    ///     <para xml:lang="zh-CN">将标注类型注册为时间线故事。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryAttribute : AutoRegistrationAttribute;

    /// <summary>
    ///     <para xml:lang="en">Adds the annotated epoch type to the given story.</para>
    ///     <para xml:lang="zh-CN">将标注历史节点类型加入指定故事。</para>
    /// </summary>
    /// <param name="storyType">
    ///     <para xml:lang="en">Target story model type.</para>
    ///     <para xml:lang="zh-CN">目标故事模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterStoryEpochAttribute(Type storyType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target story model type.</para>
        ///     <para xml:lang="zh-CN">目标故事模型类型。</para>
        /// </summary>
        public Type StoryType { get; } = storyType;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the first free position of the given era's timeline column.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入指定历史时期时间线列中的第一个空位。</para>
    /// </summary>
    /// <param name="era">
    ///     <para xml:lang="en">Target era.</para>
    ///     <para xml:lang="zh-CN">目标历史时期。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAttribute(EpochEra era) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target era.</para>
        ///     <para xml:lang="zh-CN">目标历史时期。</para>
        /// </summary>
        public EpochEra Era { get; } = era;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the nearest free column before the anchor era.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入锚点历史时期之前最近的空闲时间线列。</para>
    /// </summary>
    /// <param name="anchorEra">
    ///     <para xml:lang="en">Era used as the placement anchor.</para>
    ///     <para xml:lang="zh-CN">用作位置锚点的历史时期。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Era used as the placement anchor.</para>
        ///     <para xml:lang="zh-CN">用作位置锚点的历史时期。</para>
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the nearest free column before the reference epoch's column.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入参考历史节点所在列之前最近的空闲时间线列。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotBeforeEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Reference epoch whose column anchors the placement.</para>
        ///     <para xml:lang="zh-CN">以其所在列作为位置锚点的参考历史节点。</para>
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the nearest free column after the anchor era.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入锚点历史时期之后最近的空闲时间线列。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Era used as the placement anchor.</para>
        ///     <para xml:lang="zh-CN">用作位置锚点的历史时期。</para>
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the nearest free column after the reference epoch's column.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入参考历史节点所在列之后最近的空闲时间线列。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotAfterEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Reference epoch whose column anchors the placement.</para>
        ///     <para xml:lang="zh-CN">以其所在列作为位置锚点的参考历史节点。</para>
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the anchor era's timeline column.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入锚点历史时期所在的时间线列。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInColumnAttribute(EpochEra anchorEra) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Era whose timeline column is used.</para>
        ///     <para xml:lang="zh-CN">使用其时间线列的历史时期。</para>
        /// </summary>
        public EpochEra AnchorEra { get; } = anchorEra;
    }

    /// <summary>
    ///     <para xml:lang="en">Places the annotated mod epoch in the reference epoch's timeline column.</para>
    ///     <para xml:lang="zh-CN">将标注的模组历史节点放入参考历史节点所在的同一列。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class AutoTimelineSlotInEpochColumnAttribute(Type referenceEpochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Reference epoch whose column should be shared.</para>
        ///     <para xml:lang="zh-CN">应与其共享列的参考历史节点。</para>
        /// </summary>
        public Type ReferenceEpochType { get; } = referenceEpochType;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers an Archaic Tooth transcendence mapping from the annotated starter card to an ancient
    ///         card.
    ///     </para>
    ///     <para xml:lang="zh-CN">注册由“古老牙齿”将标注初始卡牌转化为指定先古卡牌的超越映射。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterArchaicToothTranscendenceAttribute(Type ancientCardType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Ancient card type produced by transcendence.</para>
        ///     <para xml:lang="zh-CN">超越产生的先古卡牌类型。</para>
        /// </summary>
        public Type AncientCardType { get; } = ancientCardType;
    }

    /// <summary>
    ///     <para xml:lang="en">Registers the annotated ancient card as a preferred Dusty Tome candidate for a character.</para>
    ///     <para xml:lang="zh-CN">将标注先古卡牌注册为指定角色的“尘封魔典”优先候选。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterDustyTomeCardAttribute(Type characterType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Character type whose Dusty Tome candidates include the annotated card.</para>
        ///     <para xml:lang="zh-CN">其“尘封魔典”候选池应包含标注卡牌的角色类型。</para>
        /// </summary>
        public Type CharacterType { get; } = characterType;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers a Touch of Orobas refinement mapping from the annotated starter relic to an upgraded
    ///         relic.
    ///     </para>
    ///     <para xml:lang="zh-CN">注册由“欧洛巴斯之触”将标注初始遗物变为指定升级遗物的精炼映射。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTouchOfOrobasRefinementAttribute(Type upgradedRelicType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Upgraded relic type produced by refinement.</para>
        ///     <para xml:lang="zh-CN">精炼产生的升级遗物类型。</para>
        /// </summary>
        public Type UpgradedRelicType { get; } = upgradedRelicType;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers explicit card unlock content for the annotated epoch and requires that epoch for
    ///         those cards.
    ///     </para>
    ///     <para xml:lang="zh-CN">为标注历史节点注册显式卡牌解锁内容，并要求先揭示该历史节点才能解锁这些卡牌。</para>
    /// </summary>
    /// <param name="cardTypes">
    ///     <para xml:lang="en">Card model types revealed by the epoch.</para>
    ///     <para xml:lang="zh-CN">该历史节点揭示的卡牌模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochCardsAttribute(params Type[] cardTypes) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Card model types revealed by the epoch.</para>
        ///     <para xml:lang="zh-CN">该历史节点揭示的卡牌模型类型。</para>
        /// </summary>
        public IReadOnlyList<Type> CardTypes { get; } = cardTypes;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the annotated epoch for every registered card in the given pool.</para>
    ///     <para xml:lang="zh-CN">要求先揭示标注历史节点，才能解锁指定卡牌池中的每张已注册卡牌。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Card pool model type.</para>
    ///     <para xml:lang="zh-CN">卡牌池模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireAllCardsInPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Card pool model type.</para>
        ///     <para xml:lang="zh-CN">卡牌池模型类型。</para>
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Registers every relic in the given pool as unlock content for, and gated by, the annotated
    ///         epoch.
    ///     </para>
    ///     <para xml:lang="zh-CN">将指定遗物池中的每件遗物注册为标注历史节点的解锁内容，并要求先揭示该节点。</para>
    /// </summary>
    /// <param name="poolType">
    ///     <para xml:lang="en">Relic pool model type.</para>
    ///     <para xml:lang="zh-CN">遗物池模型类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterEpochRelicsFromPoolAttribute(Type poolType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Relic pool model type.</para>
        ///     <para xml:lang="zh-CN">遗物池模型类型。</para>
        /// </summary>
        public Type PoolType { get; } = poolType;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the given epoch before the annotated content type is unlocked.</para>
    ///     <para xml:lang="zh-CN">要求先揭示指定历史节点，才能解锁标注内容类型。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Required epoch type.</para>
    ///     <para xml:lang="zh-CN">所需历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RequireEpochAttribute(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Required epoch type.</para>
        ///     <para xml:lang="zh-CN">所需历史节点类型。</para>
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     <para xml:lang="en">Base metadata for character-to-epoch unlock registrations.</para>
    ///     <para xml:lang="zh-CN">角色与历史节点解锁条件注册的基础元数据。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    public abstract class CharacterEpochRegistrationAttributeBase(Type epochType) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Target epoch type.</para>
        ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
        /// </summary>
        public Type EpochType { get; } = epochType;
    }

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after completing any run as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色完成任意一局游戏后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after winning a run as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色通关一次后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterWinAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after winning at or above a given ascension as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色在指定进阶等级或更高等级通关后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    /// <param name="ascensionLevel">
    ///     <para xml:lang="en">Minimum ascension level.</para>
    ///     <para xml:lang="zh-CN">最低进阶等级。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionWinAttribute(Type epochType, int ascensionLevel)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     <para xml:lang="en">Minimum ascension level required for the unlock.</para>
        ///     <para xml:lang="zh-CN">解锁所需的最低进阶等级。</para>
        /// </summary>
        public int AscensionLevel { get; } = ascensionLevel;
    }

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after defeating a number of elites as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色击败指定数量的精英后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    /// <param name="requiredEliteWins">
    ///     <para xml:lang="en">Required elite victories.</para>
    ///     <para xml:lang="zh-CN">所需精英击败次数。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterEliteVictoriesAttribute(Type epochType, int requiredEliteWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     <para xml:lang="en">Required elite victories.</para>
        ///     <para xml:lang="zh-CN">所需精英击败次数。</para>
        /// </summary>
        public int RequiredEliteWins { get; } = requiredEliteWins;
    }

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after defeating a number of bosses as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色击败指定数量的 Boss 后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    /// <param name="requiredBossWins">
    ///     <para xml:lang="en">Required boss victories.</para>
    ///     <para xml:lang="zh-CN">所需 Boss 击败次数。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterBossVictoriesAttribute(Type epochType, int requiredBossWins = 15)
        : CharacterEpochRegistrationAttributeBase(epochType)
    {
        /// <summary>
        ///     <para xml:lang="en">Required boss victories.</para>
        ///     <para xml:lang="zh-CN">所需 Boss 击败次数。</para>
        /// </summary>
        public int RequiredBossWins { get; } = requiredBossWins;
    }

    /// <summary>
    ///     <para xml:lang="en">Unlocks an epoch after an Ascension 1 win as the annotated character.</para>
    ///     <para xml:lang="zh-CN">使用标注角色通关进阶 1 后揭示历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockEpochAfterAscensionOneWinAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     <para xml:lang="en">Reveals the Ascension UI for the annotated character after the given epoch is revealed.</para>
    ///     <para xml:lang="zh-CN">指定历史节点揭示后，显示标注角色的进阶界面。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RevealAscensionAfterEpochAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);

    /// <summary>
    ///     <para xml:lang="en">Grants the given epoch through the post-run character unlock flow for the annotated character.</para>
    ///     <para xml:lang="zh-CN">通过标注角色的局后角色解锁检查授予指定历史节点。</para>
    /// </summary>
    /// <param name="epochType">
    ///     <para xml:lang="en">Target epoch type.</para>
    ///     <para xml:lang="zh-CN">目标历史节点类型。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class UnlockCharacterAfterRunAsAttribute(Type epochType)
        : CharacterEpochRegistrationAttributeBase(epochType);
}
