using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Combat
{
    /// <summary>
    ///     Compatibility helpers for querying the current player turn phase.
    ///     查询当前玩家回合阶段的兼容 helper。
    /// </summary>
    public static class CombatTurnPhaseExtensions
    {
        /// <summary>
        ///     Returns whether <paramref name="model" />'s owner is currently in the "Play" turn phase.
        ///     返回 <paramref name="model" /> 的拥有者当前是否处于 “Play” 回合阶段。
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
