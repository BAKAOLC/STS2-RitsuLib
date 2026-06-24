#if !STS2_AT_LEAST_0_104_0
using CombatStateLike = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateLike = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Combat.SecondaryResources
{
    /// <summary>
    ///     Base context for player-owned secondary resource operations.
    ///     玩家所属次级资源操作的基础上下文。
    /// </summary>
    public readonly record struct SecondaryResourceContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        AbstractModel? Source);

    /// <summary>
    ///     Context for max-amount calculation.
    ///     最大数量计算上下文。
    /// </summary>
    public readonly record struct SecondaryResourceMaxContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition);

    /// <summary>
    ///     Context for amount changes.
    ///     数量变化上下文。
    /// </summary>
    public readonly record struct SecondaryResourceChangeContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        int OldAmount,
        int NewAmount,
        int Delta,
        SecondaryResourceChangeReason Reason,
        AbstractModel? Source);

    /// <summary>
    ///     Context for resource spending.
    ///     资源消耗上下文。
    /// </summary>
    public readonly record struct SecondaryResourceSpendContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        CardModel? Card,
        int Amount,
        AbstractModel? Source);

    /// <summary>
    ///     Context for resource cost modification.
    ///     资源费用修正上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCostContext(
        CombatStateLike CombatState,
        Player Player,
        CardModel Card,
        SecondaryResourceDefinition Definition,
        decimal OriginalCost);

    /// <summary>
    ///     Context for local card-attached secondary-resource cost modification.
    ///     卡牌本地附加次级资源费用修正上下文。
    /// </summary>
    public readonly record struct SecondaryResourceCardCostContext(
        CardModel Card,
        SecondaryResourceDefinition Definition,
        SecondaryResourcePlayUse Use,
        decimal OriginalCost);

    /// <summary>
    ///     Context for secondary X-value modification.
    ///     次级 X 值修正上下文。
    /// </summary>
    public readonly record struct SecondaryResourceXContext(
        CombatStateLike CombatState,
        Player Player,
        CardModel Card,
        SecondaryResourceDefinition Definition,
        int OriginalValue);
}
