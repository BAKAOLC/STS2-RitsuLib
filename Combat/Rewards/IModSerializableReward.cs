using MegaCrit.Sts2.Core.Rewards;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     Implement this on custom <see cref="Reward" /> types that need to survive combat-room save/load.
    ///     This interface only describes persistence; multiplayer side effects still need to be deterministic
    ///     or synchronized by the reward implementation.
    ///     自定义 <see cref="Reward" /> 需要在战斗房间存读档中保留时实现此接口。
    ///     此接口只描述持久化；多人副作用仍需由 reward 实现保持确定性或自行同步。
    /// </summary>
    public interface IModSerializableReward
    {
        /// <summary>
        ///     Dynamic or vanilla reward type used by <see cref="ModRewardRegistry" /> to rebuild the reward.
        ///     由 <see cref="ModRewardRegistry" /> 用于重建 reward 的动态或原版 reward type。
        /// </summary>
        RewardType ModRewardType { get; }

        /// <summary>
        ///     Optional JSON payload owned by the mod. Return null when the reward type alone is enough.
        ///     由 mod 自己维护的可选 JSON payload。仅靠 reward type 足够时返回 null。
        /// </summary>
        string? ToModRewardJson();
    }
}
