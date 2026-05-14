using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    /// <summary>
    ///     Flushes reward sideband data into <see cref="SerializableRoom.EncounterState" /> after
    ///     <see cref="CombatRoom.ToSerializable" /> creates serializable rewards.
    ///     <c>EncounterState</c>。
    ///     在 <see cref="CombatRoom.ToSerializable" /> 创建可序列化 reward 后，将 reward sideband 数据写入
    ///     <see cref="SerializableRoom.EncounterState" />。
    ///     <c>EncounterState</c>。
    /// </summary>
    internal sealed class CombatRoomToSerializableRewardExtPatch : IPatchMethod
    {
        public static string PatchId => "combat_room_to_serializable_reward_ext";

        public static string Description =>
            "Flush reward ext data into SerializableRoom.EncounterState after CombatRoom.ToSerializable";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatRoom), nameof(CombatRoom.ToSerializable), Type.EmptyTypes)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(ref SerializableRoom __result)
            // ReSharper restore InconsistentNaming
        {
            var baselibRewardPatchLoaded = RewardSerializationExt.IsBaselibRewardPatchLoaded();

            foreach (var (netId, rewards) in __result.ExtraRewards)
                for (var i = 0; i < rewards.Count; i++)
                {
                    if (!RewardSerializationExt.TryGetExtData(rewards[i], out var ext) || ext == null)
                        continue;
                    // BaseLib owns CardReward sideband serialization when present. RitsuLib still persists
                    // custom reward payloads because BaseLib does not know about RitsuLib reward ids.
                    if (baselibRewardPatchLoaded && !ext.HasCustomRewardData)
                        continue;

                    var key = RewardSerializationExt.MakeKey(netId, i);
                    __result.EncounterState ??= new();
                    __result.EncounterState[key] = RewardSerializationExt.ToJson(ext);
                }
        }
    }

    /// <summary>
    ///     Restores reward sideband data from <see cref="SerializableRoom.EncounterState" /> before
    ///     <see cref="CombatRoom.FromSerializable" /> rebuilds rewards.
    ///     在 <see cref="CombatRoom.FromSerializable" /> 重建 reward 前，从 <see cref="SerializableRoom.EncounterState" /> 还原 reward
    ///     sideband 数据。
    /// </summary>
    internal sealed class CombatRoomFromSerializableRewardExtPatch : IPatchMethod
    {
        public static string PatchId => "combat_room_from_serializable_reward_ext";

        public static string Description =>
            "Restore reward ext data from SerializableRoom.EncounterState before CombatRoom.FromSerializable";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CombatRoom), nameof(CombatRoom.FromSerializable))];
        }

        // ReSharper disable InconsistentNaming
        public static void Prefix(SerializableRoom serializableRoom)
            // ReSharper restore InconsistentNaming
        {
            if (serializableRoom.EncounterState == null)
                return;

            var baselibRewardPatchLoaded = RewardSerializationExt.IsBaselibRewardPatchLoaded();

            foreach (var (key, json) in serializableRoom.EncounterState)
            {
                if (!RewardSerializationExt.TryParseKey(key, out var netId, out var index))
                    continue;

                if (!serializableRoom.ExtraRewards.TryGetValue(netId, out var rewards))
                    continue;

                if (index < 0 || index >= rewards.Count)
                    continue;

                var ext = RewardSerializationExt.FromJson(json);
                if (ext == null)
                    continue;
                // Match the ToSerializable side: keep custom reward payloads, leave CardReward data to BaseLib.
                if (baselibRewardPatchLoaded && !ext.HasCustomRewardData)
                    continue;

                RewardSerializationExt.SetExtData(rewards[index], ext);
            }

            foreach (var (_, rewards) in serializableRoom.ExtraRewards)
            {
                var removed = rewards.RemoveAll(r => r.RewardType == RewardType.None);
                if (removed > 0)
                    Log.Warn($"[RitsuLib] Stripped {removed} RewardType.None entry(s) from ExtraRewards " +
                             "(e.g. LinkedRewardSet) — serialization for this type is not supported.");
            }
        }
    }
}
