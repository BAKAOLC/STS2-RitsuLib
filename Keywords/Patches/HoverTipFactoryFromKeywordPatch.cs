using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Keywords.Patches
{
    /// <summary>
    ///     Routes <see cref="HoverTipFactory.FromKeyword" /> calls for minted mod <see cref="CardKeyword" />
    ///     values to <see cref="ModKeywordRegistry.CreateHoverTip" /> so the hover tip uses the registered
    ///     title / description / icon instead of the slugified numeric fallback produced by
    ///     <c>CardKeywordExtensions.GetLocKeyPrefix</c> for unknown enum values. Vanilla keywords skip the
    ///     prefix entirely and fall through to the original factory.
    ///     将铸造的 mod <see cref="CardKeyword" />
    ///     值的 <see cref="HoverTipFactory.FromKeyword" /> 调用路由到 <see cref="ModKeywordRegistry.CreateHoverTip" />，使悬停提示使用已注册的
    ///     标题/描述/图标，而不是 <c>CardKeywordExtensions.GetLocKeyPrefix</c> 为未知 enum 值生成的 slugified numeric fallback。原版关键词会跳过该
    ///     prefix，并完全落回原始工厂。
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
