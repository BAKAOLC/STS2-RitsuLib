using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar session state hub: tracks current multiplayer service, peer reachability/features, and pluggable
    ///     capability providers.
    /// </summary>
    public static class RitsuLibSidecarSessionManager
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RitsuLibSidecarPeerReachability> PeerReachability = [];
        private static readonly Dictionary<ulong, RitsuLibSidecarPeerFeatures> PeerFeatures = [];
        private static readonly List<IRitsuLibSidecarCapabilityValidationRoute> ValidationRoutes = [];

        private static INetGameService? _currentNetService;
        private static long _epoch;
        private static bool _providerBootstrapped;

        /// <summary>
        ///     Current session epoch; incremented on each observed net service switch.
        /// </summary>
        public static long Epoch
        {
            get
            {
                lock (Gate)
                {
                    return _epoch;
                }
            }
        }

        /// <summary>
        ///     Event fired when a non-singleplayer service becomes active.
        /// </summary>
        public static event Action<SidecarSessionBoundEvent>? SessionBound;

        /// <summary>
        ///     Event fired when session transitions to unbound/singleplayer.
        /// </summary>
        public static event Action<SidecarSessionUnboundEvent>? SessionUnbound;

        /// <summary>
        ///     Event fired on reachability transitions.
        /// </summary>
        public static event Action<SidecarPeerReachabilityChangedEvent>? PeerReachabilityChanged;

        /// <summary>
        ///     Event fired when handshake information marks a peer as sidecar-capable.
        /// </summary>
        public static event Action<SidecarHandshakeCompletedEvent>? HandshakeCompleted;

        /// <summary>
        ///     Ensures built-in capability providers are registered once.
        /// </summary>
        public static void EnsureProvidersBootstrapped()
        {
            lock (Gate)
            {
                if (_providerBootstrapped)
                    return;

                ValidationRoutes.Add(new RitsuLibSidecarManualHintValidationRoute());
                ValidationRoutes.Add(new RitsuLibSidecarNativeTrailerValidationRoute());
                ValidationRoutes.Add(new RitsuLibSidecarSteamLobbyValidationRoute());
                _providerBootstrapped = true;
            }
        }

        /// <summary>
        ///     Registers an additional validation route (deduplicated by concrete type).
        /// </summary>
        public static void RegisterValidationRoute(IRitsuLibSidecarCapabilityValidationRoute route)
        {
            ArgumentNullException.ThrowIfNull(route);
            EnsureProvidersBootstrapped();
            lock (Gate)
            {
                if (ValidationRoutes.Any(r => r.GetType() == route.GetType()))
                    return;
                ValidationRoutes.Add(route);
                ValidationRoutes.Sort(static (a, b) => a.Order.CompareTo(b.Order));
            }
        }

        /// <summary>
        ///     Observes current net service and updates session state when it changes.
        /// </summary>
        public static void ObserveNetService(INetGameService? netService)
        {
            EnsureProvidersBootstrapped();
            SidecarSessionBoundEvent? boundEvt = null;
            SidecarSessionUnboundEvent? unboundEvt = null;
            ulong[] seededPeers = [];
            lock (Gate)
            {
                if (ReferenceEquals(_currentNetService, netService))
                    return;

                _epoch++;
                PeerReachability.Clear();
                PeerFeatures.Clear();
                _currentNetService = netService;
                if (netService == null || netService.Type == NetGameType.Singleplayer)
                {
                    unboundEvt = new SidecarSessionUnboundEvent(_epoch);
                }
                else
                {
                    SeedKnownPeers(netService);
                    seededPeers = [..PeerReachability.Keys];
                    boundEvt = new SidecarSessionBoundEvent(netService, _epoch);
                }
            }

            if (unboundEvt is { } u)
            {
                Trace($"Session unbound epoch={u.Epoch}");
                SessionUnbound?.Invoke(u);
            }

            if (boundEvt is not { } b) return;
            DispatchPublishLocalEvidence(b.NetService);
            foreach (var peer in seededPeers)
                RefreshReachabilityFromProviders(peer);
            Trace($"Session bound epoch={b.Epoch}, netType={b.NetService.Type}, netId={b.NetService.NetId}");
            SessionBound?.Invoke(b);
        }

        /// <summary>
        ///     Returns true only when the peer is currently <see cref="RitsuLibSidecarPeerReachability.Supported" />.
        /// </summary>
        public static bool CanSendToPeer(ulong peerNetId)
        {
            return TryGetReachability(peerNetId, out var reachability)
                   && reachability == RitsuLibSidecarPeerReachability.Supported;
        }

        /// <summary>
        ///     Tries to read current reachability for a peer.
        /// </summary>
        public static bool TryGetReachability(ulong peerNetId, out RitsuLibSidecarPeerReachability reachability)
        {
            lock (Gate)
            {
                return PeerReachability.TryGetValue(peerNetId, out reachability);
            }
        }

        /// <summary>
        ///     Returns a snapshot of peers currently allowed for sidecar sends.
        /// </summary>
        public static IReadOnlyList<ulong> GetSupportedPeersSnapshot()
        {
            lock (Gate)
            {
                return PeerReachability.Where(p => p.Value == RitsuLibSidecarPeerReachability.Supported)
                    .Select(p => p.Key)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Records a host-side peer connection, seeds <c>Unknown</c>, then refreshes from providers.
        /// </summary>
        public static void NotePeerConnected(ulong peerNetId)
        {
            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unknown,
                RitsuLibSidecarDiscoveryPolicy.ReasonPeerConnected);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     Removes reachability/feature state for a disconnected peer.
        /// </summary>
        public static void NotePeerDisconnected(ulong peerNetId)
        {
            lock (Gate)
            {
                PeerReachability.Remove(peerNetId);
                PeerFeatures.Remove(peerNetId);
            }
        }

        /// <summary>
        ///     Stores handshake feature result and updates peer reachability according to handshake result.
        /// </summary>
        public static void NoteHandshakeFromPeer(ulong peerNetId, RitsuLibSidecarPeerFeatures features, bool accepted)
        {
            lock (Gate)
            {
                PeerFeatures[peerNetId] = features;
            }

            if (!accepted)
            {
                UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unsupported,
                    RitsuLibSidecarDiscoveryPolicy.ReasonHandshake);
                return;
            }

            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Supported,
                RitsuLibSidecarDiscoveryPolicy.ReasonHandshake);
            HandshakeCompleted?.Invoke(new(peerNetId, features, Epoch));
        }

        /// <summary>
        ///     Tries to read last known feature flags for a peer.
        /// </summary>
        public static bool TryGetPeerFeatures(ulong peerNetId, out RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                return PeerFeatures.TryGetValue(peerNetId, out features);
            }
        }

        /// <summary>
        ///     Sets a manual reachability hint and re-evaluates provider verdicts.
        /// </summary>
        public static void SetPeerReachabilityHint(ulong peerNetId, RitsuLibSidecarPeerReachability reachability)
        {
            RitsuLibSidecarCapabilityHints.SetHint(peerNetId, reachability);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     Re-evaluates a peer using registered providers; first non-null verdict wins.
        /// </summary>
        public static void RefreshReachabilityFromProviders(ulong peerNetId)
        {
            EnsureProvidersBootstrapped();
            INetGameService? netService;
            List<IRitsuLibSidecarCapabilityValidationRoute> routes;
            lock (Gate)
            {
                netService = _currentNetService;
                routes = [..ValidationRoutes];
            }

            if (netService == null || netService.Type == NetGameType.Singleplayer)
                return;

            foreach (var route in routes)
            {
                if (!route.IsAvailable(netService))
                    continue;

                var verdict = route.TryResolve(netService, peerNetId);
                if (verdict == null)
                    continue;

                UpdateReachability(peerNetId, verdict.Value,
                    $"{RitsuLibSidecarDiscoveryPolicy.RouteReasonPrefix}{route.Name}");
                return;
            }
        }

        /// <summary>
        ///     Re-evaluates all currently known peers using registered providers.
        /// </summary>
        public static void RefreshAllReachabilityFromProviders()
        {
            ulong[] peers;
            lock (Gate)
            {
                if (_currentNetService == null || _currentNetService.Type == NetGameType.Singleplayer)
                    return;
                peers = [..PeerReachability.Keys];
            }

            foreach (var peer in peers)
                RefreshReachabilityFromProviders(peer);
        }

        private static void SeedKnownPeers(INetGameService netService)
        {
            switch (netService)
            {
                case NetHostGameService host:
                    foreach (var peer in host.ConnectedPeers)
                        PeerReachability[peer.peerId] = RitsuLibSidecarPeerReachability.Unknown;
                    break;
                case NetClientGameService client:
                    PeerReachability[client.HostNetId] = RitsuLibSidecarPeerReachability.Unknown;
                    break;
            }
        }

        private static void UpdateReachability(
            ulong peerNetId,
            RitsuLibSidecarPeerReachability next,
            string reason)
        {
            SidecarPeerReachabilityChangedEvent? evt;
            lock (Gate)
            {
                var previous = PeerReachability.GetValueOrDefault(peerNetId, RitsuLibSidecarPeerReachability.Unknown);
                if (previous == next)
                {
                    PeerReachability[peerNetId] = next;
                    return;
                }

                PeerReachability[peerNetId] = next;
                evt = new SidecarPeerReachabilityChangedEvent(peerNetId, previous, next, reason, _epoch);
            }

            if (evt is not { } changed) return;
            Trace(
                $"Peer reachability changed peer={changed.PeerNetId}, {changed.Previous}->{changed.Current}, reason={changed.Reason}, epoch={changed.Epoch}");
            PeerReachabilityChanged?.Invoke(changed);
        }

        private static void Trace(string text)
        {
            RitsuLibFramework.Logger.Info($"[Sidecar] {text}");
        }

        private static void DispatchPublishLocalEvidence(INetGameService netService)
        {
            List<IRitsuLibSidecarCapabilityValidationRoute> routes;
            lock (Gate)
            {
                routes = [..ValidationRoutes];
            }

            foreach (var route in routes.Where(route => route.IsAvailable(netService)))
                route.PublishLocalEvidence(netService);
        }
    }
}
