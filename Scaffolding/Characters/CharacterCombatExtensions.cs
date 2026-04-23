using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Extension methods on <see cref="Creature" /> and <see cref="Player" /> for common combat queries
    ///     (powers, orbs, energy).
    /// </summary>
    public static class CharacterCombatExtensions
    {
        /// <summary>
        ///     Returns the first active power instance of type <typeparamref name="TPower" />, if any.
        /// </summary>
        public static TPower? FindPower<TPower>(this Creature creature) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.Powers.OfType<TPower>().FirstOrDefault();
        }

        /// <summary>
        ///     Whether the creature currently has at least <paramref name="minimumAmount" /> stacks of
        ///     <typeparamref name="TPower" />.
        /// </summary>
        public static bool HasPower<TPower>(this Creature creature, int minimumAmount = 1) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.FindPower<TPower>()?.Amount >= minimumAmount;
        }

        /// <summary>
        ///     Current stack count of <typeparamref name="TPower" />, or zero if absent.
        /// </summary>
        public static int GetPowerAmount<TPower>(this Creature creature) where TPower : PowerModel
        {
            ArgumentNullException.ThrowIfNull(creature);
            return creature.FindPower<TPower>()?.Amount ?? 0;
        }

        /// <summary>
        ///     Whether the player’s orb queue currently contains at least one orb of type <typeparamref name="TOrb" />.
        /// </summary>
        public static bool HasOrb<TOrb>(this Player player) where TOrb : OrbModel
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Any() == true;
        }

        /// <summary>
        ///     Counts orbs of type <typeparamref name="TOrb" /> in the queue.
        /// </summary>
        public static int GetOrbCount<TOrb>(this Player player) where TOrb : OrbModel
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Count() ?? 0;
        }

        /// <summary>
        ///     Current combat energy, or zero if not in a combat state.
        /// </summary>
        public static int GetEnergy(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.Energy ?? 0;
        }

        /// <summary>
        ///     Maximum energy for the current combat state, or zero if unavailable.
        /// </summary>
        public static int GetMaxEnergy(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.MaxEnergy ?? 0;
        }

        /// <summary>
        ///     Orb queue capacity in combat, or zero if unavailable.
        /// </summary>
        public static int GetOrbCapacity(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return player.PlayerCombatState?.OrbQueue.Capacity ?? 0;
        }
    }
}
