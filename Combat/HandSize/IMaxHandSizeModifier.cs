using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Extensible modifier contract for player max-hand-size calculation.
    ///     玩家最大手牌数计算的可扩展 modifier 契约。
    /// </summary>
    public interface IMaxHandSizeModifier
    {
        /// <summary>
        ///     Early pass modifier.
        ///     早期 pass modifier。
        /// </summary>
        int ModifyMaxHandSize(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }

        /// <summary>
        ///     Late pass modifier.
        ///     晚期 pass modifier。
        /// </summary>
        int ModifyMaxHandSizeLate(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }
    }
}
