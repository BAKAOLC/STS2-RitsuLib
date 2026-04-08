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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, object[] __args, ref Task __result)
            // ReSharper restore InconsistentNaming
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(IRunState runState, ref Task __result)
            // ReSharper restore InconsistentNaming
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(RunManager __instance, ref Task<AbstractRoom?> __result)
            // ReSharper restore InconsistentNaming
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Prefix(MethodBase __originalMethod, RunManager __instance, object[] __args)
            // ReSharper restore InconsistentNaming
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
        /// </summary>
        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, RunManager __instance, ref Task __result)
            // ReSharper restore InconsistentNaming
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
