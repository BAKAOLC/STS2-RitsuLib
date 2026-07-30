using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Applies the matching <see cref="ModCardHandOutlineRegistry" /> color after the vanilla hand-card
    ///         highlight is updated.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在原版手牌高亮更新后，应用匹配的 <see cref="ModCardHandOutlineRegistry" /> 颜色。
    ///     </para>
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
