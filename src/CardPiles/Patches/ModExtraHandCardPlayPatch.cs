using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     Makes an active extra-hand holder discoverable by <c>NCardPlayQueue</c> immediately before vanilla
    ///     enqueues a manual play.
    ///     在原版加入手动打牌动作前，使活动的额外手牌 holder 可被 <c>NCardPlayQueue</c> 找到。
    /// </summary>
    internal sealed class ModExtraHandCardPlayPreparePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_play_prepare";
        public static string Description => "Prepare playable extra-hand holders for vanilla card-play enqueue";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        public static void Prefix(NCardPlay __instance)
        {
            ModExtraHandPlayCoordinator.PrepareForEnqueue(__instance);
        }
    }

    /// <summary>
    ///     Restores a queued extra-hand card to its source pile when the vanilla action is canceled.
    ///     原版动作取消时，将已排队的额外手牌卡牌恢复到来源牌堆。
    /// </summary>
    internal sealed class ModExtraHandCardPlayCancelPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_card_play_cancel";
        public static string Description => "Restore canceled queued extra-hand cards to their source pile";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PlayCardAction), "CancelAction", Type.EmptyTypes)];
        }

        public static void Postfix(PlayCardAction __instance)
        {
            ModExtraHandPlayCoordinator.RestoreCancelledAction(__instance);
        }
    }
}
