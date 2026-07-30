using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">Sends raw Sidecar envelopes over the vanilla transport without the game's <c>INetMessage</c> serialization path.</para>
    ///     <para xml:lang="zh-CN">在不经过游戏 <c>INetMessage</c> 序列化路径的情况下，通过原版传输发送原始 Sidecar 信封。</para>
    /// </summary>
    public static class RitsuLibSidecarSend
    {
        /// <summary>
        ///     <para xml:lang="en">Maps <see cref="NetTransferMode" /> to a recommended ENet channel distinct from vanilla channels 0 and 1.</para>
        ///     <para xml:lang="zh-CN">将 <see cref="NetTransferMode" /> 映射到不同于原版通道 0 和 1 的推荐 ENet 通道。</para>
        /// </summary>
        /// <param name="mode">
        ///     <para xml:lang="en">Reliable or unreliable send mode.</para>
        ///     <para xml:lang="zh-CN">可靠或不可靠发送模式。</para>
        /// </param>
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
        ///     <para xml:lang="en">Sends one envelope from a client to its host.</para>
        ///     <para xml:lang="zh-CN">从客户端向其主机发送一个信封。</para>
        /// </summary>
        public static bool TrySendToHost(
            RunManager? runManager,
            byte[] envelope,
            NetTransferMode mode,
            int channel)
        {
            return TrySendToHost(runManager?.NetService, envelope, mode, channel);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Sends one envelope from a client to its host through an existing <see cref="INetGameService" />,
        ///         including during a lobby before <see cref="RunManager.NetService" /> is assigned.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过现有 <see cref="INetGameService" /> 从客户端向其主机发送一个信封，包括尚未分配
        ///         <see cref="RunManager.NetService" /> 的大厅阶段。
        ///     </para>
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
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(client.HostNetId))
            {
                RitsuLibSidecarNetTrace.TraceSkippedSend(
                    RitsuLibSidecarTransportTracePaths.ClientToHost,
                    client.HostNetId,
                    RitsuLibSidecarSessionManager.TryGetReachability(client.HostNetId, out var r)
                        ? r
                        : RitsuLibSidecarPeerReachability.Unknown);
                return false;
            }

            try
            {
                client.NetClient.SendMessageToHost(envelope, envelope.Length, mode, channel);
            }
            catch (InvalidOperationException)
            {
                RitsuLibSidecarSessionManager.NoteTransportConnectionMissing(client.HostNetId);
                return false;
            }

            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.TraceOutbound(RitsuLibSidecarTransportTracePaths.ClientToHost, envelope, mode,
                channel);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Sends one envelope from a host to a single peer.</para>
        ///     <para xml:lang="zh-CN">从主机向单个对等方发送一个信封。</para>
        /// </summary>
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
            if (!RitsuLibSidecarSessionManager.CanSendToPeer(peerNetId))
            {
                RitsuLibSidecarNetTrace.TraceSkippedSend(
                    RitsuLibSidecarTransportTracePaths.HostToPeer,
                    peerNetId,
                    RitsuLibSidecarSessionManager.TryGetReachability(peerNetId, out var r)
                        ? r
                        : RitsuLibSidecarPeerReachability.Unknown);
                return false;
            }

            try
            {
                host.NetHost.SendMessageToClient(peerNetId, envelope, envelope.Length, mode, channel);
            }
            catch (InvalidOperationException)
            {
                RitsuLibSidecarSessionManager.NoteTransportConnectionMissing(peerNetId);
                return false;
            }

            RitsuLibSidecarTrafficCounters.AddOutgoing(1, envelope.Length);
            RitsuLibSidecarNetTrace.TraceOutbound(
                RitsuLibSidecarTransportTracePaths.HostToPeer,
                envelope,
                mode,
                channel,
                peerNetId);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to send an envelope from a host to every peer ready for vanilla-style broadcast replication.
        ///         For a valid host transport, it returns <see langword="true" /> even when no eligible peer receives
        ///         the envelope or every eligible send fails with <see cref="InvalidOperationException" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试从主机向每个已准备好原版式广播复制的对等方发送信封。对于有效的主机传输，即使没有符合条件的对等方收到信封，或每次
        ///         符合条件的发送都因 <see cref="InvalidOperationException" /> 失败，它仍返回 <see langword="true" />。
        ///     </para>
        /// </summary>
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
                if (!RitsuLibSidecarSessionManager.CanSendToPeer(peer.peerId))
                {
                    RitsuLibSidecarNetTrace.TraceSkippedSend(
                        RitsuLibSidecarTransportTracePaths.HostToBroadcastReady,
                        peer.peerId,
                        RitsuLibSidecarSessionManager.TryGetReachability(peer.peerId, out var r)
                            ? r
                            : RitsuLibSidecarPeerReachability.Unknown);
                    continue;
                }

                try
                {
                    host.NetHost.SendMessageToClient(peer.peerId, envelope, envelope.Length, mode, channel);
                }
                catch (InvalidOperationException)
                {
                    RitsuLibSidecarSessionManager.NoteTransportConnectionMissing(peer.peerId);
                    continue;
                }

                ops++;
                bytes += envelope.Length;
            }

            if (ops <= 0)
                return true;

            RitsuLibSidecarTrafficCounters.AddOutgoing(ops, bytes);
            RitsuLibSidecarNetTrace.TraceOutbound(
                RitsuLibSidecarTransportTracePaths.HostToBroadcastReady,
                envelope,
                mode,
                channel,
                broadcastPeerCount: ops);

            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempts to send the same raw envelope to every <see cref="NetHostGameService.ConnectedPeers" /> entry
        ///         without requiring <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetClientData.readyForBroadcasting" />.
        ///         For a valid host transport, it returns <see langword="true" /> even when no peer receives the
        ///         envelope or every send fails with <see cref="InvalidOperationException" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试向每个 <see cref="NetHostGameService.ConnectedPeers" /> 条目发送同一原始信封，而不要求
        ///         <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetClientData.readyForBroadcasting" />。对于有效的主机传输，
        ///         即使没有对等方收到信封，或每次发送都因 <see cref="InvalidOperationException" /> 失败，它仍返回
        ///         <see langword="true" />。
        ///     </para>
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
                if (!RitsuLibSidecarSessionManager.CanSendToPeer(peer.peerId))
                {
                    RitsuLibSidecarNetTrace.TraceSkippedSend(
                        RitsuLibSidecarTransportTracePaths.HostToAllConnected,
                        peer.peerId,
                        RitsuLibSidecarSessionManager.TryGetReachability(peer.peerId, out var r)
                            ? r
                            : RitsuLibSidecarPeerReachability.Unknown);
                    continue;
                }

                try
                {
                    host.NetHost.SendMessageToClient(peer.peerId, envelope, envelope.Length, mode, channel);
                }
                catch (InvalidOperationException)
                {
                    RitsuLibSidecarSessionManager.NoteTransportConnectionMissing(peer.peerId);
                    continue;
                }

                ops++;
                bytes += envelope.Length;
            }

            if (ops <= 0)
                return true;

            RitsuLibSidecarTrafficCounters.AddOutgoing(ops, bytes);
            RitsuLibSidecarNetTrace.TraceOutbound(
                RitsuLibSidecarTransportTracePaths.HostToAllConnected,
                envelope,
                mode,
                channel,
                broadcastPeerCount: ops);
            return true;
        }
    }
}
