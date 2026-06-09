using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     Postfixes vanilla card description builders so mod keywords with
    ///     <see cref="ModKeywordDefinition.CardDescriptionPlacement" /> inject BBCode like vanilla keywords.
    ///     对原版卡牌描述构建器追加 postfix，使带 <see cref="ModKeywordDefinition.CardDescriptionPlacement" /> 的 mod 关键词像原版关键词一样注入 BBCode。
    /// </summary>
    internal sealed class ModKeywordCardDescriptionPatches : IPatchMethod
    {
        public static string PatchId => "card_mod_keyword_description";
        public static string Description => "Inject mod keyword BBCode into CardModel description rendering";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [CardDescriptionPatchTarget.Create()];
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            ModKeywordCardDescriptionInjector.AppendFragments(__instance, ref __result);
        }
    }
}
