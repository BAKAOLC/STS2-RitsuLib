using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Registers a custom in-hand outline / highlight tint (<see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
        ///     <c>Modulate</c>) for <typeparamref name="TCard" />. Multiple rules may be registered; the highest
        ///     <see cref="ModCardHandOutlineRule.Priority" /> among matching predicates wins.
        ///     为 <typeparamref name="TCard" /> 注册自定义手牌轮廓/高亮色调（<see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCardHighlight" />
        ///     <c>Modulate</c>）。可以注册多条规则；匹配谓词中最高的
        ///     <see cref="ModCardHandOutlineRule.Priority" /> 胜出。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRule rule) where TCard : CardModel
        {
            EnsureMutable("register card hand outline rule");
            ModCardHandOutlineRegistry.Register<TCard>(rule);
        }

        /// <summary>
        ///     Registers several <see cref="ModCardHandOutlineRule" /> values for the same card type (e.g. different
        ///     priorities / conditions).
        ///     为同一卡牌类型注册多个 <see cref="ModCardHandOutlineRule" /> 值（例如不同的
        ///     优先级/条件）。
        /// </summary>
        public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineRule[] rules) where TCard : CardModel
        {
            ArgumentNullException.ThrowIfNull(rules);
            EnsureMutable("register card hand outline rules");
            foreach (var rule in rules)
                ModCardHandOutlineRegistry.Register<TCard>(rule);
        }
    }
}
