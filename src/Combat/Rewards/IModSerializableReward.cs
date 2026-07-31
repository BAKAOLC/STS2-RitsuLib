using MegaCrit.Sts2.Core.Rewards;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines the persistence data required to restore a custom <see cref="Reward" /> with a combat room.
    ///         Reward side effects must still be deterministic on every client or explicitly synchronized.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义自定义 <see cref="Reward" /> 随战斗房间存档并恢复时所需的持久化数据。
    ///         奖励的副作用仍须在各客户端确定性执行，或由实现显式同步。
    ///     </para>
    /// </summary>
    public interface IModSerializableReward
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the dynamic or native reward type used by <see cref="ModRewardRegistry" /> to rebuild the
        ///         reward.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <see cref="ModRewardRegistry" /> 重建该奖励时使用的动态或原版奖励类型。
        ///     </para>
        /// </summary>
        RewardType ModRewardType { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates the optional mod-owned JSON payload. Returns <see langword="null" /> when the reward type
        ///         contains enough information to restore the reward.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建由模组维护的可选 JSON 载荷。仅凭奖励类型即可恢复奖励时返回
        ///         <see langword="null" />。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The mod-owned JSON payload, or <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">由模组维护的 JSON 载荷，或 <see langword="null" />。</para>
        /// </returns>
        string? ToModRewardJson();
    }
}
