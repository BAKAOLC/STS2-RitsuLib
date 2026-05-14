using MegaCrit.Sts2.Core.Rewards;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     Immutable registration entry for a custom reward type.
    ///     自定义 reward type 的不可变注册 entry。
    /// </summary>
    public sealed record ModRewardDefinition(
        string ModId,
        string Id,
        RewardType RewardType);
}
