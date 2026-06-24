using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Reachability state for a remote peer from the sidecar sender's point of view.
    ///     从 sidecar 发送方视角看的远程 peer 可达性状态。
    /// </summary>
    public enum RitsuLibSidecarPeerReachability
    {
        /// <summary>
        ///     No safe capability verdict yet.
        ///     尚无安全的能力判定。
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Peer is confirmed to support sidecar traffic.
        ///     已确认 peer 支持 sidecar 流量。
        /// </summary>
        Supported = 1,

        /// <summary>
        ///     Peer is confirmed incompatible and must not receive sidecar packets.
        ///     已确认 peer 不兼容，且不得接收 sidecar 数据包。
        /// </summary>
        Unsupported = 2,
    }

    /// <summary>
    ///     Raised when sidecar session binds to a multiplayer <see cref="INetGameService" />.
    ///     当 sidecar 会话绑定到多人 <see cref="INetGameService" /> 时引发。
    /// </summary>
    public readonly record struct SidecarSessionBoundEvent(INetGameService NetService, long Epoch);

    /// <summary>
    ///     Raised when sidecar session becomes unbound.
    ///     当 sidecar 会话变为未绑定时引发。
    /// </summary>
    public readonly record struct SidecarSessionUnboundEvent(long Epoch);

    /// <summary>
    ///     Raised when a peer reachability state transitions.
    ///     peer 可达性状态转换时引发。
    /// </summary>
    public readonly record struct SidecarPeerReachabilityChangedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerReachability Previous,
        RitsuLibSidecarPeerReachability Current,
        string Reason,
        long Epoch);

    /// <summary>
    ///     Raised when handshake metadata for a peer is accepted.
    ///     接受某个 peer 的握手元数据时引发。
    /// </summary>
    public readonly record struct SidecarHandshakeCompletedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerFeatures Features,
        long Epoch);
}
