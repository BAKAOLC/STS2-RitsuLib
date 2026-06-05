using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="NCardPlay.TryPlayCard" /> for <see cref="TargetType.AnyPlayer" /> in multiplayer.
    ///     Vanilla treats AnyPlayer as a non-targeted type, calling <c>TryManualPlay(null)</c>
    ///     and discarding the selected target.
    ///     Direct field/method access where the publicized STS2 reference exposes them; <c>Cleanup(bool)</c> remains
    ///     <c>protected</c> in the reference build, so invocation uses Harmony <see cref="AccessTools" />.
    ///     修复多人模式下 <see cref="NCardPlay.TryPlayCard" /> 对 <see cref="TargetType.AnyPlayer" /> 的处理。
    ///     原版会把 AnyPlayer 视为非目标类型，调用 <c>TryManualPlay(null)</c>
    ///     并丢弃选中的目标。
    ///     并丢弃选中的目标。
    ///     在 publicized STS2 引用暴露字段/方法时直接访问；<c>Cleanup(bool)</c> 在引用构建中仍为
    ///     <c>protected</c>，因此调用使用 Harmony <see cref="AccessTools" />。
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

            __instance._isTryingToPlayCard = true;
            var played = card!.TryManualPlay(target);
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
