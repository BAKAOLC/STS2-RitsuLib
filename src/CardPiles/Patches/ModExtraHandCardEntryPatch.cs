using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes cards entering an extra hand through the vanilla hand-holder animation.
    ///     </para>
    ///     <para xml:lang="zh-CN">使进入额外手牌的卡牌使用原版手牌容器动画。</para>
    /// </summary>
    internal sealed class ModExtraHandCardEntryPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_entry";

        public static string Description =>
            "Use vanilla hand-holder motion when cards enter an extra hand";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCardFlyVfx), nameof(NCardFlyVfx.Create),
                    [typeof(NCard), typeof(PileType), typeof(bool), typeof(string)]),
            ];
        }

        public static bool Prefix(
            NCard card,
            PileType pileType,
            bool isAddingToPile,
            ref NCardFlyVfx? __result)
        {
            if (!isAddingToPile
                || !ModCardPileRegistry.TryGetByPileType(pileType, out var definition)
                || definition.Style != ModCardPileUiStyle.ExtraHand
                || ModCardPileButtonRegistry.TryGetExtraHand(definition) is not { } extraHand
                || !extraHand.TryBeginHandEntryAnimation(card))
                return true;

            __result = null;
            return false;
        }
    }
}
