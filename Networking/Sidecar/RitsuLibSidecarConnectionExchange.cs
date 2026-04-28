using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>Initiates a best-effort capability exchange over <see cref="RitsuLibSidecarControlOpcodes.Handshake" />.</summary>
    public static class RitsuLibSidecarConnectionExchange
    {
        /// <summary>
        ///     If a <c>RunManager</c> with <c>NetService</c> exists, sends a
        ///     <see cref="RitsuLibSidecarControlOpcodes.Handshake" /> with the current wire version and
        ///     <see cref="RitsuLibSidecarPeerFeatures.ChunkedStreams" />; host uses broadcast, client uses
        ///     <see cref="RitsuLibSidecarHighLevelSend.TrySendAsClient" />. No-op if not in a run.
        /// </summary>
        public static void TrySendLocalHello()
        {
            var rm = RunManager.Instance;
            if (rm?.NetService == null)
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
