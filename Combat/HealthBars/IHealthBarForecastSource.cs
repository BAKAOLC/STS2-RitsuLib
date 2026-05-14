#if !STS2_AT_LEAST_0_104_0
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
    ///     传给生命条 forecast source 的运行时上下文。
    /// </summary>
    /// <param name="Creature">
    ///     Creature whose health bar is being rendered.
    ///     正在渲染生命条的生物。
    /// </param>
    public readonly record struct HealthBarForecastContext(Creature Creature)
    {
        /// <summary>
        ///     Current combat state, when the creature is in combat.
        ///     生物处于战斗中时的当前战斗状态。
        /// </summary>
        public CombatStateLike? CombatState => Creature.CombatState;

        /// <summary>
        ///     Side whose turn is currently active, when available.
        ///     当前行动中的阵营（如果可用）。
        /// </summary>
        public CombatSide? CurrentSide => Creature.CombatState?.CurrentSide;
    }

    /// <summary>
    ///     Produces one or more health bar forecast segments for a creature.
    ///     为生物生成一个或多个生命条 forecast 片段。
    /// </summary>
    /// <remarks>
    ///     Power models can implement this directly and will be discovered automatically from
    ///     <see cref="Creature.Powers" /> without any extra registration.
    ///     Non-power forecast sources can be registered through the content-pack workflow.
    ///     能力模型可以直接实现此接口，并会从
    ///     <see cref="Creature.Powers" /> 自动发现，无需额外注册。
    ///     非能力 forecast source 可以通过内容包工作流注册。
    /// </remarks>
    public interface IHealthBarForecastSource
    {
        /// <summary>
        ///     Returns forecast segments to render for <paramref name="context" />.
        ///     返回要为 <paramref name="context" /> 渲染的 forecast 片段。
        /// </summary>
        IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
    }
}
