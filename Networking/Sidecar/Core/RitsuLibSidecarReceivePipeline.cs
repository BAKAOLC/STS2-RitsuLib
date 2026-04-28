using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar demux runs inside Harmony prefixes on <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />
    ///     / client receive entry points; <see cref="RitsuLibSidecarBus.Dispatch" /> therefore shares that callback’s
    ///     threading model (not documented as Godot main thread).
    /// </summary>
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
                RitsuLibSidecarNetTrace.WarnEnvelopeRejected(outcome, packetBytes.Length, channel);
                return true;
            }

            var ctx = new RitsuLibSidecarDispatchContext(senderId, mode, channel, isHostIngest, parsed);
            RitsuLibSidecarTrafficCounters.AddIncoming(packetBytes.Length, ctx.Payload.Length);
            RitsuLibSidecarChecksumDiagnostics.EnsureSubscribed();
            RitsuLibSidecarPacketLog.IncomingParsed(in ctx);
            RitsuLibSidecarBus.Dispatch(in ctx);
            return true;
        }
    }
}
