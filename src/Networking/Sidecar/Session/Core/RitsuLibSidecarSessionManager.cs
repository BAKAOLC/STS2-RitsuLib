using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Platform;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar session state hub: tracks current multiplayer service, peer reachability/features, and pluggable
    ///     capability providers.
    ///     Sidecar 会话状态中心：跟踪当前多人 service、peer 可达性/feature，以及可插拔
    ///     能力提供方。
    /// </summary>
    public static class RitsuLibSidecarSessionManager
    {
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, RitsuLibSidecarPeerReachability> PeerReachability = [];
        private static readonly Dictionary<ulong, RitsuLibSidecarPeerFeatures> PeerFeatures = [];
        private static readonly HashSet<ulong> HandshakeNegotiationTerminalPeers = [];
        private static readonly List<IRitsuLibSidecarCapabilityValidationRoute> ValidationRoutes = [];
        private static IRitsuLibSidecarCapabilityValidationRoute[] _validationRoutesSnapshot = [];
        private static ulong[] _peerIdsSnapshot = [];
        private static ulong[] _supportedPeerIdsSnapshot = [];

        private static INetGameService? _currentNetService;
        private static long _epoch;
        private static bool _providerBootstrapped;

        /// <summary>
        ///     Current session epoch; incremented on each observed net service switch.
        ///     当前会话纪元；每次观察到 net service 切换时递增。
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
        ///     非单人 service 变为活动状态时触发的事件。
        /// </summary>
        public static event Action<SidecarSessionBoundEvent>? SessionBound;

        /// <summary>
        ///     Event fired when session transitions to unbound/singleplayer.
        ///     会话转换为未绑定/单人状态时触发的事件。
        /// </summary>
        public static event Action<SidecarSessionUnboundEvent>? SessionUnbound;

        /// <summary>
        ///     Event fired on reachability transitions.
        ///     可达性转换时触发的事件。
        /// </summary>
        public static event Action<SidecarPeerReachabilityChangedEvent>? PeerReachabilityChanged;

        /// <summary>
        ///     Event fired when handshake information marks a peer as sidecar-capable.
        ///     握手信息将 peer 标记为 sidecar-capable 时触发的事件。
        /// </summary>
        public static event Action<SidecarHandshakeCompletedEvent>? HandshakeCompleted;

        /// <summary>
        ///     Ensures built-in capability providers are registered once.
        ///     确保内置能力提供方只注册一次。
        /// </summary>
        public static void EnsureProvidersBootstrapped()
        {
            lock (Gate)
            {
                if (_providerBootstrapped)
                    return;

                ValidationRoutes.Add(new RitsuLibSidecarManualHintValidationRoute());
                ValidationRoutes.Add(new RitsuLibSidecarNativeTrailerValidationRoute());
                if (!RitsuLibMobileSteamRuntime.SuppressNativeSteamIntegration)
                    ValidationRoutes.Add(new RitsuLibSidecarSteamLobbyValidationRoute());
                RebuildValidationRoutesSnapshotLocked();
                _providerBootstrapped = true;
            }
        }

        /// <summary>
        ///     Registers an additional validation route (deduplicated by concrete type).
        ///     注册额外验证路由（按具体类型去重）。
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
                RebuildValidationRoutesSnapshotLocked();
            }
        }

        /// <summary>
        ///     Observes current net service and updates session state when it changes.
        ///     观察当前 net service，并在其变化时更新会话状态。
        /// </summary>
        public static void ObserveNetService(INetGameService? netService)
        {
            EnsureProvidersBootstrapped();
            SidecarSessionBoundEvent? boundEvt = null;
            SidecarSessionUnboundEvent? unboundEvt = null;
            ulong[] seededPeers = [];
            lock (Gate)
            {
                if (IsSemanticallySameService(_currentNetService, netService))
                    return;

                _epoch++;
                PeerReachability.Clear();
                PeerFeatures.Clear();
                HandshakeNegotiationTerminalPeers.Clear();
                _currentNetService = netService;
                if (netService == null || netService.Type == NetGameType.Singleplayer)
                {
                    unboundEvt = new SidecarSessionUnboundEvent(_epoch);
                }
                else
                {
                    SeedKnownPeers(netService);
                    RebuildPeerSnapshotsLocked();
                    seededPeers = _peerIdsSnapshot;
                    boundEvt = new SidecarSessionBoundEvent(netService, _epoch);
                }

                if (netService == null || netService.Type == NetGameType.Singleplayer)
                    RebuildPeerSnapshotsLocked();
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
        ///     仅当 peer 当前为 <see cref="RitsuLibSidecarPeerReachability.Supported" /> 时返回 true。
        /// </summary>
        public static bool CanSendToPeer(ulong peerNetId)
        {
            return TryGetReachability(peerNetId, out var reachability)
                   && reachability == RitsuLibSidecarPeerReachability.Supported;
        }

        /// <summary>
        ///     Tries to read current reachability for a peer.
        ///     尝试读取 peer 的当前可达性。
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
        ///     返回当前允许进行 sidecar 发送的 peer 快照。
        /// </summary>
        public static IReadOnlyList<ulong> GetSupportedPeersSnapshot()
        {
            lock (Gate)
            {
                return [.. _supportedPeerIdsSnapshot];
            }
        }

        internal static IReadOnlyList<ulong> GetSupportedPeersForIteration()
        {
            lock (Gate)
            {
                return _supportedPeerIdsSnapshot;
            }
        }

        /// <summary>
        ///     Records a host-side peer connection, seeds <c>Unknown</c>, then refreshes from providers.
        ///     记录主机侧 peer 连接，播种 <c>Unknown</c>，然后从提供方刷新。
        /// </summary>
        public static void NotePeerConnected(ulong peerNetId)
        {
            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unknown,
                RitsuLibSidecarDiscoveryPolicy.ReasonPeerConnected);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     Removes reachability/feature state for a disconnected peer.
        ///     移除断开 peer 的可达性/feature 状态。
        /// </summary>
        public static void NotePeerDisconnected(ulong peerNetId)
        {
            lock (Gate)
            {
                if (PeerReachability.Remove(peerNetId))
                    RebuildPeerSnapshotsLocked();
                PeerFeatures.Remove(peerNetId);
                HandshakeNegotiationTerminalPeers.Remove(peerNetId);
            }

            RitsuLibSidecarConnectionExchange.RemoveNegotiationStateForPeer(peerNetId);
        }

        /// <summary>
        ///     Marks the peer as terminal for outbound handshake negotiation (transport budget, ack timeout, etc.) and
        ///     forces <see cref="RitsuLibSidecarPeerReachability.Unsupported" /> for session stability.
        ///     将 peer 标记为出站握手协商的终止状态（传输预算、ack 超时等），并
        ///     强制会话中的 <see cref="RitsuLibSidecarPeerReachability.Unsupported" /> 以保持稳定。
        /// </summary>
        public static void NoteHandshakeNegotiationAborted(ulong peerNetId, string reason)
        {
            lock (Gate)
            {
                HandshakeNegotiationTerminalPeers.Add(peerNetId);
            }

            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unsupported, reason);
        }

        /// <summary>
        ///     Marks the peer as terminal-unreachable for sidecar sends due to a transport-layer failure indicating the
        ///     peer connection is missing (e.g. host transport no longer has a connection entry for the peer).
        ///     This prevents per-frame resend loops from repeatedly throwing.
        ///     由于传输层失败表明 peer 连接缺失，将 peer 标记为 sidecar 发送的终止不可达状态
        ///     （例如主机传输不再有该 peer 的连接条目）。
        ///     这可防止逐帧重发循环反复抛出异常。
        /// </summary>
        public static void NoteTransportConnectionMissing(ulong peerNetId)
        {
            lock (Gate)
            {
                HandshakeNegotiationTerminalPeers.Add(peerNetId);
            }

            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unsupported,
                RitsuLibSidecarDiscoveryPolicy.ReasonTransportConnectionMissing);
        }

        /// <summary>
        ///     Stores handshake feature result and updates peer reachability according to handshake result.
        ///     存储握手 feature 结果，并根据握手结果更新 peer 可达性。
        /// </summary>
        public static void NoteHandshakeFromPeer(ulong peerNetId, RitsuLibSidecarPeerFeatures features, bool accepted)
        {
            lock (Gate)
            {
                PeerFeatures[peerNetId] = features;
                if (accepted)
                    HandshakeNegotiationTerminalPeers.Remove(peerNetId);
                else
                    HandshakeNegotiationTerminalPeers.Add(peerNetId);
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
        ///     尝试读取 peer 的最后已知 feature flags。
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
        ///     设置手动可达性提示，并重新评估提供方判定。
        /// </summary>
        public static void SetPeerReachabilityHint(ulong peerNetId, RitsuLibSidecarPeerReachability reachability)
        {
            RitsuLibSidecarCapabilityHints.SetHint(peerNetId, reachability);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     Re-evaluates a peer using registered providers; first non-null verdict wins.
        ///     使用已注册提供方重新评估 peer；第一个非 null 判定获胜。
        /// </summary>
        public static void RefreshReachabilityFromProviders(ulong peerNetId)
        {
            EnsureProvidersBootstrapped();
            INetGameService? netService;
            IRitsuLibSidecarCapabilityValidationRoute[] routes;
            lock (Gate)
            {
                netService = _currentNetService;
                routes = _validationRoutesSnapshot;
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

                var resolved = verdict.Value;
                lock (Gate)
                {
                    if (resolved == RitsuLibSidecarPeerReachability.Supported &&
                        HandshakeNegotiationTerminalPeers.Contains(peerNetId))
                        resolved = RitsuLibSidecarPeerReachability.Unsupported;
                }

                UpdateReachability(peerNetId, resolved,
                    $"{RitsuLibSidecarDiscoveryPolicy.RouteReasonPrefix}{route.Name}");
                return;
            }
        }

        /// <summary>
        ///     Re-evaluates all currently known peers using registered providers.
        ///     使用已注册提供方重新评估所有当前已知 peer。
        /// </summary>
        public static void RefreshAllReachabilityFromProviders()
        {
            ulong[] peers;
            lock (Gate)
            {
                if (_currentNetService == null || _currentNetService.Type == NetGameType.Singleplayer)
                    return;
                peers = _peerIdsSnapshot;
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
                var existed = PeerReachability.TryGetValue(peerNetId, out var previous);
                if (!existed)
                    previous = RitsuLibSidecarPeerReachability.Unknown;

                if (previous == next)
                {
                    PeerReachability[peerNetId] = next;
                    if (!existed)
                        RebuildPeerSnapshotsLocked();
                    return;
                }

                PeerReachability[peerNetId] = next;
                RebuildPeerSnapshotsLocked();
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
            IRitsuLibSidecarCapabilityValidationRoute[] routes;
            lock (Gate)
            {
                routes = _validationRoutesSnapshot;
            }

            foreach (var route in routes)
                if (route.IsAvailable(netService))
                    route.PublishLocalEvidence(netService);
        }

        private static void RebuildValidationRoutesSnapshotLocked()
        {
            _validationRoutesSnapshot = [.. ValidationRoutes];
        }

        private static void RebuildPeerSnapshotsLocked()
        {
            _peerIdsSnapshot = [.. PeerReachability.Keys];
            _supportedPeerIdsSnapshot =
            [
                .. PeerReachability
                    .Where(static pair => pair.Value == RitsuLibSidecarPeerReachability.Supported)
                    .Select(static pair => pair.Key),
            ];
        }

        private static bool IsSemanticallySameService(INetGameService? a, INetGameService? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.Type == b.Type && a.NetId == b.NetId;
        }
    }
}
