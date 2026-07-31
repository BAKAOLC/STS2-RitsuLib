using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     <para xml:lang="en">Groups one or more untyped hand-card outline rules.</para>
    ///     <para xml:lang="zh-CN">组合一条或多条非泛型手牌描边规则。</para>
    /// </summary>
    public readonly record struct ModCardHandOutlineRules
    {
        private readonly ModCardHandOutlineSwitchRule[] _rules;

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set from the supplied rules.</para>
        ///     <para xml:lang="zh-CN">使用给定规则创建规则集。</para>
        /// </summary>
        public ModCardHandOutlineRules(params ModCardHandOutlineSwitchRule[] rules)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = [.. rules];
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set from the supplied rules.</para>
        ///     <para xml:lang="zh-CN">使用给定规则创建规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules Of(params ModCardHandOutlineSwitchRule[] rules)
        {
            return new(rules);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set containing one fixed-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条固定颜色规则的规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules Fixed(
            Func<CardModel, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            return new(ModCardHandOutlineSwitchRule.Fixed(when, color, priority, visibleWhenUnplayable));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one fixed-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条固定颜色规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Fixed<TCard>(
            Func<TCard, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
            where TCard : CardModel
        {
            return ModCardHandOutlineRules<TCard>.Fixed(when, color, priority, visibleWhenUnplayable);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set containing one color-resolving rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条颜色解析规则的规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules Switch(
            Func<CardModel, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
        {
            return new(ModCardHandOutlineSwitchRule.Switch(colorWhen, priority, visibleWhenUnplayable,
                refreshEveryFrame));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one color-resolving rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条颜色解析规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Switch<TCard>(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
            where TCard : CardModel
        {
            return ModCardHandOutlineRules<TCard>.Switch(colorWhen, priority, visibleWhenUnplayable,
                refreshEveryFrame);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set containing one predicate-guarded dynamic-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条由谓词控制的动态颜色规则的规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules Dynamic(
            Func<CardModel, bool> when,
            Func<CardModel, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            return new(ModCardHandOutlineSwitchRule.Dynamic(when, colorWhen, priority, visibleWhenUnplayable));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one predicate-guarded dynamic-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条由谓词控制的动态颜色规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Dynamic<TCard>(
            Func<TCard, bool> when,
            Func<TCard, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
            where TCard : CardModel
        {
            return ModCardHandOutlineRules<TCard>.Dynamic(when, colorWhen, priority, visibleWhenUnplayable);
        }

        internal IEnumerable<ModCardHandOutlineSwitchRule> Enumerate()
        {
            return _rules ?? [];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Groups one or more hand-card outline rules typed for <typeparamref name="TCard" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         组合一条或多条适用于 <typeparamref name="TCard" /> 的泛型手牌描边规则。
    ///     </para>
    /// </summary>
    public readonly record struct ModCardHandOutlineRules<TCard> where TCard : CardModel
    {
        private readonly ModCardHandOutlineSwitchRule<TCard>[] _rules;

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set from the supplied rules.</para>
        ///     <para xml:lang="zh-CN">使用给定规则创建泛型规则集。</para>
        /// </summary>
        public ModCardHandOutlineRules(params ModCardHandOutlineSwitchRule<TCard>[] rules)
        {
            ArgumentNullException.ThrowIfNull(rules);
            _rules = [.. rules];
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set from the supplied rules.</para>
        ///     <para xml:lang="zh-CN">使用给定规则创建泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Of(params ModCardHandOutlineSwitchRule<TCard>[] rules)
        {
            return new(rules);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one fixed-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条固定颜色规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Fixed(
            Func<TCard, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            return new(ModCardHandOutlineSwitchRule<TCard>.Fixed(when, color, priority, visibleWhenUnplayable));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one color-resolving rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条颜色解析规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Switch(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
        {
            return new(ModCardHandOutlineSwitchRule<TCard>.Switch(
                colorWhen,
                priority,
                visibleWhenUnplayable,
                refreshEveryFrame));
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule set containing one predicate-guarded dynamic-color rule.</para>
        ///     <para xml:lang="zh-CN">创建包含一条由谓词控制的动态颜色规则的泛型规则集。</para>
        /// </summary>
        public static ModCardHandOutlineRules<TCard> Dynamic(
            Func<TCard, bool> when,
            Func<TCard, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            return new(ModCardHandOutlineSwitchRule<TCard>.Dynamic(when, colorWhen, priority, visibleWhenUnplayable));
        }

        /// <summary>
        ///     <para xml:lang="en">Converts typed rules to the untyped registry representation.</para>
        ///     <para xml:lang="zh-CN">将泛型规则集转换为注册表使用的非泛型表示。</para>
        /// </summary>
        public static implicit operator ModCardHandOutlineRules(ModCardHandOutlineRules<TCard> rules)
        {
            return rules.ToUntyped();
        }

        internal ModCardHandOutlineRules ToUntyped()
        {
            return ModCardHandOutlineRules.Of([.. (_rules ?? []).Select(static rule => rule.ToUntyped())]);
        }
    }
}
