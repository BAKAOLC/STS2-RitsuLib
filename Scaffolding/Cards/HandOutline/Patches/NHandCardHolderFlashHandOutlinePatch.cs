using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandOutline.Patches
{
    /// <summary>
    ///     Tints the brief hand flash VFX to match <see cref="ModCardHandOutlineRegistry" /> when a rule matches.
    ///     规则匹配时，将短暂的手牌闪光 VFX 染色为匹配 <see cref="ModCardHandOutlineRegistry" />。
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

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NHandCardHolder __instance)
        {
            if (!ModCardHandOutlinePatchHelper.TryGetRule(__instance, out var model, out var rule))
                return;

            ModCardHandOutlinePatchHelper.ApplyFlash(__instance, model, rule);
        }
    }
}
