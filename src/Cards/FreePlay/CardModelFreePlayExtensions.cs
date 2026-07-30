using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.FreePlay.Patches;

namespace STS2RitsuLib.Cards.FreePlay
{
    /// <summary>
    ///     <para xml:lang="en">Provides free-play helpers whose duration differs from the game's default behavior.</para>
    ///     <para xml:lang="zh-CN">提供持续时间不同于游戏默认行为的免费出牌辅助方法。</para>
    /// </summary>
    public static class CardModelFreePlayExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Makes the card's fixed base costs free for the rest of the current turn, including every subsequent
        ///         play of that card during the turn.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使卡牌的固定基础费用在当前回合剩余时间内免费，包括该牌在本回合内的后续每次打出。
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para xml:lang="en">
        ///         Unlike <see cref="CardModel.SetToFreeThisTurn" />, this state is not removed after the card is played.
        ///         It expires during end-of-turn cleanup. X costs retain their normal game behavior.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         与 <see cref="CardModel.SetToFreeThisTurn" /> 不同，此状态不会在卡牌打出后移除，而是在回合结束清理时
        ///         失效。X 费用保持游戏的正常行为。
        ///     </para>
        /// </remarks>
        /// <param name="card">
        ///     <para xml:lang="en">Card whose fixed energy, star, and registered secondary-resource costs become free.</para>
        ///     <para xml:lang="zh-CN">固定能量、星星和已注册次级资源费用将变为免费的卡牌。</para>
        /// </param>
        public static void SetToFreeForRestOfTurn(this CardModel card)
        {
            ArgumentNullException.ThrowIfNull(card);

            card.EnergyCost.SetThisTurn(0);
            card.AddTemporaryStarCost(new()
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
