using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace STS2RitsuLib.Networking.Sidecar
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Sidecar demultiplexing runs inside Harmony prefixes on the host and client receive entry points.
    ///         <see cref="RitsuLibSidecarBus.Dispatch" /> therefore uses the receive callback's threading model,
    ///         which is not documented as the Godot main thread.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         Sidecar 多路分解在主机与客户端接收入口的 Harmony 前缀中运行。
    ///         因此 <see cref="RitsuLibSidecarBus.Dispatch" /> 使用接收回调的线程模型，
    ///         而该线程未被记录为 Godot 主线程。
    ///     </para>
    /// </summary>
    internal static class RitsuLibSidecarReceivePipeline
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> when vanilla
        ///         <see cref="MegaCrit.Sts2.Core.Multiplayer.NetMessageBus" /> must not process this packet.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         游戏原版 <see cref="MegaCrit.Sts2.Core.Multiplayer.NetMessageBus" /> 不应处理此数据包时，
        ///         返回 <see langword="true" />。
        ///     </para>
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
