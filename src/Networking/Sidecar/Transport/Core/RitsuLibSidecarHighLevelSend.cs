using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Higher-level send helpers: builds envelopes with a delivery tag and picks channel / transfer mode. Control
    ///     opcodes and chunked streams should use <see cref="RitsuLibSidecarDeliverySemantics.StableSync" />.
    ///     较高层发送辅助方法：构建带投递标签的 envelope，并选择 channel / transfer mode。控制
    ///     opcode 和分块流应使用 <see cref="RitsuLibSidecarDeliverySemantics.StableSync" />。
    /// </summary>
    public static class RitsuLibSidecarHighLevelSend
    {
        /// <summary>
        ///     Client → host (single connection).
        ///     客户端 → 主机（单连接）。
        /// </summary>
        /// <param name="runManager">
        ///     Current run; must expose a connected <c>NetClientGameService</c> when sending.
        ///     当前跑局；发送时必须暴露已连接的 <c>NetClientGameService</c>。
        /// </param>
        /// <param name="opcode">
        ///     Sidecar opcode (user or control).
        ///     Sidecar opcode（用户或控制）。
        /// </param>
        /// <param name="payload">
        ///     Logical payload bytes placed after the envelope header.
        ///     放在 envelope header 之后的逻辑载荷字节。
        /// </param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        ///     映射到 transfer mode 和 channel；
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 会按 stable sync 处理。
        /// </param>
        /// <param name="extraFlags">
        ///     Additional wire flags OR’d into the envelope.
        ///     按 OR 写入 envelope 的额外线标志。
        /// </param>
        /// <param name="gzip">
        ///     When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.
        ///     为 <c>true</c> 时，对逻辑载荷进行 gzip 压缩并设置 gzip 标志。
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     Bytes appended after the delivery tag in the header extension.
        ///     追加到 header 扩展中投递标签之后的字节。
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
        ///     Host → one peer.
        ///     主机 → 一个 peer。
        /// </summary>
        /// <param name="runManager">
        ///     Current run; must expose a connected <c>NetHostGameService</c> when sending.
        ///     当前跑局；发送时必须暴露已连接的 <c>NetHostGameService</c>。
        /// </param>
        /// <param name="peerNetId">
        ///     Vanilla peer id for <c>SendMessageToClient</c>.
        ///     <c>SendMessageToClient</c> 使用的原版 peer id。
        /// </param>
        /// <param name="opcode">
        ///     Sidecar opcode (user or control).
        ///     Sidecar opcode（用户或控制）。
        /// </param>
        /// <param name="payload">
        ///     Logical payload bytes placed after the envelope header.
        ///     放在 envelope header 之后的逻辑载荷字节。
        /// </param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        ///     映射到 transfer mode 和 channel；
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 会按 stable sync 处理。
        /// </param>
        /// <param name="extraFlags">
        ///     Additional wire flags OR’d into the envelope.
        ///     按 OR 写入 envelope 的额外线标志。
        /// </param>
        /// <param name="gzip">
        ///     When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.
        ///     为 <c>true</c> 时，对逻辑载荷进行 gzip 压缩并设置 gzip 标志。
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     Bytes appended after the delivery tag in the header extension.
        ///     追加到 header 扩展中投递标签之后的字节。
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
        ///     Host → all ready-to-broadcast peers.
        ///     主机 → 所有 ready-to-broadcast peer。
        /// </summary>
        /// <param name="runManager">
        ///     Current run; must expose a connected <c>NetHostGameService</c> when sending.
        ///     当前跑局；发送时必须暴露已连接的 <c>NetHostGameService</c>。
        /// </param>
        /// <param name="opcode">
        ///     Sidecar opcode (user or control).
        ///     Sidecar opcode（用户或控制）。
        /// </param>
        /// <param name="payload">
        ///     Logical payload bytes placed after the envelope header.
        ///     放在 envelope header 之后的逻辑载荷字节。
        /// </param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        ///     映射到 transfer mode 和 channel；
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> 会按 stable sync 处理。
        /// </param>
        /// <param name="extraFlags">
        ///     Additional wire flags OR’d into the envelope.
        ///     按 OR 写入 envelope 的额外线标志。
        /// </param>
        /// <param name="gzip">
        ///     When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.
        ///     为 <c>true</c> 时，对逻辑载荷进行 gzip 压缩并设置 gzip 标志。
        /// </param>
        /// <param name="additionalHeaderExtension">
        ///     Bytes appended after the delivery tag in the header extension.
        ///     追加到 header 扩展中投递标签之后的字节。
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
        ///     Host → every connected client, including before vanilla marks peers ready to broadcast (e.g. lobby handshake).
        ///     主机 → 每个已连接客户端，包括原版将 peer 标记为 ready to broadcast 之前的阶段（例如 lobby handshake）。
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
