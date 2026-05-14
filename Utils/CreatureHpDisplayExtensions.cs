using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Host-version-stable helpers for checking whether a creature should be rendered with an infinite HP display.
    ///     用于检查生物是否应以无限 HP 显示渲染的宿主版本稳定辅助方法。
    /// </summary>
    public static class CreatureHpDisplayExtensions
    {
        /// <summary>
        ///     Returns <c>true</c> when the creature is currently rendered with an infinite HP indicator.
        ///     当该生物当前以无限 HP 指示器渲染时，返回 <c>true</c>。
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
