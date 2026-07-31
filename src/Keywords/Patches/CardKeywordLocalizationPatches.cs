using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes native <c>CardKeywordExtensions.GetTitle</c> calls for minted values through the corresponding
    ///         registered mod keyword metadata.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将动态生成关键词值的原版 <c>CardKeywordExtensions.GetTitle</c> 调用路由到对应的模组关键词注册信息。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.First)]
    internal sealed class CardKeywordGetTitleModRoutePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_keyword_get_title_mod_route";

        public static string Description =>
            "Route CardKeywordExtensions.GetTitle to ModKeywordRegistry for minted mod CardKeyword values";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(CardKeywordExtensions),
                    nameof(CardKeywordExtensions.GetTitle),
                    [typeof(CardKeyword)]),
            ];
        }

        public static bool Prefix(CardKeyword keyword, ref LocString __result)
        {
            if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                return true;

            __result = new(definition.TitleTable, definition.TitleKey);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes native <c>CardKeywordExtensions.GetDescription</c> calls for minted values through the
    ///         corresponding registered mod keyword metadata.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将动态生成关键词值的原版 <c>CardKeywordExtensions.GetDescription</c> 调用路由到对应的模组关键词注册信息。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.First)]
    internal sealed class CardKeywordGetDescriptionModRoutePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_keyword_get_description_mod_route";

        public static string Description =>
            "Route CardKeywordExtensions.GetDescription to ModKeywordRegistry for minted mod CardKeyword values";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(CardKeywordExtensions),
                    nameof(CardKeywordExtensions.GetDescription),
                    [typeof(CardKeyword)]),
            ];
        }

        public static bool Prefix(CardKeyword keyword, ref LocString __result)
        {
            if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                return true;

            __result = new(definition.DescriptionTable, definition.DescriptionKey);
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes native <c>CardKeywordExtensions.GetCardText</c> calls for minted values through the corresponding
    ///         registered mod keyword metadata.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将动态生成关键词值的原版 <c>CardKeywordExtensions.GetCardText</c> 调用路由到对应的模组关键词注册信息。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.First)]
    internal sealed class CardKeywordGetCardTextModRoutePatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_card_keyword_get_card_text_mod_route";

        public static string Description =>
            "Route CardKeywordExtensions.GetCardText to ModKeywordRegistry for minted mod CardKeyword values";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(
                    typeof(CardKeywordExtensions),
                    nameof(CardKeywordExtensions.GetCardText),
                    [typeof(CardKeyword)]),
            ];
        }

        public static bool Prefix(CardKeyword keyword, ref string __result)
        {
            if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                return true;

            __result = ModKeywordRegistry.GetCardText(definition.Id);
            return false;
        }
    }
}
