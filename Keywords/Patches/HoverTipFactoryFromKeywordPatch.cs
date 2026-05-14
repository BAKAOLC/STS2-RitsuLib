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
    public sealed class HoverTipFactoryFromKeywordPatch : IPatchMethod
    {
        private static readonly Dictionary<CardKeyword, IHoverTip> ModKeywordTipCache = [];
        private static readonly Lock SyncRoot = new();

        /// <inheritdoc />
        public static string PatchId => "ritsulib_hover_tip_factory_from_keyword_mod_route";

        /// <inheritdoc />
        public static string Description =>
            "Route HoverTipFactory.FromKeyword to ModKeywordRegistry for minted mod CardKeyword values";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Short-circuits mod keyword lookups before vanilla's slug-based <see cref="HoverTip" /> construction
        ///     runs, returning a cached registry-built tip. Non-mod values return <c>true</c> so vanilla executes.
        ///     在原版基于 slug 的 <see cref="HoverTip" /> 构造
        ///     运行前短路 mod 关键词查找，返回一个注册表构建的缓存提示。非 mod 值返回 <c>true</c>，使原版继续执行。
        /// </summary>
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
        // ReSharper restore InconsistentNaming
    }
}
