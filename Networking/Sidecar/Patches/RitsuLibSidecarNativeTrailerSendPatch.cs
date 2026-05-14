using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Multiplayer.Transport.ENet;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    internal sealed class RitsuLibSidecarNativeTrailerSendPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_native_trailer_send";
        public static bool IsCritical => false;
        public static string Description => "Append native trailer marker to vanilla network packets (ENet)";

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
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Prefix(ref byte[] bytes, ref int length)
        {
            RitsuLibSidecarNativeTrailerEvidence.TryAppendLocalTrailer(ref bytes, ref length);
        }
    }

    /// <summary>
    ///     Steam transport send hooks; omitted on mobile and skipped when the host assembly has no Steam transport types.
    ///     Steam 传输发送 hook；在移动端省略，并在主机程序集没有 Steam 传输类型时跳过。
    /// </summary>
    internal sealed class RitsuLibSidecarNativeTrailerSteamSendPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_native_trailer_send_steam";
        public static bool IsCritical => false;
        public static string Description => "Append native trailer marker to vanilla network packets (Steam)";

        public static ModPatchTarget[] GetTargets()
        {
            var transportAssembly = typeof(NetTransferMode).Assembly;
            var steamHost = transportAssembly.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamHost",
                false);
            var steamClient = transportAssembly.GetType(
                "MegaCrit.Sts2.Core.Multiplayer.Transport.Steam.SteamClient",
                false);
            if (steamHost == null || steamClient == null)
                return [];

            return
            [
                new(
                    steamHost,
                    "SendMessageToClient",
                    [typeof(ulong), typeof(byte[]), typeof(int), typeof(NetTransferMode), typeof(int)]),
                new(
                    steamClient,
                    "SendMessageToHost",
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
