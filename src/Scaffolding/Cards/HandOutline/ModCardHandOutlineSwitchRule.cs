using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     Type-erased hand-card outline rule used by the registry storage layer. Prefer
    ///     <see cref="ModCardHandOutlineSwitchRule{TCard}" /> at public registration call sites.
    ///     注册表存储层使用的类型擦除手牌描边规则。公开注册调用点优先使用
    ///     <see cref="ModCardHandOutlineSwitchRule{TCard}" />。
    /// </summary>
    /// <param name="ColorWhen">
    ///     Returns a color when this rule should apply; returns <see langword="null" /> to skip the rule.
    ///     规则应生效时返回颜色；返回 <see langword="null" /> 时跳过规则。
    /// </param>
    /// <param name="Priority">
    ///     When several rules return a color, the highest <paramref name="Priority" /> wins; ties favor the most recently
    ///     registered rule.
    ///     多条规则返回颜色时，最高 <paramref name="Priority" /> 获胜；平手时优先最近注册的规则。
    /// </param>
    /// <param name="VisibleWhenUnplayable">
    ///     If true, the highlight is forced visible with this color even when vanilla would not show one.
    ///     如果为 true，即使原版不会显示高亮，也会强制以此颜色显示。
    /// </param>
    /// <param name="RefreshEveryFrame">
    ///     If true, the resolver is polled while the holder is alive so dynamic colors can change without a card refresh.
    ///     如果为 true，holder 存活期间会轮询解析器，使动态颜色无需卡牌刷新即可变化。
    /// </param>
    public readonly record struct ModCardHandOutlineSwitchRule(
        Func<CardModel, Color?> ColorWhen,
        int Priority = 0,
        bool VisibleWhenUnplayable = false,
        bool RefreshEveryFrame = true)
    {
        /// <summary>
        ///     Creates a rule from a color resolver, usually written as a switch expression.
        ///     从颜色解析器创建规则，通常配合 switch expression 使用。
        /// </summary>
        public static ModCardHandOutlineSwitchRule Switch(
            Func<CardModel, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
        {
            ArgumentNullException.ThrowIfNull(colorWhen);
            return new(colorWhen, priority, visibleWhenUnplayable, refreshEveryFrame);
        }

        /// <summary>
        ///     Creates a typed rule from a color resolver for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 从颜色解析器创建类型化规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Switch<TCard>(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
            where TCard : CardModel
        {
            return ModCardHandOutlineSwitchRule<TCard>.Switch(
                colorWhen,
                priority,
                visibleWhenUnplayable,
                refreshEveryFrame);
        }

        /// <summary>
        ///     Creates a fixed-color rule guarded by a predicate.
        ///     创建由谓词控制的固定颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule Fixed(
            Func<CardModel, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            ArgumentNullException.ThrowIfNull(when);
            return new(card => when(card) ? color : null, priority, visibleWhenUnplayable, false);
        }

        /// <summary>
        ///     Creates a typed fixed-color rule guarded by a predicate for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 创建类型化固定颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Fixed<TCard>(
            Func<TCard, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
            where TCard : CardModel
        {
            return ModCardHandOutlineSwitchRule<TCard>.Fixed(when, color, priority, visibleWhenUnplayable);
        }

        /// <summary>
        ///     Creates a dynamic-color rule guarded by a predicate.
        ///     创建由谓词控制的动态颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule Dynamic(
            Func<CardModel, bool> when,
            Func<CardModel, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            ArgumentNullException.ThrowIfNull(when);
            ArgumentNullException.ThrowIfNull(colorWhen);
            return new(card => when(card) ? colorWhen(card) : null, priority, visibleWhenUnplayable);
        }

        /// <summary>
        ///     Creates a typed dynamic-color rule guarded by a predicate for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 创建类型化动态颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Dynamic<TCard>(
            Func<TCard, bool> when,
            Func<TCard, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
            where TCard : CardModel
        {
            return ModCardHandOutlineSwitchRule<TCard>.Dynamic(when, colorWhen, priority, visibleWhenUnplayable);
        }

        internal Color? ResolveColor(CardModel card)
        {
            return ColorWhen(card);
        }
    }

    /// <summary>
    ///     A typed hand-card outline rule whose resolver returns the outline color to apply, or <see langword="null" />
    ///     when the rule does not match the current card state. This shape is intended for switch expressions and dynamic
    ///     colors.
    ///     类型化手牌卡牌描边规则：解析器返回要应用的描边颜色，当前卡牌状态不匹配时返回 <see langword="null" />。
    ///     此结构适合 switch expression 和动态颜色。
    /// </summary>
    /// <typeparam name="TCard">
    ///     Card model type this rule is registered for.
    ///     此规则注册到的卡牌模型类型。
    /// </typeparam>
    /// <param name="ColorWhen">
    ///     Returns a color when this rule should apply; returns <see langword="null" /> to skip the rule.
    ///     规则应生效时返回颜色；返回 <see langword="null" /> 时跳过规则。
    /// </param>
    /// <param name="Priority">
    ///     When several rules return a color, the highest <paramref name="Priority" /> wins; ties favor the most recently
    ///     registered rule.
    ///     多条规则返回颜色时，最高 <paramref name="Priority" /> 获胜；平手时优先最近注册的规则。
    /// </param>
    /// <param name="VisibleWhenUnplayable">
    ///     If true, the highlight is forced visible with this color even when vanilla would not show one.
    ///     如果为 true，即使原版不会显示高亮，也会强制以此颜色显示。
    /// </param>
    /// <param name="RefreshEveryFrame">
    ///     If true, the resolver is polled while the holder is alive so dynamic colors can change without a card refresh.
    ///     如果为 true，holder 存活期间会轮询解析器，使动态颜色无需卡牌刷新即可变化。
    /// </param>
    public readonly record struct ModCardHandOutlineSwitchRule<TCard>(
        Func<TCard, Color?> ColorWhen,
        int Priority = 0,
        bool VisibleWhenUnplayable = false,
        bool RefreshEveryFrame = true)
        where TCard : CardModel
    {
        /// <summary>
        ///     Creates a rule from a typed color resolver, usually written as a switch expression.
        ///     从类型化颜色解析器创建规则，通常配合 switch expression 使用。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Switch(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
        {
            ArgumentNullException.ThrowIfNull(colorWhen);
            return new(colorWhen, priority, visibleWhenUnplayable, refreshEveryFrame);
        }

        /// <summary>
        ///     Creates a fixed-color rule guarded by a typed predicate.
        ///     创建由类型化谓词控制的固定颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Fixed(
            Func<TCard, bool> when,
            Color color,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            ArgumentNullException.ThrowIfNull(when);
            return new(card => when(card) ? color : null, priority, visibleWhenUnplayable, false);
        }

        /// <summary>
        ///     Creates a dynamic-color rule guarded by a typed predicate.
        ///     创建由类型化谓词控制的动态颜色规则。
        /// </summary>
        public static ModCardHandOutlineSwitchRule<TCard> Dynamic(
            Func<TCard, bool> when,
            Func<TCard, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            ArgumentNullException.ThrowIfNull(when);
            ArgumentNullException.ThrowIfNull(colorWhen);
            return new(card => when(card) ? colorWhen(card) : null, priority, visibleWhenUnplayable);
        }

        /// <summary>
        ///     Converts a typed rule to the type-erased registry representation.
        ///     将类型化规则转换为注册表使用的类型擦除表示。
        /// </summary>
        public static implicit operator ModCardHandOutlineSwitchRule(ModCardHandOutlineSwitchRule<TCard> rule)
        {
            return rule.ToUntyped();
        }

        internal ModCardHandOutlineSwitchRule ToUntyped()
        {
            ArgumentNullException.ThrowIfNull(ColorWhen);
            var colorWhen = ColorWhen;
            return new(
                card => card is TCard typed ? colorWhen(typed) : null,
                Priority,
                VisibleWhenUnplayable,
                RefreshEveryFrame);
        }
    }
}
