using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     Reusable condition functions matching common vanilla card patterns (Evil Eye, Fetch, Osty attacks). Prefer
    ///     <see cref="CardModelHandGlowExtensions" /> on <see cref="CardModel" /> for override bodies.
    ///     匹配常见原版卡牌模式（Evil Eye、Fetch、Osty 攻击）的可复用条件函数。重写体中优先
    ///     在 <see cref="CardModel" /> 上使用 <see cref="CardModelHandGlowExtensions" />。
    /// </summary>
    public static class ModCardHandGlowPredicates
    {
        /// <summary>
        ///     Same idea as vanilla Osty attack cards: red when the owner’s companion is not present.
        ///     与原版 Osty 攻击卡牌相同的思路：拥有者的伙伴不在场时显示红色。
        /// </summary>
        public static bool OwnerCompanionOstyMissing(CardModel card)
        {
            return card.Owner?.IsOstyMissing == true;
        }

        /// <summary>
        ///     Same history shape as <see cref="MegaCrit.Sts2.Core.Models.Cards.EvilEye" />: any of the owner’s cards was
        ///     exhausted this turn (often drives gold while a stronger effect line is active).
        ///     与 <see cref="MegaCrit.Sts2.Core.Models.Cards.EvilEye" /> 相同的历史形态：拥有者的任意卡牌
        ///     本回合已被消耗（通常在更强效果行生效时驱动金色）。
        /// </summary>
        public static bool AnyOfOwnersCardsExhaustedThisTurn(CardModel card)
        {
            var owner = card.Owner;
            var combat = card.CombatState;
            var history = CombatManager.Instance?.History;
            if (owner is null || combat is null || history is null)
                return false;

            return history.Entries.OfType<CardExhaustedEntry>()
                .Any(e => e.HappenedThisTurn(combat) && e.Card.Owner == owner);
        }

        /// <summary>
        ///     Same history shape as <see cref="MegaCrit.Sts2.Core.Models.Cards.Fetch" /> gold: this card has not finished a
        ///     play this turn.
        ///     与 <see cref="MegaCrit.Sts2.Core.Models.Cards.Fetch" /> 金色高亮相同的历史形态：此卡本回合
        ///     尚未完成一次打出。
        /// </summary>
        public static bool ThisCardNotFinishedPlayThisTurn(CardModel card)
        {
            var combat = card.CombatState;
            var history = CombatManager.Instance?.History;
            if (combat is null || history is null)
                return false;

            return !history.CardPlaysFinished.Any(e =>
                e.CardPlay.Card == card && e.HappenedThisTurn(combat));
        }
    }
}
