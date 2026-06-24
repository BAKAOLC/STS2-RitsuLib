using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Implement on a pool model to override the large energy icon path resolved from
    ///     <see cref="EnergyIconHelper.GetPath(string)" />.
    ///     在池模型上实现，用于覆盖从
    ///     <see cref="EnergyIconHelper.GetPath(string)" /> 解析到的大型能量图标路径。
    /// </summary>
    public interface IModBigEnergyIconPool
    {
        /// <summary>
        ///     Custom large energy icon path for this pool’s <see cref="MegaCrit.Sts2.Core.Models.IPoolModel.EnergyColorName" />.
        ///     此池的 <see cref="MegaCrit.Sts2.Core.Models.IPoolModel.EnergyColorName" /> 对应的自定义大型能量图标路径。
        /// </summary>
        string? BigEnergyIconPath { get; }
    }

    /// <summary>
    ///     Prefixes <see cref="EnergyIconHelper.GetPath(string)" /> so pools implementing <see cref="IModBigEnergyIconPool" />
    ///     can replace the resolved big icon path.
    ///     为 <see cref="EnergyIconHelper.GetPath(string)" /> 添加前缀，使实现 <see cref="IModBigEnergyIconPool" /> 的池
    ///     可以替换解析出的大图标路径。
    /// </summary>
    internal class EnergyIconHelperPathPatch : IPatchMethod
    {
        public static string PatchId => "energy_icon_helper_big_icon_override";

        public static string Description =>
            "Allow mod pools to override the large energy icon path resolved by EnergyIconHelper";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), [typeof(string)]),
            ];
        }

        /// <summary>
        ///     Delegates to <see cref="ModBigEnergyIconHelper.TryOverridePath" /> to substitute a cached pool override.
        ///     委托给 <see cref="ModBigEnergyIconHelper.TryOverridePath" />，以替换为缓存的池覆盖。
        /// </summary>
        public static bool Prefix(string prefix, ref string __result)
        {
            return ModBigEnergyIconHelper.TryOverridePath(prefix, ref __result);
        }
    }

    internal static class ModBigEnergyIconHelper
    {
        private static readonly Lock UnmappedContentWarningGate = new();
        private static readonly HashSet<string> UnmappedContentWarningLoggedKeys = new(StringComparer.Ordinal);
        private static Dictionary<string, string>? _cache;

        public static bool TryOverridePath(string prefix, ref string result)
        {
            _cache ??= BuildCache();

            if (!_cache.TryGetValue(prefix, out var path))
                return true;

            result = path;
            return false;
        }

        private static Dictionary<string, string> BuildCache()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var character in ModContentRegistry.GetModCharacters())
                AddPoolIfMapped(dict, character.CardPool);

            var cardPools = ModelDb.AllCardPools.ToArray();
            foreach (var pool in cardPools)
                AddPoolIfMapped(dict, pool);
            WarnUnmappedCards(cardPools);

            var relicPools = ModelDb.AllRelicPools.ToArray();
            foreach (var pool in relicPools)
                AddPoolIfMapped(dict, pool);
            WarnUnmappedRelics(relicPools);

            var potionPools = ModelDb.AllPotionPools.ToArray();
            foreach (var pool in potionPools)
                AddPoolIfMapped(dict, pool);
            WarnUnmappedPotions(potionPools);

            return dict;
        }

        private static void WarnUnmappedCards(CardPoolModel[] pools)
        {
            foreach (var card in ModelDb.AllCards)
            {
                if (pools.Any(pool => pool.AllCardIds.Contains(card.Id)))
                    continue;

                WarnUnmappedContentOnce("card", card.Id);
            }
        }

        private static void WarnUnmappedRelics(RelicPoolModel[] pools)
        {
            foreach (var relic in ModelDb.AllRelics)
            {
                if (pools.Any(pool => pool.AllRelicIds.Contains(relic.Id)))
                    continue;

                WarnUnmappedContentOnce("relic", relic.Id);
            }
        }

        private static void WarnUnmappedPotions(PotionPoolModel[] pools)
        {
            foreach (var potion in ModelDb.AllPotions)
            {
                if (pools.Any(pool => pool.AllPotionIds.Contains(potion.Id)))
                    continue;

                WarnUnmappedContentOnce("potion", potion.Id);
            }
        }

        private static void WarnUnmappedContentOnce(string contentType, ModelId id)
        {
            var key = contentType + "\0" + id;
            lock (UnmappedContentWarningGate)
            {
                if (!UnmappedContentWarningLoggedKeys.Add(key))
                    return;
            }

            RitsuLibFramework.Logger.Warn(
                $"[Content] {contentType} '{id}' is registered in ModelDb but is not contained in any {contentType} pool. " +
                "Skipping it while building energy icon overrides.");
        }

        private static void AddPoolIfMapped(Dictionary<string, string> dict, IPoolModel pool)
        {
            if (pool is not IModBigEnergyIconPool mapped)
                return;

            if (string.IsNullOrWhiteSpace(mapped.BigEnergyIconPath))
                return;

            if (!AssetPathDiagnostics.Exists(mapped.BigEnergyIconPath!, pool,
                    nameof(IModBigEnergyIconPool.BigEnergyIconPath)))
                return;

            dict.TryAdd(pool.EnergyColorName, mapped.BigEnergyIconPath!);
        }
    }
}
