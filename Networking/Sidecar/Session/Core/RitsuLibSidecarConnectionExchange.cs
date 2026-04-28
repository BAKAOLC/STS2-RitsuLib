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
        ///     Client-side handshake bootstrap. Host-side proactive broadcast is intentionally disabled to avoid sending
        ///     sidecar envelopes to unknown peers.
        /// </summary>
        public static void TrySendClientHelloIfReachable(INetGameService? netService)
        {
            if (netService == null || netService.Type == NetGameType.Singleplayer)
                return;
            if (netService is not NetClientGameService client)
                return;
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(client.HostNetId))
            {
                if (!RitsuLibSidecarNetDiagnosticsOptions.TraceSessionState) return;
                var known = RitsuLibSidecarSessionManager.TryGetReachability(client.HostNetId, out var r)
                    ? r
                    : RitsuLibSidecarPeerReachability.Unknown;
                RitsuLibFramework.Logger.Info(
                    $"[Sidecar] Skip client handshake to host={client.HostNetId}, reachability={known}");

                return;
            }

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);
            RitsuLibSidecarHighLevelSend.TrySendAsClient(
                netService,
                RitsuLibSidecarControlOpcodes.Handshake,
                buf,
                RitsuLibSidecarDeliverySemantics.StableSync);
        }
    }
}
