namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarPacketLog
    {
        internal static void IncomingParsed(in RitsuLibSidecarDispatchContext ctx)
        {
            RitsuLibSidecarNetTrace.TraceInboundParsed(in ctx);
        }
    }
}
