using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <see cref="HoverTipFactory.FromKeyword" /> calls for minted mod <see cref="CardKeyword" /> values
    ///         to <see cref="ModKeywordRegistry.CreateHoverTip(string)" />. Registered titles, descriptions, and icons
    ///         are therefore used instead of the numeric localization-key fallback that
    ///         <c>CardKeywordExtensions.GetLocKeyPrefix</c> produces for unknown enum values. Native keywords and
    ///         unregistered values continue through the original factory unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将动态生成的模组 <see cref="CardKeyword" /> 值传入
    ///         <see cref="HoverTipFactory.FromKeyword" /> 时，改由
    ///         <see cref="ModKeywordRegistry.CreateHoverTip(string)" /> 创建悬停提示，从而使用注册的标题、描述和
    ///         图标，而不是 <c>CardKeywordExtensions.GetLocKeyPrefix</c> 为未知枚举值生成的数字本地化键。
    ///         原版关键词和未注册值仍由原方法处理。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.First)]
    internal sealed class HoverTipFactoryFromKeywordPatch : IPatchMethod
    {
        public static string PatchId => "ritsulib_hover_tip_factory_from_keyword_mod_route";

        public static string Description =>
            "Route HoverTipFactory.FromKeyword to ModKeywordRegistry for minted mod CardKeyword values";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))];
        }

        public static bool Prefix(CardKeyword keyword, ref IHoverTip __result)
        {
            if (!ModKeywordRegistry.TryGetByCardKeyword(keyword, out var definition))
                return true;

            __result = ModKeywordRegistry.CreateHoverTip(definition.Id);
            return false;
        }
    }
}
