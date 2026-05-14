using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline
{
    /// <summary>
    ///     Custom hand-card outline tint (drives <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
    ///     <c>Modulate</c> after vanilla playable / gold / red). Register with
    ///     <see cref="ModCardHandOutlineRegistry" /> or <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;()</c>.
    ///     自定义手牌卡牌描边色调（在原版可打出/金色/红色之后驱动 <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
    ///     <c>Modulate</c>）。通过
    ///     <see cref="ModCardHandOutlineRegistry" /> 或 <c>ModContentRegistry.RegisterCardHandOutline&lt;TCard&gt;()</c> 注册。
    /// </summary>
    /// <param name="When">
    ///     When this returns true for the card instance, the outline color may apply.
    ///     对卡牌实例返回 true 时，描边颜色可生效。
    /// </param>
    /// <param name="Color">
    ///     Godot modulate color (alpha is respected; vanilla highlights use ~0.98).
    ///     Godot modulate 颜色（alpha 会被尊重；原版高亮约为 0.98）。
    /// </param>
    /// <param name="Priority">
    ///     When several rules match, the highest <paramref name="Priority" /> wins; ties favor the most recently registered
    ///     rule.
    ///     多条规则匹配时，最高 <paramref name="Priority" /> 获胜；平手时优先最近注册的
    ///     规则。
    /// </param>
    /// <param name="VisibleWhenUnplayable">
    ///     If true, the highlight is forced visible with this color even when the card is not playable and vanilla would not
    ///     show gold/red (still only while combat is in progress).
    ///     如果为 true，即使卡牌不可打出且原版不会
    ///     显示金色/红色，也会强制以此颜色显示高亮（仍仅在战斗进行中）。
    /// </param>
    public readonly record struct ModCardHandOutlineRule(
        Func<CardModel, bool> When,
        Color Color,
        int Priority = 0,
        bool VisibleWhenUnplayable = false)
    {
        /// <summary>
        ///     Optional dynamic color resolver. When assigned and <see cref="When" /> passes, this is evaluated each refresh
        ///     to produce the current hand outline color.
        ///     可选动态颜色解析器。赋值后且 <see cref="When" /> 通过时，每次刷新
        ///     都会求值以生成当前手牌描边颜色。
        /// </summary>
        public Func<CardModel, Color>? DynamicColor { get; init; }

        /// <summary>
        ///     Creates a rule with a dynamic color resolver.
        ///     创建带动态颜色解析器的规则。
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
    }
}
