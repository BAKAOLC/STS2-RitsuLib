using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides common power, orb, and energy queries for <see cref="Creature" /> and <see cref="Player" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于查询 <see cref="Creature" /> 与 <see cref="Player" /> 能力、充能球和能量的常用方法。
    ///     </para>
    /// </summary>
    public static class CharacterCombatExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the first power of type <typeparamref name="TPower" />, or <see langword="null" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回第一个 <typeparamref name="TPower" /> 类型的能力；不存在时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        public static TPower? FindPower<TPower>(this Creature creature) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.Powers.OfType<TPower>().FirstOrDefault();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the creature has at least <paramref name="minimumAmount" /> stacks of
        ///         <typeparamref name="TPower" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回生物是否至少拥有 <paramref name="minimumAmount" /> 层
        ///         <typeparamref name="TPower" />。
        ///     </para>
        /// </summary>
        public static bool HasPower<TPower>(this Creature creature, int minimumAmount = 1) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.FindPower<TPower>()?.Amount >= minimumAmount;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the current <typeparamref name="TPower" /> amount, or zero when absent.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <typeparamref name="TPower" /> 的当前层数；不存在时返回零。
        ///     </para>
        /// </summary>
        public static int GetPowerAmount<TPower>(this Creature creature) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.FindPower<TPower>()?.Amount ?? 0;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the player's orb queue contains an orb of type <typeparamref name="TOrb" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回玩家的充能球队列中是否包含 <typeparamref name="TOrb" /> 类型的充能球。
        ///     </para>
        /// </summary>
        public static bool HasOrb<TOrb>(this Player player) where TOrb : OrbModel
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Any() == true;
        }

        /// <summary>
        ///     <para xml:lang="en">Counts <typeparamref name="TOrb" /> instances in the player's orb queue.</para>
        ///     <para xml:lang="zh-CN">
        ///         统计玩家充能球队列中 <typeparamref name="TOrb" /> 类型的充能球数量。
        ///     </para>
        /// </summary>
        public static int GetOrbCount<TOrb>(this Player player) where TOrb : OrbModel
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Count() ?? 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns current combat energy, or zero outside combat.</para>
        ///     <para xml:lang="zh-CN">返回当前战斗能量；不在战斗中时返回零。</para>
        /// </summary>
        public static int GetEnergy(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.Energy ?? 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns maximum combat energy, or zero outside combat.</para>
        ///     <para xml:lang="zh-CN">返回战斗中的最大能量；不在战斗中时返回零。</para>
        /// </summary>
        public static int GetMaxEnergy(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.MaxEnergy ?? 0;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns combat orb capacity, or zero outside combat.</para>
        ///     <para xml:lang="zh-CN">返回战斗中的充能球槽位数；不在战斗中时返回零。</para>
        /// </summary>
        public static int GetOrbCapacity(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Capacity ?? 0;
        }
    }
}
