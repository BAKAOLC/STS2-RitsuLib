using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Notifies the destination extra-hand container when a card's fly-in visual completes.
    ///     </para>
    ///     <para xml:lang="zh-CN">卡牌飞入动画完成时通知目标额外手牌容器。</para>
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
            if (CardField?.GetValue(__instance) is not NCard { Model: { } card })
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
