using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.FreePlay.Patches
{
    /// <summary>
    ///     Binds engine-level SetToFree calls into <see cref="FreePlayBindingRegistry" /> markers.
    /// </summary>
    internal sealed class CardModelSetToFreeBindingPatch : IPatchMethod
    {
        public static string PatchId => "card_model_set_to_free_binding";
        public static string Description => "Bind CardModel.SetToFree* calls to FreePlayBindingRegistry markers";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.SetToFreeThisTurn)),
                new(typeof(CardModel), nameof(CardModel.SetToFreeThisCombat)),
            ];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(MethodBase __originalMethod, CardModel __instance)
            // ReSharper restore InconsistentNaming
        {
            switch (__originalMethod.Name)
            {
                case nameof(CardModel.SetToFreeThisTurn):
                    FreePlayBindingRegistry.MarkCardFreeNextPlay(__instance);
                    return;
                case nameof(CardModel.SetToFreeThisCombat):
                    FreePlayBindingRegistry.MarkCardFreeThisCombat(__instance);
                    break;
            }
        }
    }
}
