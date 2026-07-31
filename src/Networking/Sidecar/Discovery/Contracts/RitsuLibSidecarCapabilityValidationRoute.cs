using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a validation route used to determine whether peers are reachable through the sidecar protocol.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示用于判断能否通过 sidecar 协议连接对等端的验证路由。
    ///     </para>
    /// </summary>
    public interface IRitsuLibSidecarCapabilityValidationRoute
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the route name used in diagnostics.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取诊断中使用的路由名称。
        ///     </para>
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the route order. Routes with lower values run earlier.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取路由顺序；值越小，执行越早。
        ///     </para>
        /// </summary>
        int Order { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when the route can use the specified network service.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当该路由可使用指定网络服务时返回 <see langword="true" />。
        ///     </para>
        /// </summary>
        bool IsAvailable(INetGameService netService);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Publishes local out-of-band evidence when required by the route.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在该路由需要时发布本地带外证据。
        ///     </para>
        /// </summary>
        void PublishLocalEvidence(INetGameService netService);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Resolves the reachability of a peer, or returns <see langword="null" /> when the route cannot decide.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         判断对等端是否可达；当该路由无法作出判断时返回 <see langword="null" />。
        ///     </para>
        /// </summary>
        RitsuLibSidecarPeerReachability? TryResolve(INetGameService netService, ulong peerNetId);
    }
}
