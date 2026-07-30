#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;

namespace STS2RitsuLib.Combat.PlayerResources
{
    /// <summary>
    ///     <para xml:lang="en">Provides context for a successful gain of a built-in player resource.</para>
    ///     <para xml:lang="zh-CN">提供玩家成功获得游戏内置资源时的上下文。</para>
    /// </summary>
    public readonly record struct PlayerResourceGainContext(
        CombatStateLike CombatState,
        Player Player,
        PlayerResourceKind Resource,
        int Amount,
        int OldAmount,
        int NewAmount);
}
