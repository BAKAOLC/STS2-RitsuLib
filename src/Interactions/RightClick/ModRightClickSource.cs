using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     UI surface that initiated a model right-click request.
    ///     发起模型右键请求的 UI 表面。
    /// </summary>
    public enum ModRightClickSource
    {
        /// <summary>
        ///     Unspecified or legacy caller.
        ///     未指定或旧版调用方。
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Card holder in the local player's hand.
        ///     本地玩家手牌中的卡牌 holder。
        /// </summary>
        HandCard = 1,

        /// <summary>
        ///     Card holder in a combat pile screen.
        ///     战斗牌堆界面中的卡牌 holder。
        /// </summary>
        CombatPileCard = 2,

        /// <summary>
        ///     Relic inventory holder.
        ///     遗物栏 holder。
        /// </summary>
        Relic = 3,

        /// <summary>
        ///     Combat power node.
        ///     战斗能力节点。
        /// </summary>
        Power = 4,

        /// <summary>
        ///     Potion holder.
        ///     药水 holder。
        /// </summary>
        Potion = 5,

        /// <summary>
        ///     Combat orb node.
        ///     战斗充能球节点。
        /// </summary>
        Orb = 6,
    }

    internal static class ModRightClickCardPilePolicy
    {
        public static bool IsSupported(PileType pileType)
        {
            return pileType is PileType.Draw or PileType.Discard or PileType.Exhaust;
        }
    }
}
