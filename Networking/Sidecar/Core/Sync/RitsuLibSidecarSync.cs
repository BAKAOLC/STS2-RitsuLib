using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarSync
    {
        public const ulong MessageOpcode = 0x20;
        public const ulong ActionRequestOpcode = 0x21;
        public const ulong ActionAnnouncementOpcode = 0x22;

        private const int VanillaReliableChannel = 0;
        private const int InitialOffset = 0;
        private const int InitialIndex = 0;
        private const int EmptyLength = 0;
        private const int NoVanillaMessagesWaiting = 0;
        private const ulong DefaultDescriptorOpcode = 0;
        private const ulong DefaultNetId = 0;
        private const byte FalseByte = 0;
        private const byte TrueByte = 1;
        private const byte Version = 1;
        private const int VersionSize = RitsuLibSidecarBinaryLayout.ByteSize;
        private const int RouteSize = RitsuLibSidecarBinaryLayout.ByteSize;
        private const int BooleanSize = RitsuLibSidecarBinaryLayout.ByteSize;
        private const int GameActionTypeSize = RitsuLibSidecarBinaryLayout.ByteSize;
        private const int LengthPrefixSize = RitsuLibSidecarBinaryLayout.U32Size;
        private const int DescriptorOpcodeSize = RitsuLibSidecarBinaryLayout.U64Size;
        private const int NetIdSize = RitsuLibSidecarBinaryLayout.U64Size;
        private const int HookActionIdSize = RitsuLibSidecarBinaryLayout.U32Size;

        private const int MessagePacketFixedSize =
            VersionSize +
            DescriptorOpcodeSize +
            NetIdSize +
            RouteSize +
            BooleanSize +
            LengthPrefixSize +
            LengthPrefixSize;

        private const int ActionPacketFixedSize =
            VersionSize +
            DescriptorOpcodeSize +
            NetIdSize +
            HookActionIdSize +
            GameActionTypeSize +
            LengthPrefixSize +
            LengthPrefixSize;

        private static readonly Lock Gate = new();
        private static readonly Dictionary<NetMessageBus, List<BufferedSyncContext>> WaitingForNetBus = [];
        private static readonly List<LocationBufferedSyncContext> WaitingForLocation = [];
        private static readonly HashSet<RunLocation> VisitedLocations = [];

        private static readonly AccessTools.FieldRef<NetHostGameService, NetMessageBus> HostMessageBus =
            AccessTools.FieldRefAccess<NetHostGameService, NetMessageBus>("_messageBus");

        private static readonly AccessTools.FieldRef<NetClientGameService, NetMessageBus> ClientMessageBus =
            AccessTools.FieldRefAccess<NetClientGameService, NetMessageBus>("_messageBus");

        private static readonly AccessTools.FieldRef<NetMessageBus, bool> NetBusIsBuffering =
            AccessTools.FieldRefAccess<NetMessageBus, bool>("_isBufferingMessages");

        private static readonly AccessTools.FieldRef<NetMessageBus, List<(INetMessage, ulong)>> NetBusBufferedMessages =
            AccessTools.FieldRefAccess<NetMessageBus, List<(INetMessage, ulong)>>("_bufferedMessages");

        private static readonly FieldInfo? LocationWaitingMessagesField =
            AccessTools.Field(typeof(RunLocationTargetedMessageBuffer), "_messagesWaitingOnLocationChange");

        private static readonly FieldInfo? LocationVisitedLocationsField =
            AccessTools.Field(typeof(RunLocationTargetedMessageBuffer), "_visitedLocations");

        private static readonly FieldInfo? LocationCurrentLocationField =
            AccessTools.Field(typeof(RunLocationTargetedMessageBuffer), "<CurrentLocation>k__BackingField");

        private static readonly MethodInfo? LocationCallHandlersOfTypeMethod =
            AccessTools.Method(typeof(RunLocationTargetedMessageBuffer), "CallHandlersOfType");

        public static bool TrySendToHost(NetClientGameService client, ulong opcode, ReadOnlySpan<byte> payload)
        {
            return RitsuLibSidecarSend.TrySendToHost(
                client,
                CreateEnvelope(opcode, payload),
                NetTransferMode.Reliable,
                VanillaReliableChannel);
        }

        public static bool TrySendToPeer(
            NetHostGameService host,
            ulong peerNetId,
            ulong opcode,
            ReadOnlySpan<byte> payload)
        {
            return RitsuLibSidecarSend.TrySendToPeer(
                host,
                peerNetId,
                CreateEnvelope(opcode, payload),
                NetTransferMode.Reliable,
                VanillaReliableChannel);
        }

        public static bool TryBroadcastToReadyPeers(
            NetHostGameService host,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            ulong? excludePeerId)
        {
            var envelope = CreateEnvelope(opcode, payload);
            return host.ConnectedPeers.Where(peer => peer.readyForBroadcasting && peer.peerId != excludePeerId)
                .All(peer => RitsuLibSidecarSend.TrySendToPeer(host, peer.peerId, envelope, NetTransferMode.Reliable,
                    VanillaReliableChannel));
        }

        public static byte[] WriteMessagePacket(
            ulong descriptorOpcode,
            ulong originalSenderNetId,
            RitsuLibSidecarSyncMessageRoute route,
            bool locationTargeted,
            RunLocation location,
            ReadOnlySpan<byte> payload)
        {
            var locationBytes = locationTargeted ? WriteLocation(location) : [];
            var buffer = new byte[
                MessagePacketFixedSize +
                locationBytes.Length +
                payload.Length];
            var span = buffer.AsSpan();
            var offset = InitialOffset;
            span[offset++] = Version;
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, DescriptorOpcodeSize), descriptorOpcode);
            offset += DescriptorOpcodeSize;
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, NetIdSize), originalSenderNetId);
            offset += NetIdSize;
            span[offset++] = (byte)route;
            span[offset++] = locationTargeted ? TrueByte : FalseByte;
            WriteBytes(span, ref offset, locationBytes);
            WriteBytes(span, ref offset, payload);
            return buffer;
        }

        public static bool TryReadMessagePacket(
            ReadOnlySpan<byte> span,
            out RitsuLibSidecarSyncMessagePacket packet)
        {
            packet = default;
            var offset = InitialOffset;
            if (!TryReadMessageHeader(span, ref offset, out var descriptorOpcode, out var originalSender,
                    out var route, out var locationTargeted, out var location))
                return false;
            if (!TryReadBytes(span, ref offset, out var payload) || offset != span.Length)
                return false;

            packet = new(
                descriptorOpcode,
                originalSender,
                route,
                locationTargeted,
                location,
                payload.ToArray());
            return true;
        }

        public static byte[] WriteActionPacket(
            ulong descriptorOpcode,
            ulong ownerNetId,
            uint hookActionId,
            byte gameActionType,
            RunLocation location,
            ReadOnlySpan<byte> payload)
        {
            var locationBytes = WriteLocation(location);
            var buffer = new byte[
                ActionPacketFixedSize +
                locationBytes.Length +
                payload.Length];
            var span = buffer.AsSpan();
            var offset = InitialOffset;
            span[offset++] = Version;
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, DescriptorOpcodeSize), descriptorOpcode);
            offset += DescriptorOpcodeSize;
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, NetIdSize), ownerNetId);
            offset += NetIdSize;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, HookActionIdSize), hookActionId);
            offset += HookActionIdSize;
            span[offset++] = gameActionType;
            WriteBytes(span, ref offset, locationBytes);
            WriteBytes(span, ref offset, payload);
            return buffer;
        }

        public static bool TryReadActionPacket(
            ReadOnlySpan<byte> span,
            out RitsuLibSidecarSyncActionPacket packet)
        {
            packet = default;
            var offset = InitialOffset;
            if (span.Length < ActionPacketFixedSize || span[offset++] != Version)
                return false;

            var descriptorOpcode = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset, DescriptorOpcodeSize));
            offset += DescriptorOpcodeSize;
            var ownerNetId = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset, NetIdSize));
            offset += NetIdSize;
            var hookActionId = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, HookActionIdSize));
            offset += HookActionIdSize;
            var gameActionType = span[offset++];
            if (!TryReadLocation(span, ref offset, out var location))
                return false;
            if (!TryReadBytes(span, ref offset, out var payload) || offset != span.Length)
                return false;

            packet = new(
                descriptorOpcode,
                ownerNetId,
                hookActionId,
                gameActionType,
                location,
                payload.ToArray());
            return true;
        }

        public static bool TryBufferIncoming(INetGameService netService, in RitsuLibSidecarDispatchContext context)
        {
            if (!IsSyncOpcode(context.Opcode) || !TryGetMessageBus(netService, out var bus) || !NetBusIsBuffering(bus))
                return false;

            var bufferedContext = context.WithOwnedEnvelopeMemory();
            if (netService is NetHostGameService host &&
                TryRelayHostBroadcastBeforeBuffer(host, in bufferedContext, out var localOnlyContext))
                bufferedContext = localOnlyContext;

            lock (Gate)
            {
                if (!WaitingForNetBus.TryGetValue(bus, out var waiting))
                {
                    waiting = [];
                    WaitingForNetBus[bus] = waiting;
                }

                waiting.Add(new(NetBusBufferedMessages(bus).Count, bufferedContext));
            }

            return true;
        }

        public static bool ReleaseNetBusBuffer(NetMessageBus bus, bool bufferMessages)
        {
            if (bufferMessages || !NetBusIsBuffering(bus))
                return true;

            List<BufferedSyncContext>? sidecar;
            lock (Gate)
            {
                if (!WaitingForNetBus.Remove(bus, out sidecar))
                    return true;
            }

            var vanilla = NetBusBufferedMessages(bus);
            var vanillaMessages = vanilla.ToArray();
            vanilla.Clear();
            NetBusIsBuffering(bus) = false;

            var sidecarIndex = InitialIndex;
            sidecar.Sort((a, b) => a.VanillaCountBefore.CompareTo(b.VanillaCountBefore));
            for (var vanillaIndex = InitialIndex; vanillaIndex < vanillaMessages.Length; vanillaIndex++)
            {
                while (sidecarIndex < sidecar.Count &&
                       sidecar[sidecarIndex].VanillaCountBefore <= vanillaIndex)
                {
                    DispatchReleased(sidecar[sidecarIndex].Context);
                    sidecarIndex++;
                }

                var (message, senderId) = vanillaMessages[vanillaIndex];
                bus.SendMessageToAllHandlers(message, senderId);
            }

            while (sidecarIndex < sidecar.Count)
            {
                DispatchReleased(sidecar[sidecarIndex].Context);
                sidecarIndex++;
            }

            return false;
        }

        public static bool TryDeferForLocation(
            bool locationTargeted,
            RunLocation location,
            RitsuLibSidecarDispatchContext context)
        {
            lock (Gate)
            {
                if (!locationTargeted)
                    return false;
                if (VisitedLocations.Contains(location))
                    return false;
                if (RunManager.Instance?.RunLocationTargetedBuffer?.CurrentLocation == location)
                {
                    VisitedLocations.Add(location);
                    return false;
                }

                WaitingForLocation.Add(new(location, GetLocationWaitingCount(), context.WithOwnedEnvelopeMemory()));
                return true;
            }
        }

        public static bool ReleaseLocationBuffer(RunLocationTargetedMessageBuffer buffer, RunLocation location)
        {
            if (!TryGetLocationBufferState(
                    buffer,
                    out var vanillaWaiting,
                    out var visitedLocations,
                    out var currentLocationField,
                    out var callHandlers))
                return true;

            List<LocationBufferedSyncContext> sidecar;
            lock (Gate)
            {
                VisitedLocations.Add(location);
                if (!HasReleasableLocationSidecar(location))
                    return true;

                sidecar = [..WaitingForLocation];
                WaitingForLocation.Clear();
            }

            currentLocationField.SetValue(buffer, location);
            visitedLocations.Add(location);
            sidecar.Sort((a, b) => a.VanillaCountBefore.CompareTo(b.VanillaCountBefore));

            var sidecarIndex = InitialIndex;
            var releasedVanilla = InitialIndex;
            for (var i = InitialIndex; i < vanillaWaiting.Count; i++)
            {
                var blocked = vanillaWaiting[i];
                if (blocked == null || !TryReadBlockedLocationMessage(blocked, out var blockedMessage))
                    continue;

                if (!visitedLocations.Contains(blockedMessage.Location))
                    continue;

                while (sidecarIndex < sidecar.Count &&
                       sidecar[sidecarIndex].VanillaCountBefore <= releasedVanilla)
                {
                    ReleaseLocationSidecar(sidecar[sidecarIndex], location);
                    sidecarIndex++;
                }

                InvokeLocationHandlers(
                    callHandlers,
                    buffer,
                    blockedMessage.MessageType,
                    blockedMessage.Message,
                    blockedMessage.SenderId);
                vanillaWaiting.RemoveAt(i);
                i--;
                releasedVanilla++;
            }

            while (sidecarIndex < sidecar.Count)
            {
                ReleaseLocationSidecar(sidecar[sidecarIndex], location);
                sidecarIndex++;
            }

            return false;
        }

        public static void Clear()
        {
            lock (Gate)
            {
                WaitingForNetBus.Clear();
                WaitingForLocation.Clear();
                VisitedLocations.Clear();
            }
        }

        private static byte[] CreateEnvelope(ulong opcode, ReadOnlySpan<byte> payload)
        {
            return RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                RitsuLibSidecarDeliverySemantics.StableSync);
        }

        private static void DispatchReleased(RitsuLibSidecarDispatchContext context)
        {
            switch (context.Opcode)
            {
                case MessageOpcode:
                    RitsuLibSidecarSyncMessages.HandleBuffered(in context);
                    break;
                case ActionRequestOpcode:
                    RitsuLibSidecarSyncActions.HandleBufferedRequest(in context);
                    break;
            }
        }

        private static bool IsSyncOpcode(ulong opcode)
        {
            return opcode is MessageOpcode or ActionRequestOpcode or ActionAnnouncementOpcode;
        }

        private static bool TryRelayHostBroadcastBeforeBuffer(
            NetHostGameService host,
            in RitsuLibSidecarDispatchContext context,
            out RitsuLibSidecarDispatchContext localOnlyContext)
        {
            localOnlyContext = context;
            if (context.Opcode != MessageOpcode ||
                !TryReadMessagePacket(context.Payload.Span, out var packet) ||
                packet.Route != RitsuLibSidecarSyncMessageRoute.ClientToHostAndBroadcast)
                return false;

            if (!CanSendToAllReadyPeers(host, context.SenderNetId))
                return false;

            if (!TryBroadcastToReadyPeers(host, MessageOpcode, context.Payload.Span, context.SenderNetId))
                return false;

            var localPayload = WriteMessagePacket(
                packet.DescriptorOpcode,
                packet.OriginalSenderNetId,
                RitsuLibSidecarSyncMessageRoute.Direct,
                packet.LocationTargeted,
                packet.Location,
                packet.Payload);
            var localEnvelope = new RitsuLibSidecarEnvelope.ParsedEnvelope(
                context.Envelope.WireFormatVersion,
                context.Envelope.Flags,
                context.Envelope.Opcode,
                context.Envelope.HeaderExtension,
                localPayload);
            localOnlyContext = new(
                context.SenderNetId,
                context.TransferMode,
                context.Channel,
                context.IsHostIngest,
                localEnvelope);
            return true;
        }

        private static bool CanSendToAllReadyPeers(NetHostGameService host, ulong? excludePeerId)
        {
            return host.ConnectedPeers.Where(peer => peer.readyForBroadcasting && peer.peerId != excludePeerId)
                .All(peer => RitsuLibSidecarSessionManager.CanSendToPeer(peer.peerId));
        }

        private static bool TryGetMessageBus(INetGameService netService, out NetMessageBus bus)
        {
            switch (netService)
            {
                case NetHostGameService host:
                    bus = HostMessageBus(host);
                    return true;
                case NetClientGameService client:
                    bus = ClientMessageBus(client);
                    return true;
                default:
                    bus = null!;
                    return false;
            }
        }

        private static int GetLocationWaitingCount()
        {
            return RunManager.Instance?.RunLocationTargetedBuffer is { } buffer &&
                   LocationWaitingMessagesField?.GetValue(buffer) is ICollection collection
                ? collection.Count
                : NoVanillaMessagesWaiting;
        }

        private static bool TryGetLocationBufferState(
            RunLocationTargetedMessageBuffer buffer,
            out IList waitingMessages,
            out HashSet<RunLocation> visitedLocations,
            out FieldInfo currentLocationField,
            out MethodInfo callHandlers)
        {
            waitingMessages = null!;
            visitedLocations = null!;
            currentLocationField = null!;
            callHandlers = null!;
            if (LocationWaitingMessagesField?.GetValue(buffer) is not IList waiting ||
                LocationVisitedLocationsField?.GetValue(buffer) is not HashSet<RunLocation> visited ||
                LocationCurrentLocationField == null ||
                LocationCallHandlersOfTypeMethod == null)
                return false;

            waitingMessages = waiting;
            visitedLocations = visited;
            currentLocationField = LocationCurrentLocationField;
            callHandlers = LocationCallHandlersOfTypeMethod;
            return true;
        }

        private static void ReleaseLocationSidecar(LocationBufferedSyncContext pending, RunLocation releasedLocation)
        {
            if (pending.Location != releasedLocation && !VisitedLocations.Contains(pending.Location))
            {
                lock (Gate)
                {
                    WaitingForLocation.Add(pending);
                }

                return;
            }

            DispatchReleased(pending.Context);
        }

        private static bool HasReleasableLocationSidecar(RunLocation releasedLocation)
        {
            for (var i = InitialIndex; i < WaitingForLocation.Count; i++)
                if (WaitingForLocation[i].Location == releasedLocation ||
                    VisitedLocations.Contains(WaitingForLocation[i].Location))
                    return true;

            return false;
        }

        private static void InvokeLocationHandlers(
            MethodInfo callHandlers,
            RunLocationTargetedMessageBuffer buffer,
            Type messageType,
            INetMessage message,
            ulong senderId)
        {
            try
            {
                callHandlers.Invoke(buffer, [messageType, message, senderId]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        private static bool TryReadBlockedLocationMessage(object blocked, out BlockedLocationMessage message)
        {
            message = default;
            var type = blocked.GetType();
            var locationField = AccessTools.Field(type, "location");
            var messageField = AccessTools.Field(type, "message");
            var senderIdField = AccessTools.Field(type, "senderId");
            var messageTypeField = AccessTools.Field(type, "messageType");
            if (locationField == null || messageField == null || senderIdField == null || messageTypeField == null)
                return false;

            if (locationField.GetValue(blocked) is not RunLocation location ||
                messageField.GetValue(blocked) is not INetMessage netMessage ||
                senderIdField.GetValue(blocked) is not ulong senderId ||
                messageTypeField.GetValue(blocked) is not Type messageType)
                return false;

            message = new(location, netMessage, senderId, messageType);
            return true;
        }

        private static bool TryGetLocation(RitsuLibSidecarDispatchContext context, out RunLocation location)
        {
            location = default;
            switch (context.Opcode)
            {
                case MessageOpcode when
                    TryReadMessagePacket(context.Payload.Span, out var message) &&
                    message.LocationTargeted:
                    location = message.Location;
                    return true;
                case ActionRequestOpcode when
                    TryReadActionPacket(context.Payload.Span, out var action):
                    location = action.Location;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryReadMessageHeader(
            ReadOnlySpan<byte> span,
            ref int offset,
            out ulong descriptorOpcode,
            out ulong originalSenderNetId,
            out RitsuLibSidecarSyncMessageRoute route,
            out bool locationTargeted,
            out RunLocation location)
        {
            descriptorOpcode = DefaultDescriptorOpcode;
            originalSenderNetId = DefaultNetId;
            route = RitsuLibSidecarSyncMessageRoute.Direct;
            locationTargeted = false;
            location = default;
            if (span.Length < MessagePacketFixedSize || span[offset++] != Version)
                return false;

            descriptorOpcode = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset, DescriptorOpcodeSize));
            offset += DescriptorOpcodeSize;
            originalSenderNetId = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset, NetIdSize));
            offset += NetIdSize;
            route = (RitsuLibSidecarSyncMessageRoute)span[offset++];
            locationTargeted = span[offset++] != FalseByte;
            if (!locationTargeted)
                return TryReadBytes(span, ref offset, out var skipped) && skipped.Length == EmptyLength;

            return TryReadLocation(span, ref offset, out location);
        }

        private static byte[] WriteLocation(RunLocation location)
        {
            var writer = new PacketWriter { WarnOnGrow = false };
            writer.Write(location);
            writer.ZeroByteRemainder();
            return writer.Buffer.AsSpan(InitialOffset, writer.BytePosition).ToArray();
        }

        private static bool TryReadLocation(ReadOnlySpan<byte> span, ref int offset, out RunLocation location)
        {
            location = default;
            if (!TryReadBytes(span, ref offset, out var locationBytes) || locationBytes.Length == EmptyLength)
                return false;

            try
            {
                var reader = new PacketReader();
                reader.Reset(locationBytes.ToArray());
                location = reader.Read<RunLocation>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteBytes(Span<byte> span, ref int offset, ReadOnlySpan<byte> bytes)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, LengthPrefixSize), bytes.Length);
            offset += LengthPrefixSize;
            bytes.CopyTo(span[offset..]);
            offset += bytes.Length;
        }

        private static bool TryReadBytes(ReadOnlySpan<byte> span, ref int offset, out ReadOnlySpan<byte> bytes)
        {
            bytes = default;
            if (span.Length - offset < LengthPrefixSize)
                return false;

            var length = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, LengthPrefixSize));
            offset += LengthPrefixSize;
            if (length < EmptyLength || span.Length - offset < length)
                return false;

            bytes = span.Slice(offset, length);
            offset += length;
            return true;
        }

        private readonly record struct BufferedSyncContext(
            int VanillaCountBefore,
            RitsuLibSidecarDispatchContext Context);

        private readonly record struct LocationBufferedSyncContext(
            RunLocation Location,
            int VanillaCountBefore,
            RitsuLibSidecarDispatchContext Context);

        private readonly record struct BlockedLocationMessage(
            RunLocation Location,
            INetMessage Message,
            ulong SenderId,
            Type MessageType);
    }

    internal readonly record struct RitsuLibSidecarSyncMessagePacket(
        ulong DescriptorOpcode,
        ulong OriginalSenderNetId,
        RitsuLibSidecarSyncMessageRoute Route,
        bool LocationTargeted,
        RunLocation Location,
        byte[] Payload);

    internal readonly record struct RitsuLibSidecarSyncActionPacket(
        ulong DescriptorOpcode,
        ulong OwnerNetId,
        uint HookActionId,
        byte GameActionType,
        RunLocation Location,
        byte[] Payload);
}
