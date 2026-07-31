using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     <para xml:lang="en">Defines an untyped hand-card outline rule.</para>
    ///     <para xml:lang="zh-CN">定义非泛型手牌描边规则。</para>
    /// </summary>
    /// <param name="ColorWhen">
    ///     <para xml:lang="en">
    ///         Returns the outline color, or <see langword="null" /> when the rule does not match.
    ///     </para>
    ///     <para xml:lang="zh-CN">返回描边颜色；规则不匹配时返回 <see langword="null" />。</para>
    /// </param>
    /// <param name="Priority">
    ///     <para xml:lang="en">
    ///         Selection priority. Higher values win; ties favor the most recently registered rule.
    ///     </para>
    ///     <para xml:lang="zh-CN">选择优先级；值较高者优先，相同时采用最近注册的规则。</para>
    /// </param>
    /// <param name="VisibleWhenUnplayable">
    ///     <para xml:lang="en">
    ///         Whether to show the outline during combat when the vanilla holder would hide its highlight.
    ///     </para>
    ///     <para xml:lang="zh-CN">战斗中原版手牌容器会隐藏高亮时，是否仍显示描边。</para>
    /// </param>
    /// <param name="RefreshEveryFrame">
    ///     <para xml:lang="en">Whether to evaluate and apply the rule every process frame.</para>
    ///     <para xml:lang="zh-CN">是否在每个处理帧评估并应用规则。</para>
    /// </param>
    public readonly record struct ModCardHandOutlineSwitchRule(
        Func<CardModel, Color?> ColorWhen,
        int Priority = 0,
        bool VisibleWhenUnplayable = false,
        bool RefreshEveryFrame = true)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a rule from a color resolver.</para>
        ///     <para xml:lang="zh-CN">使用颜色解析器创建规则。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a rule typed for <typeparamref name="TCard" /> from a color resolver.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用颜色解析器创建适用于 <typeparamref name="TCard" /> 的泛型规则。
        ///     </para>
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
        ///     <para xml:lang="en">Creates a fixed-color rule guarded by a predicate.</para>
        ///     <para xml:lang="zh-CN">创建由谓词控制的固定颜色规则。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a fixed-color rule typed for <typeparamref name="TCard" /> and guarded by a predicate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TCard" /> 创建由谓词控制的泛型固定颜色规则。
        ///     </para>
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
        ///     <para xml:lang="en">Creates a dynamic-color rule guarded by a predicate.</para>
        ///     <para xml:lang="zh-CN">创建由谓词控制的动态颜色规则。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a dynamic-color rule typed for <typeparamref name="TCard" /> and guarded by a predicate.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TCard" /> 创建由谓词控制的泛型动态颜色规则。
        ///     </para>
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
    ///     <para xml:lang="en">
    ///         Defines a hand-card outline rule typed for <typeparamref name="TCard" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义适用于 <typeparamref name="TCard" /> 的泛型手牌描边规则。</para>
    /// </summary>
    /// <typeparam name="TCard">
    ///     <para xml:lang="en">Card model type accepted by the resolver.</para>
    ///     <para xml:lang="zh-CN">解析器接受的卡牌模型类型。</para>
    /// </typeparam>
    /// <param name="ColorWhen">
    ///     <para xml:lang="en">
    ///         Returns the outline color, or <see langword="null" /> when the rule does not match.
    ///     </para>
    ///     <para xml:lang="zh-CN">返回描边颜色；规则不匹配时返回 <see langword="null" />。</para>
    /// </param>
    /// <param name="Priority">
    ///     <para xml:lang="en">
    ///         Selection priority. Higher values win; ties favor the most recently registered rule.
    ///     </para>
    ///     <para xml:lang="zh-CN">选择优先级；值较高者优先，相同时采用最近注册的规则。</para>
    /// </param>
    /// <param name="VisibleWhenUnplayable">
    ///     <para xml:lang="en">
    ///         Whether to show the outline during combat when the vanilla holder would hide its highlight.
    ///     </para>
    ///     <para xml:lang="zh-CN">战斗中原版手牌容器会隐藏高亮时，是否仍显示描边。</para>
    /// </param>
    /// <param name="RefreshEveryFrame">
    ///     <para xml:lang="en">Whether to evaluate and apply the rule every process frame.</para>
    ///     <para xml:lang="zh-CN">是否在每个处理帧评估并应用规则。</para>
    /// </param>
    public readonly record struct ModCardHandOutlineSwitchRule<TCard>(
        Func<TCard, Color?> ColorWhen,
        int Priority = 0,
        bool VisibleWhenUnplayable = false,
        bool RefreshEveryFrame = true)
        where TCard : CardModel
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a typed rule from a color resolver.</para>
        ///     <para xml:lang="zh-CN">使用颜色解析器创建泛型规则。</para>
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
        ///     <para xml:lang="en">Creates a fixed-color rule guarded by a typed predicate.</para>
        ///     <para xml:lang="zh-CN">创建由泛型谓词控制的固定颜色规则。</para>
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
        ///     <para xml:lang="en">Creates a dynamic-color rule guarded by a typed predicate.</para>
        ///     <para xml:lang="zh-CN">创建由泛型谓词控制的动态颜色规则。</para>
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
        ///     <para xml:lang="en">Converts a typed rule to the untyped registry representation.</para>
        ///     <para xml:lang="zh-CN">将泛型规则转换为注册表使用的非泛型表示。</para>
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
