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
        private static readonly Lock Gate = new();
        private static readonly Dictionary<ulong, long> HelloSentEpochByPeer = [];

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

        private static void TrySendHelloToPeerIfReachable(INetGameService netService, ulong peerNetId)
        {
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId))
                return;
            if (!TryMarkHelloAsPending(peerNetId))
                return;

            RitsuLibFramework.Logger.Info(
                $"[Sidecar] Handshake queued peer={peerNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}, netType={netService.Type}");

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);

            var ok = netService switch
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

            if (ok)
            {
                RitsuLibFramework.Logger.Info(
                    $"[Sidecar] Handshake sent peer={peerNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}, netType={netService.Type}, opcode={RitsuLibSidecarControlOpcodes.Handshake}, payloadLen={buf.Length}");
                return;
            }

            UnmarkHelloPending(peerNetId);
            RitsuLibFramework.Logger.Warn(
                $"[Sidecar] Handshake send failed peer={peerNetId}, epoch={RitsuLibSidecarSessionManager.Epoch}, netType={netService.Type}");
        }

        private static bool TryMarkHelloAsPending(ulong peerNetId)
        {
            var epoch = RitsuLibSidecarSessionManager.Epoch;
            lock (Gate)
            {
                if (HelloSentEpochByPeer.TryGetValue(peerNetId, out var sentEpoch) && sentEpoch == epoch)
                    return false;
                HelloSentEpochByPeer[peerNetId] = epoch;
                return true;
            }
        }

        private static void UnmarkHelloPending(ulong peerNetId)
        {
            var epoch = RitsuLibSidecarSessionManager.Epoch;
            lock (Gate)
            {
                if (HelloSentEpochByPeer.TryGetValue(peerNetId, out var sentEpoch) && sentEpoch == epoch)
                    HelloSentEpochByPeer.Remove(peerNetId);
            }
        }
    }
}
