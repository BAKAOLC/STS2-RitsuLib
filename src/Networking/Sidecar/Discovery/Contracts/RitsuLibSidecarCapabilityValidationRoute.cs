using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     One validation route in the unified sidecar reachability-discovery flow.
    ///     统一 sidecar 可达性发现流程中的一条验证路由。
    /// </summary>
    public interface IRitsuLibSidecarCapabilityValidationRoute
    {
        /// <summary>
        ///     Route name for diagnostics.
        ///     用于诊断的路由名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Lower value executes earlier in the validation flow.
        ///     值越低，在验证流程中越早执行。
        /// </summary>
        int Order { get; }

        /// <summary>
        ///     Returns true when this route can operate for the current net service.
        ///     当此路由可为当前 net service 工作时返回 true。
        /// </summary>
        bool IsAvailable(INetGameService netService);

        /// <summary>
        ///     Publishes local out-of-band evidence, if required by this route.
        ///     如该路由需要，则发布本地带外证据。
        /// </summary>
        void PublishLocalEvidence(INetGameService netService);

        /// <summary>
        ///     Resolves one peer reachability verdict; returns null when this route has no verdict.
        ///     解析一个对等端可达性判定；当此路由没有判定时返回 null。
        /// </summary>
        RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId);
    }
}
