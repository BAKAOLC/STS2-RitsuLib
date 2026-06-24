using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Publishes room entering and entered lifecycle events from <see cref="Hook" /> before/after room entry.
    ///     在进入房间前后通过 <see cref="Hook" /> 发布房间进入中和已进入生命周期事件。
    /// </summary>
    internal sealed class BeforeRoomEnteredLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "room_hook_lifecycle_before_room_entered";
        public static string Description => "Publish room entering lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.BeforeRoomEntered), [typeof(IRunState), typeof(AbstractRoom)])];
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(IRunState __0, AbstractRoom __1)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new RoomEnteringEvent(__0, __1, DateTimeOffset.UtcNow),
                nameof(RoomEnteringEvent));
        }
    }

    internal sealed class AfterRoomEnteredLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "room_hook_lifecycle_after_room_entered";
        public static string Description => "Publish room entered lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Hook), nameof(Hook.AfterRoomEntered), [typeof(IRunState), typeof(AbstractRoom)])];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState __0, AbstractRoom __1, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RoomEnteredEvent(__0, __1, DateTimeOffset.UtcNow),
                    nameof(RoomEnteredEvent)));
        }
    }

    /// <summary>
    ///     Publishes an act-entered lifecycle event from <see cref="Hook.AfterActEntered" />.
    ///     从 <see cref="Hook.AfterActEntered" /> 发布章节已进入生命周期事件。
    /// </summary>
    internal class ActHookLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "act_hook_lifecycle";
        public static string Description => "Publish act entry lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterActEntered), [typeof(IRunState)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: publishes <see cref="ActEnteredEvent" /> after the hook task completes.
        ///     Harmony postfix：在 hook task 完成后发布 <see cref="ActEnteredEvent" />。
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(IRunState runState, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
            {
                RitsuLibFramework.PublishLifecycleEvent(
                    new ActEnteredEvent(runState, runState.CurrentActIndex, DateTimeOffset.UtcNow),
                    nameof(ActEnteredEvent)
                );
            });
        }
    }

    /// <summary>
    ///     Publishes a room-exited lifecycle event when <see cref="RunManager" /> finishes exiting the current room.
    ///     当 <see cref="RunManager" /> 完成退出当前房间时发布房间已退出生命周期事件。
    /// </summary>
    internal class RoomExitLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "room_exit_lifecycle";
        public static string Description => "Publish room exit lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), "ExitCurrentRoom"),
            ];
        }

        public static void Postfix(RunManager __instance, ref Task<AbstractRoom?> __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, room =>
            {
                if (room == null)
                    return;

                RitsuLibFramework.PublishLifecycleEvent(
                    new RoomExitedEvent(__instance, room, DateTimeOffset.UtcNow),
                    nameof(RoomExitedEvent)
                );
            });
        }
    }

    /// <summary>
    ///     Publishes act-entering and terminal-rewards-screen continuation lifecycle events on <see cref="RunManager" />.
    ///     在 <see cref="RunManager" /> 上发布章节进入中和终端奖励界面继续生命周期事件。
    /// </summary>
    internal sealed class ActEnteringLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "act_transition_lifecycle_enter_act";
        public static string Description => "Resolve registered act-enter forces/pools and publish act entering events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), nameof(RunManager.EnterAct), [typeof(int), typeof(bool)])];
        }

        public static void Prefix(RunManager __instance, int __0, bool __1)
        {
            var state = __instance.State;
            if (state != null && ModContentRegistry.HasAnyActEnterRegistration)
                ModContentRegistry.ResolveActEnterForEnterAct(__instance, state, __0);

            RitsuLibFramework.PublishLifecycleEvent(
                new ActEnteringEvent(__instance, __0, __1, DateTimeOffset.UtcNow),
                nameof(ActEnteringEvent));
        }
    }

    internal sealed class RewardsScreenContinuingLifecyclePatch : IPatchMethod
    {
        public static string PatchId => "act_transition_lifecycle_terminal_rewards_continue";
        public static string Description => "Publish terminal rewards screen continuation lifecycle events";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), nameof(RunManager.ProceedFromTerminalRewardsScreen), Type.EmptyTypes)];
        }

        public static void Postfix(RunManager __instance, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RewardsScreenContinuingEvent(__instance, DateTimeOffset.UtcNow),
                    nameof(RewardsScreenContinuingEvent)
                ));
        }
    }
}
