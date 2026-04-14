using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     Sideband storage for extended reward serialization data.
    ///     Attached to <see cref="SerializableReward" /> instances via <see cref="ConditionalWeakTable{TKey,TValue}" />.
    ///     Persisted in <see cref="SerializableRoom.EncounterState" /> under keys prefixed with
    ///     <see cref="KeyPrefix" />.
    /// </summary>
    internal static class RewardSerializationExt
    {
        internal const string KeyPrefix = "__mod_reward_ext_";

        private static readonly ConditionalWeakTable<SerializableReward, RewardExtData> ExtTable = new();

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
            if (!key.StartsWith(KeyPrefix)) return false;

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
            catch
            {
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
    }

    [JsonSerializable(typeof(RewardExtData))]
    internal sealed partial class RewardExtJsonContext : JsonSerializerContext;
}
