using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Characters
{
    public static class CharacterCombatExtensions
    {
        extension(Creature creature)
        {
            public TPower? FindPower<TPower>() where TPower : PowerModel
            {
                ArgumentNullException.ThrowIfNull(creature);
                return creature.Powers.OfType<TPower>().FirstOrDefault();
            }

            public bool HasPower<TPower>(int minimumAmount = 1) where TPower : PowerModel
            {
                ArgumentNullException.ThrowIfNull(creature);
                return creature.FindPower<TPower>()?.Amount >= minimumAmount;
            }

            public int GetPowerAmount<TPower>() where TPower : PowerModel
            {
                ArgumentNullException.ThrowIfNull(creature);
                return creature.FindPower<TPower>()?.Amount ?? 0;
            }
        }

        extension(Player player)
        {
            public bool HasOrb<TOrb>() where TOrb : OrbModel
            {
                ArgumentNullException.ThrowIfNull(player);
                return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Any() == true;
            }

            public int GetOrbCount<TOrb>() where TOrb : OrbModel
            {
                ArgumentNullException.ThrowIfNull(player);
                return player.PlayerCombatState?.OrbQueue.Orbs.OfType<TOrb>().Count() ?? 0;
            }

            public int GetEnergy()
            {
                ArgumentNullException.ThrowIfNull(player);
                return player.PlayerCombatState?.Energy ?? 0;
            }

            public int GetMaxEnergy()
            {
                ArgumentNullException.ThrowIfNull(player);
                return player.PlayerCombatState?.MaxEnergy ?? 0;
            }

            public int GetOrbCapacity()
            {
                ArgumentNullException.ThrowIfNull(player);
                return player.PlayerCombatState?.OrbQueue.Capacity ?? 0;
            }
        }
    }
}
