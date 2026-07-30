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
    ///     <para xml:lang="en">Provides the creature and combat state used to produce health-bar forecasts.</para>
    ///     <para xml:lang="zh-CN">提供生成生命条预测时使用的生物和战斗状态。</para>
    /// </summary>
    /// <param name="Creature">
    ///     <para xml:lang="en">The creature whose health bar is being rendered.</para>
    ///     <para xml:lang="zh-CN">正在渲染生命条的生物。</para>
    /// </param>
    public readonly record struct HealthBarForecastContext(Creature Creature)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the current combat state when the creature is in combat.</para>
        ///     <para xml:lang="zh-CN">获取生物处于战斗中时的当前战斗状态。</para>
        /// </summary>
        public CombatStateLike? CombatState => Creature.CombatState;

        /// <summary>
        ///     <para xml:lang="en">Gets the side whose turn is active, when available.</para>
        ///     <para xml:lang="zh-CN">获取当前正在行动的阵营；无法确定时为空。</para>
        /// </summary>
        public CombatSide? CurrentSide => Creature.CombatState?.CurrentSide;
    }

    /// <summary>
    ///     <para xml:lang="en">Produces health-bar forecast segments for a creature.</para>
    ///     <para xml:lang="zh-CN">为生物生成生命条预测片段。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Power models that implement this interface are discovered automatically in
    ///         <see cref="Creature.Powers" />. Register other sources through
    ///         <see cref="HealthBarForecastRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         实现该接口的能力模型会从 <see cref="Creature.Powers" /> 中自动发现。
    ///         其他来源应通过 <see cref="HealthBarForecastRegistry" /> 注册。
    ///     </para>
    /// </remarks>
    public interface IHealthBarForecastSource
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the forecast segments to render for <paramref name="context" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取要为 <paramref name="context" /> 渲染的预测片段。
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     <para xml:lang="en">The creature and combat context being rendered.</para>
        ///     <para xml:lang="zh-CN">正在渲染的生物和战斗上下文。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A non-null sequence of forecast segments.</para>
        ///     <para xml:lang="zh-CN">非空的预测片段序列。</para>
        /// </returns>
        IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context);
    }
}
