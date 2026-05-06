using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow.Patches
{
    /// <summary>
    ///     ORs <see cref="ModCardHandGlowRegistry" /> red rules into <see cref="CardModel.ShouldGlowRed" />.
    /// </summary>
    internal sealed class CardModelShouldGlowRedRegistryPatch : IPatchMethod
    {
        public static string PatchId => "card_model_should_glow_red_registry";

        public static string Description => "Merge ModCardHandGlowRegistry red predicates into CardModel.ShouldGlowRed";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "ShouldGlowRed", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result)
                return;

            if (ModCardHandGlowRegistry.EvaluateRegistryRed(__instance))
                __result = true;
        }
    }
}
