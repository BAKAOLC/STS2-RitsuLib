using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Initiates sidecar capability negotiation over <see cref="RitsuLibSidecarControlOpcodes.Handshake" />.
    /// </summary>
    public static class RitsuLibSidecarConnectionExchange
    {
        private const int HelloMaxPacketAttempts = 6;

        private const int HelloAckTimeoutMilliseconds = 12000;

        private const int HelloInitialBackoffMilliseconds = 250;

        private const int HelloMaxBackoffMilliseconds = 8000;

        private const int HelloAckTimeoutRetryDeferMilliseconds = 250;

        private static readonly Lock ExchangeGate = new();

        /// <remarks>
        ///     Cleared when <see cref="RitsuLibSidecarSessionManager.Epoch" /> changes; entries may be dropped on success /
        ///     terminal negotiation failure.
        /// </remarks>
        private static readonly Dictionary<ulong, HelloOutboundNegotiationState> NegotiationByPeer = [];

        private static long _negotiationAlignedEpoch;

        /// <summary>
        ///     Drops outbound handshake pacing / ack bookkeeping for one peer (e.g. multiplayer disconnect hooks).
        /// </summary>
        public static void RemoveNegotiationStateForPeer(ulong peerNetId)
        {
            lock (ExchangeGate)
            {
                NegotiationByPeer.Remove(peerNetId);
            }
        }

        /// <summary>
        ///     Correlates a received handshake ack against the outbound attempt started by this assembly.
        /// </summary>
        public static void NotifyOutboundHandshakeAck(ulong responderNetId, bool negotiationOk)
        {
            lock (ExchangeGate)
            {
                var epochNow = RitsuLibSidecarSessionManager.Epoch;
                if (!NegotiationByPeer.TryGetValue(responderNetId, out var state))
                    return;

                if (state.SessionEpoch != epochNow || state.Phase != NegotiationOutboundPhase.AwaitingAck)
                    return;

                NegotiationByPeer.Remove(responderNetId);
            }

            if (!negotiationOk)
                return;

            RitsuLibFramework.Logger.Debug(
                $"[Sidecar] Handshake negotiation completed peer={responderNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}");
        }

        /// <summary>
        ///     Drops all outbound handshake bookkeeping immediately after multiplayer session teardown (paired with epoch
        ///     advancement from <see cref="RitsuLibSidecarSessionManager.ObserveNetService" />(<see langword="null" />)).
        /// </summary>
        public static void DiscardNegotiationStateAfterSessionEnds()
        {
            var epochNow = RitsuLibSidecarSessionManager.Epoch;
            lock (ExchangeGate)
            {
                NegotiationByPeer.Clear();
                _negotiationAlignedEpoch = epochNow;
            }
        }

        /// <summary>
        ///     Expires outbound handshake ack waits when the transport delivers no ack before the deadline.
        /// </summary>
        public static void TickHandshakeNegotiation()
        {
            EnsureExchangeEpochAligned();
            var now = Environment.TickCount64;
            ulong[] peers;
            lock (ExchangeGate)
            {
                peers = [..NegotiationByPeer.Keys];
            }

            foreach (var peerNetId in peers)
                TryProcessAckTimeout(peerNetId, now);
        }

        /// <summary>
        ///     Same as <see cref="TrySendClientHelloIfReachable" /> using <see cref="RunManager.Instance" />’s
        ///     <see cref="RunManager.NetService" /> (non-null only after run setup; use lobby ctor patches for
        ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby" /> phase).
        /// </summary>
        public static void TrySendLocalClientHello()
        {
            TrySendClientHelloIfReachable(RunManager.Instance?.NetService);
        }

        /// <summary>
        ///     Attempts sidecar handshake only for peers already resolved as
        ///     <see cref="RitsuLibSidecarPeerReachability.Supported" />.
        /// </summary>
        public static void TrySendClientHelloIfReachable(INetGameService? netService)
        {
            if (netService == null || netService.Type == NetGameType.Singleplayer)
                return;

            EnsureExchangeEpochAligned();

            switch (netService)
            {
                case NetClientGameService client:
                    TrySendHelloToPeerIfReachable(netService, client.HostNetId);
                    break;
                case NetHostGameService:
                    foreach (var peerNetId in RitsuLibSidecarSessionManager.GetSupportedPeersSnapshot())
                        TrySendHelloToPeerIfReachable(netService, peerNetId);
                    break;
            }
        }

        private static void EnsureExchangeEpochAligned()
        {
            var epochNow = RitsuLibSidecarSessionManager.Epoch;
            if (_negotiationAlignedEpoch == epochNow)
                return;

            lock (ExchangeGate)
            {
                NegotiationByPeer.Clear();
            }

            _negotiationAlignedEpoch = epochNow;
        }

        private static void TryProcessAckTimeout(ulong peerNetId, long nowTickCount64)
        {
            var signalAckTimeoutBudgetExhausted = false;
            lock (ExchangeGate)
            {
                if (!NegotiationByPeer.TryGetValue(peerNetId, out var state))
                    return;

                var epochNow = RitsuLibSidecarSessionManager.Epoch;
                if (state.SessionEpoch != epochNow || state.Phase != NegotiationOutboundPhase.AwaitingAck)
                    return;

                if (nowTickCount64 < state.AckDeadlineTickCount64)
                    return;

                if (state.PacketsConsumed >= HelloMaxPacketAttempts)
                {
                    NegotiationByPeer.Remove(peerNetId);
                    signalAckTimeoutBudgetExhausted = true;
                }
                else
                {
                    state.Phase = NegotiationOutboundPhase.Idle;
                    state.NextTransportAttemptTickCount64 =
                        nowTickCount64 + HelloAckTimeoutRetryDeferMilliseconds;
                    NegotiationByPeer[peerNetId] = state;
                }
            }

            if (!signalAckTimeoutBudgetExhausted)
                return;

            RitsuLibSidecarSessionManager.NoteHandshakeNegotiationAborted(peerNetId,
                RitsuLibSidecarDiscoveryPolicy.ReasonHandshakeAckTimeout);
            RitsuLibFramework.Logger.Warn(
                $"[Sidecar] Handshake abandoned after repeated ack timeouts (packet budget exhausted) peer={peerNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}");
        }

        private static void TrySendHelloToPeerIfReachable(INetGameService netService, ulong peerNetId)
        {
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId))
                return;

            var epochNow = RitsuLibSidecarSessionManager.Epoch;
            var now = Environment.TickCount64;

            bool logDispatchQueued;
            lock (ExchangeGate)
            {
                GetOrResetPeerState(epochNow, peerNetId, out var state);

                if (state.Phase == NegotiationOutboundPhase.AwaitingAck)
                    return;

                if (state.PacketsConsumed >= HelloMaxPacketAttempts)
                    return;

                if (now < state.NextTransportAttemptTickCount64)
                    return;

                logDispatchQueued = state.PacketsConsumed == 0;
                NegotiationByPeer[peerNetId] = state;
            }

            if (logDispatchQueued)
                RitsuLibFramework.Logger.Info(
                    $"[Sidecar] Handshake queued peer={peerNetId}, epoch={epochNow}, netType={netService.Type}");

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);

            var sent = netService switch
            {
                NetClientGameService => RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    netService,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync),
                NetHostGameService => RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    netService,
                    peerNetId,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync),
                _ => false,
            };

            var signalTransportBudgetExhausted = false;
            var signalFirstTransportFailureLog = false;
            var signalWireHandshakeSentLog = false;
            lock (ExchangeGate)
            {
                if (!NegotiationByPeer.TryGetValue(peerNetId, out var state) || state.SessionEpoch != epochNow)
                    return;

                state.PacketsConsumed++;

                if (!sent)
                {
                    var nextBackoffMs = state.TransportBackoffMilliseconds == 0
                        ? HelloInitialBackoffMilliseconds
                        : state.TransportBackoffMilliseconds * 2;
                    if (nextBackoffMs > HelloMaxBackoffMilliseconds)
                        nextBackoffMs = HelloMaxBackoffMilliseconds;

                    state.TransportBackoffMilliseconds = nextBackoffMs;
                    state.NextTransportAttemptTickCount64 = now + nextBackoffMs;
                    state.Phase = NegotiationOutboundPhase.Idle;

                    if (state.PacketsConsumed >= HelloMaxPacketAttempts)
                    {
                        NegotiationByPeer.Remove(peerNetId);
                        signalTransportBudgetExhausted = true;
                    }
                    else
                    {
                        if (!state.LoggedFirstTransportFailureWarn)
                        {
                            state.LoggedFirstTransportFailureWarn = true;
                            signalFirstTransportFailureLog = true;
                        }

                        NegotiationByPeer[peerNetId] = state;
                    }
                }
                else
                {
                    state.Phase = NegotiationOutboundPhase.AwaitingAck;
                    state.AckDeadlineTickCount64 = now + HelloAckTimeoutMilliseconds;
                    state.TransportBackoffMilliseconds = 0;
                    NegotiationByPeer[peerNetId] = state;
                    signalWireHandshakeSentLog = true;
                }
            }

            if (signalTransportBudgetExhausted)
            {
                RitsuLibSidecarSessionManager.NoteHandshakeNegotiationAborted(peerNetId,
                    RitsuLibSidecarDiscoveryPolicy.ReasonHandshakeTransportBudget);
                RitsuLibFramework.Logger.Warn(
                    $"[Sidecar] Handshake abandoned: transport send budget exhausted peer={peerNetId}, epoch={epochNow}, netType={netService.Type}");
                return;
            }

            if (signalFirstTransportFailureLog)
                RitsuLibFramework.Logger.Warn(
                    $"[Sidecar] Handshake send failed peer={peerNetId}, epoch={epochNow}, netType={netService.Type}; retrying with backoff (no further transport-failure logs until negotiation ends)");

            if (signalWireHandshakeSentLog)
                RitsuLibFramework.Logger.Info(
                    $"[Sidecar] Handshake sent peer={peerNetId}, epoch={epochNow}, netType={netService.Type}, opcode={RitsuLibSidecarControlOpcodes.Handshake}, payloadLen={buf.Length}");
        }

        private static void GetOrResetPeerState(long epochNow, ulong peerNetId, out HelloOutboundNegotiationState state)
        {
            if (!NegotiationByPeer.TryGetValue(peerNetId, out state) || state.SessionEpoch != epochNow)
                state = new()
                {
                    SessionEpoch = epochNow,
                    Phase = NegotiationOutboundPhase.Idle,
                    PacketsConsumed = 0,
                    NextTransportAttemptTickCount64 = 0,
                    TransportBackoffMilliseconds = 0,
                    AckDeadlineTickCount64 = 0,
                    LoggedFirstTransportFailureWarn = false,
                };
        }

        private enum NegotiationOutboundPhase : byte
        {
            Idle = 0,
            AwaitingAck = 1,
        }

        private struct HelloOutboundNegotiationState
        {
            internal long SessionEpoch;

            internal NegotiationOutboundPhase Phase;

            internal int PacketsConsumed;

            internal long NextTransportAttemptTickCount64;

            internal int TransportBackoffMilliseconds;

            internal long AckDeadlineTickCount64;

            internal bool LoggedFirstTransportFailureWarn;
        }
    }
}
