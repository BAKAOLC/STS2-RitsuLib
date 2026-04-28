using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    internal static class RitsuLibSidecarReceivePipeline
    {
        /// <summary>
        ///     When true, vanilla <see cref="MegaCrit.Sts2.Core.Multiplayer.NetMessageBus" /> must not see this packet.
        /// </summary>
        internal static bool ShouldSuppressVanillaDeserialize(
            ulong senderId,
            byte[] packetBytes,
            NetTransferMode mode,
            int channel,
            bool isHostIngest)
        {
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();
            if (!RitsuLibSidecarWire.MatchesMagic(packetBytes))
                return false;

            var outcome = RitsuLibSidecarEnvelope.TryParse(packetBytes, out var parsed);
            if (outcome != RitsuLibSidecarEnvelope.ParseOutcome.Ok)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Sidecar] magic matched but envelope rejected ({outcome}), len={packetBytes.Length}, ch={channel}");
                return true;
            }

            var ctx = new RitsuLibSidecarDispatchContext(senderId, mode, channel, isHostIngest, parsed);
            RitsuLibSidecarChecksumDiagnostics.EnsureSubscribed();
            RitsuLibSidecarPacketLog.IncomingParsed(in ctx);
            RitsuLibSidecarBus.Dispatch(in ctx);
            return true;
        }
    }
}
