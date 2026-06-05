using System.Reflection;
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
    public class RoomHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "room_hook_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish room entry lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.BeforeRoomEntered), [typeof(IRunState), typeof(AbstractRoom)]),
                new(typeof(Hook), nameof(Hook.AfterRoomEntered), [typeof(IRunState), typeof(AbstractRoom)]),
            ];
        }

        /// <summary>
        ///     Harmony prefix: publishes <see cref="RoomEnteringEvent" /> before the original hook body for both
        ///     <see cref="Hook.BeforeRoomEntered" /> and <see cref="Hook.AfterRoomEntered" /> targets.
        ///     Harmony prefix：对 <see cref="Hook.BeforeRoomEntered" /> 和 <see cref="Hook.AfterRoomEntered" /> 两个目标，
        ///     在原始 hook 主体前发布 <see cref="RoomEnteringEvent" />。
        /// </summary>
        public static void Prefix(IRunState runState, AbstractRoom room)
        {
            RitsuLibFramework.PublishLifecycleEvent(
                new RoomEnteringEvent(runState, room, DateTimeOffset.UtcNow),
                nameof(RoomEnteringEvent)
            );
        }

        /// <summary>
        ///     Harmony postfix: for <see cref="Hook.AfterRoomEntered" />, publishes <see cref="RoomEnteredEvent" /> after
        ///     the original task completes.
        ///     <see cref="RoomEnteredEvent" />。
        ///     Harmony postfix：对 <see cref="Hook.AfterRoomEntered" />，在
        ///     原始任务完成后发布 <see cref="RoomEnteredEvent" />。
        ///     <see cref="RoomEnteredEvent" />。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
        {
            __result = __originalMethod.Name switch
            {
                nameof(Hook.AfterRoomEntered) => LifecyclePatchTaskBridge.After(__result,
                    () => RitsuLibFramework.PublishLifecycleEvent(
                        new RoomEnteredEvent((IRunState)__args[0], (AbstractRoom)__args[1], DateTimeOffset.UtcNow),
                        nameof(RoomEnteredEvent))),
                _ => __result,
            };
        }
    }

    /// <summary>
    ///     Publishes an act-entered lifecycle event from <see cref="Hook.AfterActEntered" />.
    ///     从 <see cref="Hook.AfterActEntered" /> 发布章节已进入生命周期事件。
    /// </summary>
    public class ActHookLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "act_hook_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish act entry lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Hook), nameof(Hook.AfterActEntered), [typeof(IRunState)]),
            ];
        }

        /// <summary>
        ///     Harmony postfix: publishes <see cref="ActEnteredEvent" /> after the hook task completes.
        ///     Harmony postfix：在 hook 任务完成后发布 <see cref="ActEnteredEvent" />。
        /// </summary>
        public static void Postfix(IRunState runState, ref Task __result)
        {
            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new ActEnteredEvent(runState, runState.CurrentActIndex, DateTimeOffset.UtcNow),
                    nameof(ActEnteredEvent)
                ));
        }
    }

    /// <summary>
    ///     Publishes a room-exited lifecycle event when <see cref="RunManager" /> finishes exiting the current room.
    ///     当 <see cref="RunManager" /> 完成退出当前房间时发布房间已退出生命周期事件。
    /// </summary>
    public class RoomExitLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "room_exit_lifecycle";

        /// <inheritdoc />
        public static string Description => "Publish room exit lifecycle events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), "ExitCurrentRoom"),
            ];
        }

        /// <summary>
        ///     Harmony postfix: when exit resolves to a non-null room, publishes <see cref="RoomExitedEvent" />.
        ///     Harmony postfix：当退出结果解析为非 null 房间时发布 <see cref="RoomExitedEvent" />。
        /// </summary>
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
    public class ActTransitionLifecyclePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "act_transition_lifecycle";

        /// <inheritdoc />
        public static string Description =>
            "Resolve registered act-enter forces/pools on EnterAct, then publish act transition and rewards continuation events";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.EnterAct), [typeof(int), typeof(bool)]),
                new(typeof(RunManager), nameof(RunManager.ProceedFromTerminalRewardsScreen), Type.EmptyTypes),
            ];
        }

        /// <summary>
        ///     Harmony prefix: for <see cref="RunManager.EnterAct" />, publishes <see cref="ActEnteringEvent" />.
        ///     Harmony prefix：对 <see cref="RunManager.EnterAct" /> 发布 <see cref="ActEnteringEvent" />。
        /// </summary>
        public static void Prefix(MethodBase __originalMethod, RunManager __instance, object[] __args)
        {
            if (__originalMethod.Name != nameof(RunManager.EnterAct))
                return;

            var state = __instance.State;
            if (state != null && ModContentRegistry.HasAnyActEnterRegistration)
                ModContentRegistry.ResolveActEnterForEnterAct(__instance, state, (int)__args[0]);

            RitsuLibFramework.PublishLifecycleEvent(
                new ActEnteringEvent(__instance, (int)__args[0], (bool)__args[1], DateTimeOffset.UtcNow),
                nameof(ActEnteringEvent)
            );
        }

        /// <summary>
        ///     Harmony postfix: for <see cref="RunManager.ProceedFromTerminalRewardsScreen" />, publishes
        ///     <see cref="RewardsScreenContinuingEvent" /> after the task completes.
        ///     <see cref="RewardsScreenContinuingEvent" />。
        ///     Harmony postfix：对 <see cref="RunManager.ProceedFromTerminalRewardsScreen" />，在任务完成后发布
        ///     <see cref="RewardsScreenContinuingEvent" />。
        ///     <see cref="RewardsScreenContinuingEvent" />。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, RunManager __instance, ref Task __result)
        {
            if (__originalMethod.Name != nameof(RunManager.ProceedFromTerminalRewardsScreen))
                return;

            __result = LifecyclePatchTaskBridge.After(__result, () =>
                RitsuLibFramework.PublishLifecycleEvent(
                    new RewardsScreenContinuingEvent(__instance, DateTimeOffset.UtcNow),
                    nameof(RewardsScreenContinuingEvent)
                ));
        }
    }
}
