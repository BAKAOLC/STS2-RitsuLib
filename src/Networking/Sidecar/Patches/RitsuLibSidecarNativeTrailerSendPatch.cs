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

        public static void Prefix(ref byte[] bytes, ref int length)
        {
            RitsuLibSidecarNativeTrailerEvidence.TryAppendLocalTrailer(ref bytes, ref length);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds native-trailer send hooks for the Steam transport when its transport types are available.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 Steam 传输类型可用时，为其添加原生尾部标记的发送钩子。
    ///     </para>
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

        public static void Prefix(ref byte[] bytes, ref int length)
        {
            RitsuLibSidecarNativeTrailerEvidence.TryAppendLocalTrailer(ref bytes, ref length);
        }
    }
}
