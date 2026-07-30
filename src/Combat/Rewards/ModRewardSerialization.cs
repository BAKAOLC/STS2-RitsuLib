using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">Creates serializable data for RitsuLib custom rewards.</para>
    ///     <para xml:lang="zh-CN">为 RitsuLib 自定义奖励创建可序列化数据。</para>
    /// </summary>
    public static class ModRewardSerialization
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="SerializableReward" /> from an <see cref="IModSerializableReward" />.
        ///         Custom rewards should return this value from their <c>ToSerializable</c> override.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         根据 <see cref="IModSerializableReward" /> 创建 <see cref="SerializableReward" />。
        ///         自定义奖励应从其 <c>ToSerializable</c> 重写中返回此结果。
        ///     </para>
        /// </summary>
        /// <param name="reward">
        ///     <para xml:lang="en">The custom reward to serialize.</para>
        ///     <para xml:lang="zh-CN">要序列化的自定义奖励。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The serializable reward data.</para>
        ///     <para xml:lang="zh-CN">可序列化的奖励数据。</para>
        /// </returns>
        public static SerializableReward CreateSerializable(IModSerializableReward reward)
        {
            ArgumentNullException.ThrowIfNull(reward);
            return CreateSerializable(reward.ModRewardType, reward.ToModRewardJson());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a <see cref="SerializableReward" /> for a registered custom reward type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为已注册的自定义奖励类型创建 <see cref="SerializableReward" />。
        ///     </para>
        /// </summary>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The registered reward type.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励类型。</para>
        /// </param>
        /// <param name="json">
        ///     <para xml:lang="en">Optional mod-owned JSON data used to restore the reward.</para>
        ///     <para xml:lang="zh-CN">用于恢复奖励、由模组维护的可选 JSON 数据。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The serializable reward data.</para>
        ///     <para xml:lang="zh-CN">可序列化的奖励数据。</para>
        /// </returns>
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
        ///     <para xml:lang="en">
        ///         Creates a <see cref="SerializableReward" /> for a registered custom reward type and serializes a
        ///         mod-owned payload with source-generated JSON metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为已注册的自定义奖励类型创建 <see cref="SerializableReward" />，并使用源生成的 JSON
        ///         元数据序列化由模组维护的载荷。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPayload">
        ///     <para xml:lang="en">The payload type.</para>
        ///     <para xml:lang="zh-CN">载荷类型。</para>
        /// </typeparam>
        /// <param name="rewardType">
        ///     <para xml:lang="en">The registered reward type.</para>
        ///     <para xml:lang="zh-CN">已注册的奖励类型。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The mod-owned payload to serialize.</para>
        ///     <para xml:lang="zh-CN">要序列化、由模组维护的载荷。</para>
        /// </param>
        /// <param name="jsonTypeInfo">
        ///     <para xml:lang="en">The source-generated JSON metadata for <typeparamref name="TPayload" />.</para>
        ///     <para xml:lang="zh-CN"><typeparamref name="TPayload" /> 的源生成 JSON 元数据。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The serializable reward data.</para>
        ///     <para xml:lang="zh-CN">可序列化的奖励数据。</para>
        /// </returns>
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
