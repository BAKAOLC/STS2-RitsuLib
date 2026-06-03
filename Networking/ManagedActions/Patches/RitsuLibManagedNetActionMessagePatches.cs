using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.ManagedActions.Patches
{
    internal static class RitsuLibManagedNetActionMessagePatches
    {
        internal sealed class MessageBusSerialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_message_bus_serialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Serialize RitsuLib-managed action messages before vanilla action id lookup";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NetMessageBus), nameof(NetMessageBus.SerializeMessage)),
                ];
            }

            public static bool Prefix<T>(
                ulong senderId,
                T message,
                ref byte[] __result,
                ref int length)
                where T : INetMessage
            {
                if (!TrySerializeManagedActionMessage(senderId, message, out var bytes, out var writtenLength))
                    return true;

                __result = bytes;
                length = writtenLength;
                return false;
            }
        }

        internal sealed class MessageBusDeserialize : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_message_bus_deserialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Deserialize action queue messages through RitsuLib-managed action reader";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NetMessageBus), nameof(NetMessageBus.TryDeserializeMessage)),
                ];
            }

            public static bool Prefix(
                byte[] packetBytes,
                ref bool __result,
                out INetMessage? message,
                out ulong? overrideSenderId)
            {
                if (!TryDeserializeManagedActionMessage(packetBytes, out message, out overrideSenderId))
                    return true;

                __result = true;
                return false;
            }
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

        private static bool TrySerializeManagedActionMessage<T>(
            ulong senderId,
            T message,
            out byte[] bytes,
            out int length)
            where T : INetMessage
        {
            bytes = [];
            length = 0;

            var writer = new PacketWriter();
            switch (message)
            {
                case RequestEnqueueActionMessage request
                    when request.action is RitsuLibManagedNetAction:
                    writer.WriteByte((byte)request.ToId());
                    writer.WriteULong(senderId);
                    writer.Write(request.location);
                    RitsuLibManagedNetActions.TryWriteNetAction(writer, request.action);
                    break;

                case ActionEnqueuedMessage announcement
                    when announcement.action is RitsuLibManagedNetAction:
                    writer.WriteByte((byte)announcement.ToId());
                    writer.WriteULong(senderId);
                    writer.WriteULong(announcement.playerId);
                    writer.Write(announcement.location);
                    RitsuLibManagedNetActions.TryWriteNetAction(writer, announcement.action);
                    break;

                default:
                    return false;
            }

            length = (int)Math.Ceiling(writer.BitPosition / 8f);
            bytes = writer.Buffer;
            return true;
        }

        private static bool TryDeserializeManagedActionMessage(
            byte[] packetBytes,
            out INetMessage? message,
            out ulong? overrideSenderId)
        {
            message = null;
            overrideSenderId = null;

            var reader = new PacketReader();
            reader.Reset(packetBytes);
            var messageId = reader.ReadByte();
            if (!MessageTypes.TryGetMessageType(messageId, out var messageType))
                return false;

            if (messageType != typeof(RequestEnqueueActionMessage) &&
                messageType != typeof(ActionEnqueuedMessage))
                return false;

            overrideSenderId = reader.ReadULong();
            if (messageType == typeof(RequestEnqueueActionMessage))
            {
                message = new RequestEnqueueActionMessage
                {
                    location = reader.Read<RunLocation>(),
                    action = RitsuLibManagedNetActions.ReadNetAction(reader),
                };
                return true;
            }

            message = new ActionEnqueuedMessage
            {
                playerId = reader.ReadULong(),
                location = reader.Read<RunLocation>(),
                action = RitsuLibManagedNetActions.ReadNetAction(reader),
            };
            return true;
        }
    }
}
