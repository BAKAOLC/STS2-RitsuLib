using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies the matching <see cref="ModCardHandOutlineRegistry" /> color to the hand-card flash.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将匹配的 <see cref="ModCardHandOutlineRegistry" /> 颜色应用到手牌闪光效果。
    ///     </para>
    /// </summary>
    internal sealed class NHandCardHolderFlashHandOutlinePatch : IPatchMethod
    {
        public static string PatchId => "n_hand_card_holder_flash_hand_outline";

        public static string Description => "Apply ModCardHandOutlineRegistry colors to NHandCardHolder.Flash";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHandCardHolder), nameof(NHandCardHolder.Flash), true)];
        }

        public static void Postfix(NHandCardHolder __instance)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(__instance, out var model, out var rule))
                return;

            ModCardHandOutlinePatchHelper.ApplyFlash(__instance, model, rule);
        }
    }
}
