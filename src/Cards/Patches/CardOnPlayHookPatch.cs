using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Cards.Patches
{
    internal sealed class CardOnPlayHookPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_on_play_hook";

        public static string Description => "Dispatch RitsuLib card OnPlay hooks from CardModel.OnPlayWrapper";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                PatchTarget.AsyncMethod(typeof(CardModel), nameof(CardModel.OnPlayWrapper),
                    typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)),
            ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const string operation = "[CardOnPlayHook] OnPlayWrapper injection";
            var onPlayMethod = HarmonyIl.RequireMethod(
                AccessTools.Method(
                    typeof(CardModel),
                    "OnPlay",
                    [typeof(PlayerChoiceContext), typeof(CardPlay)]),
                operation);
            var wrapperMethod = HarmonyIl.RequireMethod(
                AccessTools.Method(
                    typeof(CardOnPlayHook),
                    nameof(CardOnPlayHook.RunCardOnPlayHooks)),
                operation);
            var rewriter = HarmonyIlRewriter.From(instructions);

#if STS2_AT_LEAST_0_109_0
            var report = rewriter.RedirectCalls(
                operation,
                called => called == onPlayMethod ? wrapperMethod : null,
                code => code.Any(HarmonyIl.IsCall(wrapperMethod)));
#else
            var report = HarmonyAsyncIl.RedirectAwaitedCalls(
                rewriter,
                operation,
                onPlayMethod,
                wrapperMethod,
                code => code.Any(HarmonyIl.IsCall(wrapperMethod)));
#endif
            return rewriter.InstructionsChecked(report);
        }
    }
}
