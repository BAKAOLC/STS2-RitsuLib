using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Provides host-version-stable helpers for checking whether a creature displays infinite HP.</para>
    ///     <para xml:lang="zh-CN">提供跨宿主版本保持稳定的辅助方法，用于检查生物是否显示无限生命值。</para>
    /// </summary>
    public static class CreatureHpDisplayExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Returns whether the creature's HP display is currently in infinite mode.</para>
        ///     <para xml:lang="zh-CN">返回该生物的生命值显示当前是否处于无限模式。</para>
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
