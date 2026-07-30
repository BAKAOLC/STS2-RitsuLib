using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Builds sidecar envelopes with delivery metadata and sends them using the corresponding transport mode
    ///         and channel.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         构建包含投递元数据的 sidecar 信封，并使用对应的传输模式和通道发送。
    ///     </para>
    /// </summary>
    public static class RitsuLibSidecarHighLevelSend
    {
        /// <summary>
        ///     <para xml:lang="en">Tries to send one sidecar message from a client to the host.</para>
        ///     <para xml:lang="zh-CN">尝试从客户端向主机发送一条 sidecar 消息。</para>
        /// </summary>
        /// <param name="runManager">
        ///     <para xml:lang="en">The current run, which must use a connected client network service.</para>
        ///     <para xml:lang="zh-CN">当前一局游戏，且必须使用已连接的客户端网络服务。</para>
        /// </param>
        /// <param name="opcode">
        ///     <para xml:lang="en">The user or control opcode.</para>
        ///     <para xml:lang="zh-CN">用户或控制操作码。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The logical payload.</para>
        ///     <para xml:lang="zh-CN">逻辑载荷。</para>
        /// </param>
        /// <param name="deliverySemantics">
        ///     <para xml:lang="en">The requested delivery semantics.</para>
        ///     <para xml:lang="zh-CN">请求的投递语义。</para>
        /// </param>
        /// <param name="extraFlags">
        ///     <para xml:lang="en">Additional wire flags.</para>
        ///     <para xml:lang="zh-CN">其他线路标志。</para>
        /// </param>
        /// <param name="gzip">
        ///     <para xml:lang="en">Whether to force gzip compression.</para>
        ///     <para xml:lang="zh-CN">是否强制使用 gzip 压缩。</para>
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     <para xml:lang="en">Bytes appended after the delivery tag.</para>
        ///     <para xml:lang="zh-CN">追加到投递标签之后的字节。</para>
        /// </param>
        public static bool TrySendAsClient(
            RunManager? runManager,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var netService = runManager?.NetService;
            var env = CreateEnvelopeForHost(
                netService,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToHost(netService, env, mode, ch);
        }

        /// <inheritdoc
        ///     cref="TrySendAsClient(RunManager?, ulong, ReadOnlySpan{byte}, RitsuLibSidecarDeliverySemantics, RitsuLibSidecarWireFlags, bool, ReadOnlySpan{byte})" />
        public static bool TrySendAsClient(
            INetGameService? netService,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var env = CreateEnvelopeForHost(
                netService,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToHost(netService, env, mode, ch);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to send one sidecar message from the host to a peer.</para>
        ///     <para xml:lang="zh-CN">尝试从主机向一个对等端发送一条 sidecar 消息。</para>
        /// </summary>
        /// <param name="runManager">
        ///     <para xml:lang="en">The current run, which must use a connected host network service.</para>
        ///     <para xml:lang="zh-CN">当前一局游戏，且必须使用已连接的主机网络服务。</para>
        /// </param>
        /// <param name="peerNetId">
        ///     <para xml:lang="en">The target peer's network ID.</para>
        ///     <para xml:lang="zh-CN">目标对等端的网络 ID。</para>
        /// </param>
        /// <param name="opcode">
        ///     <para xml:lang="en">The user or control opcode.</para>
        ///     <para xml:lang="zh-CN">用户或控制操作码。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The logical payload.</para>
        ///     <para xml:lang="zh-CN">逻辑载荷。</para>
        /// </param>
        /// <param name="deliverySemantics">
        ///     <para xml:lang="en">The requested delivery semantics.</para>
        ///     <para xml:lang="zh-CN">请求的投递语义。</para>
        /// </param>
        /// <param name="extraFlags">
        ///     <para xml:lang="en">Additional wire flags.</para>
        ///     <para xml:lang="zh-CN">其他线路标志。</para>
        /// </param>
        /// <param name="gzip">
        ///     <para xml:lang="en">Whether to force gzip compression.</para>
        ///     <para xml:lang="zh-CN">是否强制使用 gzip 压缩。</para>
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     <para xml:lang="en">Bytes appended after the delivery tag.</para>
        ///     <para xml:lang="zh-CN">追加到投递标签之后的字节。</para>
        /// </param>
        public static bool TrySendAsHostToPeer(
            RunManager? runManager,
            ulong peerNetId,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var env = CreateEnvelopeForPeer(
                peerNetId,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToPeer(runManager?.NetService, peerNetId, env, mode, ch);
        }

        /// <inheritdoc
        ///     cref="TrySendAsHostToPeer(RunManager?, ulong, ulong, ReadOnlySpan{byte}, RitsuLibSidecarDeliverySemantics, RitsuLibSidecarWireFlags, bool, ReadOnlySpan{byte})" />
        public static bool TrySendAsHostToPeer(
            INetGameService? netService,
            ulong peerNetId,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var env = CreateEnvelopeForPeer(
                peerNetId,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToPeer(netService, peerNetId, env, mode, ch);
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to send one sidecar message to every peer ready for broadcast.</para>
        ///     <para xml:lang="zh-CN">尝试向所有已准备好接收广播的对等端发送一条 sidecar 消息。</para>
        /// </summary>
        /// <param name="runManager">
        ///     <para xml:lang="en">The current run, which must use a connected host network service.</para>
        ///     <para xml:lang="zh-CN">当前一局游戏，且必须使用已连接的主机网络服务。</para>
        /// </param>
        /// <param name="opcode">
        ///     <para xml:lang="en">The user or control opcode.</para>
        ///     <para xml:lang="zh-CN">用户或控制操作码。</para>
        /// </param>
        /// <param name="payload">
        ///     <para xml:lang="en">The logical payload.</para>
        ///     <para xml:lang="zh-CN">逻辑载荷。</para>
        /// </param>
        /// <param name="deliverySemantics">
        ///     <para xml:lang="en">The requested delivery semantics.</para>
        ///     <para xml:lang="zh-CN">请求的投递语义。</para>
        /// </param>
        /// <param name="extraFlags">
        ///     <para xml:lang="en">Additional wire flags.</para>
        ///     <para xml:lang="zh-CN">其他线路标志。</para>
        /// </param>
        /// <param name="gzip">
        ///     <para xml:lang="en">Whether to force gzip compression.</para>
        ///     <para xml:lang="zh-CN">是否强制使用 gzip 压缩。</para>
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     <para xml:lang="en">Bytes appended after the delivery tag.</para>
        ///     <para xml:lang="zh-CN">追加到投递标签之后的字节。</para>
        /// </param>
        public static bool TrySendAsHostBroadcast(
            RunManager? runManager,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var netService = runManager?.NetService;
            var env = CreateEnvelopeForBroadcast(
                netService,
                true,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TryBroadcastToReadyPeers(netService, env, mode, ch);
        }

        /// <inheritdoc
        ///     cref="TrySendAsHostBroadcast(RunManager?, ulong, ReadOnlySpan{byte}, RitsuLibSidecarDeliverySemantics, RitsuLibSidecarWireFlags, bool, ReadOnlySpan{byte})" />
        public static bool TrySendAsHostBroadcast(
            INetGameService? netService,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var env = CreateEnvelopeForBroadcast(
                netService,
                true,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TryBroadcastToReadyPeers(netService, env, mode, ch);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to send one sidecar message to every connected client, including clients not yet ready for
        ///         vanilla broadcast.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试向所有已连接客户端发送一条 sidecar 消息，包括尚未准备好接收原版广播的客户端。
        ///     </para>
        /// </summary>
        public static bool TrySendAsHostBroadcastToAllConnected(
            INetGameService? netService,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags = RitsuLibSidecarWireFlags.None,
            bool gzip = false,
            ReadOnlySpan<byte> additionalHeaderExtension = default)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            var env = CreateEnvelopeForBroadcast(
                netService,
                false,
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TryBroadcastToAllConnectedClients(netService, env, mode, ch);
        }

        private static RitsuLibSidecarDeliverySemantics Resolve(RitsuLibSidecarDeliverySemantics s)
        {
            return s is RitsuLibSidecarDeliverySemantics.Unspecified
                ? RitsuLibSidecarDeliverySemantics.StableSync
                : s;
        }

        private static byte[] CreateEnvelopeForHost(
            INetGameService? netService,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags,
            bool gzip,
            ReadOnlySpan<byte> additionalHeaderExtension)
        {
            var compression = gzip
                ? RitsuLibSidecarPayloadCompression.Gzip
                : netService is NetClientGameService client
                    ? RitsuLibSidecarPayloadCompressionSelector.ForPeer(opcode, payload, client.HostNetId)
                    : RitsuLibSidecarPayloadCompression.None;
            return RitsuLibSidecar.CreateEnvelopeWithDeliveryCompressed(
                opcode,
                payload,
                deliverySemantics,
                compression,
                extraFlags,
                additionalHeaderExtension);
        }

        private static byte[] CreateEnvelopeForPeer(
            ulong peerNetId,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags,
            bool gzip,
            ReadOnlySpan<byte> additionalHeaderExtension)
        {
            var compression = gzip
                ? RitsuLibSidecarPayloadCompression.Gzip
                : RitsuLibSidecarPayloadCompressionSelector.ForPeer(opcode, payload, peerNetId);
            return RitsuLibSidecar.CreateEnvelopeWithDeliveryCompressed(
                opcode,
                payload,
                deliverySemantics,
                compression,
                extraFlags,
                additionalHeaderExtension);
        }

        private static byte[] CreateEnvelopeForBroadcast(
            INetGameService? netService,
            bool readyOnly,
            ulong opcode,
            ReadOnlySpan<byte> payload,
            RitsuLibSidecarDeliverySemantics deliverySemantics,
            RitsuLibSidecarWireFlags extraFlags,
            bool gzip,
            ReadOnlySpan<byte> additionalHeaderExtension)
        {
            var compression = gzip
                ? RitsuLibSidecarPayloadCompression.Gzip
                : RitsuLibSidecarPayloadCompressionSelector.ForPeers(
                    opcode,
                    payload,
                    BroadcastTargetPeerIds(netService, readyOnly));
            return RitsuLibSidecar.CreateEnvelopeWithDeliveryCompressed(
                opcode,
                payload,
                deliverySemantics,
                compression,
                extraFlags,
                additionalHeaderExtension);
        }

        private static IEnumerable<ulong> BroadcastTargetPeerIds(INetGameService? netService, bool readyOnly)
        {
            if (netService is not NetHostGameService host)
                yield break;

            foreach (var peer in host.ConnectedPeers)
            {
                if (readyOnly && !peer.readyForBroadcasting)
                    continue;

                yield return peer.peerId;
            }
        }
    }
}
