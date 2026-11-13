using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.ManagedActions.Patches
{
    internal static class RitsuLibManagedNetActionMessagePatches
    {
        private const int ReplayEventTypeBits = 3;
        private const int ReplayGameActionPlayerIdBits = 64;
        private const int ReplayGameActionPayloadOffsetBits = ReplayEventTypeBits + ReplayGameActionPlayerIdBits;

        private static PacketReader CreateProbeReader(PacketReader reader)
        {
            var probe = new PacketReader();
            probe.Reset(reader.Buffer);
            probe.BitPosition = reader.BitPosition;
            return probe;
        }

        internal sealed class RequestSerialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_request_serialize";
            public static bool IsCritical => true;
            public static string Description => "Serialize RitsuLib-managed actions inside vanilla action requests";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(RequestEnqueueActionMessage),
                        nameof(RequestEnqueueActionMessage.Serialize),
                        [typeof(PacketWriter)]),
                ];
            }

            public static bool Prefix(RequestEnqueueActionMessage __instance, PacketWriter writer)
            {
                if (__instance.action is not RitsuLibManagedNetAction)
                    return true;

                writer.Write(__instance.location);
                RitsuLibManagedNetActions.TryWriteNetAction(writer, __instance.action);
                return false;
            }
        }

        internal sealed class RequestDeserialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_request_deserialize";
            public static bool IsCritical => true;
            public static string Description => "Deserialize RitsuLib-managed actions inside vanilla action requests";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(RequestEnqueueActionMessage),
                        nameof(RequestEnqueueActionMessage.Deserialize),
                        [typeof(PacketReader)]),
                ];
            }

            public static bool Prefix(ref RequestEnqueueActionMessage __instance, PacketReader reader)
            {
                var probe = CreateProbeReader(reader);
                var location = probe.Read<RunLocation>();
                if (!RitsuLibManagedNetActions.NextPayloadIsManagedAction(probe))
                    return true;

                var action = RitsuLibManagedNetActions.ReadNetAction(probe);
                __instance.location = location;
                __instance.action = action;
                reader.BitPosition = probe.BitPosition;
                return false;
            }
        }

        internal sealed class AnnouncementSerialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_announcement_serialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Serialize RitsuLib-managed actions inside vanilla action announcements";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(ActionEnqueuedMessage),
                        nameof(ActionEnqueuedMessage.Serialize),
                        [typeof(PacketWriter)]),
                ];
            }

            public static bool Prefix(ActionEnqueuedMessage __instance, PacketWriter writer)
            {
                if (__instance.action is not RitsuLibManagedNetAction)
                    return true;

                writer.WriteULong(__instance.playerId);
                writer.Write(__instance.location);
                RitsuLibManagedNetActions.TryWriteNetAction(writer, __instance.action);
                return false;
            }
        }

        internal sealed class AnnouncementDeserialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_announcement_deserialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Deserialize RitsuLib-managed actions inside vanilla action announcements";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(ActionEnqueuedMessage),
                        nameof(ActionEnqueuedMessage.Deserialize),
                        [typeof(PacketReader)]),
                ];
            }

            public static bool Prefix(ref ActionEnqueuedMessage __instance, PacketReader reader)
            {
                var probe = CreateProbeReader(reader);
                var playerId = probe.ReadULong();
                var location = probe.Read<RunLocation>();
                if (!RitsuLibManagedNetActions.NextPayloadIsManagedAction(probe))
                    return true;

                var action = RitsuLibManagedNetActions.ReadNetAction(probe);
                __instance.playerId = playerId;
                __instance.location = location;
                __instance.action = action;
                reader.BitPosition = probe.BitPosition;
                return false;
            }
        }

        internal sealed class ReplayEventSerialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_replay_event_serialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Serialize RitsuLib-managed actions inside combat replay events";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CombatReplayEvent),
                        nameof(CombatReplayEvent.Serialize),
                        [typeof(PacketWriter)]),
                ];
            }

            public static bool Prefix(CombatReplayEvent __instance, PacketWriter writer)
            {
                if (__instance.eventType != CombatReplayEventType.GameAction ||
                    __instance.action is not RitsuLibManagedNetAction)
                    return true;

                writer.WriteInt((int)__instance.eventType, ReplayEventTypeBits);
                writer.WriteULong(__instance.playerId!.Value);
                RitsuLibManagedNetActions.TryWriteNetAction(writer, __instance.action);
                return false;
            }
        }

        internal sealed class ReplayEventDeserialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_replay_event_deserialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Deserialize RitsuLib-managed actions inside combat replay events";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(CombatReplayEvent),
                        nameof(CombatReplayEvent.Deserialize),
                        [typeof(PacketReader)]),
                ];
            }

            public static bool Prefix(ref CombatReplayEvent __instance, PacketReader reader)
            {
                if (!ReplayEventPayloadIsManagedGameAction(reader))
                    return true;

                __instance.eventType = (CombatReplayEventType)reader.ReadInt(ReplayEventTypeBits);
                __instance.playerId = reader.ReadULong();
                __instance.action = RitsuLibManagedNetActions.ReadNetAction(reader);
                return false;
            }

            private static bool ReplayEventPayloadIsManagedGameAction(PacketReader reader)
            {
                return RitsuLibManagedNetActions.TryPeekInt(
                           reader,
                           0,
                           ReplayEventTypeBits,
                           out var eventType) &&
                       eventType == (int)CombatReplayEventType.GameAction &&
                       RitsuLibManagedNetActions.NextPayloadIsManagedAction(
                           reader,
                           ReplayGameActionPayloadOffsetBits);
            }
        }
    }
}
