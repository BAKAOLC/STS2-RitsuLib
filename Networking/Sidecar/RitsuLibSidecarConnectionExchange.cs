using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Initiates a best-effort capability exchange over <see cref="RitsuLibSidecarControlOpcodes.Handshake" />.</summary>
    public static class RitsuLibSidecarConnectionExchange
    {
        /// <summary>
        ///     Same as <see cref="TrySendHelloForNetService" /> using <see cref="RunManager.Instance" />’s
        ///     <see cref="RunManager.NetService" /> (non-null only after run setup; use lobby ctor patches for
        ///     <see cref="MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby" /> phase).
        /// </summary>
        public static void TrySendLocalHello()
        {
            TrySendHelloForNetService(RunManager.Instance?.NetService);
        }

        /// <summary>
        ///     When <paramref name="netService" /> is non-null and not <see cref="NetGameType.Singleplayer" />, sends a
        ///     <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> (host broadcast, client to host).
        /// </summary>
        public static void TrySendHelloForNetService(INetGameService? netService)
        {
            if (netService == null || netService.Type == NetGameType.Singleplayer)
                return;

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);
            if (netService is NetHostGameService)
                RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                    netService,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
            else
                RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    netService,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
        }
    }
}
