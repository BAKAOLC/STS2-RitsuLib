using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    internal sealed class RitsuLibSidecarNativeTrailerSendPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_native_trailer_send";
        public static bool IsCritical => false;
        public static string Description => "Append native trailer marker to vanilla network packets";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(ENetHost),
                    nameof(ENetHost.SendMessageToClient),
                    [typeof(ulong), typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]),
                new(
                    typeof(ENetClient),
                    nameof(ENetClient.SendMessageToHost),
                    [typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]),
                new(
                    typeof(SteamHost),
                    nameof(SteamHost.SendMessageToClient),
                    [typeof(ulong), typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]),
                new(
                    typeof(SteamClient),
                    nameof(SteamClient.SendMessageToHost),
                    [typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(ref byte[] bytes, ref int length)
        {
            RitsuLibSidecarNativeTrailerEvidence.TryAppendLocalTrailer(ref bytes, ref length);
        }
    }
}
