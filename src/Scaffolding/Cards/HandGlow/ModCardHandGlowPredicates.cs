using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     <para xml:lang="en">Provides reusable hand-glow conditions derived from vanilla card behavior.</para>
    ///     <para xml:lang="zh-CN">提供依据原版卡牌行为实现的可复用手牌发光条件。</para>
    /// </summary>
    public static class ModCardHandGlowPredicates
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the card owner's Osty is missing, as used by vanilla Osty attack cards.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回卡牌拥有者的奥斯提是否缺席，与原版奥斯提攻击牌一致。</para>
        /// </summary>
        public static bool OwnerCompanionOstyMissing(CardModel card)
        {
            return card.Owner?.IsOstyMissing == true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the card's owner exhausted a card this turn, matching
        ///         <see cref="MegaCrit.Sts2.Core.Models.Cards.EvilEye" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回卡牌拥有者本回合是否消耗过卡牌，与
        ///         <see cref="MegaCrit.Sts2.Core.Models.Cards.EvilEye" /> 一致。
        ///     </para>
        /// </summary>
        public static bool AnyOfOwnersCardsExhaustedThisTurn(CardModel card)
        {
            var owner = card.Owner;
            var combat = card.CombatState;
            var history = CombatManager.Instance?.History;
            if (owner is null || combat is null || history is null)
                return false;

            return history.Entries.OfType<CardExhaustedEntry>()
                .Any(e => e.HappenedThisTurn(combat) && e.Actor == owner.Creature);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether this card has not finished a play this turn, matching the gold-glow condition of
        ///         <see cref="MegaCrit.Sts2.Core.Models.Cards.Fetch" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回此卡本回合是否尚未完成一次打出，与
        ///         <see cref="MegaCrit.Sts2.Core.Models.Cards.Fetch" /> 的金色发光条件一致。
        ///     </para>
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
