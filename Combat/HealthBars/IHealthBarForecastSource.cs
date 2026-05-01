#if STS2_V_0_103_2
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace STS2RitsuLib.Combat.HealthBars
{
    /// <summary>
    ///     Runtime context passed to health bar forecast sources.
    /// </summary>
    /// <param name="Creature">Creature whose health bar is being rendered.</param>
    public readonly record struct HealthBarForecastContext(Creature Creature)
    {
        /// <summary>
        ///     Current combat state, when the creature is in combat.
        /// </summary>
        public CombatStateLike? CombatState => Creature.CombatState;

        /// <summary>
        ///     Side whose turn is currently active, when available.
        /// </summary>
        public CombatSide? CurrentSide => Creature.CombatState?.CurrentSide;
    }

    /// <summary>
    ///     Produces one or more health bar forecast segments for a creature.
    /// </summary>
    /// <remarks>
    ///     Power models can implement this directly and will be discovered automatically from
    ///     <see cref="Creature.Powers" /> without any extra registration.
    ///     Non-power forecast sources can be registered through the content-pack workflow.
    /// </remarks>
    public interface IHealthBarForecastSource
    {
        /// <summary>
        ///     Returns forecast segments to render for <paramref name="context" />.
        /// </summary>
        IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
    }
}
