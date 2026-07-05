#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    internal static class OrbModelCapabilityPatches
    {
        private const string MissingOrbPatchWarning =
            "[ModelCapabilities] Orb lifecycle patch did not find the expected awaited call site.";

        private static async Task RunBeforeTurnEndWithCapability(
            OrbModel orb,
            PlayerChoiceContext choiceContext)
        {
            await orb.BeforeTurnEndOrbTrigger(choiceContext);
            if (orb.Owner.Creature.CombatState != null)
                await ModelCapabilityHost.AfterOwnerOrbBeforeTurnEndTriggered(orb, choiceContext);
        }

        private static async Task RunAfterTurnStartWithCapability(
            OrbModel orb,
            PlayerChoiceContext choiceContext)
        {
            await orb.AfterTurnStartOrbTrigger(choiceContext);
            if (orb.Owner.Creature.CombatState != null)
                await ModelCapabilityHost.AfterOwnerOrbAfterTurnStartTriggered(orb, choiceContext);
        }

        private static async Task RunPassiveWithCapability(
            OrbModel orb,
            PlayerChoiceContext choiceContext,
            Creature? target)
        {
            await orb.Passive(choiceContext, target);
            if (orb.Owner.Creature.CombatState != null)
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

        private static IEnumerable<CodeInstruction> RedirectSingleAwaitedCall(
            IEnumerable<CodeInstruction> instructions,
            string operation,
            string patchId,
            MethodInfo? fromMethod,
            MethodInfo? wrapperMethod)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            if (fromMethod == null || wrapperMethod == null)
                return rewriter.Instructions();

            var report = HarmonyAsyncIl.RedirectAwaitedCalls(
                rewriter,
                operation,
                fromMethod,
                wrapperMethod,
                code => code.Any(HarmonyIl.IsCall(wrapperMethod)));
            if (!report.Succeeded || report.Applied != 1)
                RitsuLibFramework.Logger.Warn($"{MissingOrbPatchWarning} Patch={patchId}. {report.Describe()}");

            return rewriter.InstructionsChecked(operation);
        }

        internal sealed class OrbQueueBeforeTurnEndPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_before_turn_end_trigger";
            public static string Description => "Notify orb capabilities after OrbQueue.BeforeTurnEnd trigger calls";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return [PatchTarget.AsyncMethod<OrbQueue>(nameof(OrbQueue.BeforeTurnEnd), typeof(PlayerChoiceContext))];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return RedirectSingleAwaitedCall(
                    instructions,
                    "[OrbCapabilities] OrbQueue.BeforeTurnEnd trigger notification",
                    PatchId,
                    AccessTools.Method(
                        typeof(OrbModel),
                        nameof(OrbModel.BeforeTurnEndOrbTrigger),
                        [typeof(PlayerChoiceContext)]),
                    AccessTools.Method(
                        typeof(OrbModelCapabilityPatches),
                        nameof(RunBeforeTurnEndWithCapability)));
            }
        }

        internal sealed class OrbQueueAfterTurnStartPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_after_turn_start_trigger";
            public static string Description => "Notify orb capabilities after OrbQueue.AfterTurnStart trigger calls";
            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                    [PatchTarget.AsyncMethod<OrbQueue>(nameof(OrbQueue.AfterTurnStart), typeof(PlayerChoiceContext))];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return RedirectSingleAwaitedCall(
                    instructions,
                    "[OrbCapabilities] OrbQueue.AfterTurnStart trigger notification",
                    PatchId,
                    AccessTools.Method(
                        typeof(OrbModel),
                        nameof(OrbModel.AfterTurnStartOrbTrigger),
                        [typeof(PlayerChoiceContext)]),
                    AccessTools.Method(
                        typeof(OrbModelCapabilityPatches),
                        nameof(RunAfterTurnStartWithCapability)));
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
#if STS2_AT_LEAST_0_108_0
                    PatchTarget.AsyncMethod(
                        typeof(OrbCmd),
                        nameof(OrbCmd.Passive),
                        typeof(PlayerChoiceContext),
                        typeof(OrbModel),
                        typeof(Creature),
                        typeof(bool)),
#else
                    PatchTarget.AsyncMethod(
                        typeof(OrbCmd),
                        nameof(OrbCmd.Passive),
                        typeof(PlayerChoiceContext),
                        typeof(OrbModel),
                        typeof(Creature)),
#endif
                ];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return RedirectSingleAwaitedCall(
                    instructions,
                    "[OrbCapabilities] OrbCmd.Passive direct passive notification",
                    PatchId,
                    AccessTools.Method(
                        typeof(OrbModel),
                        nameof(OrbModel.Passive),
                        [typeof(PlayerChoiceContext), typeof(Creature)]),
                    AccessTools.Method(
                        typeof(OrbModelCapabilityPatches),
                        nameof(RunPassiveWithCapability)));
            }
        }

        internal sealed class OrbModelTriggerPassivePatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_orb_capability_trigger_passive";

            public static string Description =>
                "Notify orb capabilities after each OrbModel.TriggerPassive passive call";

            public static bool IsCritical => true;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    PatchTarget.AsyncMethod<OrbModel>(
                        nameof(OrbModel.TriggerPassive),
                        typeof(PlayerChoiceContext),
                        typeof(Creature)),
                ];
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return RedirectSingleAwaitedCall(
                    instructions,
                    "[OrbCapabilities] OrbModel.TriggerPassive passive notification",
                    PatchId,
                    AccessTools.Method(
                        typeof(OrbModel),
                        nameof(OrbModel.Passive),
                        [typeof(PlayerChoiceContext), typeof(Creature)]),
                    AccessTools.Method(
                        typeof(OrbModelCapabilityPatches),
                        nameof(RunPassiveWithCapability)));
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
