using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="NCardPlay.TryPlayCard" /> for <see cref="CustomTargetType.Anyone" /> by ensuring the selected
    ///     target is passed into <c>TryManualPlay</c> rather than being dropped as if it were a non-targeted card.
    ///     修复 <see cref="NCardPlay.TryPlayCard" /> 对 <see cref="CustomTargetType.Anyone" /> 的处理，确保选中的
    ///     目标传入 <c>TryManualPlay</c>，而不是像非目标卡牌一样被丢弃。
    /// </summary>
    internal sealed class NCardPlayTryPlayCardAnyonePatch : IPatchMethod
    {
        private static readonly Action<NCardPlay, bool> InvokeCleanup =
            AccessTools.MethodDelegate<Action<NCardPlay, bool>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)])!);

        public static string PatchId => "card_anyone_try_play_card";

        public static string Description =>
            "Fix NCardPlay.TryPlayCard to pass target for Anyone";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NCardPlay __instance, Creature? target)
            // ReSharper restore InconsistentNaming
        {
            var card = __instance.Card;
            if (card == null || card.TargetType != CustomTargetType.Anyone)
                return true;

            if (target == null || __instance.Holder.CardModel == null)
            {
                __instance.CancelPlayCard();
                return false;
            }

            if (!__instance.Holder.CardModel.CanPlayTargeting(target))
            {
                __instance.CannotPlayThisCardFtueCheck(__instance.Holder.CardModel);
                __instance.CancelPlayCard();
                return false;
            }

            __instance._isTryingToPlayCard = true;
            var played = card.TryManualPlay(target);
            __instance._isTryingToPlayCard = false;

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
