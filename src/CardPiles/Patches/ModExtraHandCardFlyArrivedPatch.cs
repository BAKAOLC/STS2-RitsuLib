using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Routes completion of an exact vanilla card-fly visual to the corresponding extra-hand visual.
    ///     将原版单张卡牌飞行视觉的完成事件路由到对应的额外手牌视觉。
    /// </summary>
    internal sealed class ModExtraHandCardFlyArrivedPatch : IPatchMethod
    {
        private static readonly FieldInfo? CardField = AccessTools.Field(typeof(NCardFlyVfx), "_card");
        private static readonly FieldInfo? IsAddingField = AccessTools.Field(typeof(NCardFlyVfx), "_isAddingToPile");

        public static string PatchId => "ritsulib_extra_hand_card_fly_arrived";
        public static string Description => "Notify extra-hand visuals when their exact vanilla card fly completes";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyVfx), "OnCardExitedTree", Type.EmptyTypes)];
        }

        public static void Prefix(NCardFlyVfx __instance)
        {
            if (CardField?.GetValue(__instance) is not NCard { Model: CardModel card })
                return;
            if (IsAddingField?.GetValue(__instance) is not true)
                return;
            if (card.Pile is not { } pile
                || !ModCardPileRegistry.TryGetByPileType(pile.Type, out var definition)
                || definition.Style != ModCardPileUiStyle.ExtraHand)
                return;

            ModCardPileButtonRegistry.TryGetExtraHand(definition)?.NotifyCardArrived(card);
        }
    }
}
