using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
#if !STS2_AT_LEAST_0_105_0
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
#endif

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Includes RitsuLib-managed non-Spine death animation durations in combat reward timing.
    /// </summary>
    public class NCombatUiNonSpineDeathAnimationRewardDelayPatch : IPatchMethod
    {
#if STS2_AT_LEAST_0_105_0
        private static readonly AccessTools.FieldRef<NCombatUi, CancellationTokenSource> CtsRef =
            AccessTools.FieldRefAccess<NCombatUi, CancellationTokenSource>("_cts");
#endif

        private static readonly FieldInfo? IsDebugSlowRewardsField =
            AccessTools.Field(typeof(NCombatUi), "_isDebugSlowRewards");

#if !STS2_AT_LEAST_0_105_0
        private static readonly AccessTools.FieldRef<NCombatUi, CombatState> StateRef =
            AccessTools.FieldRefAccess<NCombatUi, CombatState>("_state");
#endif

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ncombat_ui_non_spine_death_animation_reward_delay";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Include RitsuLib-managed non-Spine death animation durations when delaying combat rewards";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCombatUi), "ShowRewards", [typeof(CombatRoom)])];
        }

        /// <summary>
        ///     Replaces vanilla reward timing with equivalent logic that also considers RitsuLib non-Spine death
        ///     delays.
        /// </summary>
        public static bool Prefix(NCombatUi __instance, CombatRoom room, ref Task __result)
        {
            __result = ShowRewards(__instance, room);
            return false;
        }

        private static async Task ShowRewards(NCombatUi ui, CombatRoom room)
        {
            var delaySeconds = GetRemovingCreatureRewardDelay();

#if STS2_AT_LEAST_0_105_0
            var token = CtsRef(ui).Token;

            if (IsDebugSlowRewards())
                await Cmd.Wait(delaySeconds + 3f, token);
            else if (room.RoomType == RoomType.Boss)
                await Cmd.CustomScaledWait(delaySeconds * 0.5f, delaySeconds + 1f, false, token);
            else
                await Cmd.CustomScaledWait(0.5f, delaySeconds + 1f, false, token);
#else
            if (IsDebugSlowRewards())
                await Cmd.Wait(delaySeconds + 3f);
            else if (room.RoomType == RoomType.Boss)
                await Cmd.CustomScaledWait(delaySeconds * 0.5f, delaySeconds + 1f);
            else
                await Cmd.CustomScaledWait(0.5f, delaySeconds + 1f);
#endif

            await OfferRoomEndRewards(ui, room);
        }

        private static Task OfferRoomEndRewards(NCombatUi ui, CombatRoom room)
        {
#if STS2_AT_LEAST_0_105_0
            return room.OfferRoomEndRewards();
#else
            var me = LocalContext.GetMe(StateRef(ui))!;
            return RewardsCmd.OfferForRoomEnd(me, room);
#endif
        }

        private static float GetRemovingCreatureRewardDelay()
        {
            var delaySeconds = 0f;
            var room = NCombatRoom.Instance;
            if (room == null)
                return delaySeconds;

            foreach (var node in room.RemovingCreatureNodes)
            {
                if (!GodotObject.IsInstanceValid(node))
                    continue;

                if (node is { HasSpineAnimation: true, IsPlayingDeathAnimation: true })
                {
                    delaySeconds = Math.Max(delaySeconds, node.GetCurrentAnimationTimeRemaining());
                    continue;
                }

                if (RitsuNonSpineDeathAnimationDelayer.TryGetDelaySeconds(node, out var ritsuDelay))
                    delaySeconds = Math.Max(delaySeconds, ritsuDelay);

                var monster = node.Entity.Monster;
                if (monster is { HasDeathAnimLengthOverride: true })
                    delaySeconds = Math.Max(delaySeconds, monster.DeathAnimLengthOverride);
            }

            return delaySeconds;
        }

        private static bool IsDebugSlowRewards()
        {
            return IsDebugSlowRewardsField?.GetValue(null) is true;
        }
    }
}
