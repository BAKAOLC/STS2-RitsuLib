using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
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

        /// <summary>Client sends one envelope to the host.</summary>
        public static bool TrySendToHost(
            RunManager? runManager,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            return TrySendToHost(runManager?.NetService, envelope, mode, channel);
        }

        /// <summary>
        ///     Client sends one envelope to the host using an existing <see cref="INetGameService" /> (e.g. lobby
        ///     before <see cref="RunManager" /> has <see cref="RunManager.NetService" /> assigned).
        /// </summary>
        public static bool TrySendToHost(
            INetGameService? netService,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (netService is not NetClientGameService { IsConnected: true } client ||
                client.NetClient == null)
                return false;

            client.NetClient.SendMessageToHost(envelope, envelope.Length, mode, channel);
            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.TraceOutbound("client->host", envelope, mode, channel);
            return true;
        }

        /// <summary>Host sends one envelope to a single peer.</summary>
        public static bool TrySendToPeer(
            RunManager? runManager,
            ulong peerNetId,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            return TrySendToPeer(runManager?.NetService, peerNetId, envelope, mode, channel);
        }

        /// <inheritdoc cref="TrySendToPeer(RunManager?, ulong, byte[], NetTransferMode, int)" />
        public static bool TrySendToPeer(
            INetGameService? netService,
            ulong peerNetId,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (netService is not NetHostGameService { IsConnected: true } host || host.NetHost == null)
                return false;

            host.NetHost.SendMessageToClient(peerNetId, envelope, envelope.Length, mode, channel);
            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.TraceOutbound("host->peer", envelope, mode, channel, peerNetId);
            return true;
        }

        /// <summary>Host broadcasts to every peer that is ready for vanilla-style broadcast replication.</summary>
        public static bool TryBroadcastToReadyPeers(
            RunManager? runManager,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            return TryBroadcastToReadyPeers(runManager?.NetService, envelope, mode, channel);
        }

        /// <inheritdoc cref="TryBroadcastToReadyPeers(RunManager?, byte[], NetTransferMode, int)" />
        public static bool TryBroadcastToReadyPeers(
            INetGameService? netService,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (netService is not NetHostGameService { IsConnected: true } host || host.NetHost == null)
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

            if (ops <= 0)
                return true;

            RitsuLibSidecarTrafficCounters.AddOutgoing(ops, bytes);
            RitsuLibSidecarNetTrace.TraceOutbound(
                "host->broadcast",
                envelope,
                mode,
                channel,
                broadcastPeerCount: ops);

            return true;
        }

        /// <summary>
        ///     Host sends the same raw envelope to every <see cref="NetHostGameService.ConnectedPeers" /> entry, without
        ///     requiring <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetClientData.readyForBroadcasting" />. Use in lobby
        ///     (or any phase before vanilla marks peers ready) when ready-only broadcast would skip every peer.
        /// </summary>
        public static bool TryBroadcastToAllConnectedClients(
            INetGameService? netService,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (netService is not NetHostGameService { IsConnected: true } host || host.NetHost == null)
                return false;

            var ops = 0;
            var bytes = 0L;
            foreach (var peer in host.ConnectedPeers)
            {
                host.NetHost.SendMessageToClient(peer.peerId, envelope, envelope.Length, mode, channel);
                ops++;
                bytes += envelope.Length;
            }

            if (ops <= 0)
                return true;

            RitsuLibSidecarTrafficCounters.AddOutgoing(ops, bytes);
            RitsuLibSidecarNetTrace.TraceOutbound(
                "host->all_connected",
                envelope,
                mode,
                channel,
                broadcastPeerCount: ops);
            return true;
        }
    }
}
