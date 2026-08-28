using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.CardPiles.Patches
{
    internal sealed class ModExtraHandHandViewPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_hand_views";
        public static string Description => "Include opted-in extra hands in base-game hand queries and turn-end views";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CombatManager), "CheckForEmptyHand",
                [
#if STS2_AT_LEAST_0_110_0
                    AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatTurnState")
                    ?? throw new TypeLoadException("Could not resolve CombatTurnState."),
#endif
                    typeof(PlayerChoiceContext),
                    typeof(Player),
                ], MethodType.Async),
                PatchTarget.AsyncMethod<CombatManager>("DoTurnEnd"),
                PatchTarget.AsyncMethod<CombatManager>("FlushPlayerHand"),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            const string operation = "[ExtraHand] route base-game hand operation through its opted-in view";
            var originalGetter = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(PileTypeExtensions), nameof(PileTypeExtensions.GetPile),
                    [typeof(PileType), typeof(Player)]), operation);
            var stateMachineName = __originalMethod.DeclaringType?.Name ?? string.Empty;
            var resolverName = stateMachineName switch
            {
                _ when stateMachineName.Contains("<CheckForEmptyHand>", StringComparison.Ordinal) =>
                    nameof(ModExtraHandHandView.GetForQueries),
                _ when stateMachineName.Contains("<DoTurnEnd>", StringComparison.Ordinal) =>
                    nameof(ModExtraHandHandView.GetForTurnEnd),
                _ when stateMachineName.Contains("<FlushPlayerHand>", StringComparison.Ordinal) =>
                    nameof(ModExtraHandHandView.GetForFlush),
                _ => throw new InvalidOperationException(
                    $"{operation} received unexpected target '{__originalMethod.DeclaringType?.FullName}'."),
            };
            var resolver = HarmonyIl.RequireMethod(
                AccessTools.Method(typeof(ModExtraHandHandView), resolverName), operation);
            var rewriter = HarmonyIlRewriter.From(instructions);
            var report = rewriter.RedirectCalls(
                operation,
                called => called == originalGetter ? resolver : null,
                code => code.Any(HarmonyIl.IsCall(resolver)));
            report.RequireSucceeded();
            return rewriter.InstructionsChecked(report.Operation);
        }
    }
}
