using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Initiates a best-effort capability exchange over <see cref="RitsuLibSidecarControlOpcodes.Handshake" />.</summary>
    public static class RitsuLibSidecarConnectionExchange
    {
        /// <summary>
        ///     When <see cref="RunManager.Instance" /> has a non-singleplayer <see cref="RunManager.NetService" />,
        ///     sends a <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> with the current wire version and
        ///     <see cref="RitsuLibSidecarPeerFeatures.ChunkedStreams" /> (host broadcast, client to host). No-op if
        ///     <c>RunManager</c> is missing, <c>NetService</c> is null, or the session is single-player.
        /// </summary>
        public static void TrySendLocalHello()
        {
            var rm = RunManager.Instance;
            if (rm?.NetService is not { Type: not NetGameType.Singleplayer })
                return;

            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var buf = new byte[RitsuLibSidecarHandshakeBinary.HandshakePayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteHandshake(
                buf.AsSpan(),
                RitsuLibSidecarWire.CurrentWireFormatVersion,
                RitsuLibSidecarWire.SupportedWireFormatVersionMax,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);
            if (rm.NetService is NetHostGameService)
                RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                    rm,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
            else
                RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    rm,
                    RitsuLibSidecarControlOpcodes.Handshake,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
        }
    }
}
