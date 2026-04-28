using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar wire tracing via <see cref="RitsuLibFramework.CreateLogger(string, LogType)" /> with
    ///     <see cref="Const.ModId" /> and <see cref="LogType.Network" />.
    /// </summary>
    internal static class RitsuLibSidecarNetTrace
    {
        private static readonly Logger Logger = RitsuLibFramework.CreateLogger(Const.ModId, LogType.Network);

        internal static void TraceInboundParsed(in RitsuLibSidecarDispatchContext ctx)
        {
            if (!RitsuLibSidecarNetDiagnosticsOptions.TraceIncomingPackets)
                return;

            Logger.Info(
                $"[Sidecar] Inbound parsed opcode={ctx.Opcode}, sender={ctx.SenderNetId}, payloadLen={ctx.Payload.Length}, transferMode={ctx.TransferMode}, channel={ctx.Channel}, hostIngest={ctx.IsHostIngest}");
        }

        internal static void WarnEnvelopeRejected(RitsuLibSidecarEnvelope.ParseOutcome outcome, int wireLen,
            int channel)
        {
            Logger.Warn($"[Sidecar] Magic matched but envelope rejected ({outcome}), len={wireLen}, ch={channel}");
        }

        internal static void TraceOutbound(
            string path,
            ReadOnlySpan<byte> envelope,
            NetTransferMode mode,
            int channel,
            ulong? peerNetId = null,
            int? broadcastPeerCount = null)
        {
            if (!RitsuLibSidecarNetDiagnosticsOptions.TraceOutgoingPackets)
                return;

            var opcodeText = RitsuLibSidecarWire.TryPeekOpcode(envelope, out var op)
                ? op.ToString()
                : "?";

            var peerPart = peerNetId is { } id ? $", peer={id}" : string.Empty;
            var bc = broadcastPeerCount is { } n ? $", broadcastPeers={n}" : string.Empty;

            Logger.Info(
                $"[Sidecar] Outbound {path} opcode={opcodeText}, wireLen={envelope.Length}, mode={mode}, ch={channel}{peerPart}{bc}");
        }
    }
}
