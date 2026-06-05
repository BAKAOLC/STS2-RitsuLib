using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.ManagedActions.Patches
{
    internal static class RitsuLibManagedNetActionMessagePatches
    {
        private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, ActionQueueSet> ActionQueueSetRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, ActionQueueSet>("_actionQueueSet");

        private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, INetGameService> NetServiceRef =
            AccessTools.FieldRefAccess<ActionQueueSynchronizer, INetGameService>("_netService");

        private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>
            MessageBufferRef =
                AccessTools.FieldRefAccess<ActionQueueSynchronizer, RunLocationTargetedMessageBuffer>(
                    "_messageBuffer");

        private static readonly AccessTools.FieldRef<ActionQueueSynchronizer, List<GameAction>>
            RequestedActionsWaitingForPlayerTurnRef =
                AccessTools.FieldRefAccess<ActionQueueSynchronizer, List<GameAction>>(
                    "_requestedActionsWaitingForPlayerTurn");

        private static bool TrySendManagedClientRequest(ActionQueueSynchronizer synchronizer, GameAction action)
        {
            if (action.ActionType == GameActionType.CombatPlayPhaseOnly &&
                synchronizer.CombatState == ActionSynchronizerCombatState.NotPlayPhase)
            {
                RequestedActionsWaitingForPlayerTurnRef(synchronizer).Add(action);
                return true;
            }

            if (NetServiceRef(synchronizer) is not NetClientGameService
                    {
                        IsConnected: true, NetClient: not null,
                    }
                    client)
                return false;

            if (action.ToNetAction() is not RitsuLibManagedNetAction netAction)
                return false;

            var message = new RequestEnqueueActionMessage
            {
                action = netAction,
                location = MessageBufferRef(synchronizer).CurrentLocation,
            };
            SendManagedActionRequest(client, message);
            return true;
        }

        private static bool TrySendManagedHostAnnouncement(
            ActionQueueSynchronizer synchronizer,
            GameAction action,
            ulong actionOwnerId)
        {
            if (NetServiceRef(synchronizer) is not NetHostGameService { IsConnected: true, NetHost: not null } host)
                return false;

            if (action.ToNetAction() is not RitsuLibManagedNetAction netAction)
                return false;

            var message = new ActionEnqueuedMessage
            {
                playerId = actionOwnerId,
                location = MessageBufferRef(synchronizer).CurrentLocation,
                action = netAction,
            };
            SendManagedActionAnnouncement(host, message);
            ActionQueueSetRef(synchronizer).EnqueueWithoutSynchronizing(action);
            return true;
        }

        private static void SendManagedActionRequest(
            NetClientGameService client,
            RequestEnqueueActionMessage message)
        {
            var (bytes, length) = SerializeManagedActionMessage(client.NetId, message);
            client.NetClient!.SendMessageToHost(bytes, length, message.Mode, message.Mode.ToChannelId());
        }

        private static void SendManagedActionAnnouncement(
            NetHostGameService host,
            ActionEnqueuedMessage message)
        {
            var (bytes, length) = SerializeManagedActionMessage(host.NetId, message);
            foreach (var peer in host.ConnectedPeers)
                if (peer.readyForBroadcasting)
                    host.NetHost!.SendMessageToClient(
                        peer.peerId,
                        bytes,
                        length,
                        message.Mode,
                        message.Mode.ToChannelId());
        }

        private static (byte[] Bytes, int Length) SerializeManagedActionMessage(
            ulong senderId,
            RequestEnqueueActionMessage message)
        {
            var writer = CreateMessageWriter(senderId, message);
            writer.Write(message.location);
            RitsuLibManagedNetActions.TryWriteNetAction(writer, message.action);
            return (writer.Buffer, (int)Math.Ceiling(writer.BitPosition / 8f));
        }

        private static (byte[] Bytes, int Length) SerializeManagedActionMessage(
            ulong senderId,
            ActionEnqueuedMessage message)
        {
            var writer = CreateMessageWriter(senderId, message);
            writer.WriteULong(message.playerId);
            writer.Write(message.location);
            RitsuLibManagedNetActions.TryWriteNetAction(writer, message.action);
            return (writer.Buffer, (int)Math.Ceiling(writer.BitPosition / 8f));
        }

        private static PacketWriter CreateMessageWriter(ulong senderId, INetMessage message)
        {
            var writer = new PacketWriter();
            writer.WriteByte((byte)message.ToId());
            writer.WriteULong(senderId);
            return writer;
        }

        internal sealed class RequestEnqueueManagedAction : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_request_enqueue_direct_send";
            public static bool IsCritical => true;

            public static string Description =>
                "Send client RitsuLib-managed action requests without vanilla action id lookup";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(ActionQueueSynchronizer),
                        nameof(ActionQueueSynchronizer.RequestEnqueue),
                        [typeof(GameAction)]),
                ];
            }

            public static bool Prefix(ActionQueueSynchronizer __instance, GameAction action)
            {
                return !TrySendManagedClientRequest(__instance, action);
            }
        }

        internal sealed class EnqueueManagedAction : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_host_enqueue_direct_send";
            public static bool IsCritical => true;

            public static string Description =>
                "Broadcast host RitsuLib-managed action announcements without vanilla action id lookup";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(ActionQueueSynchronizer),
                        "EnqueueAction",
                        [typeof(GameAction), typeof(ulong)]),
                ];
            }

            public static bool Prefix(ActionQueueSynchronizer __instance, GameAction action, ulong actionOwnerId)
            {
                return !TrySendManagedHostAnnouncement(__instance, action, actionOwnerId);
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
