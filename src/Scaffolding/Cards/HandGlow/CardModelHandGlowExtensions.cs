using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides <see cref="CardModel" /> extension methods for common hand-glow conditions.
    ///     </para>
    ///     <para xml:lang="zh-CN">提供常用手牌发光条件的 <see cref="CardModel" /> 扩展方法。</para>
    /// </summary>
    public static class CardModelHandGlowExtensions
    {
        /// <inheritdoc cref="ModCardHandGlowPredicates.OwnerCompanionOstyMissing" />
        public static bool ModHandGlowOwnerCompanionOstyMissing(this CardModel card)
        {
            return ModCardHandGlowPredicates.OwnerCompanionOstyMissing(card);
        }

        /// <inheritdoc cref="ModCardHandGlowPredicates.AnyOfOwnersCardsExhaustedThisTurn" />
        public static bool ModHandGlowAnyOfOwnersCardsExhaustedThisTurn(this CardModel card)
        {
            return ModCardHandGlowPredicates.AnyOfOwnersCardsExhaustedThisTurn(card);
        }

        /// <inheritdoc cref="ModCardHandGlowPredicates.ThisCardNotFinishedPlayThisTurn" />
        public static bool ModHandGlowThisCardNotFinishedPlayThisTurn(this CardModel card)
        {
            return ModCardHandGlowPredicates.ThisCardNotFinishedPlayThisTurn(card);
        }
    }
}
