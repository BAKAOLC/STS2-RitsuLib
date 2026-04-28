using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Intercepts sidecar envelopes on the client before <see cref="NetMessageBus" /> runs.
    /// </summary>
    internal sealed class RitsuLibSidecarNetClientReceivePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_net_client_receive";

        public static bool IsCritical => false;

        public static string Description => "Demux RitsuLib sidecar packets before vanilla NetMessageBus on client";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(NetClientGameService),
                    nameof(NetClientGameService.OnPacketReceived),
                    [typeof(ulong), typeof(byte[]), typeof(NetTransferMode), typeof(int)]),
            ];
        }

        public static bool Prefix(ulong senderId, byte[] packetBytes, NetTransferMode mode, int channel)
        {
            RitsuLibSidecarNativeTrailerEvidence.ObserveInbound(senderId, packetBytes);
            return !RitsuLibSidecarReceivePipeline.ShouldSuppressVanillaDeserialize(
                senderId,
                packetBytes,
                mode,
                channel,
                false);
        }
    }
}
