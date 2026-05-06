using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarNativeTrailerValidationRoute : IRitsuLibSidecarCapabilityValidationRoute
    {
        public string Name => RitsuLibSidecarDiscoveryPolicy.RouteNameNativeTrailer;
        public int Order => RitsuLibSidecarDiscoveryPolicy.RouteOrderNativeTrailer;

        public bool IsAvailable(INetGameService netService)
        {
            return true;
        }

        public void PublishLocalEvidence(INetGameService netService)
        {
        }

        public RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId)
        {
            return RitsuLibSidecarNativeTrailerEvidence.HasCurrentSessionEvidence(peerNetId)
                ? RitsuLibSidecarPeerReachability.Supported
                : null;
        }
    }
}
