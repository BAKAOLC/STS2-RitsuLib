using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.CardPiles.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Moves the active extra-hand holder into the vanilla hand container before manual-play enqueue.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         手动打牌动作入队前，将当前额外手牌卡牌容器移入原版手牌容器。
    ///     </para>
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
    ///     <para xml:lang="en">
    ///         Cancels active extra-hand targeting before the vanilla hand starts another card play.
    ///     </para>
    ///     <para xml:lang="zh-CN">原版手牌开始另一次出牌前，取消生效中的额外手牌目标选择。</para>
    /// </summary>
    internal sealed class ModExtraHandVanillaCardPlaySwitchPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_extra_hand_vanilla_card_play_switch";
        public static string Description => "Switch from extra-hand targeting to a newly selected vanilla hand card";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NPlayerHand), "StartCardPlay", [typeof(NHandCardHolder), typeof(bool)])];
        }

        public static void Prefix()
        {
            ModExtraHandPlayCoordinator.CancelActiveTargeting();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Restores a queued extra-hand card to its source pile when the vanilla action is canceled.
    ///     </para>
    ///     <para xml:lang="zh-CN">原版动作取消时，将已排队的额外手牌卡牌恢复到来源牌堆。</para>
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
