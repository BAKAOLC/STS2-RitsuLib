#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    internal static class OrbModelCapabilityPatches
    {
        private static readonly AccessTools.FieldRef<OrbQueue, Player> OrbQueueOwner =
            AccessTools.FieldRefAccess<OrbQueue, Player>("_owner");

        private static async Task RunBeforeTurnEnd(OrbQueue orbQueue, PlayerChoiceContext choiceContext)
        {
            var owner = OrbQueueOwner(orbQueue);
            foreach (var orb in orbQueue.Orbs.ToList())
            {
                if (owner.Creature.CombatState == null)
                    return;

                var triggerCount = Hook.ModifyOrbPassiveTriggerCount(
                    owner.Creature.CombatState,
                    orb,
                    1,
                    out var modifyingModels);
                await Hook.AfterModifyingOrbPassiveTriggerCount(owner.Creature.CombatState, orb, modifyingModels);
                if (owner.Creature.CombatState == null)
                    return;

                for (var i = 0; i < triggerCount; i++)
                {
                    await orb.BeforeTurnEndOrbTrigger(choiceContext);
                    if (owner.Creature.CombatState == null)
                        return;

                    await ModelCapabilityHost.AfterOwnerOrbBeforeTurnEndTriggered(orb, choiceContext);
                    await SmallWait(owner);
                }
            }
        }

        private static async Task RunAfterTurnStart(OrbQueue orbQueue, PlayerChoiceContext choiceContext)
        {
            var owner = OrbQueueOwner(orbQueue);
            foreach (var orb in orbQueue.Orbs.ToList())
            {
                if (owner.Creature.CombatState == null)
                    return;

                var triggerCount = Hook.ModifyOrbPassiveTriggerCount(
                    owner.Creature.CombatState,
                    orb,
                    1,
                    out var modifyingModels);
                await Hook.AfterModifyingOrbPassiveTriggerCount(owner.Creature.CombatState, orb, modifyingModels);
                if (owner.Creature.CombatState == null)
                    return;

                for (var i = 0; i < triggerCount; i++)
                {
                    await orb.AfterTurnStartOrbTrigger(choiceContext);
                    if (owner.Creature.CombatState == null)
                        return;

                    await ModelCapabilityHost.AfterOwnerOrbAfterTurnStartTriggered(orb, choiceContext);
                    await SmallWait(owner);
                }
            }
        }

        private static async Task SmallWait(Player owner)
        {
            if (LocalContext.IsMe(owner))
                await Cmd.CustomScaledWait(0.1f, 0.25f);
            else
                await Cmd.Wait(0.05f);
        }

        private static async Task AfterExplicitPassive(
            Task originalTask,
            OrbModel orb,
            PlayerChoiceContext choiceContext,
            Creature? target)
        {
            await originalTask;
            await ModelCapabilityHost.AfterOwnerOrbPassiveTriggered(orb, choiceContext, target);
        }

        private static async Task AfterEvokeHook(
            Task originalTask,
            OrbModel orb,
            PlayerChoiceContext choiceContext,
            IEnumerable<Creature> targets)
        {
            await originalTask;
            await ModelCapabilityHost.AfterOwnerOrbEvoked(orb, choiceContext, targets);
        }

        internal sealed class OrbQueueBeforeTurnEndPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_before_turn_end_trigger";
            public static string Description => "Notify orb capabilities after OrbQueue.BeforeTurnEnd trigger calls";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbQueue), nameof(OrbQueue.BeforeTurnEnd), [typeof(PlayerChoiceContext)])];
            }

            public static bool Prefix(OrbQueue __instance, PlayerChoiceContext choiceContext, ref Task __result)
            {
                __result = RunBeforeTurnEnd(__instance, choiceContext);
                return false;
            }
        }

        internal sealed class OrbQueueAfterTurnStartPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_after_turn_start_trigger";
            public static string Description => "Notify orb capabilities after OrbQueue.AfterTurnStart trigger calls";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [new(typeof(OrbQueue), nameof(OrbQueue.AfterTurnStart), [typeof(PlayerChoiceContext)])];
            }

            public static bool Prefix(OrbQueue __instance, PlayerChoiceContext choiceContext, ref Task __result)
            {
                __result = RunAfterTurnStart(__instance, choiceContext);
                return false;
            }
        }

        internal sealed class OrbCmdPassivePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_explicit_passive";
            public static string Description => "Notify orb capabilities after OrbCmd.Passive";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(OrbCmd), nameof(OrbCmd.Passive),
                        [typeof(PlayerChoiceContext), typeof(OrbModel), typeof(Creature)]),
                ];
            }

            public static void Postfix(
                PlayerChoiceContext choiceContext,
                OrbModel orb,
                Creature? target,
                ref Task __result)
            {
                __result = AfterExplicitPassive(__result, orb, choiceContext, target);
            }
        }

        internal sealed class AfterOrbEvokedHookPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_evoke";
            public static string Description => "Notify orb capabilities after Hook.AfterOrbEvoked";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(Hook), nameof(Hook.AfterOrbEvoked),
                    [
                        typeof(PlayerChoiceContext), typeof(CombatStateCompat), typeof(OrbModel),
                        typeof(IEnumerable<Creature>),
                    ]),
                ];
            }

            public static void Postfix(
                PlayerChoiceContext choiceContext,
                OrbModel orb,
                IEnumerable<Creature> targets,
                ref Task __result)
            {
                __result = AfterEvokeHook(__result, orb, choiceContext, targets);
            }
        }
    }
}
