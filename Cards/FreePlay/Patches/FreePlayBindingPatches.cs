using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.FreePlay.Patches
{
    /// <summary>
    ///     Binds engine-level SetToFree calls into <see cref="FreePlayBindingRegistry" /> markers.
    ///     将引擎级 SetToFree 调用绑定到 <see cref="FreePlayBindingRegistry" /> 标记。
    /// </summary>
    internal sealed class CardModelSetToFreeThisTurnBindingPatch : IPatchMethod
    {
        public static string PatchId => "card_model_set_to_free_this_turn_binding";
        public static string Description => "Bind CardModel.SetToFreeThisTurn calls to FreePlayBindingRegistry markers";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.SetToFreeThisTurn))];
        }

        public static void Postfix(CardModel __instance)
        {
            FreePlayBindingRegistry.MarkCardFreeNextPlay(__instance);
        }
    }

    internal sealed class CardModelSetToFreeThisCombatBindingPatch : IPatchMethod
    {
        public static string PatchId => "card_model_set_to_free_this_combat_binding";

        public static string Description =>
            "Bind CardModel.SetToFreeThisCombat calls to FreePlayBindingRegistry markers";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), nameof(CardModel.SetToFreeThisCombat))];
        }

        public static void Postfix(CardModel __instance)
        {
            FreePlayBindingRegistry.MarkCardFreeThisCombat(__instance);
        }
    }
}
