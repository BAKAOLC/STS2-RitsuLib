using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
#if STS2_AT_LEAST_0_106_1
    /// <summary>
    ///     <para xml:lang="en">
    ///         Releases queued sidecar synchronization packets within the vanilla <see cref="NetMessageBus" /> buffer
    ///         ordering.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按照原版 <see cref="NetMessageBus" /> 的缓冲顺序释放排队的 sidecar 同步数据包。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Releases queued sidecar synchronization packets within the vanilla run-location buffer ordering.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         按照原版局内位置的缓冲顺序释放排队的 sidecar 同步数据包。
    ///     </para>
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
