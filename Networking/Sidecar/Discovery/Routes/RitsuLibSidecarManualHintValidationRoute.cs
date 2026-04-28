using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal sealed class RitsuLibSidecarManualHintValidationRoute : IRitsuLibSidecarCapabilityValidationRoute
    {
        public string Name => RitsuLibSidecarDiscoveryPolicy.RouteNameManualHint;
        public int Order => RitsuLibSidecarDiscoveryPolicy.RouteOrderManualHint;

        public bool IsAvailable(INetGameService netService)
        {
            return true;
        }

        public void PublishLocalEvidence(INetGameService netService)
        {
        }

        public RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId)
        {
            return RitsuLibSidecarCapabilityHints.TryGetHint(peerNetId);
        }
    }
}
