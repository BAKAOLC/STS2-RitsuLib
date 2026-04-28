using MegaCrit.Sts2.Core.Logging;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarPacketLog
    {
        private static readonly Logger NetLogger = new("RitsuLibSidecar", LogType.Network);

        internal static void IncomingParsed(in RitsuLibSidecarDispatchContext ctx)
        {
            if (!RitsuLibSidecarNetDiagnosticsOptions.TraceIncomingPackets)
                return;

            NetLogger.Debug(
                $"Received sidecar opcode={ctx.Opcode}, sender={ctx.SenderNetId}, payloadLen={ctx.Payload.Length}, transferMode={ctx.TransferMode}, channel={ctx.Channel}, hostIngest={ctx.IsHostIngest}");
        }
    }
}
