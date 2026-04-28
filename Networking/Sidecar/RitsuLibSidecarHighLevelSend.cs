using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Higher-level send helpers: builds envelopes with a delivery tag and picks channel / transfer mode. Control
    ///     opcodes and chunked streams should use <see cref="RitsuLibSidecarDeliverySemantics.StableSync" />.
    /// </summary>
    public static class RitsuLibSidecarHighLevelSend
    {
        /// <summary>Client → host (single connection).</summary>
        /// <param name="runManager">Current run; must expose a connected <c>NetClientGameService</c> when sending.</param>
        /// <param name="opcode">Sidecar opcode (user or control).</param>
        /// <param name="payload">Logical payload bytes placed after the envelope header.</param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        /// </param>
        /// <param name="extraFlags">Additional wire flags OR’d into the envelope.</param>
        /// <param name="gzip">When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.</param>
        /// <param name="additionalHeaderExtension">Bytes appended after the delivery tag in the header extension.</param>
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToHost(runManager?.NetService, env, mode, ch);
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToHost(netService, env, mode, ch);
        }

        /// <summary>Host → one peer.</summary>
        /// <param name="runManager">Current run; must expose a connected <c>NetHostGameService</c> when sending.</param>
        /// <param name="peerNetId">Vanilla peer id for <c>SendMessageToClient</c>.</param>
        /// <param name="opcode">Sidecar opcode (user or control).</param>
        /// <param name="payload">Logical payload bytes placed after the envelope header.</param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        /// </param>
        /// <param name="extraFlags">Additional wire flags OR’d into the envelope.</param>
        /// <param name="gzip">When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.</param>
        /// <param name="additionalHeaderExtension">Bytes appended after the delivery tag in the header extension.</param>
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TrySendToPeer(netService, peerNetId, env, mode, ch);
        }

        /// <summary>Host → all ready-to-broadcast peers.</summary>
        /// <param name="runManager">Current run; must expose a connected <c>NetHostGameService</c> when sending.</param>
        /// <param name="opcode">Sidecar opcode (user or control).</param>
        /// <param name="payload">Logical payload bytes placed after the envelope header.</param>
        /// <param name="deliverySemantics">
        ///     Maps to transfer mode and channel;
        ///     <see cref="RitsuLibSidecarDeliverySemantics.Unspecified" /> is treated as stable sync.
        /// </param>
        /// <param name="extraFlags">Additional wire flags OR’d into the envelope.</param>
        /// <param name="gzip">When <c>true</c>, gzip-compresses the logical payload and sets the gzip flag.</param>
        /// <param name="additionalHeaderExtension">Bytes appended after the delivery tag in the header extension.</param>
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TryBroadcastToReadyPeers(runManager?.NetService, env, mode, ch);
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
            var env = RitsuLibSidecar.CreateEnvelopeWithDelivery(
                opcode,
                payload,
                deliverySemantics,
                extraFlags,
                gzip,
                additionalHeaderExtension);
            RitsuLibSidecarNetworkMapping.GetNetworkParameters(Resolve(deliverySemantics), out var mode, out var ch);
            return RitsuLibSidecarSend.TryBroadcastToReadyPeers(netService, env, mode, ch);
        }

        private static RitsuLibSidecarDeliverySemantics Resolve(RitsuLibSidecarDeliverySemantics s)
        {
            return s is RitsuLibSidecarDeliverySemantics.Unspecified
                ? RitsuLibSidecarDeliverySemantics.StableSync
                : s;
        }
    }
}
