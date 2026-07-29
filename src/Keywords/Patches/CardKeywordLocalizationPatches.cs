using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Uses registered mod keyword metadata when the native <c>CardKeywordExtensions.GetTitle</c> method
    ///         receives a registered mod <see cref="CardKeyword" />. Native keywords and unregistered values continue
    ///         through the original method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当原版 <c>CardKeywordExtensions.GetTitle</c> 方法收到已注册的模组 <see cref="CardKeyword" /> 时，
    ///         使用其注册信息。原版关键词和未注册值仍由原方法处理。
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
    ///         Uses registered mod keyword metadata when the native <c>CardKeywordExtensions.GetDescription</c>
    ///         method receives a registered mod <see cref="CardKeyword" />. Native keywords and unregistered values
    ///         continue through the original method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当原版 <c>CardKeywordExtensions.GetDescription</c> 方法收到已注册的模组
    ///         <see cref="CardKeyword" /> 时，使用其注册信息。原版关键词和未注册值仍由原方法处理。
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
    ///         Uses registered localization when the native <c>CardKeywordExtensions.GetCardText</c> method receives
    ///         a registered mod <see cref="CardKeyword" />. Native keywords and unregistered values continue through
    ///         the original method.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         当原版 <c>CardKeywordExtensions.GetCardText</c> 方法收到已注册的模组
    ///         <see cref="CardKeyword" /> 时，使用其注册的本地化文本。原版关键词和未注册值仍由原方法处理。
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
