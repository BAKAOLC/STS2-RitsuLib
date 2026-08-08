using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores supplemental reward serialization data. Data is first associated with
    ///         <see cref="SerializableReward" /> instances through a
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" />, then persisted in
    ///         <see cref="SerializableRoom.EncounterState" /> under keys prefixed by <see cref="KeyPrefix" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         存储奖励的补充序列化数据。数据先通过 <see cref="ConditionalWeakTable{TKey,TValue}" />
    ///         与 <see cref="SerializableReward" /> 实例关联，再以带有 <see cref="KeyPrefix" /> 前缀的键
    ///         持久化到 <see cref="SerializableRoom.EncounterState" />。
    ///     </para>
    /// </summary>
    internal static class RewardSerializationExt
    {
        internal const string KeyPrefix = "__mod_reward_ext_";

        private static readonly ConditionalWeakTable<SerializableReward, RewardExtData> ExtTable = [];

        internal static void SetExtData(SerializableReward reward, RewardExtData data)
        {
            ExtTable.AddOrUpdate(reward, data);
        }

        internal static bool TryGetExtData(SerializableReward reward, out RewardExtData? data)
        {
            if (ExtTable.TryGetValue(reward, out data!))
                return true;
            data = null;
            return false;
        }

        internal static string MakeKey(ulong netId, int index)
        {
            return $"{KeyPrefix}{netId}_{index}";
        }

        internal static bool TryParseKey(string key, out ulong netId, out int index)
        {
            netId = 0;
            index = 0;
            if (!key.StartsWith(KeyPrefix, StringComparison.Ordinal)) return false;

            var rest = key.AsSpan(KeyPrefix.Length);
            var sep = rest.IndexOf('_');
            if (sep < 0) return false;

            return ulong.TryParse(rest[..sep], out netId)
                   && int.TryParse(rest[(sep + 1)..], out index);
        }

        internal static string ToJson(RewardExtData data)
        {
            return JsonSerializer.Serialize(data, RewardExtJsonContext.Default.RewardExtData);
        }

        internal static RewardExtData? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize(json, RewardExtJsonContext.Default.RewardExtData);
            }
            catch (JsonException ex)
            {
                RitsuLibFramework.Logger.Debug(
                    $"[RitsuLib] Reward ext JSON deserialize failed: {ex.Message}");
                return null;
            }
            catch (NotSupportedException ex)
            {
                RitsuLibFramework.Logger.Debug(
                    $"[RitsuLib] Reward ext JSON deserialize not supported: {ex.Message}");
                return null;
            }
        }

        internal static bool IsBaselibRewardPatchLoaded()
        {
            return Type.GetType(
                "BaseLib.Patches.Fixes.CardRewardToSerializablePatch, BaseLib") != null;
        }
    }

    internal sealed class RewardExtData
    {
        internal bool HasRitsuLibData => CustomRewardJson != null || LinkedRewardSet != null;

        [JsonPropertyName("flags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Flags { get; set; }

        [JsonPropertyName("custom_card_ids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? CustomCardIds { get; set; }

        [JsonPropertyName("is_custom_pool")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsCustomPool { get; set; }

        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Source { get; set; }

        [JsonPropertyName("rarity_odds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RarityOdds { get; set; }

        [JsonPropertyName("custom_reward_json")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CustomRewardJson { get; set; }

        [JsonPropertyName("linked_reward_set")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LinkedRewardSetExtData? LinkedRewardSet { get; set; }
    }

    internal sealed class LinkedRewardSetExtData
    {
        [JsonPropertyName("mode")] public int Mode { get; set; }

        [JsonPropertyName("children")] public List<LinkedRewardChildExtData> Children { get; set; } = [];
    }

    internal sealed class LinkedRewardChildExtData
    {
        [JsonPropertyName("reward")] public string RewardJson { get; set; } = string.Empty;

        [JsonPropertyName("extension")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExtensionJson { get; set; }
    }

    [JsonSerializable(typeof(RewardExtData))]
    [JsonSerializable(typeof(LinkedRewardSetExtData))]
    [JsonSerializable(typeof(LinkedRewardChildExtData))]
    internal sealed partial class RewardExtJsonContext : JsonSerializerContext;
}
