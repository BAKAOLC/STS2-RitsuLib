using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Cards.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds registered mod keyword text after the native card description builder has assembled its result.
    ///         Keywords configured for the beginning or end are inserted at the corresponding boundary, matching the
    ///         two placement groups used by the game.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在原版卡牌描述构建器完成文本后，加入已注册模组关键词的文本。配置为描述前或描述后的关键词会
    ///         分别插入对应边界，与游戏使用的两组插入位置一致。
    ///     </para>
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
