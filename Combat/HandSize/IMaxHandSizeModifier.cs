using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.HandSize
{
    /// <summary>
    ///     Extensible modifier contract for player max-hand-size calculation.
    /// </summary>
    public interface IMaxHandSizeModifier
    {
        /// <summary>
        ///     Early pass modifier.
        /// </summary>
        int ModifyMaxHandSize(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }

        /// <summary>
        ///     Late pass modifier.
        /// </summary>
        int ModifyMaxHandSizeLate(Player player, int currentMaxHandSize)
        {
            return currentMaxHandSize;
        }
    }
}
