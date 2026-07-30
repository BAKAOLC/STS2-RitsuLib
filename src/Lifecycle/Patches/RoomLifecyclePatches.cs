using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Publishes lifecycle events before and after a room's entry logic.</para>
    ///     <para xml:lang="zh-CN">在房间进入逻辑执行前后发布生命周期事件。</para>
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
    ///     <para xml:lang="en">Publishes an Act-entered lifecycle event after <see cref="Hook.AfterActEntered" /> completes.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="Hook.AfterActEntered" /> 完成后发布章节已进入生命周期事件。</para>
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
        ///     <para xml:lang="en">Publishes <see cref="ActEnteredEvent" /> after the hook task completes.</para>
        ///     <para xml:lang="zh-CN">在钩子任务完成后发布 <see cref="ActEnteredEvent" />。</para>
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
    ///     <para xml:lang="en">Publishes a room-exited lifecycle event after <see cref="RunManager" /> exits the current room.</para>
    ///     <para xml:lang="zh-CN">在 <see cref="RunManager" /> 退出当前房间后发布房间已退出生命周期事件。</para>
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
    ///     <para xml:lang="en">Publishes Act-entering and final Rewards-screen continuation events from <see cref="RunManager" />.</para>
    ///     <para xml:lang="zh-CN">从 <see cref="RunManager" /> 发布章节进入中事件以及最终奖励界面的继续事件。</para>
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
