using MegaCrit.Sts2.Core.Entities.Cards;

namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     <para xml:lang="en">Identifies the UI from which a model right-click request originated.</para>
    ///     <para xml:lang="zh-CN">标识模型右键请求的来源界面。</para>
    /// </summary>
    public enum ModRightClickSource
    {
        /// <summary>
        ///     <para xml:lang="en">An unspecified or legacy caller.</para>
        ///     <para xml:lang="zh-CN">未指定或旧版调用方。</para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     <para xml:lang="en">A card holder in the local player's hand.</para>
        ///     <para xml:lang="zh-CN">本地玩家手牌中的卡牌容器。</para>
        /// </summary>
        HandCard = 1,

        /// <summary>
        ///     <para xml:lang="en">A card holder in a combat pile screen.</para>
        ///     <para xml:lang="zh-CN">战斗牌堆界面中的卡牌容器。</para>
        /// </summary>
        CombatPileCard = 2,

        /// <summary>
        ///     <para xml:lang="en">A relic-inventory holder.</para>
        ///     <para xml:lang="zh-CN">遗物栏中的遗物容器。</para>
        /// </summary>
        Relic = 3,

        /// <summary>
        ///     <para xml:lang="en">A combat power node.</para>
        ///     <para xml:lang="zh-CN">战斗中的能力节点。</para>
        /// </summary>
        Power = 4,

        /// <summary>
        ///     <para xml:lang="en">A potion holder.</para>
        ///     <para xml:lang="zh-CN">药水容器。</para>
        /// </summary>
        Potion = 5,

        /// <summary>
        ///     <para xml:lang="en">A combat orb node.</para>
        ///     <para xml:lang="zh-CN">战斗中的充能球节点。</para>
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
