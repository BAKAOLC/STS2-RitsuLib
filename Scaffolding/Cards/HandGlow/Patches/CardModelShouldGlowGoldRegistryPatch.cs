using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow.Patches
{
    /// <summary>
    ///     ORs <see cref="ModCardHandGlowRegistry" /> gold rules into <see cref="CardModel.ShouldGlowGold" />.
    /// </summary>
    internal sealed class CardModelShouldGlowGoldRegistryPatch : IPatchMethod
    {
        public static string PatchId => "card_model_should_glow_gold_registry";

        public static string Description =>
            "Merge ModCardHandGlowRegistry gold predicates into CardModel.ShouldGlowGold";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "ShouldGlowGold", null, true, MethodType.Getter)];
        }

        // ReSharper disable InconsistentNaming
        public static void Postfix(CardModel __instance, ref bool __result)
            // ReSharper restore InconsistentNaming
        {
            if (__result)
                return;

            if (ModCardHandGlowRegistry.EvaluateRegistryGold(__instance))
                __result = true;
        }
    }
}
