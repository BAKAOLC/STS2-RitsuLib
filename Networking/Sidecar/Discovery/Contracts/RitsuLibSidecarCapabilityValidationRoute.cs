using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     One validation route in the unified sidecar reachability-discovery flow.
    /// </summary>
    public interface IRitsuLibSidecarCapabilityValidationRoute
    {
        /// <summary>
        ///     Route name for diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Lower value executes earlier in the validation flow.
        /// </summary>
        int Order { get; }

        /// <summary>
        ///     Returns true when this route can operate for the current net service.
        /// </summary>
        bool IsAvailable(INetGameService netService);

        /// <summary>
        ///     Publishes local out-of-band evidence, if required by this route.
        /// </summary>
        void PublishLocalEvidence(INetGameService netService);

        /// <summary>
        ///     Resolves one peer reachability verdict; returns null when this route has no verdict.
        /// </summary>
        RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId);
    }
}
