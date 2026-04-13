using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Combat.CardTargeting.Patches
{
    /// <summary>
    ///     Fixes <see cref="NCardPlay.TryPlayCard" /> for <see cref="TargetType.AnyPlayer" /> in multiplayer.
    ///     Vanilla treats AnyPlayer as a non-targeted type, calling <c>TryManualPlay(null)</c>
    ///     and discarding the selected target.
    /// </summary>
    internal sealed class NCardPlayTryPlayCardAnyPlayerPatch : IPatchMethod
    {
        private static readonly Func<NCardPlay, CardModel?> GetCard =
            AccessTools.MethodDelegate<Func<NCardPlay, CardModel?>>(
                AccessTools.DeclaredPropertyGetter(typeof(NCardPlay), "Card"));

        private static readonly AccessTools.FieldRef<NCardPlay, bool> IsTryingToPlayCardRef =
            AccessTools.FieldRefAccess<NCardPlay, bool>("_isTryingToPlayCard");

        private static readonly Action<NCardPlay, CardModel> CannotPlayThisCardFtueCheck =
            AccessTools.MethodDelegate<Action<NCardPlay, CardModel>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", [typeof(CardModel)]));

        private static readonly Action<NCardPlay> AutoDisableCannotPlayCardFtueCheck =
            AccessTools.MethodDelegate<Action<NCardPlay>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "AutoDisableCannotPlayCardFtueCheck"));

        private static readonly Action<NCardPlay, bool> Cleanup =
            AccessTools.MethodDelegate<Action<NCardPlay, bool>>(
                AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", [typeof(bool)]));

        public static string PatchId => "card_any_player_try_play_card";

        public static string Description =>
            "Fix NCardPlay.TryPlayCard to treat AnyPlayer as single-target in multiplayer";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(NCardPlay __instance, Creature? target)
            // ReSharper restore InconsistentNaming
        {
            var card = GetCard(__instance);
            if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card))
                return true;

            if (target == null)
            {
                __instance.CancelPlayCard();
                return false;
            }

            if (!__instance.Holder.CardModel!.CanPlayTargeting(target))
            {
                CannotPlayThisCardFtueCheck(__instance, __instance.Holder.CardModel!);
                __instance.CancelPlayCard();
                return false;
            }

            IsTryingToPlayCardRef(__instance) = true;
            var played = card!.TryManualPlay(target);
            IsTryingToPlayCardRef(__instance) = false;

            if (played)
            {
                AutoDisableCannotPlayCardFtueCheck(__instance);
                if (__instance.Holder.IsInsideTree())
                {
                    var size = __instance.GetViewport().GetVisibleRect().Size;
                    __instance.Holder.SetTargetPosition(new(size.X / 2f, size.Y - __instance.Holder.Size.Y));
                }

                Cleanup(__instance, true);
                NCombatRoom.Instance?.Ui.Hand.TryGrabFocus();
            }
            else
            {
                __instance.CancelPlayCard();
            }

            return false;
        }
    }
}
