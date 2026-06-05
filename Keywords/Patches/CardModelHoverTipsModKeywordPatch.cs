using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Strips hover tips for mod keywords whose <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> is
    ///     <c>false</c> from the vanilla <see cref="CardModel.HoverTips" /> enumeration. Mod keywords now live
    ///     inside vanilla <c>CardModel.Keywords</c> as minted <c>CardKeyword</c> values, so vanilla already
    ///     iterates them and calls <see cref="HoverTipFactory.FromKeyword" /> on each; the Registry routing
    ///     patch (<see cref="HoverTipFactoryFromKeywordPatch" />) returns a real hover tip for every mod
    ///     keyword. This postfix is only required to honor the opt-out flag.
    ///     从原版 <see cref="CardModel.HoverTips" /> 枚举中剥离 <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> 为
    ///     <c>false</c> 的 mod 关键词悬停提示。mod 关键词现在以铸造的 <c>CardKeyword</c> 值存在于
    ///     原版 <c>CardModel.Keywords</c> 内，因此原版已经会
    ///     遍历它们并对每个调用 <see cref="HoverTipFactory.FromKeyword" />；注册表路由
    ///     补丁（<see cref="HoverTipFactoryFromKeywordPatch" />）会为每个 mod
    ///     关键词返回真实悬停提示。此 postfix 只用于遵守退出标记。
    /// </summary>
    public sealed class CardModelHoverTipsModKeywordPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_card_model_hover_tips_mod_keyword_exclude";

        /// <inheritdoc />
        public static string Description =>
            "Remove mod keyword hover tips from CardModel.HoverTips when IncludeInCardHoverTip is false";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(CardModel), "HoverTips", MethodType.Getter)];
        }

        /// <summary>
        ///     Removes any mod-keyword hover tip that vanilla produced (via
        ///     <see cref="HoverTipFactory.FromKeyword" />) but is marked non-hoverable in the registry.
        ///     移除原版生成的、但在注册表中标记为不可悬停的任何 mod-keyword 悬停提示（通过
        ///     <see cref="HoverTipFactory.FromKeyword" /> 生成）。
        /// </summary>
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

            __result = __result.Where(tip => !toRemove.Contains(tip)).ToArray();
        }
    }
}
