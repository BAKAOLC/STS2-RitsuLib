using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Manages sidecar capability negotiation through
    ///         <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> messages.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> 消息管理 sidecar 功能协商。
    ///     </para>
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
        ///     <para xml:lang="en">
        ///         State is cleared when <see cref="RitsuLibSidecarSessionManager.Epoch" /> changes. Successful
        ///         negotiation remains complete until disconnection or the next epoch.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <see cref="RitsuLibSidecarSessionManager.Epoch" /> 变化时会清除状态。协商成功后，该对等端在断开连接
        ///         或进入下一纪元前保持完成状态。
        ///     </para>
        /// </remarks>
        private static readonly Dictionary<ulong, HelloOutboundNegotiationState> NegotiationByPeer = [];

        private static long _negotiationAlignedEpoch;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes outbound handshake pacing and acknowledgement state for a peer.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除指定对等端的出站握手节流和确认状态。
        ///     </para>
        /// </summary>
        public static void RemoveNegotiationStateForPeer(ulong peerNetId)
        {
            lock (ExchangeGate)
            {
                NegotiationByPeer.Remove(peerNetId);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Applies a received handshake acknowledgement to the matching outbound negotiation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将收到的握手确认应用到对应的出站协商。
        ///     </para>
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

                if (!negotiationOk)
                {
                    NegotiationByPeer.Remove(responderNetId);
                    return;
                }

                state.Phase = NegotiationOutboundPhase.Completed;
                NegotiationByPeer[responderNetId] = state;
            }

            RitsuLibFramework.Logger.Debug(
                $"[Sidecar] Handshake negotiation completed peer={responderNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes all outbound handshake state after a multiplayer session ends.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         多人会话结束后移除所有出站握手状态。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Processes acknowledgement timeouts for pending outbound handshakes.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         处理待定出站握手的确认超时。
        ///     </para>
        /// </summary>
        public static void TickHandshakeNegotiation()
        {
            EnsureExchangeEpochAligned();
            var now = Environment.TickCount64;
            ulong[] peers;
            lock (ExchangeGate)
            {
                var hasPendingAcknowledgement = false;
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var state in NegotiationByPeer.Values)
                {
                    if (state.Phase != NegotiationOutboundPhase.AwaitingAck)
                        continue;

                    hasPendingAcknowledgement = true;
                    break;
                }

                if (!hasPendingAcknowledgement)
                    return;

                peers = [.. NegotiationByPeer.Keys];
            }

            foreach (var peerNetId in peers)
                TryProcessAckTimeout(peerNetId, now);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts a client handshake using the active observed Sidecar service, falling back to the service from
        ///         <see cref="RunManager.Instance" /> when no service has been observed yet.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用当前观察到的 Sidecar 服务尝试客户端握手；尚未观察到服务时，回退使用
        ///         <see cref="RunManager.Instance" /> 中的服务。
        ///     </para>
        /// </summary>
        public static void TrySendLocalClientHello()
        {
            TrySendClientHelloIfReachable(
                RitsuLibSidecarSessionManager.CurrentNetService ?? RunManager.Instance?.NetService);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts a sidecar handshake with peers already marked
        ///         <see cref="RitsuLibSidecarPeerReachability.Supported" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         仅对已标记为 <see cref="RitsuLibSidecarPeerReachability.Supported" /> 的对等端尝试 sidecar 握手。
        ///     </para>
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
                    foreach (var peerNetId in RitsuLibSidecarSessionManager.GetSupportedPeersForIteration())
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

                switch (state.Phase)
                {
                    case NegotiationOutboundPhase.AwaitingAck:
                    case NegotiationOutboundPhase.Completed:
                        return;
                }

                if (state.PacketsConsumed >= HelloMaxPacketAttempts)
                    return;

                if (now < state.NextTransportAttemptTickCount64)
                    return;

                logDispatchQueued = state.PacketsConsumed == 0;
                NegotiationByPeer[peerNetId] = state;
            }

            if (logDispatchQueued)
                RitsuLibFramework.Logger.Debug(
                    $"[Sidecar] Handshake queued peer={peerNetId}, epoch={epochNow}, netType={netService.Type}");

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarSupportedFeatures.All);

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
                RitsuLibFramework.Logger.Debug(
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
            Completed = 2,
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
