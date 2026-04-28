using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Reachability state for a remote peer from the sidecar sender's point of view.
    /// </summary>
    public enum RitsuLibSidecarPeerReachability
    {
        /// <summary>
        ///     No safe capability verdict yet.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Peer is confirmed to support sidecar traffic.
        /// </summary>
        Supported = 1,

        /// <summary>
        ///     Peer is confirmed incompatible and must not receive sidecar packets.
        /// </summary>
        Unsupported = 2,
    }

    /// <summary>
    ///     Raised when sidecar session binds to a multiplayer <see cref="INetGameService" />.
    /// </summary>
    public readonly record struct SidecarSessionBoundEvent(INetGameService NetService, long Epoch);

    /// <summary>
    ///     Raised when sidecar session becomes unbound.
    /// </summary>
    public readonly record struct SidecarSessionUnboundEvent(long Epoch);

    /// <summary>
    ///     Raised when a peer reachability state transitions.
    /// </summary>
    public readonly record struct SidecarPeerReachabilityChangedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerReachability Previous,
        RitsuLibSidecarPeerReachability Current,
        string Reason,
        long Epoch);

    /// <summary>
    ///     Raised when handshake metadata for a peer is accepted.
    /// </summary>
    public readonly record struct SidecarHandshakeCompletedEvent(
        ulong PeerNetId,
        RitsuLibSidecarPeerFeatures Features,
        long Epoch);
}
