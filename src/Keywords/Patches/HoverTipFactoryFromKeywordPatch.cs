using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Routes <see cref="HoverTipFactory.FromKeyword" /> calls for registered mod <see cref="CardKeyword" />
    ///         values to <see cref="ModKeywordRegistry.CreateHoverTip(string)" />, using their registered title,
    ///         description, and icon instead of the numeric localization-key fallback for unknown enum values. Native
    ///         keywords and unregistered values continue through the original factory unchanged.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将已注册模组 <see cref="CardKeyword" /> 值的 <see cref="HoverTipFactory.FromKeyword" /> 调用路由到
    ///         <see cref="ModKeywordRegistry.CreateHoverTip(string)" />，使用其已注册的标题、描述与图标，
    ///         而不是未知枚举值对应的数字本地化键回退。原版关键词和未注册值仍由原方法处理。
    ///     </para>
    /// </summary>
    [HarmonyBefore(Const.BaseLibHarmonyId)]
    [HarmonyPriority(Priority.First)]
    internal sealed class HoverTipFactoryFromKeywordPatch : IPatchMethod
    {
        private static readonly Dictionary<CardKeyword, IHoverTip> ModKeywordTipCache = [];
        private static readonly Lock SyncRoot = new();
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

            lock (SyncRoot)
            {
                if (!ModKeywordTipCache.TryGetValue(keyword, out var cached))
                {
                    cached = ModKeywordRegistry.CreateHoverTip(definition.Id);
                    ModKeywordTipCache[keyword] = cached;
                }

                __result = cached;
            }

            return false;
        }
    }
}
