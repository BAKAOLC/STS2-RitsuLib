using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Specifies whether a remote peer can receive sidecar traffic.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         指定远程对等端能否接收 sidecar 流量。
    ///     </para>
    /// </summary>
    public enum RitsuLibSidecarPeerReachability
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         No capability verdict is available yet.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尚无可用的功能判定。
        ///     </para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The peer is confirmed to support sidecar traffic.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已确认该对等端支持 sidecar 流量。
        ///     </para>
        /// </summary>
        Supported = 1,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The peer is confirmed incompatible and must not receive sidecar packets.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         已确认该对等端不兼容，不得向其发送 sidecar 数据包。
        ///     </para>
        /// </summary>
        Unsupported = 2,
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a sidecar session binding to a multiplayer <see cref="INetGameService" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述 sidecar 会话与多人 <see cref="INetGameService" /> 的绑定。
    ///     </para>
    /// </summary>
    public readonly record struct SidecarSessionBoundEvent(INetGameService NetService, long Epoch);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a sidecar session becoming unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述 sidecar 会话解除绑定。
    ///     </para>
    /// </summary>
    public readonly record struct SidecarSessionUnboundEvent(long Epoch);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a change in peer reachability.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述对等端可达性发生变化。
    ///     </para>
    /// </summary>
    public readonly record struct SidecarPeerReachabilityChangedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerReachability Previous,
        RitsuLibSidecarPeerReachability Current,
        string Reason,
        long Epoch);

    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes an accepted sidecar handshake from a peer.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述已接受的对等端 sidecar 握手。
    ///     </para>
    /// </summary>
    public readonly record struct SidecarHandshakeCompletedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerFeatures Features,
        long Epoch);
}
