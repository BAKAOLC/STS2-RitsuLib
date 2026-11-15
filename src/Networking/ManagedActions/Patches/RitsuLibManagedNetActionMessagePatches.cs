using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
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
        private const int ReplayEventTypeBits = 3;
        private const int ReplayGameActionPlayerIdBits = 64;
        private const int ReplayGameActionPayloadOffsetBits = ReplayEventTypeBits + ReplayGameActionPlayerIdBits;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SendRequest(INetGameService service, RequestEnqueueActionMessage message)
        {
            if (message.action is not RitsuLibManagedNetAction ||
                service is not NetClientGameService { IsConnected: true, NetClient: { } client })
            {
                service.SendMessage(message);
                return;
            }

            var writer = new PacketWriter();
            writer.WriteByte((byte)message.ToId());
            writer.WriteULong(service.NetId);
            RequestSerialize.Prefix(message, writer);
            client.SendMessageToHost(writer.Buffer, writer.BytePosition, message.Mode, message.Mode.ToChannelId());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SendAnnouncement(INetGameService service, ActionEnqueuedMessage message)
        {
            if (message.action is not RitsuLibManagedNetAction ||
                service is not NetHostGameService { IsConnected: true, NetHost: { } transport } host)
            {
                service.SendMessage(message);
                return;
            }

            var writer = new PacketWriter();
            writer.WriteByte((byte)message.ToId());
            writer.WriteULong(service.NetId);
            AnnouncementSerialize.Prefix(message, writer);
            foreach (var peer in host.ConnectedPeers)
                if (peer.readyForBroadcasting)
                    transport.SendMessageToClient(peer.peerId, writer.Buffer, writer.BytePosition,
                        message.Mode, message.Mode.ToChannelId());
        }

        internal sealed class QueueSend : IPatchMethod
        {
            public static string PatchId => "ritsulib_managed_net_action_queue_send";
            public static bool IsCritical => true;

            public static string Description =>
                "Send managed queue actions through a non-inlined serialization boundary";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(ActionQueueSynchronizer), nameof(ActionQueueSynchronizer.RequestEnqueue),
                        [typeof(GameAction)]),
                    new(typeof(ActionQueueSynchronizer), "EnqueueAction", [typeof(GameAction), typeof(ulong)]),
                ];
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var replacements = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.operand is MethodInfo
                        {
                            DeclaringType: { } declaringType,
                            Name: nameof(INetGameService.SendMessage),
                            IsGenericMethod: true,
                        } method && declaringType == typeof(INetGameService) && method.GetParameters().Length == 1)
                    {
                        var messageType = method.GetGenericArguments()[0];
                        var replacement = messageType == typeof(RequestEnqueueActionMessage)
                            ? nameof(SendRequest)
                            : messageType == typeof(ActionEnqueuedMessage)
                                ? nameof(SendAnnouncement)
                                : null;
                        if (replacement != null)
                        {
                            instruction.opcode = OpCodes.Call;
                            instruction.operand = AccessTools.DeclaredMethod(
                                typeof(RitsuLibManagedNetActionMessagePatches), replacement);
                            replacements++;
                        }
                    }

                    yield return instruction;
                }

                if (replacements != 1)
                    throw new InvalidOperationException(
                        $"Expected one action-message send call, found {replacements}.");
            }
        }

        internal sealed class BusDeserialize : IPatchMethod
        {
            private static readonly AccessTools.FieldRef<NetMessageBus, PacketReader> ReaderRef =
                AccessTools.FieldRefAccess<NetMessageBus, PacketReader>("_reader");

            public static string PatchId => "ritsulib_managed_net_action_bus_deserialize";
            public static bool IsCritical => true;

            public static string Description =>
                "Read managed actions before vanilla message deserializers can be inlined";

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(NetMessageBus), nameof(NetMessageBus.TryDeserializeMessage),
                        [typeof(byte[]), typeof(INetMessage).MakeByRefType(), typeof(ulong?).MakeByRefType()]),
                ];
            }

            private static bool Prefix(NetMessageBus __instance, byte[] packetBytes, ref INetMessage? message,
                ref ulong? overrideSenderId, ref bool __result)
            {
                if (packetBytes.Length < 9)
                    return true;
                var reader = ReaderRef(__instance);
                reader.Reset(packetBytes);
                if (!MessageTypes.TryGetMessageType(reader.ReadByte(), out var type) ||
                    (type != typeof(RequestEnqueueActionMessage) && type != typeof(ActionEnqueuedMessage)))
                    return true;
                var sender = reader.ReadULong();
                if (type == typeof(RequestEnqueueActionMessage))
                {
                    var request = new RequestEnqueueActionMessage();
                    if (RequestDeserialize.Prefix(ref request, reader))
                        return true;
                    message = request;
                }
                else
                {
                    var announcement = new ActionEnqueuedMessage();
                    if (AnnouncementDeserialize.Prefix(ref announcement, reader))
                        return true;
                    message = announcement;
                }

                overrideSenderId = sender;
                __result = true;
                return false;
            }
        }

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
