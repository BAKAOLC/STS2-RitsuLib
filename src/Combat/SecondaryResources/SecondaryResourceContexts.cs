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
    ///     <para xml:lang="en">Provides shared data for operations on a player's secondary resource.</para>
    ///     <para xml:lang="zh-CN">提供操作玩家次级资源时使用的共享数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        AbstractModel? Source);

    /// <summary>
    ///     <para xml:lang="en">Provides data for maximum-amount calculation.</para>
    ///     <para xml:lang="zh-CN">提供计算最大数量时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceMaxContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition);

    /// <summary>
    ///     <para xml:lang="en">Describes a committed resource amount change.</para>
    ///     <para xml:lang="zh-CN">描述已提交的资源数量变化。</para>
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
    ///     <para xml:lang="en">Describes a proposed resource payment.</para>
    ///     <para xml:lang="zh-CN">描述拟执行的资源支付。</para>
    /// </summary>
    public readonly record struct SecondaryResourceSpendContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        CardModel? Card,
        int Amount,
        AbstractModel? Source);

    /// <summary>
    ///     <para xml:lang="en">Provides data for resolving the insufficient-payment policy.</para>
    ///     <para xml:lang="zh-CN">提供解析资源不足支付策略时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceInsufficientPaymentContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        CardModel Card,
        string UseId,
        SecondaryResourceUseKind Kind,
        int Cost,
        int AmountAvailable,
        int AmountToSpend,
        int Shortfall,
        AbstractModel? Source);

    /// <summary>
    ///     <para xml:lang="en">Provides data for side-effect-free planning of a replacement payment.</para>
    ///     <para xml:lang="zh-CN">提供无副作用地规划替代支付时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceShortfallResolutionContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        CardModel Card,
        string UseId,
        SecondaryResourceUseKind Kind,
        int Cost,
        int AmountAvailable,
        int AmountToSpend,
        int Shortfall,
        AbstractModel? Source);

    /// <summary>
    ///     <para xml:lang="en">Describes a committed required payment that still has a shortfall.</para>
    ///     <para xml:lang="zh-CN">描述已提交且仍有缺口的必需支付。</para>
    /// </summary>
    public readonly record struct SecondaryResourceShortfallContext(
        CombatStateLike CombatState,
        Player Player,
        SecondaryResourceDefinition Definition,
        CardModel Card,
        string UseId,
        SecondaryResourceUseKind Kind,
        int Cost,
        int AmountAvailable,
        int AmountSpent,
        int OriginalShortfall,
        int CoveredShortfall,
        int Shortfall,
        AbstractModel? Source,
        SecondaryResourcePlayLedger Ledger);

    /// <summary>
    ///     <para xml:lang="en">Provides data for global resource-cost modification.</para>
    ///     <para xml:lang="zh-CN">提供全局修正资源费用时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceCostContext(
        CombatStateLike CombatState,
        Player Player,
        CardModel Card,
        SecondaryResourceDefinition Definition,
        decimal OriginalCost);

    /// <summary>
    ///     <para xml:lang="en">Provides data for a card-local secondary-resource cost modifier.</para>
    ///     <para xml:lang="zh-CN">提供卡牌局部修正次级资源费用时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceCardCostContext(
        CardModel Card,
        SecondaryResourceDefinition Definition,
        SecondaryResourcePlayUse Use,
        decimal OriginalCost);

    /// <summary>
    ///     <para xml:lang="en">Provides data for modifying a captured secondary X value.</para>
    ///     <para xml:lang="zh-CN">提供修正已捕获次级 X 值时使用的数据。</para>
    /// </summary>
    public readonly record struct SecondaryResourceXContext(
        CombatStateLike CombatState,
        Player Player,
        CardModel Card,
        SecondaryResourceDefinition Definition,
        int OriginalValue);
}
