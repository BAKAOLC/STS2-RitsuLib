using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Makes <see cref="NCardPlay.TryPlayCard" /> pass the selected creature when playing a multiplayer
    ///         <see cref="TargetType.AnyPlayer" /> card.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         使 <see cref="NCardPlay.TryPlayCard" /> 在多人模式中打出
    ///         <see cref="TargetType.AnyPlayer" /> 卡牌时传递已选生物。
    ///     </para>
    /// </summary>
    internal sealed class NCardPlayTryPlayCardAnyPlayerPatch : IPatchMethod
    {
        private static readonly Action<NCardPlay, bool> InvokeCleanup =
            AccessTools.MethodDelegate<Action<NCardPlay, bool>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)])!);

        public static string PatchId => "card_any_player_try_play_card";

        public static string Description =>
            "Fix NCardPlay.TryPlayCard to treat AnyPlayer as single-target in multiplayer";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        public static bool Prefix(NCardPlay __instance, Creature? target)
        {
            var card = __instance.Card;
            if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card))
                return true;

            if (target == null)
            {
                __instance.CancelPlayCard();
                return false;
            }

            if (!__instance.Holder.CardModel!.CanPlayTargeting(target))
            {
                __instance.CannotPlayThisCardFtueCheck(__instance.Holder.CardModel!);
                __instance.CancelPlayCard();
                return false;
            }

            bool played;
            __instance._isTryingToPlayCard = true;
            try
            {
                played = card!.TryManualPlay(target);
            }
            finally
            {
                __instance._isTryingToPlayCard = false;
            }

            if (played)
            {
                __instance.AutoDisableCannotPlayCardFtueCheck();
                if (__instance.Holder.IsInsideTree())
                {
                    var size = __instance.GetViewport().GetVisibleRect().Size;
                    __instance.Holder.SetTargetPosition(new(size.X / 2f, size.Y - __instance.Holder.Size.Y));
                }

                InvokeCleanup(__instance, true);
                CardPlayUiFocus.AfterCardPlayFinished();
            }
            else
            {
                __instance.CancelPlayCard();
            }

            return false;
        }
    }
}
