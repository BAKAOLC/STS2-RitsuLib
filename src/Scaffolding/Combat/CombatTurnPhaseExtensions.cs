using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Combat
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides compatibility extensions for querying the current player turn phase.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供用于查询当前玩家回合阶段的兼容扩展方法。
    ///     </para>
    /// </summary>
    public static class CombatTurnPhaseExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether the owner of <paramref name="model" /> is currently in the
        ///         <see cref="PlayerTurnPhase.Play" /> phase.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="model" /> 的拥有者当前是否处于
        ///         <see cref="PlayerTurnPhase.Play" /> 阶段。
        ///     </para>
        /// </summary>
        public static bool IsOwnerPlayPhase(this CardModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

#if !STS2_AT_LEAST_0_104_0
            return CombatManager.Instance.IsPlayPhase;
#else
            return model.Owner?.PlayerCombatState?.Phase == PlayerTurnPhase.Play;
#endif
        }
    }
}
