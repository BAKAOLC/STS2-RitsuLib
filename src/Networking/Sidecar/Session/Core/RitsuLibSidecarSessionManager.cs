using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using STS2RitsuLib.Platform;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Maintains Sidecar session state, including the current multiplayer service, peer reachability and
    ///         features, and pluggable capability-validation routes.
    ///     </para>
    ///     <para xml:lang="zh-CN">维护 Sidecar 会话状态，包括当前多人游戏服务、对等方可达性和功能以及可插拔的能力验证路由。</para>
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
        ///     <para xml:lang="en">Current session epoch, incremented for each observed network-service switch.</para>
        ///     <para xml:lang="zh-CN">当前会话纪元；每次观察到网络服务切换时递增。</para>
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
        ///     <para xml:lang="en">Raised after a non-singleplayer service has become the active session service.</para>
        ///     <para xml:lang="zh-CN">在非单人游戏服务成为活动会话服务后引发。</para>
        /// </summary>
        public static event Action<SidecarSessionBoundEvent>? SessionBound;

        /// <summary>
        ///     <para xml:lang="en">Raised after the session transitions to an unbound or single-player service.</para>
        ///     <para xml:lang="zh-CN">在会话转换为未绑定或单人游戏服务后引发。</para>
        /// </summary>
        public static event Action<SidecarSessionUnboundEvent>? SessionUnbound;

        /// <summary>
        ///     <para xml:lang="en">Raised after a peer's reachability state changes.</para>
        ///     <para xml:lang="zh-CN">在对等方的可达性状态改变后引发。</para>
        /// </summary>
        public static event Action<SidecarPeerReachabilityChangedEvent>? PeerReachabilityChanged;

        /// <summary>
        ///     <para xml:lang="en">Raised after accepted handshake information marks a peer as Sidecar-capable.</para>
        ///     <para xml:lang="zh-CN">在已接受的握手信息将对等方标记为支持 Sidecar 后引发。</para>
        /// </summary>
        public static event Action<SidecarHandshakeCompletedEvent>? HandshakeCompleted;

        /// <summary>
        ///     <para xml:lang="en">Ensures the built-in capability-validation routes are registered once.</para>
        ///     <para xml:lang="zh-CN">确保内置能力验证路由只注册一次。</para>
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
        ///     <para xml:lang="en">Registers an additional validation route, deduplicated by concrete route type.</para>
        ///     <para xml:lang="zh-CN">注册额外验证路由，并按路由具体类型去重。</para>
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
        ///     <para xml:lang="en">
        ///         Observes the current network service and updates session state when its instance changes. It raises
        ///         session events only after committing the new state; event subscribers run synchronously.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         观察当前网络服务，并在其实例变更时更新会话状态。仅在提交新状态后引发会话事件；事件订阅者同步运行。
        ///     </para>
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
        ///     <para xml:lang="en">Returns whether the peer is currently <see cref="RitsuLibSidecarPeerReachability.Supported" />.</para>
        ///     <para xml:lang="zh-CN">返回该对等方当前是否为 <see cref="RitsuLibSidecarPeerReachability.Supported" />。</para>
        /// </summary>
        public static bool CanSendToPeer(ulong peerNetId)
        {
            return TryGetReachability(peerNetId, out var reachability)
                   && reachability == RitsuLibSidecarPeerReachability.Supported;
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to read the current reachability state for a peer.</para>
        ///     <para xml:lang="zh-CN">尝试读取对等方的当前可达性状态。</para>
        /// </summary>
        public static bool TryGetReachability(ulong peerNetId, out RitsuLibSidecarPeerReachability reachability)
        {
            lock (Gate)
            {
                return PeerReachability.TryGetValue(peerNetId, out reachability);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a copy of the peers currently allowed to receive Sidecar sends.</para>
        ///     <para xml:lang="zh-CN">返回当前允许接收 Sidecar 发送的对等方副本。</para>
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
        ///     <para xml:lang="en">Records a host-side peer connection, seeds <c>Unknown</c>, then refreshes provider verdicts.</para>
        ///     <para xml:lang="zh-CN">记录主机侧对等方连接，设为 <c>Unknown</c>，然后刷新提供方判定。</para>
        /// </summary>
        public static void NotePeerConnected(ulong peerNetId)
        {
            UpdateReachability(peerNetId, RitsuLibSidecarPeerReachability.Unknown,
                RitsuLibSidecarDiscoveryPolicy.ReasonPeerConnected);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes reachability, feature, and handshake-negotiation state for a disconnected peer.</para>
        ///     <para xml:lang="zh-CN">移除已断开对等方的可达性、功能和握手协商状态。</para>
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
        ///     <para xml:lang="en">
        ///         Marks a peer terminal for outbound handshake negotiation, such as after a transport-budget or
        ///         acknowledgement-timeout failure, and forces it to <see cref="RitsuLibSidecarPeerReachability.Unsupported" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将对等方标记为出站握手协商的终止状态，例如传输预算或确认超时失败后，并强制其为
        ///         <see cref="RitsuLibSidecarPeerReachability.Unsupported" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Marks a peer terminal-unreachable for Sidecar sends after a transport failure indicates its
        ///         connection is missing, preventing per-frame resend loops from repeatedly throwing.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在传输失败表明对等方连接缺失后，将其标记为 Sidecar 发送的终止不可达状态，以防逐帧重发循环反复抛出异常。
        ///     </para>
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
        ///     <para xml:lang="en">Stores handshake features and updates peer reachability from the acceptance result.</para>
        ///     <para xml:lang="zh-CN">存储握手功能，并根据接受结果更新对等方可达性。</para>
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
        ///     <para xml:lang="en">Tries to read the last known feature flags for a peer.</para>
        ///     <para xml:lang="zh-CN">尝试读取对等方最后已知的功能标志。</para>
        /// </summary>
        public static bool TryGetPeerFeatures(ulong peerNetId, out RitsuLibSidecarPeerFeatures features)
        {
            lock (Gate)
            {
                return PeerFeatures.TryGetValue(peerNetId, out features);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Sets a manual reachability hint, then re-evaluates provider verdicts.</para>
        ///     <para xml:lang="zh-CN">设置手动可达性提示，然后重新评估提供方判定。</para>
        /// </summary>
        public static void SetPeerReachabilityHint(ulong peerNetId, RitsuLibSidecarPeerReachability reachability)
        {
            RitsuLibSidecarCapabilityHints.SetHint(peerNetId, reachability);
            RefreshReachabilityFromProviders(peerNetId);
        }

        /// <summary>
        ///     <para xml:lang="en">Re-evaluates a peer through registered routes; the first non-null verdict wins.</para>
        ///     <para xml:lang="zh-CN">通过已注册路由重新评估对等方；第一个非 <see langword="null" /> 的判定获胜。</para>
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
        ///     <para xml:lang="en">Re-evaluates all currently known peers through the registered routes.</para>
        ///     <para xml:lang="zh-CN">通过已注册路由重新评估所有当前已知对等方。</para>
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

    }
}
