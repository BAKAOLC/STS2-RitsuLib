using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Networking.Sidecar.Patches
{
    /// <summary>
    ///     Releases sidecar sync packets inside the vanilla <see cref="NetMessageBus" /> buffer order.
    ///     在原版 <see cref="NetMessageBus" /> 缓冲顺序中释放 sidecar 同步包。
    /// </summary>
    internal sealed class RitsuLibSidecarSyncNetBufferPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_sync_net_buffer";
        public static bool IsCritical => false;

        public static string Description =>
            "Release sidecar sync packets inside the vanilla NetMessageBus buffer order";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NetMessageBus), nameof(NetMessageBus.SetBufferMessages), [typeof(bool)], true),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NetMessageBus __instance, bool bufferMessages)
        {
            return RitsuLibSidecarSync.ReleaseNetBusBuffer(__instance, bufferMessages);
        }
    }

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

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(RunLocationTargetedMessageBuffer __instance, RunLocation location)
        {
            return RitsuLibSidecarSync.ReleaseLocationBuffer(__instance, location);
        }
    }

    /// <summary>
    ///     Binds sidecar sync action payloads after vanilla hook actions are enqueued.
    ///     在原版 hook action 入队后绑定 sidecar 同步动作载荷。
    /// </summary>
    internal sealed class RitsuLibSidecarSyncHookActionEnqueuedPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_sidecar_sync_hook_action_enqueued";
        public static bool IsCritical => false;
        public static string Description => "Bind sidecar sync action payloads to vanilla hook actions after enqueue";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(ActionQueueSynchronizer),
                    "HandleHookActionEnqueuedMessage",
                    [typeof(HookActionEnqueuedMessage), typeof(ulong)]),
            ];
        }

        public static void Postfix(HookActionEnqueuedMessage message)
        {
            RitsuLibSidecarSyncActions.TryBindEnqueuedHookAction(
                message.hookActionId,
                message.ownerId,
                message.gameActionType);
        }
    }
}
