using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Registers a custom in-hand outline / highlight tint (<see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
        ///     <c>Modulate</c>) for <typeparamref name="TCard" />. Multiple rules may be registered; the highest
        ///     <see cref="ModCardHandOutlineSwitchRule.Priority" /> among matching resolvers wins.
        ///     为 <typeparamref name="TCard" /> 注册自定义手牌轮廓/高亮色调（<see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
        ///     <c>Modulate</c>）。可以注册多条规则；匹配谓词中最高的
        ///     <see cref="ModCardHandOutlineSwitchRule.Priority" /> 胜出。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard> rules) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rules");
            ModCardHandOutlineRegistry.Register(rules);
        }

        /// <summary>
        ///     Registers a custom in-hand outline / highlight tint rule for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册自定义手牌轮廓/高亮色调规则。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard> rule) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rule");
            ModCardHandOutlineRegistry.Register(rule);
        }

        /// <summary>
        ///     Registers several custom in-hand outline / highlight tint rules for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册多条自定义手牌轮廓/高亮色调规则。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[] rules)
            where TCard : CardModel
        {
            RegisterCardHandOutline(ModCardHandOutlineRules<TCard>.Of(rules));
        }

        /// <summary>
        ///     Registers type-erased custom in-hand outline / highlight tint rules for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册类型擦除自定义手牌轮廓/高亮色调规则。
        /// </summary>
        [Obsolete("Use RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard>).")]
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRules rules) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rules");
            ModCardHandOutlineRegistry.Register<TCard>(rules);
        }

        /// <summary>
        ///     Registers a type-erased custom in-hand outline / highlight tint rule for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册类型擦除自定义手牌轮廓/高亮色调规则。
        /// </summary>
        [Obsolete("Use RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule rule) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rule");
            ModCardHandOutlineRegistry.Register<TCard>(rule);
        }

        /// <summary>
        ///     Registers several type-erased custom in-hand outline / highlight tint rules for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册多条类型擦除自定义手牌轮廓/高亮色调规则。
        /// </summary>
        [Obsolete("Use RegisterCardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[]).")]
        public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineSwitchRule[] rules) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rules");
            ModCardHandOutlineRegistry.Register<TCard>(ModCardHandOutlineRules.Of(rules));
        }

        /// <summary>
        ///     Registers a switch-style custom hand outline resolver for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册 switch 风格的自定义手牌描边解析器。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(
            Func<TCard, Color?> colorWhen,
            int priority = 0,
            bool visibleWhenUnplayable = false,
            bool refreshEveryFrame = true)
            where TCard : CardModel
        {
            RegisterCardHandOutline(ModCardHandOutlineSwitchRule<TCard>.Switch(
                colorWhen,
                priority,
                visibleWhenUnplayable,
                refreshEveryFrame));
        }

        /// <summary>
        ///     Registers a legacy custom in-hand outline / highlight tint rule for <typeparamref name="TCard" />.
        ///     为 <typeparamref name="TCard" /> 注册旧版自定义手牌轮廓/高亮色调规则。
        /// </summary>
        [Obsolete(
            "Use RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard>), RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>), or RegisterCardHandOutline<TCard>(Func<TCard, Color?>).")]
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rule");
            ModCardHandOutlineRegistry.Register<TCard>(rule.ToSwitchRule());
        }

        /// <summary>
        ///     Registers several legacy <see cref="ModCardHandOutlineRule" /> values for the same card type (e.g. different
        ///     priorities / conditions).
        ///     为同一卡牌类型注册多个旧版 <see cref="ModCardHandOutlineRule" /> 值（例如不同的
        ///     优先级/条件）。
        /// </summary>
        [Obsolete(
            "Use RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard>) or RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>).")]
        public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineRule[] rules) where TCard : CardModel
        {
            ArgumentNullException.ThrowIfNull(rules);
            EnsureMutable("register card hand outline rules");
            ModCardHandOutlineRegistry.Register<TCard>(
                ModCardHandOutlineRules.Of(rules.Select(static rule => rule.ToSwitchRule()).ToArray()));
        }
    }
}
