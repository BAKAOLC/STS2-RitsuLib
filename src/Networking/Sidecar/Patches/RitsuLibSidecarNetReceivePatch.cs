using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Inspects and dispatches inbound sidecar and coalesced packets before vanilla network deserialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在原版网络反序列化前检查并分发收到的 sidecar 数据包和合并数据包。
    ///     </para>
    /// </summary>
    internal sealed class RitsuLibSidecarNetReceivePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_net_receive";
        public static bool IsCritical => true;
        public static string Description => "Demux RitsuLib inbound packets before vanilla NetMessageBus";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(NetHostGameService),
                    nameof(NetHostGameService.OnPacketReceived),
                    [typeof(ulong), typeof(byte[]), typeof(NetTransferMode), typeof(int)]),
                new(
                    typeof(NetClientGameService),
                    nameof(NetClientGameService.OnPacketReceived),
                    [typeof(ulong), typeof(byte[]), typeof(NetTransferMode), typeof(int)]),
            ];
        }

        public static bool Prefix(
            INetGameService __instance,
            ulong senderId,
            byte[] packetBytes,
            NetTransferMode mode,
            int channel)
        {
            var isHostIngest = __instance is NetHostGameService;
            RitsuLibSidecarNativeTrailerEvidence.ObserveInbound(senderId, packetBytes);
            RitsuLibSidecarConnectionExchange.TrySendClientHelloIfReachable(__instance);
            return !RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize(
                __instance,
                senderId,
                packetBytes,
                mode,
                channel,
                isHostIngest);
        }
    }
}
