using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sends raw sidecar envelopes on the vanilla transport without using the game INetMessage serialization path.
    /// </summary>
    public static class RitsuLibSidecarSend
    {
        /// <summary>
        ///     Maps <see cref="NetTransferMode" /> to a recommended ENet channel distinct from vanilla 0/1.
        /// </summary>
        /// <param name="mode">Reliable or unreliable send mode.</param>
        public static int RecommendedChannel(NetTransferMode mode)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return mode switch
            {
                NetTransferMode.Reliable => RitsuLibSidecarWire.RecommendedReliableChannel,
                NetTransferMode.Unreliable => RitsuLibSidecarWire.RecommendedUnreliableChannel,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }

        /// <summary>
        ///     Client sends one envelope to the host.
        /// </summary>
        /// <param name="runManager">Current run; needs a connected client service to send.</param>
        /// <param name="envelope">Full on-wire sidecar bytes (magic through payload).</param>
        /// <param name="mode">Transfer mode for the vanilla send API.</param>
        /// <param name="channel">ENet channel for the vanilla send API.</param>
        public static bool TrySendToHost(
            RunManager? runManager,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (runManager?.NetService is not NetClientGameService { IsConnected: true } client ||
                client.NetClient == null)
                return false;

            client.NetClient.SendMessageToHost(envelope, envelope.Length, mode, channel);
            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.DebugOutbound("client->host", envelope, mode, channel);
            return true;
        }

        /// <summary>
        ///     Host sends one envelope to a single peer.
        /// </summary>
        /// <param name="runManager">Current run; needs a connected host service to send.</param>
        /// <param name="peerNetId">Target client id for the vanilla send API.</param>
        /// <param name="envelope">Full on-wire sidecar bytes (magic through payload).</param>
        /// <param name="mode">Transfer mode for the vanilla send API.</param>
        /// <param name="channel">ENet channel for the vanilla send API.</param>
        public static bool TrySendToPeer(
            RunManager? runManager,
            ulong peerNetId,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (runManager?.NetService is not NetHostGameService { IsConnected: true } host || host.NetHost == null)
                return false;

            host.NetHost.SendMessageToClient(peerNetId, envelope, envelope.Length, mode, channel);
            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.DebugOutbound("host->peer", envelope, mode, channel, peerNetId);
            return true;
        }

        /// <summary>
        ///     Host broadcasts to every peer that is ready for vanilla-style broadcast replication.
        /// </summary>
        /// <param name="runManager">Current run; needs a connected host service to send.</param>
        /// <param name="envelope">Full on-wire sidecar bytes (magic through payload).</param>
        /// <param name="mode">Transfer mode for the vanilla send API.</param>
        /// <param name="channel">ENet channel for the vanilla send API.</param>
        public static bool TryBroadcastToReadyPeers(
            RunManager? runManager,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (runManager?.NetService is not NetHostGameService { IsConnected: true } host || host.NetHost == null)
                return false;

            var ops = 0;
            var bytes = 0L;
            foreach (var peer in host.ConnectedPeers)
            {
                if (!peer.readyForBroadcasting)
                    continue;

                host.NetHost.SendMessageToClient(peer.peerId, envelope, envelope.Length, mode, channel);
                ops++;
                bytes += envelope.Length;
            }

            if (ops <= 0) return true;
            RitsuLibSidecarTrafficCounters.AddOutgoing(ops, bytes);
            RitsuLibSidecarNetTrace.DebugOutbound(
                "host->broadcast",
                envelope,
                mode,
                channel,
                broadcastPeerCount: ops);

            return true;
        }
    }
}
