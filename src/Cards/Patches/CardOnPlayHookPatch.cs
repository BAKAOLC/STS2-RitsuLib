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
            var onPlayMethod = AccessTools.Method(
                typeof(CardModel),
                "OnPlay",
                [typeof(PlayerChoiceContext), typeof(CardPlay)]);
            var wrapperMethod = AccessTools.Method(
                typeof(CardOnPlayHook),
                nameof(CardOnPlayHook.RunCardOnPlayHooks));
            var rewriter = HarmonyIlRewriter.From(instructions);
            if (onPlayMethod == null || wrapperMethod == null)
                return rewriter.Instructions();

            var report = HarmonyAsyncIl.RedirectAwaitedCalls(
                rewriter,
                operation,
                onPlayMethod,
                wrapperMethod,
                code => code.Any(HarmonyIl.IsCall(wrapperMethod)));
            if (!report.Succeeded || report.Applied != 1)
                RitsuLibFramework.Logger.Warn(report.Describe());

            return rewriter.InstructionsChecked(operation);
        }
    }
}
