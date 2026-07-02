#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.PlayerResources
{
    /// <summary>
    ///     Context for successful built-in player resource gains.
    ///     内建玩家资源成功获得时的上下文。
    /// </summary>
    public readonly record struct PlayerResourceGainContext(
        CombatStateLike CombatState,
        Player Player,
        PlayerResourceKind Resource,
        int Amount,
        int OldAmount,
        int NewAmount);
}
