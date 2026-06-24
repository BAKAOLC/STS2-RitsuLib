using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     Extension methods for <see cref="CardModel" /> hand-highlight conditions; use inside
    ///     <c>ShouldGlowGoldInternal</c> / <c>ShouldGlowRedInternal</c> overrides for concise, readable code.
    ///     <see cref="CardModel" /> 手牌高亮条件的扩展方法；可在
    ///     <c>ShouldGlowGoldInternal</c> / <c>ShouldGlowRedInternal</c> 重写中使用，以获得简洁易读的代码。
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
