using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Cards.HandGlow;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers gold and red in-hand glow rules for <typeparamref name="TCard" />. Multiple
        ///         registrations for the same card type are combined with logical OR.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TCard" /> 注册手牌中的金色与红色发光规则。同一卡牌类型的多次注册
        ///         使用逻辑 OR 合并。
        ///     </para>
        /// </summary>
        public void RegisterCardHandGlow<TCard>(ModCardHandGlowRules rules) where TCard : CardModel
        {
            EnsureMutable("register card hand glow rules");
            ModCardHandGlowRegistry.Register<TCard>(rules);
        }
    }
}
