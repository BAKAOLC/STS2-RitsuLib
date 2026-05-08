using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Host-version-stable helpers for checking whether a creature should be rendered with an infinite HP display.
    /// </summary>
    public static class CreatureHpDisplayExtensions
    {
        /// <summary>
        ///     Returns <c>true</c> when the creature is currently rendered with an infinite HP indicator.
        /// </summary>
        public static bool IsInfiniteHpDisplayed(this Creature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
#if !STS2_AT_LEAST_0_105_0
            return creature.ShowsInfiniteHp;
#else
            return creature.HpDisplay.IsInfinite();
#endif
        }
    }
}
