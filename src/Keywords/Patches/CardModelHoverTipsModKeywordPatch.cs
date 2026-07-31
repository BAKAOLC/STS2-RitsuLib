using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Removes hover tips for mod keywords whose
    ///         <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> is <see langword="false" /> from the native
    ///         <see cref="CardModel.HoverTips" /> sequence. Minted mod keywords live in
    ///         <c>CardModel.Keywords</c>, so the native getter already enumerates them and calls
    ///         <see cref="HoverTipFactory.FromKeyword" />; <see cref="HoverTipFactoryFromKeywordPatch" /> supplies the
    ///         registered tip. This postfix only implements the explicit opt-out.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从原版 <see cref="CardModel.HoverTips" /> 结果中移除
    ///         <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> 为
    ///         <see langword="false" /> 的模组关键词悬停提示。动态生成的模组关键词存放在
    ///         <c>CardModel.Keywords</c> 中，因此原版属性已会逐一调用
    ///         <see cref="HoverTipFactory.FromKeyword" />，并由
    ///         <see cref="HoverTipFactoryFromKeywordPatch" /> 提供注册的提示；此后置补丁仅实现显式排除功能。
    ///     </para>
    /// </summary>
    internal sealed class CardModelHoverTipsModKeywordPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_model_hover_tips_mod_keyword_exclude";

        public static string Description =>
            "Remove mod keyword hover tips from CardModel.HoverTips when IncludeInCardHoverTip is false";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
        }

        public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            HashSet<IHoverTip>? toRemove = null;
            foreach (var keyword in __instance.Keywords)
            {
                if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                    continue;

                if (definition.IncludeInCardHoverTip)
                    continue;

                toRemove ??= [];
                toRemove.Add(HoverTipFactory.FromKeyword(keyword));
            }

            if (toRemove is null)
                return;

            __result = [.. __result.Where(tip => !toRemove.Contains(tip))];
        }
    }
}
