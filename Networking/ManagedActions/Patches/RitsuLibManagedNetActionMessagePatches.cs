using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.ManagedActions.Patches
{
    internal static class RitsuLibManagedNetActionMessagePatches
    {
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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(RequestEnqueueActionMessage __instance, PacketWriter writer)
            {
                writer.Write(__instance.location);
                if (RitsuLibManagedNetActions.TryWriteNetAction(writer, __instance.action)) return false;
                writer.WriteByte((byte)__instance.action.ToId());
                writer.Write(__instance.action);

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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(ref RequestEnqueueActionMessage __instance, PacketReader reader)
            {
                __instance.location = reader.Read<RunLocation>();
                __instance.action = RitsuLibManagedNetActions.ReadNetAction(reader);
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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(ActionEnqueuedMessage __instance, PacketWriter writer)
            {
                writer.WriteULong(__instance.playerId);
                writer.Write(__instance.location);
                if (RitsuLibManagedNetActions.TryWriteNetAction(writer, __instance.action)) return false;
                writer.WriteByte((byte)__instance.action.ToId());
                writer.Write(__instance.action);

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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(ref ActionEnqueuedMessage __instance, PacketReader reader)
            {
                __instance.playerId = reader.ReadULong();
                __instance.location = reader.Read<RunLocation>();
                __instance.action = RitsuLibManagedNetActions.ReadNetAction(reader);
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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(CombatReplayEvent __instance, PacketWriter writer)
            {
                writer.WriteInt((int)__instance.eventType, 3);
                switch (__instance.eventType)
                {
                    case CombatReplayEventType.GameAction:
                        writer.WriteULong(__instance.playerId!.Value);
                        var action = __instance.action ??
                                     throw new InvalidOperationException(
                                         "Combat replay game action event has no action.");
                        if (RitsuLibManagedNetActions.TryWriteNetAction(writer, action)) return false;
                        writer.WriteByte((byte)action.ToId());
                        writer.Write(action);
                        break;
                    case CombatReplayEventType.HookAction:
                        writer.WriteULong(__instance.playerId!.Value);
                        writer.WriteUInt(__instance.hookId!.Value);
                        writer.WriteEnum(__instance.gameActionType!.Value);
                        break;
                    case CombatReplayEventType.ResumeAction:
                        writer.WriteUInt(__instance.actionId!.Value);
                        break;
                    case CombatReplayEventType.PlayerChoice:
                        writer.WriteULong(__instance.playerId!.Value);
                        writer.WriteUInt(__instance.choiceId!.Value);
                        writer.Write(__instance.playerChoiceResult!.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(__instance.eventType));
                }

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

            // ReSharper disable once InconsistentNaming
            public static bool Prefix(ref CombatReplayEvent __instance, PacketReader reader)
            {
                __instance.eventType = (CombatReplayEventType)reader.ReadInt(3);
                switch (__instance.eventType)
                {
                    case CombatReplayEventType.GameAction:
                        __instance.playerId = reader.ReadULong();
                        __instance.action = RitsuLibManagedNetActions.ReadNetAction(reader);
                        break;
                    case CombatReplayEventType.HookAction:
                        __instance.playerId = reader.ReadULong();
                        __instance.hookId = reader.ReadUInt();
                        __instance.gameActionType = reader.ReadEnum<GameActionType>();
                        break;
                    case CombatReplayEventType.ResumeAction:
                        __instance.actionId = reader.ReadUInt();
                        break;
                    case CombatReplayEventType.PlayerChoice:
                        __instance.playerId = reader.ReadULong();
                        __instance.choiceId = reader.ReadUInt();
                        __instance.playerChoiceResult = reader.Read<NetPlayerChoiceResult>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return false;
            }
        }
    }
}
