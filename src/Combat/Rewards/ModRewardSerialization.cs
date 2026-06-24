using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     Helpers for RitsuLib reward-side save data.
    ///     RitsuLib reward 侧存档数据辅助 API。
    /// </summary>
    public static class ModRewardSerialization
    {
        /// <summary>
        ///     Creates a <see cref="SerializableReward" /> for an <see cref="IModSerializableReward" />.
        ///     Custom reward implementations should return this from their <c>ToSerializable</c> override.
        ///     为 <see cref="IModSerializableReward" /> 创建 <see cref="SerializableReward" />。自定义 reward 应在
        ///     <c>ToSerializable</c> 重写中返回它。
        /// </summary>
        public static SerializableReward CreateSerializable(IModSerializableReward reward)
        {
            ArgumentNullException.ThrowIfNull(reward);
            return CreateSerializable(reward.ModRewardType, reward.ToModRewardJson());
        }

        /// <summary>
        ///     Creates a <see cref="SerializableReward" /> for a registered custom reward type.
        ///     为已注册的自定义 reward 类型创建 <see cref="SerializableReward" />。
        /// </summary>
        public static SerializableReward CreateSerializable(RewardType rewardType, string? json = null)
        {
            var result = new SerializableReward
            {
                RewardType = rewardType,
            };

            if (json != null)
                RewardSerializationExt.SetExtData(result, new()
                {
                    CustomRewardJson = json,
                });

            return result;
        }

        /// <summary>
        ///     Creates a <see cref="SerializableReward" /> for a registered custom reward type and serializes
        ///     a mod-owned payload with a source-generated JSON contract.
        ///     为已注册的自定义 reward 类型创建 <see cref="SerializableReward" />，并用传入的 JSON 协定序列化 mod 载荷。
        /// </summary>
        public static SerializableReward CreateSerializable<TPayload>(
            RewardType rewardType,
            TPayload payload,
            JsonTypeInfo<TPayload> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            return CreateSerializable(rewardType, JsonSerializer.Serialize(payload, jsonTypeInfo));
        }
    }
}
