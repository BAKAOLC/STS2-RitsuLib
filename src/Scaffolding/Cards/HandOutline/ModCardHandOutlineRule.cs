using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Legacy predicate-based hand-card outline rule.
    ///     </para>
    ///     <para xml:lang="zh-CN">旧版基于谓词的手牌描边规则。</para>
    /// </summary>
    /// <param name="When">
    ///     <para xml:lang="en">Predicate that determines whether the rule matches.</para>
    ///     <para xml:lang="zh-CN">确定规则是否匹配的谓词。</para>
    /// </param>
    /// <param name="Color">
    ///     <para xml:lang="en">Godot modulation color applied to the outline.</para>
    ///     <para xml:lang="zh-CN">应用到描边的 Godot 调制颜色。</para>
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
    [Obsolete(
        "Use ModCardHandOutlineSwitchRule<TCard> or ModCardHandOutlineRules<TCard>. This legacy rule is kept as a forwarding adapter.")]
    public readonly record struct ModCardHandOutlineRule(
        Func<CardModel, bool> When,
        Color Color,
        int Priority = 0,
        bool VisibleWhenUnplayable = false)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional color resolver evaluated whenever the matching outline is refreshed.
        ///     </para>
        ///     <para xml:lang="zh-CN">每次刷新匹配描边时评估的可选颜色解析器。</para>
        /// </summary>
        public Func<CardModel, Color>? DynamicColor { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule with a dynamic color resolver.</para>
        ///     <para xml:lang="zh-CN">创建使用动态颜色解析器的规则。</para>
        /// </summary>
        public static ModCardHandOutlineRule Dynamic(
            Func<CardModel, bool> when,
            Func<CardModel, Color> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false)
        {
            ArgumentNullException.ThrowIfNull(when);
            ArgumentNullException.ThrowIfNull(colorWhen);
            return new(when, Colors.White, priority, visibleWhenUnplayable)
            {
                DynamicColor = colorWhen,
            };
        }

        internal Color ResolveColor(CardModel card)
        {
            return DynamicColor?.Invoke(card) ?? Color;
        }

        internal ModCardHandOutlineSwitchRule ToSwitchRule()
        {
            return DynamicColor != null
                ? ModCardHandOutlineSwitchRule.Dynamic(When, DynamicColor, Priority, VisibleWhenUnplayable)
                : ModCardHandOutlineSwitchRule.Fixed(When, Color, Priority, VisibleWhenUnplayable);
        }
    }
}
