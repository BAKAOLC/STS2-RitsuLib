using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     Sidecar demux runs inside Harmony prefixes on <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />
    ///     / client receive entry points; <see cref="RitsuLibSidecarBus.Dispatch" /> therefore shares that callback’s
    ///     threading model (not documented as Godot main thread).
    ///     Sidecar demux 运行在 <see cref="MegaCrit.Sts2.Core.Multiplayer.NetHostGameService" />
    ///     客户端接收入口点的 Harmony 前缀中；因此 <see cref="RitsuLibSidecarBus.Dispatch" /> 共享该回调的
    ///     客户端接收入口点；<c>RitsuLibSidecarBus.Dispatch</c> 因此共享该回调的
    ///     线程模型（未记录为 Godot 主线程）。
    /// </summary>
    internal static class RitsuLibSidecarReceivePipeline
    {
        /// <summary>
        ///     When true, vanilla <see cref="MegaCrit.Sts2.Core.Multiplayer.NetMessageBus" /> must not see this packet.
        ///     为 true 时，原版 <see cref="MegaCrit.Sts2.Core.Multiplayer.NetMessageBus" /> 不应看到此数据包。
        /// </summary>
        internal static bool ShouldSuppressVanillaDeserialize(
            INetGameService netService,
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
            if (RitsuLibSidecarSync.TryBufferIncoming(netService, in ctx))
                return true;

            RitsuLibSidecarBus.Dispatch(in ctx);
            return true;
        }
    }
}
