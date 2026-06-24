using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarControlPeerSend
    {
        internal static void SendToNetPeer(
            RunManager? runManager,
            ulong destinationNetId,
            ulong opcode,
            ReadOnlyMemory<byte> payload,
            RitsuLibSidecarDeliverySemantics semantics = RitsuLibSidecarDeliverySemantics.StableSync)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            switch (runManager?.NetService)
            {
                case NetHostGameService:
                    RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                        runManager,
                        destinationNetId,
                        opcode,
                        payload.Span,
                        semantics);
                    return;
                case NetClientGameService c when destinationNetId != c.HostNetId:
                    return;
                case NetClientGameService:
                    RitsuLibSidecarHighLevelSend.TrySendAsClient(
                        runManager,
                        opcode,
                        payload.Span,
                        semantics);
                    break;
            }
        }
    }
}
