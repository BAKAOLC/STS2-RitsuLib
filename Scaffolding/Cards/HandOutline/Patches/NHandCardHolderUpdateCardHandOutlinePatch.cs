using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     After vanilla hand highlight color (playable / gold / red), applies <see cref="ModCardHandOutlineRegistry" /> when
    ///     a matching rule exists.
    ///     在原版手牌高亮颜色（可打出/金色/红色）之后，存在匹配规则时应用 <see cref="ModCardHandOutlineRegistry" />。
    /// </summary>
    internal sealed class NHandCardHolderUpdateCardHandOutlinePatch : IPatchMethod
    {
        public static string PatchId => "n_hand_card_holder_update_card_hand_outline";

        public static string Description => "Apply ModCardHandOutlineRegistry colors to NHandCardHolder.UpdateCard";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard), true)];
        }

        public static void Postfix(NHandCardHolder __instance)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(__instance, out var model, out var rule))
                return;

            ModCardHandOutlinePatchHelper.ApplyHighlight(__instance, model, rule);
        }
    }
}
