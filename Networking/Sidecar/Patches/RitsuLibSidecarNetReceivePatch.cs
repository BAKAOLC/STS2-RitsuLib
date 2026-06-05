using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Intercepts inbound sidecar and coalesced hook packets before <see cref="NetMessageBus" /> runs.
    ///     在 <see cref="NetMessageBus" /> 运行前拦截入站 sidecar 和合并的 hook 数据包。
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
