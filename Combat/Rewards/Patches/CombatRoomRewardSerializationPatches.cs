using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.Rewards.Patches
{
    /// <summary>
    ///     After <see cref="CombatRoom.ToSerializable" />, flushes any
    ///     <see cref="RewardExtData" /> attached to <see cref="SerializableReward" /> instances
    ///     into <see cref="SerializableRoom.EncounterState" /> as JSON strings.
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
            if (RewardSerializationExt.IsBaselibRewardPatchLoaded())
                return;

            foreach (var (netId, rewards) in __result.ExtraRewards)
                for (var i = 0; i < rewards.Count; i++)
                {
                    if (!RewardSerializationExt.TryGetExtData(rewards[i], out var ext) || ext == null)
                        continue;

                    var key = RewardSerializationExt.MakeKey(netId, i);
                    __result.EncounterState ??= new();
                    __result.EncounterState[key] = RewardSerializationExt.ToJson(ext);
                }
        }
    }

    /// <summary>
    ///     Before <see cref="CombatRoom.FromSerializable" /> processes rewards, extracts
    ///     sideband data from <see cref="SerializableRoom.EncounterState" /> and attaches it
    ///     to the corresponding <see cref="SerializableReward" /> instances so that
    ///     <see cref="RewardFromSerializableExtPatch" /> can consume them.
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
            if (RewardSerializationExt.IsBaselibRewardPatchLoaded())
                return;

            if (serializableRoom.EncounterState == null)
                return;

            foreach (var (key, json) in serializableRoom.EncounterState)
            {
                if (!RewardSerializationExt.TryParseKey(key, out var netId, out var index))
                    continue;

                if (!serializableRoom.ExtraRewards.TryGetValue(netId, out var rewards))
                    continue;

                if (index < 0 || index >= rewards.Count)
                    continue;

                var ext = RewardSerializationExt.FromJson(json);
                if (ext != null)
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
