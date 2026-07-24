using System.Buffers.Binary;
using System.Collections;
using System.Linq.Expressions;
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
        private const int LengthPrefixSize = RitsuLibSidecarBinaryLayout.U32Size;
        private const int DescriptorOpcodeSize = RitsuLibSidecarBinaryLayout.U64Size;
        private const int NetIdSize = RitsuLibSidecarBinaryLayout.U64Size;

        private const int MessagePacketFixedSize =
            VersionSize +
            DescriptorOpcodeSize +
            NetIdSize +
            RouteSize +
            BooleanSize +
            LengthPrefixSize +
            LengthPrefixSize;

        private static readonly Lock Gate = new();
        private static readonly Dictionary<NetMessageBus, List<BufferedSyncContext>> WaitingForNetBus = [];
        private static readonly List<LocationBufferedSyncContext> WaitingForLocation = [];
        private static readonly HashSet<RunLocation> VisitedLocations = [];

        private static readonly Lock BlockedLocationAccessorGate = new();
        private static readonly Dictionary<Type, BlockedLocationAccessors?> BlockedLocationAccessorCache = [];

        private static readonly AccessTools.FieldRef<NetHostGameService, NetMessageBus>? HostMessageBusRef =
            TryCreateFieldRef<NetHostGameService, NetMessageBus>("_messageBus");

        private static readonly AccessTools.FieldRef<NetClientGameService, NetMessageBus>? ClientMessageBusRef =
            TryCreateFieldRef<NetClientGameService, NetMessageBus>("_messageBus");

        private static readonly AccessTools.FieldRef<NetMessageBus, bool>? NetBusIsBufferingRef =
            TryCreateFieldRef<NetMessageBus, bool>("_isBufferingMessages");

        private static readonly AccessTools.FieldRef<NetMessageBus, List<(INetMessage, ulong)>>?
            NetBusBufferedMessagesRef =
                TryCreateFieldRef<NetMessageBus, List<(INetMessage, ulong)>>("_bufferedMessages");

        private static readonly Func<RunLocationTargetedMessageBuffer, IList>? LocationWaitingMessages =
            TryCreateFieldGetter<RunLocationTargetedMessageBuffer, IList>("_messagesWaitingOnLocationChange");

        private static readonly AccessTools.FieldRef<RunLocationTargetedMessageBuffer, HashSet<RunLocation>>?
            LocationVisitedLocationsRef =
                TryCreateFieldRef<RunLocationTargetedMessageBuffer, HashSet<RunLocation>>("_visitedLocations");

        private static readonly AccessTools.FieldRef<RunLocationTargetedMessageBuffer, RunLocation>?
            LocationCurrentLocationRef =
                TryCreateFieldRef<RunLocationTargetedMessageBuffer, RunLocation>("<CurrentLocation>k__BackingField");

        private static readonly LocationCallHandlersDelegate? LocationCallHandlers =
            TryCreateLocationCallHandlersDelegate();

        public static bool TrySendToHost(
            NetClientGameService client,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            NetTransferMode mode,
            int channel)
        {
            var compression = RitsuLibSidecarPayloadCompressionSelector.ForPeer(opcode, payload, client.HostNetId);
            return RitsuLibSidecarSend.TrySendToHost(
                client,
                CreateEnvelope(opcode, payload, mode, compression),
                mode,
                channel);
        }

        public static bool TrySendToPeer(
            NetHostGameService host,
            ulong peerNetId,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            NetTransferMode mode,
            int channel)
        {
            var compression = RitsuLibSidecarPayloadCompressionSelector.ForPeer(opcode, payload, peerNetId);
            return RitsuLibSidecarSend.TrySendToPeer(
                host,
                peerNetId,
                CreateEnvelope(opcode, payload, mode, compression),
                mode,
                channel);
        }

        public static bool TryBroadcastToPeers(
            NetHostGameService host,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            ulong? excludePeerId,
            RitsuLibSidecarSyncBroadcastScope scope,
            NetTransferMode mode,
            int channel,
            RitsuLibSidecarSyncFailurePolicy failurePolicy)
        {
            if (failurePolicy == RitsuLibSidecarSyncFailurePolicy.Required &&
                !RitsuLibSidecarSyncMessages.CanSendToAllTargetPeers(host, excludePeerId, scope))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarSync] Required broadcast opcode {opcode} cannot reach every target peer; send suppressed.");
                return false;
            }

            var targetPeers = RitsuLibSidecarSyncMessages.TargetPeers(host, excludePeerId, scope).ToArray();
            var compression = RitsuLibSidecarPayloadCompressionSelector.ForPeers(
                opcode,
                payload,
                targetPeers.Select(peer => peer.peerId));
            var envelope = CreateEnvelope(opcode, payload, mode, compression);

            return (from peer in targetPeers
                where failurePolicy != RitsuLibSidecarSyncFailurePolicy.BestEffort ||
                      RitsuLibSidecarSessionManager.CanSendToPeer(peer.peerId)
                select RitsuLibSidecarSend.TrySendToPeer(host, peer.peerId, envelope, mode, channel)).Aggregate(true,
                (current, sent) => current & (sent || failurePolicy == RitsuLibSidecarSyncFailurePolicy.BestEffort));
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
                [.. payload]);
            return true;
        }

        public static bool TryBufferIncoming(INetGameService netService, in RitsuLibSidecarDispatchContext context)
        {
            if (!IsSyncOpcode(context.Opcode) ||
                !TryGetMessageBus(netService, out var bus) ||
                !TryGetNetBusBufferState(bus, out var isBuffering, out var vanillaMessages) ||
                !isBuffering ||
                !RitsuLibSidecarSyncMessages.ShouldBufferIncoming(context.Payload.Span))
                return false;

            var bufferedContext = context.WithOwnedEnvelopeMemory();
            if (netService is NetHostGameService host &&
                TryRelayHostBroadcastBeforeBuffer(
                    host,
                    in bufferedContext,
                    out var localOnlyContext,
                    out var suppressAfterRequiredFailure))
            {
                if (suppressAfterRequiredFailure)
                    return true;

                bufferedContext = localOnlyContext;
            }

            lock (Gate)
            {
                if (!WaitingForNetBus.TryGetValue(bus, out var waiting))
                {
                    waiting = [];
                    WaitingForNetBus[bus] = waiting;
                }

                if (!CanBufferMore_NoLock(context.Payload.Length))
                {
                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        "sync-netbus-buffer-budget",
                        "[SidecarSync] Buffered sync message suppressed by buffer budget.");
                    return true;
                }

                waiting.Add(new(vanillaMessages.Count, bufferedContext));
            }

            return true;
        }

        public static bool ReleaseNetBusBuffer(NetMessageBus bus, bool bufferMessages)
        {
            if (!TryGetNetBusBufferState(bus, out var isBuffering, out var vanilla) ||
                bufferMessages ||
                !isBuffering)
                return true;

            List<BufferedSyncContext>? sidecar;
            lock (Gate)
            {
                if (!WaitingForNetBus.Remove(bus, out sidecar))
                    return true;
            }

            var vanillaMessages = vanilla.ToArray();
            vanilla.Clear();
            SetNetBusBuffering(bus, false);

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
                if (!CanAlignLocationBuffer())
                    return false;
                if (VisitedLocations.Contains(location))
                    return false;
                if (RunManager.Instance?.RunLocationTargetedBuffer?.CurrentLocation == location)
                {
                    VisitedLocations.Add(location);
                    return false;
                }

                if (!CanBufferMore_NoLock(context.Payload.Length))
                {
                    RitsuLibSidecarRepeatedWarningLog.Warn(
                        "sync-location-buffer-budget",
                        "[SidecarSync] Location-targeted sync message suppressed by buffer budget.");
                    return true;
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
                    out var callHandlers))
                return true;

            List<LocationBufferedSyncContext> sidecar;
            lock (Gate)
            {
                VisitedLocations.Add(location);
                if (!HasReleasableLocationSidecar(location))
                    return true;

                sidecar = [.. WaitingForLocation];
                WaitingForLocation.Clear();
            }

            LocationCurrentLocationRef!(buffer) = location;
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

        private static byte[] CreateEnvelope(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            NetTransferMode mode,
            RitsuLibSidecarPayloadCompression compression)
        {
            return RitsuLibSidecar.CreateEnvelopeWithDeliveryCompressed(
                opcode,
                payload,
                DeliveryFor(mode),
                compression);
        }

        private static RitsuLibSidecarDeliverySemantics DeliveryFor(NetTransferMode mode)
        {
            return mode == NetTransferMode.Unreliable
                ? RitsuLibSidecarDeliverySemantics.BestEffort
                : RitsuLibSidecarDeliverySemantics.StableSync;
        }

        private static void DispatchReleased(RitsuLibSidecarDispatchContext context)
        {
            if (context.Opcode == MessageOpcode)
                RitsuLibSidecarSyncMessages.HandleBuffered(in context);
        }

        private static bool IsSyncOpcode(ulong opcode)
        {
            return opcode == MessageOpcode;
        }

        private static bool TryRelayHostBroadcastBeforeBuffer(
            NetHostGameService host,
            in RitsuLibSidecarDispatchContext context,
            out RitsuLibSidecarDispatchContext localOnlyContext,
            out bool suppressAfterRequiredFailure)
        {
            localOnlyContext = context;
            suppressAfterRequiredFailure = false;
            if (context.Opcode != MessageOpcode ||
                !TryReadMessagePacket(context.Payload.Span, out var packet))
                return false;

            if (!RitsuLibSidecarSyncMessages.TryGetRelayPolicy(
                    context.Payload.Span,
                    out var shouldRelay,
                    out var scope,
                    out var failurePolicy))
                return false;
            if (!shouldRelay)
                return false;

            if (failurePolicy == RitsuLibSidecarSyncFailurePolicy.Required &&
                !RitsuLibSidecarSyncMessages.CanSendToAllTargetPeers(host, context.SenderNetId, scope))
            {
                RitsuLibFramework.Logger.ErrorNoTrace(
                    $"[SidecarSync] Required relay opcode {packet.DescriptorOpcode} cannot reach every target peer; local handler suppressed.");
                suppressAfterRequiredFailure = true;
                return true;
            }

            if (!TryBroadcastToPeers(
                    host,
                    MessageOpcode,
                    context.Payload.Span,
                    context.SenderNetId,
                    scope,
                    context.TransferMode,
                    context.Channel,
                    failurePolicy))
            {
                if (failurePolicy == RitsuLibSidecarSyncFailurePolicy.Required)
                    RitsuLibFramework.Logger.ErrorNoTrace(
                        $"[SidecarSync] Required relay opcode {packet.DescriptorOpcode} send failed; local handler suppressed.");
                suppressAfterRequiredFailure = failurePolicy == RitsuLibSidecarSyncFailurePolicy.Required;
                return suppressAfterRequiredFailure;
            }

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

        private static bool TryGetMessageBus(INetGameService netService, out NetMessageBus bus)
        {
            switch (netService)
            {
                case NetHostGameService host when HostMessageBusRef != null:
                    bus = HostMessageBusRef(host);
                    return bus != null;
                case NetClientGameService client when ClientMessageBusRef != null:
                    bus = ClientMessageBusRef(client);
                    return bus != null;
                default:
                    bus = null!;
                    return false;
            }
        }

        private static bool TryGetNetBusBufferState(
            NetMessageBus bus,
            out bool isBuffering,
            out List<(INetMessage, ulong)> bufferedMessages)
        {
            isBuffering = false;
            bufferedMessages = null!;
            if (NetBusIsBufferingRef == null ||
                NetBusBufferedMessagesRef == null)
                return false;

            isBuffering = NetBusIsBufferingRef(bus);
            bufferedMessages = NetBusBufferedMessagesRef(bus);
            return true;
        }

        private static void SetNetBusBuffering(NetMessageBus bus, bool isBuffering)
        {
            if (NetBusIsBufferingRef == null)
                return;

            NetBusIsBufferingRef(bus) = isBuffering;
        }

        private static int GetLocationWaitingCount()
        {
            return RunManager.Instance?.RunLocationTargetedBuffer is { } buffer &&
                   LocationWaitingMessages?.Invoke(buffer) is ICollection collection
                ? collection.Count
                : NoVanillaMessagesWaiting;
        }

        private static bool TryGetLocationBufferState(
            RunLocationTargetedMessageBuffer buffer,
            out IList waitingMessages,
            out HashSet<RunLocation> visitedLocations,
            out LocationCallHandlersDelegate callHandlers)
        {
            waitingMessages = null!;
            visitedLocations = null!;
            callHandlers = null!;
            if (LocationWaitingMessages == null ||
                LocationVisitedLocationsRef == null ||
                LocationCurrentLocationRef == null ||
                LocationCallHandlers == null)
                return false;

            waitingMessages = LocationWaitingMessages(buffer);
            visitedLocations = LocationVisitedLocationsRef(buffer);
            callHandlers = LocationCallHandlers;
            return true;
        }

        private static bool CanAlignLocationBuffer()
        {
            return LocationWaitingMessages != null &&
                   LocationVisitedLocationsRef != null &&
                   LocationCurrentLocationRef != null &&
                   LocationCallHandlers != null;
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
            LocationCallHandlersDelegate callHandlers,
            RunLocationTargetedMessageBuffer buffer,
            Type messageType,
            INetMessage message,
            ulong senderId)
        {
            callHandlers(buffer, messageType, message, senderId);
        }

        private static bool TryReadBlockedLocationMessage(object blocked, out BlockedLocationMessage message)
        {
            message = default;
            if (!TryGetBlockedLocationAccessors(blocked.GetType(), out var accessors))
                return false;

            message = new(
                accessors.Location(blocked),
                accessors.Message(blocked),
                accessors.SenderId(blocked),
                accessors.MessageType(blocked));
            return true;
        }

        private static bool TryGetBlockedLocationAccessors(
            Type blockedType,
            out BlockedLocationAccessors accessors)
        {
            lock (BlockedLocationAccessorGate)
            {
                if (!BlockedLocationAccessorCache.TryGetValue(blockedType, out var cached))
                {
                    cached = CreateBlockedLocationAccessors(blockedType);
                    BlockedLocationAccessorCache[blockedType] = cached;
                }

                accessors = cached!;
                return cached != null;
            }
        }

        private static BlockedLocationAccessors? CreateBlockedLocationAccessors(Type blockedType)
        {
            var location = TryCreateObjectFieldGetter<RunLocation>(blockedType, "location");
            var message = TryCreateObjectFieldGetter<INetMessage>(blockedType, "message");
            var senderId = TryCreateObjectFieldGetter<ulong>(blockedType, "senderId");
            var messageType = TryCreateObjectFieldGetter<Type>(blockedType, "messageType");
            if (location == null || message == null || senderId == null || messageType == null)
                return null;

            return new(location, message, senderId, messageType);
        }

        private static AccessTools.FieldRef<TInstance, TField>? TryCreateFieldRef<TInstance, TField>(
            string fieldName)
            where TInstance : class
        {
            if (AccessTools.Field(typeof(TInstance), fieldName) == null)
                return null;

            try
            {
                return AccessTools.FieldRefAccess<TInstance, TField>(fieldName);
            }
            catch
            {
                return null;
            }
        }

        private static Func<TInstance, TField>? TryCreateFieldGetter<TInstance, TField>(string fieldName)
            where TInstance : class
        {
            var field = AccessTools.Field(typeof(TInstance), fieldName);
            if (field == null)
                return null;

            try
            {
                var instance = Expression.Parameter(typeof(TInstance), "instance");
                var fieldAccess = Expression.Field(instance, field);
                var body = Expression.Convert(fieldAccess, typeof(TField));
                return Expression.Lambda<Func<TInstance, TField>>(body, instance).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, TField>? TryCreateObjectFieldGetter<TField>(Type ownerType, string fieldName)
        {
            var field = AccessTools.Field(ownerType, fieldName);
            if (field == null)
                return null;

            try
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var typedInstance = ownerType.IsValueType
                    ? Expression.Unbox(instance, ownerType)
                    : Expression.Convert(instance, ownerType);
                var fieldAccess = Expression.Field(typedInstance, field);
                var body = Expression.Convert(fieldAccess, typeof(TField));
                return Expression.Lambda<Func<object, TField>>(body, instance).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static LocationCallHandlersDelegate? TryCreateLocationCallHandlersDelegate()
        {
            var method = AccessTools.Method(typeof(RunLocationTargetedMessageBuffer), "CallHandlersOfType");
            if (method == null)
                return null;

            try
            {
                return AccessTools.MethodDelegate<LocationCallHandlersDelegate>(method);
            }
            catch
            {
                return null;
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
            if (!Enum.IsDefined(route))
                return false;

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
            return [.. writer.Buffer.AsSpan(InitialOffset, writer.BytePosition)];
        }

        private static bool TryReadLocation(ReadOnlySpan<byte> span, ref int offset, out RunLocation location)
        {
            location = default;
            if (!TryReadBytes(span, ref offset, out var locationBytes) || locationBytes.Length == EmptyLength)
                return false;

            try
            {
                var reader = new PacketReader();
                reader.Reset([.. locationBytes]);
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

        private static bool CanBufferMore_NoLock(int nextPayloadBytes)
        {
            var contextCount = WaitingForLocation.Count + WaitingForNetBus.Values.Sum(static v => v.Count);
            if (contextCount >= RitsuLibSidecarResourcePolicy.MaxBufferedSyncContexts)
                return false;

            var bytes = WaitingForLocation.Sum(static c => c.Context.Payload.Length) +
                        WaitingForNetBus.Values.Sum(static v => v.Sum(static c => c.Context.Payload.Length));
            return bytes + nextPayloadBytes <= RitsuLibSidecarResourcePolicy.MaxBufferedSyncBytes;
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

        private delegate void LocationCallHandlersDelegate(
            RunLocationTargetedMessageBuffer buffer,
            Type messageType,
            INetMessage message,
            ulong senderId);

        private sealed record BlockedLocationAccessors(
            Func<object, RunLocation> Location,
            Func<object, INetMessage> Message,
            Func<object, ulong> SenderId,
            Func<object, Type> MessageType);
    }

    internal readonly record struct RitsuLibSidecarSyncMessagePacket(
        ulong DescriptorOpcode,
        ulong OriginalSenderNetId,
        RitsuLibSidecarSyncMessageRoute Route,
        bool LocationTargeted,
        RunLocation Location,
        byte[] Payload);
}
