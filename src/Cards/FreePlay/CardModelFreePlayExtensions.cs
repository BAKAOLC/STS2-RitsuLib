using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay.Patches;

namespace STS2RitsuLib.Cards.FreePlay
{
    /// <summary>
    ///     Free-play helpers whose duration semantics differ from the engine defaults.
    ///     提供与引擎默认持续时间语义不同的免费出牌辅助方法。
    /// </summary>
    public static class CardModelFreePlayExtensions
    {
        /// <summary>
        ///     Makes the card's fixed base costs free for the rest of the current turn, including every
        ///     subsequent play of the same card during that turn.
        ///     令卡牌的固定基础费用在当前回合剩余时间内免费，包括该牌在本回合内的后续每次打出。
        /// </summary>
        /// <remarks>
        ///     Unlike <see cref="CardModel.SetToFreeThisTurn" />, this free state is not removed after the card is
        ///     played. It expires only during end-of-turn cleanup. X costs keep their normal engine behavior.
        ///     与 <see cref="CardModel.SetToFreeThisTurn" /> 不同，此免费状态不会在卡牌打出后移除，仅在回合结束清理时失效。
        ///     X 费用保持引擎的正常行为。
        /// </remarks>
        /// <param name="card">
        ///     Card whose fixed energy, star, and registered secondary-resource costs become free.
        ///     固定能量、星星及已注册次级资源费用将变为免费的卡牌。
        /// </param>
        public static void SetToFreeForRestOfTurn(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            card.EnergyCost.SetThisTurn(0);
            card.AddTemporaryStarCost(new TemporaryCardCost
            {
                Cost = 0,
                ClearsWhenTurnEnds = true,
                ClearsWhenCardIsPlayed = false,
            });
            FreePlayBindingRegistry.MarkCardBaseCostsFreeForRestOfTurn(card);
            FreePlayCardVisuals.Refresh(card);
        }
    }
}
