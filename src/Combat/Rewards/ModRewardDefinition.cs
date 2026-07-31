using MegaCrit.Sts2.Core.Rewards;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">Describes a registered custom reward type.</para>
    ///     <para xml:lang="zh-CN">描述已注册的自定义奖励类型。</para>
    /// </summary>
    /// <param name="ModId">
    ///     <para xml:lang="en">The ID of the owning mod, or an empty string for a global registration.</para>
    ///     <para xml:lang="zh-CN">所属模组的 ID；全局注册时为空字符串。</para>
    /// </param>
    /// <param name="Id">
    ///     <para xml:lang="en">The normalized reward ID.</para>
    ///     <para xml:lang="zh-CN">规范化后的奖励 ID。</para>
    /// </param>
    /// <param name="RewardType">
    ///     <para xml:lang="en">The dynamic or native reward type assigned to the registration.</para>
    ///     <para xml:lang="zh-CN">分配给该注册项的动态或原版奖励类型。</para>
    /// </param>
    public sealed record ModRewardDefinition(
        string ModId,
        string Id,
        RewardType RewardType);
}
