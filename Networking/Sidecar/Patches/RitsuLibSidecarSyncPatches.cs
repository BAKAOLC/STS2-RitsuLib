using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
#if STS2_AT_LEAST_0_106_1
    /// <summary>
    ///     Releases sidecar sync packets inside the vanilla <see cref="NetMessageBus" /> buffer order.
    ///     在原版 <see cref="NetMessageBus" /> 缓冲顺序中释放 sidecar 同步包。
    /// </summary>
    internal sealed class RitsuLibSidecarSyncNetBufferPatch : IPatchMethod
    {
        private const string SetBufferMessagesMethodName = "SetBufferMessages";

        public static string PatchId => "ritsulib_sidecar_sync_net_buffer";
        public static bool IsCritical => false;

        public static string Description =>
            "Release sidecar sync packets inside the vanilla NetMessageBus buffer order";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NetMessageBus), SetBufferMessagesMethodName, [typeof(bool)]),
            ];
        }

        public static bool Prefix(NetMessageBus __instance, bool bufferMessages)
        {
            return RitsuLibSidecarSync.ReleaseNetBusBuffer(__instance, bufferMessages);
        }
    }
#endif

    /// <summary>
    ///     Releases sidecar sync packets inside the vanilla run-location buffer order.
    ///     在原版 run-location 缓冲顺序中释放 sidecar 同步包。
    /// </summary>
    internal sealed class RitsuLibSidecarSyncLocationChangedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_sync_location_changed";
        public static bool IsCritical => false;
        public static string Description => "Release sidecar sync packets inside the vanilla run-location buffer order";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunLocationTargetedMessageBuffer),
                    nameof(RunLocationTargetedMessageBuffer.OnLocationChanged),
                    [typeof(RunLocation)]),
            ];
        }

        public static bool Prefix(RunLocationTargetedMessageBuffer __instance, RunLocation location)
        {
            return RitsuLibSidecarSync.ReleaseLocationBuffer(__instance, location);
        }
    }
}
