using System.IO.Hashing;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarBuiltInHandlers
    {
        private const ushort DivergenceRelayTag = 1;

        private static readonly RitsuLibSidecarChunkReassembly Chunks = new();

        internal static void Register()
        {
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.Handshake, OnHandshake);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.HandshakeAck, OnHandshakeAck);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.ChunkedFrame, OnChunkedFrame);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.ChunkStreamSelectiveNack,
                OnChunkStreamSelectiveNack);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.ChunkStreamReassemblyDone,
                OnChunkStreamReassemblyDone);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpRequest,
                OnRelayDumpRequest);
            RitsuLibSidecarBus.RegisterHandler(RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpFanout,
                OnRelayDumpFanout);
        }

        private static void OnChunkStreamSelectiveNack(RitsuLibSidecarDispatchContext ctx)
        {
            if (ctx.Payload.Length < RitsuLibSidecarChunkGapBinary.SelectiveNackHeaderSize)
                return;

            try
            {
                RitsuLibSidecarChunkGapBinary.ReadSelectiveNack(
                    ctx.Payload.Span,
                    out var streamId,
                    out var userOpcode,
                    out var count,
                    out var ranges);
                if (ranges.Any(r =>
                        r.Length == 0 || r.StartIndex >= count || (ulong)r.StartIndex + r.Length > count)) return;

                var rm = RunManager.Instance;
                RitsuLibSidecarChunkOutboundRegistry.HandleSelectiveNack(
                    rm,
                    ctx.SenderNetId,
                    streamId,
                    userOpcode,
                    count,
                    ranges);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] selective NACK: {ex.Message}");
            }
        }

        private static void OnChunkStreamReassemblyDone(RitsuLibSidecarDispatchContext ctx)
        {
            if (ctx.Payload.Length < RitsuLibSidecarChunkGapBinary.ReassemblyDonePayloadSize)
                return;

            try
            {
                RitsuLibSidecarChunkGapBinary.ReadReassemblyDone(ctx.Payload.Span, out var streamId);
                RitsuLibSidecarChunkOutboundRegistry.TryRemove(streamId);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] chunk reassembly done: {ex.Message}");
            }
        }

        private static void OnRelayDumpRequest(RitsuLibSidecarDispatchContext ctx)
        {
            if (!ctx.IsHostIngest)
                return;

            RitsuLibSidecarChecksumDiagnostics.TryLogLocalCombatDump(
                $"Sidecar relay dump (request peer={ctx.SenderNetId})",
                DivergenceRelayTag);

            var rm = RunManager.Instance;
            var payload = RitsuLibSidecarDiagnosticPayload.BuildFanoutPayload(ctx.SenderNetId, DivergenceRelayTag);
            RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                rm,
                RitsuLibSidecarControlOpcodes.DiagnosticRelayDumpFanout,
                payload,
                RitsuLibSidecarDeliverySemantics.StableSync);
        }

        private static void OnRelayDumpFanout(RitsuLibSidecarDispatchContext ctx)
        {
            if (!RitsuLibSidecarDiagnosticPayload.TryParseFanout(ctx.Payload.Span, out var origin, out var tag))
                return;

            RitsuLibSidecarChecksumDiagnostics.TryLogLocalCombatDump(
                $"Sidecar coordinated dump via host broadcast (originPeer={origin})",
                tag);
        }

        private static void OnHandshake(RitsuLibSidecarDispatchContext ctx)
        {
            if (ctx.Payload.Length < RitsuLibSidecarHandshakeBinary.HandshakePayloadSize)
                return;

            RitsuLibSidecarHandshakeBinary.ReadHandshake(
                ctx.Payload.Span,
                out var wire,
                out var peerMax,
                out var feats);
            var ok = wire is >= 1 and <= RitsuLibSidecarWire.SupportedWireFormatVersionMax
                     && wire <= peerMax;
            if (!ok) RitsuLibFramework.Logger.Warn($"[Sidecar] Handshake wire version {wire} not supported.");

            var selected = ok ? wire : RitsuLibSidecarWire.CurrentWireFormatVersion;
            RitsuLibSidecarConnectionSession.SetPeerFeatures(
                ctx.SenderNetId,
                ok ? feats : RitsuLibSidecarPeerFeatures.None);

            var buf = new byte[RitsuLibSidecarHandshakeBinary.AckPayloadSize];
            RitsuLibSidecarHandshakeBinary.WriteAck(
                buf.AsSpan(),
                selected,
                ok,
                RitsuLibSidecarPeerFeatures.ChunkedStreams);
            var rm = RunManager.Instance;
            if (ctx.IsHostIngest)
                RitsuLibSidecarHighLevelSend.TrySendAsHostToPeer(
                    rm,
                    ctx.SenderNetId,
                    RitsuLibSidecarControlOpcodes.HandshakeAck,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
            else
                RitsuLibSidecarHighLevelSend.TrySendAsClient(
                    rm,
                    RitsuLibSidecarControlOpcodes.HandshakeAck,
                    buf,
                    RitsuLibSidecarDeliverySemantics.StableSync);
        }

        private static void OnHandshakeAck(RitsuLibSidecarDispatchContext ctx)
        {
            if (ctx.Payload.Length < RitsuLibSidecarHandshakeBinary.AckPayloadSize)
                return;

            RitsuLibSidecarHandshakeBinary.ReadAck(
                ctx.Payload.Span,
                out _,
                out _,
                out var ackSenderFeatures);
            RitsuLibSidecarConnectionSession.SetPeerFeatures(ctx.SenderNetId, ackSenderFeatures);
        }

        private static void OnChunkedFrame(RitsuLibSidecarDispatchContext ctx)
        {
            Chunks.IncompleteStreamRetention = RitsuLibSidecarNetDiagnosticsOptions.IncompleteChunkStreamRetention;
            try
            {
                RitsuLibSidecarChunkBinary.ReadFrame(
                    ctx.Payload.Span,
                    out var userOpcode,
                    out var streamId,
                    out var index,
                    out var count,
                    out var total,
                    out var expectedCrc,
                    out var seg);
                if (Crc32.HashToUInt32(seg) != expectedCrc)
                {
                    RitsuLibFramework.Logger.Warn("[Sidecar] Chunk segment CRC mismatch; drop.");
                    return;
                }

                if (!Chunks.TryIngest(
                        ctx.SenderNetId,
                        userOpcode,
                        streamId,
                        index,
                        count,
                        total,
                        seg,
                        out var full)
                    || full is null)
                {
                    RitsuLibSidecarChunkGapScheduler.ScheduleGapReport(
                        Chunks,
                        RunManager.Instance,
                        ctx.SenderNetId,
                        streamId,
                        userOpcode,
                        count);
                    return;
                }

                RitsuLibSidecarChunkGapScheduler.Cancel(ctx.SenderNetId, streamId);
                var done = new byte[RitsuLibSidecarChunkGapBinary.ReassemblyDonePayloadSize];
                RitsuLibSidecarChunkGapBinary.WriteReassemblyDone(done.AsSpan(), streamId);
                RitsuLibSidecarControlPeerSend.SendToNetPeer(
                    RunManager.Instance,
                    ctx.SenderNetId,
                    RitsuLibSidecarControlOpcodes.ChunkStreamReassemblyDone,
                    done);

                var inner = new RitsuLibSidecarEnvelope.ParsedEnvelope(
                    ctx.Envelope.WireFormatVersion,
                    ctx.Envelope.Flags,
                    userOpcode,
                    ReadOnlyMemory<byte>.Empty,
                    full);
                var next = new RitsuLibSidecarDispatchContext(
                    ctx.SenderNetId,
                    ctx.TransferMode,
                    ctx.Channel,
                    ctx.IsHostIngest,
                    inner);
                RitsuLibSidecarBus.Dispatch(in next);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Sidecar] Chunk frame: {ex.Message}");
            }
        }
    }
}
