using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     <para xml:lang="en">Defines extensible modifiers for calculating a player's maximum hand size.</para>
    ///     <para xml:lang="zh-CN">定义用于计算玩家手牌上限的可扩展修正器。</para>
    /// </summary>
    public interface IMaxHandSizeModifier
    {
        /// <summary>
        ///     <para xml:lang="en">Modifies the maximum hand size during the early pass.</para>
        ///     <para xml:lang="zh-CN">在早期阶段修正手牌上限。</para>
        /// </summary>
        int ModifyMaxHandSize(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }

        /// <summary>
        ///     <para xml:lang="en">Modifies the maximum hand size during the late pass.</para>
        ///     <para xml:lang="zh-CN">在后期阶段修正手牌上限。</para>
        /// </summary>
        int ModifyMaxHandSizeLate(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }
    }
}
