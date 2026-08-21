using System.Buffers.Binary;
using System.Security.Cryptography;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarEndpointProtocol
    {
        private static readonly Lock Gate = new();
        private static readonly Lock RebuildGate = new();
        private static readonly Dictionary<ulong, PeerCatalogState> PeerCatalogs = [];
        private static readonly Dictionary<ulong, RitsuLibSidecarTokenBucket> CatalogRateLimits = [];
        private static readonly Dictionary<RitsuLibSidecarEndpointKey, HostRouteState> HostRoutesByKey = [];
        private static readonly Dictionary<uint, HostRouteState> HostRoutesById = [];

        private static readonly Dictionary<uint, RitsuLibSidecarEndpointRouteDefinition> ClientRoutesById = [];
        private static readonly Dictionary<ulong, uint> SnapshotRevisionPublishedByPeer = [];
        private static readonly Dictionary<ulong, uint> SnapshotRevisionAcknowledgedByPeer = [];
        private static readonly Dictionary<SenderRouteKey, RitsuLibSidecarTokenBucket> InboundRateLimits = [];
        private static readonly Dictionary<SenderRouteKey, uint> LastIngressSequence = [];
        private static readonly Dictionary<SenderRouteKey, uint> LastDeliverySequence = [];

        private static int _registered;
        private static uint _nextRouteId;
        private static uint _hostSnapshotRevision;
        private static uint _lastClientSnapshotRevision;

        internal static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _registered, 1) != 0)
                return;

            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.EndpointCatalog, OnEndpointCatalog);
            RitsuLibSidecarBus.RegisterHandler(
                RitsuLibSidecarControlOpcodes.EndpointRouteSnapshot,
                OnRouteSnapshot);
            RitsuLibSidecarBus.RegisterHandler(
                RitsuLibSidecarControlOpcodes.EndpointRouteSnapshotAck,
                OnRouteSnapshotAck);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.EndpointIngress, OnEndpointIngress);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.EndpointDelivery, OnEndpointDelivery);
            RitsuLibSidecarSessionManager.SessionBound += OnSessionBound;
            RitsuLibSidecarSessionManager.SessionUnbound += OnSessionUnbound;
            RitsuLibSidecarSessionManager.HandshakeCompleted += OnHandshakeCompleted;
        }

        internal static void OnLocalCatalogChanged()
        {
            var netService = RitsuLibSidecarSessionManager.CurrentNetService;
            if (netService is NetHostGameService host)
            {
                RebuildHostRoutes(host);
                return;
            }

            if (netService is not NetClientGameService)
                return;
            if (!RitsuLibSidecarGodotMainLoopScheduling.TryPostToMainLoop(() =>
                    PublishLocalCatalog(RitsuLibSidecarSessionManager.CurrentNetService)))
                PublishLocalCatalog(netService);
        }

        internal static void NotePeerDisconnected(ulong peerNetId)
        {
            bool rebuild;
            lock (Gate)
            {
                rebuild = PeerCatalogs.Remove(peerNetId);
                CatalogRateLimits.Remove(peerNetId);
                SnapshotRevisionPublishedByPeer.Remove(peerNetId);
                SnapshotRevisionAcknowledgedByPeer.Remove(peerNetId);
                RemoveSenderState(peerNetId);
            }

            RitsuLibSidecarOutboundScheduler.RemovePeer(peerNetId);
            if (rebuild && RitsuLibSidecarSessionManager.CurrentNetService is NetHostGameService host)
                RebuildHostRoutes(host);
        }

        internal static RitsuLibSidecarSendResult Send(
            RitsuLibSidecarEndpointRegistration registration,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId,
            ReadOnlySpan<byte> payload)
        {
            if (registration.IsDisposed)
                return new(RitsuLibSidecarSendStatus.EndpointDisposed, 0);
            var netService = RitsuLibSidecarSessionManager.CurrentNetService;
            if (netService == null || netService.Type == NetGameType.Singleplayer)
                return new(RitsuLibSidecarSendStatus.NoSession, 0);
            if (!RitsuLibSidecarEndpointTransport.SupportsProfile(
                    netService,
                    registration.Descriptor.DeliveryProfile))
                return new(RitsuLibSidecarSendStatus.ProfileUnsupported, 0);
            if (registration.GetRoute() is not { } route)
                return new(RitsuLibSidecarSendStatus.RouteUnavailable, 0);
            if (payload.Length > route.MaxPayloadBytes)
                return new(RitsuLibSidecarSendStatus.PayloadTooLarge, 0);
            if (!registration.TryConsumeOutboundRate(payload.Length))
                return new(RitsuLibSidecarSendStatus.RateLimited, 0);

            return netService switch
            {
                NetClientGameService client => SendFromClient(
                    client,
                    registration,
                    route,
                    destination,
                    targetNetId,
                    payload),
                NetHostGameService host => SendFromHost(
                    host,
                    registration,
                    route,
                    destination,
                    targetNetId,
                    payload),
                _ => new(RitsuLibSidecarSendStatus.NoSession, 0),
            };
        }

        private static void OnSessionBound(SidecarSessionBoundEvent evt)
        {
            ResetSessionState();
            if (evt.NetService is NetHostGameService host)
                RebuildHostRoutes(host);
        }

        private static void OnSessionUnbound(SidecarSessionUnboundEvent _)
        {
            ResetSessionState();
        }

        private static void OnHandshakeCompleted(SidecarHandshakeCompletedEvent evt)
        {
            if (evt.Epoch != RitsuLibSidecarSessionManager.Epoch)
                return;
            PublishLocalCatalog(RitsuLibSidecarSessionManager.CurrentNetService);
        }

        private static void PublishLocalCatalog(INetGameService? netService)
        {
            if (netService is not NetClientGameService client ||
                !RitsuLibSidecarSessionManager.CanSendToPeer(client.HostNetId))
                return;

            var supportedProfiles = RitsuLibSidecarEndpointTransport.GetSupportedProfiles(netService);
            var advertisements = RitsuLibSidecarEndpointRegistry.GetAdvertisementsSnapshot();
            var catalog = new RitsuLibSidecarEndpointCatalog(supportedProfiles, advertisements);
            var payload = RitsuLibSidecarEndpointBinary.WriteCatalog(catalog);
            RitsuLibSidecarHighLevelSend.TrySendAsClient(
                netService,
                RitsuLibSidecarControlOpcodes.EndpointCatalog,
                payload,
                RitsuLibSidecarDeliverySemantics.StableSync);
        }

        private static void OnEndpointCatalog(RitsuLibSidecarDispatchContext context)
        {
            if (!context.IsHostIngest ||
                context.TransferMode != NetTransferMode.Reliable ||
                RitsuLibSidecarSessionManager.CurrentNetService is not NetHostGameService host ||
                !RitsuLibSidecarSessionManager.CanSendToPeer(context.SenderNetId) ||
                !TryConsumeCatalogRate(context.SenderNetId, context.Payload.Length) ||
                !RitsuLibSidecarEndpointBinary.TryReadCatalog(context.Payload.Span, out var catalog) ||
                (catalog.SupportedProfiles & RitsuLibSidecarTransportProfileMask.Control) == 0)
            {
                return;
            }

            lock (Gate)
            {
                var epoch = RitsuLibSidecarSessionManager.Epoch;
                PeerCatalogs[context.SenderNetId] = new(epoch, catalog);
                RemoveSenderState(context.SenderNetId);
            }

            RebuildHostRoutes(host);
        }

        private static void OnRouteSnapshot(RitsuLibSidecarDispatchContext context)
        {
            if (context.IsHostIngest ||
                context.TransferMode != NetTransferMode.Reliable ||
                RitsuLibSidecarSessionManager.CurrentNetService is not NetClientGameService client ||
                context.SenderNetId != client.HostNetId ||
                !RitsuLibSidecarEndpointBinary.TryReadRouteSnapshot(context.Payload.Span, out var snapshot))
            {
                return;
            }

            Dictionary<RitsuLibSidecarEndpointKey, RitsuLibSidecarEndpointRouteDefinition> acceptedByKey = [];
            Dictionary<uint, RitsuLibSidecarEndpointRouteDefinition> acceptedById = [];
            foreach (var route in snapshot.Routes)
            {
                if (!route.ParticipantNetIds.Contains(client.NetId) ||
                    !RitsuLibSidecarEndpointRegistry.TryGet(route.Key, out var registration) ||
                    registration == null ||
                    !RouteMatchesDescriptor(route, registration.Descriptor))
                    continue;
                acceptedByKey.Add(route.Key, route);
                acceptedById.Add(route.RouteId, route);
            }

            lock (Gate)
            {
                if (!IsNewerOrEqual(snapshot.Revision, _lastClientSnapshotRevision))
                    return;
                ClientRoutesById.Clear();
                foreach (var pair in acceptedById)
                    ClientRoutesById.Add(pair.Key, pair.Value);
                _lastClientSnapshotRevision = snapshot.Revision;
                LastDeliverySequence.Clear();
            }

            ApplyLocalRoutes(acceptedByKey, client.NetId);
            var ack = RitsuLibSidecarEndpointBinary.WriteRouteSnapshotAck(snapshot.Revision);
            RitsuLibSidecarHighLevelSend.TrySendAsClient(
                client,
                RitsuLibSidecarControlOpcodes.EndpointRouteSnapshotAck,
                ack,
                RitsuLibSidecarDeliverySemantics.StableSync);
        }

        private static void OnRouteSnapshotAck(RitsuLibSidecarDispatchContext context)
        {
            if (!context.IsHostIngest ||
                context.TransferMode != NetTransferMode.Reliable ||
                RitsuLibSidecarSessionManager.CurrentNetService is not NetHostGameService ||
                !RitsuLibSidecarSessionManager.CanSendToPeer(context.SenderNetId) ||
                !RitsuLibSidecarEndpointBinary.TryReadRouteSnapshotAck(context.Payload.Span, out var revision))
                return;

            lock (Gate)
            {
                if (SnapshotRevisionPublishedByPeer.TryGetValue(context.SenderNetId, out var published) &&
                    published == revision)
                    SnapshotRevisionAcknowledgedByPeer[context.SenderNetId] = revision;
            }
        }

        private static void OnEndpointIngress(RitsuLibSidecarDispatchContext context)
        {
            if (!context.IsHostIngest ||
                RitsuLibSidecarSessionManager.CurrentNetService is not NetHostGameService host ||
                !RitsuLibSidecarEndpointBinary.TryReadIngressFrame(context.Payload, out var frame))
                return;

            HostRouteState? routeState;
            lock (Gate)
            {
                HostRoutesById.TryGetValue(frame.RouteId, out routeState);
            }

            if (routeState == null)
                return;
            var route = routeState.Definition;
            if (route.Nonce != frame.Nonce ||
                !route.ParticipantNetIds.Contains(context.SenderNetId) ||
                !RitsuLibSidecarEndpointTransport.MatchesReceivedProfile(
                    route.DeliveryProfile,
                    context.TransferMode) ||
                frame.Payload.Length > route.MaxPayloadBytes ||
                !ValidateDestination(route, context.SenderNetId, host.NetId, frame.Destination, frame.TargetNetId))
                return;

            if (route.DeliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram &&
                !TryAcceptSequence(LastIngressSequence, new(context.SenderNetId, route.RouteId), frame.Sequence))
                return;

            var remoteTargets = ResolveRemoteTargets(
                route,
                context.SenderNetId,
                host.NetId,
                frame.Destination,
                frame.TargetNetId);
            var fanoutBytes = checked(frame.Payload.Length * Math.Max(1, remoteTargets.Count));
            if (!TryConsumeInboundRate(
                    context.SenderNetId,
                    route.RouteId,
                    route.DeliveryProfile,
                    fanoutBytes))
                return;

            if (frame.Destination is RitsuLibSidecarEndpointDestination.Host or
                    RitsuLibSidecarEndpointDestination.Broadcast &&
                route.ParticipantNetIds.Contains(host.NetId))
                DispatchLocal(route, context.SenderNetId, frame.Payload.ToArray());

            QueueDeliveries(
                host,
                route,
                context.SenderNetId,
                frame.Sequence,
                remoteTargets,
                frame.Payload.Span);
        }

        private static void OnEndpointDelivery(RitsuLibSidecarDispatchContext context)
        {
            if (context.IsHostIngest ||
                RitsuLibSidecarSessionManager.CurrentNetService is not NetClientGameService client ||
                context.SenderNetId != client.HostNetId ||
                !RitsuLibSidecarEndpointBinary.TryReadDeliveryFrame(context.Payload, out var frame))
                return;

            RitsuLibSidecarEndpointRouteDefinition route;
            lock (Gate)
            {
                if (!ClientRoutesById.TryGetValue(frame.RouteId, out route))
                    return;
            }

            if (route.Nonce != frame.Nonce ||
                !route.ParticipantNetIds.Contains(client.NetId) ||
                !route.ParticipantNetIds.Contains(frame.OriginalSenderNetId) ||
                frame.OriginalSenderNetId == client.NetId ||
                frame.Payload.Length > route.MaxPayloadBytes ||
                !RitsuLibSidecarEndpointTransport.MatchesReceivedProfile(
                    route.DeliveryProfile,
                    context.TransferMode))
                return;

            if (route.DeliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram &&
                !TryAcceptSequence(
                    LastDeliverySequence,
                    new(frame.OriginalSenderNetId, route.RouteId),
                    frame.Sequence))
                return;

            DispatchLocal(route, frame.OriginalSenderNetId, frame.Payload.ToArray());
        }

        private static RitsuLibSidecarSendResult SendFromClient(
            NetClientGameService client,
            RitsuLibSidecarEndpointRegistration registration,
            RitsuLibSidecarEndpointRouteDefinition route,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId,
            ReadOnlySpan<byte> payload)
        {
            if (!IsOperationAllowed(route, client.NetId, client.HostNetId, destination))
                return new(RitsuLibSidecarSendStatus.InvalidOperation, 0);
            if (!route.ParticipantNetIds.Contains(client.NetId) ||
                !ValidateDestination(route, client.NetId, client.HostNetId, destination, targetNetId))
                return new(RitsuLibSidecarSendStatus.DestinationUnavailable, 0);

            var sequence = registration.NextSequence();
            var frame = RitsuLibSidecarEndpointBinary.WriteIngressFrame(
                route.RouteId,
                route.Nonce,
                sequence,
                destination,
                targetNetId,
                payload);
            var envelope = CreateDataEnvelope(
                RitsuLibSidecarControlOpcodes.EndpointIngress,
                frame,
                route.DeliveryProfile);
            var status = RitsuLibSidecarOutboundScheduler.TryEnqueue(
                client,
                RitsuLibSidecarSessionManager.Epoch,
                client.HostNetId,
                envelope,
                route.DeliveryProfile,
                registration.Descriptor.RealtimeLifetime,
                registration);
            return new(status, status == RitsuLibSidecarSendStatus.Accepted ? 1 : 0);
        }

        private static RitsuLibSidecarSendResult SendFromHost(
            NetHostGameService host,
            RitsuLibSidecarEndpointRegistration registration,
            RitsuLibSidecarEndpointRouteDefinition route,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId,
            ReadOnlySpan<byte> payload)
        {
            if (!route.ParticipantNetIds.Contains(host.NetId))
                return new(RitsuLibSidecarSendStatus.RouteUnavailable, 0);
            if (!IsOperationAllowed(route, host.NetId, host.NetId, destination))
                return new(RitsuLibSidecarSendStatus.InvalidOperation, 0);
            if (!ValidateDestination(route, host.NetId, host.NetId, destination, targetNetId))
                return new(RitsuLibSidecarSendStatus.DestinationUnavailable, 0);

            if (destination == RitsuLibSidecarEndpointDestination.Host)
            {
                registration.Dispatch(new(host.NetId, route.ProtocolVersion, payload.ToArray()));
                return new(RitsuLibSidecarSendStatus.Accepted, 0);
            }

            var targets = ResolveRemoteTargets(route, host.NetId, host.NetId, destination, targetNetId);
            var sequence = registration.NextSequence();
            return QueueDeliveries(host, route, host.NetId, sequence, targets, payload, registration);
        }

        private static RitsuLibSidecarSendResult QueueDeliveries(
            NetHostGameService host,
            RitsuLibSidecarEndpointRouteDefinition route,
            ulong originalSenderNetId,
            uint sequence,
            IReadOnlyList<ulong> targetNetIds,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarEndpointRegistration? owner = null)
        {
            if (targetNetIds.Count == 0)
                return new(RitsuLibSidecarSendStatus.Accepted, 0);

            var frame = RitsuLibSidecarEndpointBinary.WriteDeliveryFrame(
                route.RouteId,
                route.Nonce,
                sequence,
                originalSenderNetId,
                payload);
            var envelope = CreateDataEnvelope(
                RitsuLibSidecarControlOpcodes.EndpointDelivery,
                frame,
                route.DeliveryProfile);
            var accepted = 0;
            var failure = RitsuLibSidecarSendStatus.DestinationUnavailable;
            foreach (var targetNetId in targetNetIds)
            {
                if (!CanDeliverCurrentRouteToPeer(targetNetId))
                {
                    failure = RitsuLibSidecarSendStatus.RouteUnavailable;
                    continue;
                }

                var status = RitsuLibSidecarOutboundScheduler.TryEnqueue(
                    host,
                    RitsuLibSidecarSessionManager.Epoch,
                    targetNetId,
                    envelope,
                    route.DeliveryProfile,
                    RitsuLibSidecarEndpointPolicy.DefaultRealtimeLifetime,
                    owner);
                if (status == RitsuLibSidecarSendStatus.Accepted)
                    accepted++;
                else
                    failure = status;
            }

            return accepted > 0
                ? new(RitsuLibSidecarSendStatus.Accepted, accepted)
                : new(failure, 0);
        }

        private static void RebuildHostRoutes(NetHostGameService host)
        {
            lock (RebuildGate)
            {
                RebuildHostRoutesCore(host);
            }
        }

        private static void RebuildHostRoutesCore(NetHostGameService host)
        {
            var localProfiles = RitsuLibSidecarEndpointTransport.GetSupportedProfiles(host);
            var localAdvertisements = RitsuLibSidecarEndpointRegistry.GetAdvertisementsSnapshot();
            Dictionary<ulong, PeerCatalogState> peerCatalogs;
            Dictionary<RitsuLibSidecarEndpointKey, HostRouteState> previousRoutes;
            lock (Gate)
            {
                var epoch = RitsuLibSidecarSessionManager.Epoch;
                peerCatalogs = PeerCatalogs
                    .Where(pair => pair.Value.SessionEpoch == epoch)
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value);
                previousRoutes = HostRoutesByKey.ToDictionary(static pair => pair.Key, static pair => pair.Value);
            }

            var definitions = BuildRouteDefinitions(
                host.NetId,
                localProfiles,
                localAdvertisements,
                peerCatalogs,
                previousRoutes);
            uint revision;
            Dictionary<ulong, RitsuLibSidecarEndpointRouteDefinition[]> snapshots;
            lock (Gate)
            {
                HostRoutesByKey.Clear();
                HostRoutesById.Clear();
                foreach (var definition in definitions)
                {
                    var state = new HostRouteState(definition);
                    HostRoutesByKey.Add(definition.Key, state);
                    HostRoutesById.Add(definition.RouteId, state);
                }

                InboundRateLimits.Clear();
                LastIngressSequence.Clear();
                revision = NextNonzero(ref _hostSnapshotRevision);
                snapshots = peerCatalogs.Keys.ToDictionary(
                    static peerNetId => peerNetId,
                    peerNetId => definitions.Where(route => route.ParticipantNetIds.Contains(peerNetId)).ToArray());
                foreach (var peerNetId in peerCatalogs.Keys)
                {
                    SnapshotRevisionPublishedByPeer.Remove(peerNetId);
                    SnapshotRevisionAcknowledgedByPeer.Remove(peerNetId);
                }
            }

            ApplyLocalRoutes(
                definitions.ToDictionary(static route => route.Key),
                host.NetId);
            foreach (var pair in snapshots)
            {
                var payload = RitsuLibSidecarEndpointBinary.WriteRouteSnapshot(new(revision, pair.Value));
                var sent = RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    host,
                    pair.Key,
                    RitsuLibSidecarControlOpcodes.EndpointRouteSnapshot,
                    payload,
                    RitsuLibSidecarDeliverySemantics.StableSync);
                if (!sent)
                    continue;
                lock (Gate)
                {
                    SnapshotRevisionPublishedByPeer[pair.Key] = revision;
                }
            }
        }

        private static RitsuLibSidecarEndpointRouteDefinition[] BuildRouteDefinitions(
            ulong hostNetId,
            RitsuLibSidecarTransportProfileMask hostProfiles,
            IReadOnlyList<RitsuLibSidecarEndpointAdvertisement> localAdvertisements,
            IReadOnlyDictionary<ulong, PeerCatalogState> peerCatalogs,
            IReadOnlyDictionary<RitsuLibSidecarEndpointKey, HostRouteState> previousRoutes)
        {
            var participantsByKey =
                new Dictionary<RitsuLibSidecarEndpointKey, List<AdvertisedParticipant>>();
            foreach (var advertisement in localAdvertisements)
                AddParticipant(
                    advertisement,
                    new(hostNetId, advertisement, hostProfiles, true));
            foreach (var pair in peerCatalogs)
            foreach (var advertisement in pair.Value.GetEndpoints())
                AddParticipant(
                    advertisement,
                    new(pair.Key, advertisement, pair.Value.SupportedProfiles, false));

            var definitions = new List<RitsuLibSidecarEndpointRouteDefinition>();
            foreach (var pair in participantsByKey
                         .OrderBy(static pair => pair.Key.OwnerId, StringComparer.Ordinal)
                         .ThenBy(static pair => pair.Key.Name, StringComparer.Ordinal))
            {
                if (definitions.Count >= RitsuLibSidecarEndpointPolicy.MaxHostRoutes)
                    break;
                if (!TrySelectRoute(
                        pair.Key,
                        pair.Value,
                        hostNetId,
                        hostProfiles,
                        previousRoutes,
                        out var definition))
                    continue;
                definitions.Add(definition);
            }

            return [.. definitions];

            void AddParticipant(
                RitsuLibSidecarEndpointAdvertisement advertisement,
                AdvertisedParticipant participant)
            {
                if (!participantsByKey.TryGetValue(advertisement.Key, out var list))
                {
                    list = [];
                    participantsByKey.Add(advertisement.Key, list);
                }

                list.Add(participant);
            }
        }

        private static bool TrySelectRoute(
            RitsuLibSidecarEndpointKey key,
            IReadOnlyList<AdvertisedParticipant> allParticipants,
            ulong hostNetId,
            RitsuLibSidecarTransportProfileMask hostProfiles,
            IReadOnlyDictionary<RitsuLibSidecarEndpointKey, HostRouteState> previousRoutes,
            out RitsuLibSidecarEndpointRouteDefinition definition)
        {
            definition = default;
            var hostParticipant = allParticipants.FirstOrDefault(static participant => participant.IsHost);
            IEnumerable<IGrouping<RouteContract, AdvertisedParticipant>> groups;
            if (hostParticipant != default)
            {
                var contract = new RouteContract(
                    hostParticipant.Advertisement.DeliveryProfile,
                    hostParticipant.Advertisement.Topology);
                groups = allParticipants
                    .Where(participant =>
                        participant.Advertisement.DeliveryProfile == contract.DeliveryProfile &&
                        participant.Advertisement.Topology == contract.Topology)
                    .GroupBy(_ => contract);
            }
            else
            {
                groups = allParticipants
                    .Where(static participant =>
                        participant.Advertisement.Topology == RitsuLibSidecarEndpointTopology.RelayGroup)
                    .GroupBy(static participant => new RouteContract(
                        participant.Advertisement.DeliveryProfile,
                        participant.Advertisement.Topology));
            }

            RouteSelection? best = null;
            foreach (var group in groups)
            {
                if (!ProfileSupported(hostProfiles, group.Key.DeliveryProfile))
                    continue;
                var eligible = group
                    .Where(participant => ProfileSupported(
                        participant.SupportedProfiles,
                        group.Key.DeliveryProfile))
                    .ToArray();
                foreach (var version in eligible
                             .Select(static participant => participant.Advertisement.ProtocolVersion)
                             .Distinct()
                             .OrderDescending())
                {
                    var compatible = eligible
                        .Where(participant =>
                            participant.Advertisement.MinimumCompatibleProtocolVersion <= version &&
                            participant.Advertisement.ProtocolVersion >= version)
                        .OrderBy(static participant => participant.NetId)
                        .Take(RitsuLibSidecarEndpointPolicy.MaxRouteParticipants)
                        .ToArray();
                    if (group.Key.Topology == RitsuLibSidecarEndpointTopology.HostAuthority &&
                        compatible.All(participant => participant.NetId != hostNetId))
                        continue;
                    if (compatible.Length == 0)
                        continue;

                    var selection = new RouteSelection(group.Key, version, compatible);
                    if (best == null || IsBetterSelection(selection, best.Value))
                        best = selection;
                }
            }

            if (best is not { } selected)
                return false;

            var maxPayloadBytes = selected.Participants.Min(static participant =>
                participant.Advertisement.MaxPayloadBytes);
            uint routeId;
            ulong nonce;
            if (previousRoutes.TryGetValue(key, out var previous))
            {
                routeId = previous.Definition.RouteId;
                nonce = SameWireContract(previous.Definition, selected, maxPayloadBytes)
                    ? previous.Definition.Nonce
                    : CreateNonce();
            }
            else
            {
                routeId = NextNonzero(ref _nextRouteId);
                nonce = CreateNonce();
            }

            definition = new(
                routeId,
                nonce,
                key,
                selected.ProtocolVersion,
                selected.Contract.DeliveryProfile,
                selected.Contract.Topology,
                maxPayloadBytes,
                [.. selected.Participants.Select(static participant => participant.NetId)]);
            return true;
        }

        private static bool IsBetterSelection(RouteSelection candidate, RouteSelection current)
        {
            if (candidate.Participants.Length != current.Participants.Length)
                return candidate.Participants.Length > current.Participants.Length;
            if (candidate.ProtocolVersion != current.ProtocolVersion)
                return candidate.ProtocolVersion > current.ProtocolVersion;
            if (candidate.Contract.Topology != current.Contract.Topology)
                return candidate.Contract.Topology < current.Contract.Topology;
            return candidate.Contract.DeliveryProfile < current.Contract.DeliveryProfile;
        }

        private static bool SameWireContract(
            RitsuLibSidecarEndpointRouteDefinition previous,
            RouteSelection next,
            int maxPayloadBytes)
        {
            return previous.ProtocolVersion == next.ProtocolVersion &&
                   previous.DeliveryProfile == next.Contract.DeliveryProfile &&
                   previous.Topology == next.Contract.Topology &&
                   previous.MaxPayloadBytes == maxPayloadBytes;
        }

        private static bool ValidateDestination(
            RitsuLibSidecarEndpointRouteDefinition route,
            ulong senderNetId,
            ulong hostNetId,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId)
        {
            if (!route.ParticipantNetIds.Contains(senderNetId))
                return false;
            if (route.Topology == RitsuLibSidecarEndpointTopology.HostAuthority)
            {
                if (!route.ParticipantNetIds.Contains(hostNetId))
                    return false;
                if (senderNetId != hostNetId)
                    return destination == RitsuLibSidecarEndpointDestination.Host && targetNetId == 0;
                return destination switch
                {
                    RitsuLibSidecarEndpointDestination.Broadcast => targetNetId == 0,
                    RitsuLibSidecarEndpointDestination.Peer =>
                        targetNetId != hostNetId && route.ParticipantNetIds.Contains(targetNetId),
                    _ => false,
                };
            }

            return destination switch
            {
                RitsuLibSidecarEndpointDestination.Host =>
                    targetNetId == 0 && route.ParticipantNetIds.Contains(hostNetId),
                RitsuLibSidecarEndpointDestination.Broadcast => targetNetId == 0,
                RitsuLibSidecarEndpointDestination.Peer =>
                    targetNetId != senderNetId && route.ParticipantNetIds.Contains(targetNetId),
                _ => false,
            };
        }

        private static bool IsOperationAllowed(
            RitsuLibSidecarEndpointRouteDefinition route,
            ulong senderNetId,
            ulong hostNetId,
            RitsuLibSidecarEndpointDestination destination)
        {
            if (route.Topology == RitsuLibSidecarEndpointTopology.RelayGroup)
                return true;
            return senderNetId == hostNetId
                ? destination is RitsuLibSidecarEndpointDestination.Broadcast
                    or RitsuLibSidecarEndpointDestination.Peer
                : destination == RitsuLibSidecarEndpointDestination.Host;
        }

        private static IReadOnlyList<ulong> ResolveRemoteTargets(
            RitsuLibSidecarEndpointRouteDefinition route,
            ulong senderNetId,
            ulong hostNetId,
            RitsuLibSidecarEndpointDestination destination,
            ulong targetNetId)
        {
            return destination switch
            {
                RitsuLibSidecarEndpointDestination.Host => [],
                RitsuLibSidecarEndpointDestination.Broadcast =>
                [
                    .. route.ParticipantNetIds.Where(participant =>
                        participant != senderNetId && participant != hostNetId),
                ],
                RitsuLibSidecarEndpointDestination.Peer when targetNetId != hostNetId => [targetNetId],
                _ => [],
            };
        }

        private static bool CanDeliverCurrentRouteToPeer(ulong peerNetId)
        {
            lock (Gate)
            {
                if (!SnapshotRevisionPublishedByPeer.TryGetValue(peerNetId, out var published) ||
                    published != _hostSnapshotRevision)
                    return false;
                return SnapshotRevisionAcknowledgedByPeer.TryGetValue(peerNetId, out var acknowledged) &&
                       acknowledged == published;
            }
        }

        private static bool TryConsumeInboundRate(
            ulong senderNetId,
            uint routeId,
            RitsuLibSidecarDeliveryProfile deliveryProfile,
            int chargedBytes)
        {
            RitsuLibSidecarTokenBucket limiter;
            lock (Gate)
            {
                var key = new SenderRouteKey(senderNetId, routeId);
                if (!InboundRateLimits.TryGetValue(key, out limiter!))
                {
                    limiter = deliveryProfile switch
                    {
                        RitsuLibSidecarDeliveryProfile.Control => new(
                            RitsuLibSidecarEndpointPolicy.DefaultControlPacketsPerSecond,
                            RitsuLibSidecarEndpointPolicy.DefaultControlBytesPerSecond),
                        RitsuLibSidecarDeliveryProfile.RealtimeDatagram => new(
                            RitsuLibSidecarEndpointPolicy.DefaultRealtimePacketsPerSecond,
                            RitsuLibSidecarEndpointPolicy.DefaultRealtimeBytesPerSecond),
                        RitsuLibSidecarDeliveryProfile.BulkStream => new(
                            RitsuLibSidecarEndpointPolicy.DefaultBulkPacketsPerSecond,
                            RitsuLibSidecarEndpointPolicy.DefaultBulkBytesPerSecond),
                        _ => throw new ArgumentOutOfRangeException(nameof(deliveryProfile)),
                    };
                    InboundRateLimits.Add(key, limiter);
                }
            }

            return limiter.TryConsume(chargedBytes);
        }

        private static bool TryConsumeCatalogRate(ulong senderNetId, int chargedBytes)
        {
            RitsuLibSidecarTokenBucket limiter;
            lock (Gate)
            {
                if (!CatalogRateLimits.TryGetValue(senderNetId, out limiter!))
                {
                    limiter = new(
                        RitsuLibSidecarEndpointPolicy.CatalogUpdatesPerSecond,
                        RitsuLibSidecarEndpointPolicy.CatalogBytesPerSecond);
                    CatalogRateLimits.Add(senderNetId, limiter);
                }
            }

            return limiter.TryConsume(chargedBytes);
        }

        private static bool TryAcceptSequence(
            Dictionary<SenderRouteKey, uint> sequenceBySenderRoute,
            SenderRouteKey key,
            uint sequence)
        {
            lock (Gate)
            {
                if (!sequenceBySenderRoute.TryGetValue(key, out var previous))
                {
                    sequenceBySenderRoute[key] = sequence;
                    return true;
                }

                if (unchecked((int)(sequence - previous)) <= 0)
                    return false;
                sequenceBySenderRoute[key] = sequence;
                return true;
            }
        }

        private static void DispatchLocal(
            RitsuLibSidecarEndpointRouteDefinition route,
            ulong originalSenderNetId,
            byte[] payload)
        {
            if (!RitsuLibSidecarEndpointRegistry.TryGet(route.Key, out var registration) ||
                registration == null ||
                registration.IsDisposed ||
                !RouteMatchesDescriptor(route, registration.Descriptor))
                return;
            registration.Dispatch(new(originalSenderNetId, route.ProtocolVersion, payload));
        }

        private static void ApplyLocalRoutes(
            IReadOnlyDictionary<RitsuLibSidecarEndpointKey, RitsuLibSidecarEndpointRouteDefinition> routes,
            ulong localNetId)
        {
            foreach (var registration in RitsuLibSidecarEndpointRegistry.GetRegistrationsSnapshot())
            {
                var key = new RitsuLibSidecarEndpointKey(
                    registration.Descriptor.OwnerId,
                    registration.Descriptor.Name);
                registration.ApplyRoute(
                    routes.TryGetValue(key, out var route) &&
                    route.ParticipantNetIds.Contains(localNetId) &&
                    RouteMatchesDescriptor(route, registration.Descriptor)
                        ? route
                        : null);
            }
        }

        private static bool RouteMatchesDescriptor(
            RitsuLibSidecarEndpointRouteDefinition route,
            RitsuLibSidecarEndpointDescriptor descriptor)
        {
            return route.DeliveryProfile == descriptor.DeliveryProfile &&
                   route.Topology == descriptor.Topology &&
                   route.ProtocolVersion >= descriptor.MinimumCompatibleProtocolVersion &&
                   route.ProtocolVersion <= descriptor.ProtocolVersion &&
                   route.MaxPayloadBytes <= descriptor.MaxPayloadBytes;
        }

        private static byte[] CreateDataEnvelope(
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliveryProfile deliveryProfile)
        {
            var legacyDelivery = deliveryProfile == RitsuLibSidecarDeliveryProfile.RealtimeDatagram
                ? RitsuLibSidecarDeliverySemantics.BestEffort
                : RitsuLibSidecarDeliverySemantics.StableSync;
            return RitsuLibSidecar.CreateEnvelopeWithDeliveryCompressed(
                opcode,
                payload,
                legacyDelivery,
                RitsuLibSidecarPayloadCompression.None);
        }

        private static bool ProfileSupported(
            RitsuLibSidecarTransportProfileMask profiles,
            RitsuLibSidecarDeliveryProfile profile)
        {
            return profile switch
            {
                RitsuLibSidecarDeliveryProfile.Control =>
                    (profiles & RitsuLibSidecarTransportProfileMask.Control) != 0,
                RitsuLibSidecarDeliveryProfile.RealtimeDatagram =>
                    (profiles & RitsuLibSidecarTransportProfileMask.RealtimeDatagram) != 0,
                RitsuLibSidecarDeliveryProfile.BulkStream =>
                    (profiles & RitsuLibSidecarTransportProfileMask.BulkStream) != 0,
                _ => false,
            };
        }

        private static ulong CreateNonce()
        {
            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            ulong nonce;
            do
            {
                RandomNumberGenerator.Fill(bytes);
                nonce = BinaryPrimitives.ReadUInt64BigEndian(bytes);
            } while (nonce == 0);

            return nonce;
        }

        private static uint NextNonzero(ref uint value)
        {
            value = unchecked(value + 1);
            if (value == 0)
                value = 1;
            return value;
        }

        private static bool IsNewerOrEqual(uint candidate, uint previous)
        {
            return candidate == previous || unchecked((int)(candidate - previous)) > 0;
        }

        private static void ResetSessionState()
        {
            lock (RebuildGate)
            {
                lock (Gate)
                {
                    PeerCatalogs.Clear();
                    CatalogRateLimits.Clear();
                    HostRoutesByKey.Clear();
                    HostRoutesById.Clear();
                    ClientRoutesById.Clear();
                    SnapshotRevisionPublishedByPeer.Clear();
                    SnapshotRevisionAcknowledgedByPeer.Clear();
                    InboundRateLimits.Clear();
                    LastIngressSequence.Clear();
                    LastDeliverySequence.Clear();
                    _nextRouteId = 0;
                    _hostSnapshotRevision = 0;
                    _lastClientSnapshotRevision = 0;
                }

                RitsuLibSidecarOutboundScheduler.Clear();
                foreach (var registration in RitsuLibSidecarEndpointRegistry.GetRegistrationsSnapshot())
                    registration.ApplyRoute(null);
            }
        }

        private static void RemoveSenderState(ulong senderNetId)
        {
            foreach (var key in InboundRateLimits.Keys.Where(key => key.SenderNetId == senderNetId).ToArray())
                InboundRateLimits.Remove(key);
            foreach (var key in LastIngressSequence.Keys.Where(key => key.SenderNetId == senderNetId).ToArray())
                LastIngressSequence.Remove(key);
            foreach (var key in LastDeliverySequence.Keys.Where(key => key.SenderNetId == senderNetId).ToArray())
                LastDeliverySequence.Remove(key);
        }

        private readonly record struct PeerCatalogState(
            long SessionEpoch,
            RitsuLibSidecarEndpointCatalog Catalog)
        {
            internal RitsuLibSidecarTransportProfileMask SupportedProfiles => Catalog.SupportedProfiles;

            internal IEnumerable<RitsuLibSidecarEndpointAdvertisement> GetEndpoints() => Catalog.Endpoints;
        }

        private readonly record struct AdvertisedParticipant(
            ulong NetId,
            RitsuLibSidecarEndpointAdvertisement Advertisement,
            RitsuLibSidecarTransportProfileMask SupportedProfiles,
            bool IsHost);

        private readonly record struct RouteContract(
            RitsuLibSidecarDeliveryProfile DeliveryProfile,
            RitsuLibSidecarEndpointTopology Topology);

        private readonly record struct RouteSelection(
            RouteContract Contract,
            ushort ProtocolVersion,
            AdvertisedParticipant[] Participants);

        private readonly record struct SenderRouteKey(ulong SenderNetId, uint RouteId);

        private sealed class HostRouteState(RitsuLibSidecarEndpointRouteDefinition definition)
        {
            internal RitsuLibSidecarEndpointRouteDefinition Definition { get; } = definition;
        }
    }
}
