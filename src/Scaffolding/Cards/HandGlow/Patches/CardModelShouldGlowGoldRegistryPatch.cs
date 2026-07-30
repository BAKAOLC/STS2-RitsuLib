using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds <see cref="ModCardHandGlowRegistry" /> gold rules to
    ///         <see cref="CardModel.ShouldGlowGold" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="ModCardHandGlowRegistry" /> 的金色规则加入
    ///         <see cref="CardModel.ShouldGlowGold" /> 的计算。
    ///     </para>
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

        public static void Postfix(CardModel __instance, ref bool __result)
        {
            if (__result)
                return;

            if (ModCardHandGlowRegistry.EvaluateRegistryGold(__instance))
                __result = true;
        }
    }
}
