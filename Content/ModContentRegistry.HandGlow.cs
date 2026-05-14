using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     Registers hand-highlight rules for <typeparamref name="TCard" /> (gold / red borders in hand during combat
        ///     play phase). Same semantics as overriding <see cref="CardModel.ShouldGlowGoldInternal" /> and
        ///     <see cref="CardModel.ShouldGlowRedInternal" />, but keeps logic in data/registration. Multiple registrations for
        ///     the same type OR-merge.
        ///     为 <typeparamref name="TCard" /> 注册手牌高亮规则（战斗出牌阶段手牌中的金色/红色边框）。
        ///     语义与重写 <see cref="CardModel.ShouldGlowGoldInternal" /> 和
        ///     出牌阶段）相同。语义与重写 <c>CardModel.ShouldGlowGoldInternal</c> 和
        ///     <see cref="CardModel.ShouldGlowRedInternal" /> 相同，但将逻辑保留在数据/注册中。
        ///     同一类型的多个注册会按 OR 合并。
        /// </summary>
        public void RegisterCardHandGlow<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            EnsureMutable("register card hand glow rules");
            ModCardHandGlowRegistry.Register<TCard>(rules);
        }
    }
}
