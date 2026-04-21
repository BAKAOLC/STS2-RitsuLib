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
    /// </summary>
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
