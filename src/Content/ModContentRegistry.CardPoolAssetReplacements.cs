using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static long _cardPoolAssetReplacementWriteOrder;

        private static readonly Dictionary<string, Dictionary<string, CardPoolAssetReplacementLayer>>
            RegisteredCardPoolAssetReplacementsByEntry =
                new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers asset replacements for a card-pool ID entry. Non-null fields from later
        ///         registrations take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为卡牌牌池 ID 条目注册资源替换。后注册的非空字段优先。
        ///     </para>
        /// </summary>
        public void RegisterCardPoolAssetReplacement(string cardPoolEntry, CardPoolAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cardPoolEntry);
            ArgumentNullException.ThrowIfNull(assetProfile);
            var normalizedEntry = NormalizeOwnedModelIdEntry(cardPoolEntry);

            lock (SyncRoot)
            {
                if (!RegisteredCardPoolAssetReplacementsByEntry.TryGetValue(normalizedEntry, out var perMod))
                {
                    perMod = new(StringComparer.OrdinalIgnoreCase);
                    RegisteredCardPoolAssetReplacementsByEntry[normalizedEntry] = perMod;
                }

                perMod[ModId] = new(assetProfile, NextCardPoolAssetReplacementWriteOrder());
            }

            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info($"[Content] Registered card-pool asset replacement for '{normalizedEntry}'.");
        }

        /// <inheritdoc cref="RegisterCardPoolAssetReplacement(string,CardPoolAssetProfile)" />
        public void RegisterCardPoolAssetReplacement<TPool>(CardPoolAssetProfile assetProfile)
            where TPool : CardPoolModel
        {
            RegisterCardPoolAssetReplacement(ModelDb.GetId<TPool>().Entry, assetProfile);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes this mod's asset replacements for the specified card-pool ID entry.
        ///     </para>
        ///     <para xml:lang="zh-CN">移除此模组为指定卡牌牌池 ID 条目注册的资源替换。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when a registration was removed; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除了注册时为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        public bool RemoveCardPoolAssetReplacement(string cardPoolEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cardPoolEntry);
            var canonical = NormalizeOwnedModelIdEntry(cardPoolEntry);
            bool removed;

            lock (SyncRoot)
            {
                removed = RegisteredCardPoolAssetReplacementsByEntry.TryGetValue(canonical, out var perMod) &&
                          perMod.Remove(ModId);
                if (removed && perMod!.Count == 0)
                    RegisteredCardPoolAssetReplacementsByEntry.Remove(canonical);
            }

            if (!removed) return removed;
            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info($"[Content] Removed card-pool asset replacement for '{canonical}'.");

            return removed;
        }

        /// <inheritdoc cref="RemoveCardPoolAssetReplacement(string)" />
        public bool RemoveCardPoolAssetReplacement<TPool>()
            where TPool : CardPoolModel
        {
            return RemoveCardPoolAssetReplacement(ModelDb.GetId<TPool>().Entry);
        }

        internal static bool TryGetEffectiveCardPoolAssetReplacement(
            string cardPoolEntry,
            out CardPoolAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cardPoolEntry);
            var canonical = NormalizeOwnedModelIdEntry(cardPoolEntry);

            lock (SyncRoot)
            {
                if (RegisteredCardPoolAssetReplacementsByEntry.TryGetValue(canonical, out var layersByMod) &&
                    layersByMod.Count != 0)
                    return TryMergeCardPoolAssetReplacementLayers(layersByMod.Values, out assetProfile);

                assetProfile = CardPoolAssetProfile.Empty;
                return false;
            }
        }

        private static bool TryMergeCardPoolAssetReplacementLayers(
            IEnumerable<CardPoolAssetReplacementLayer> layers,
            out CardPoolAssetProfile mergedProfile)
        {
            var ordered = layers.ToList();
            if (ordered.Count == 0)
            {
                mergedProfile = CardPoolAssetProfile.Empty;
                return false;
            }

            ordered.Sort(static (x, y) => x.WriteOrder.CompareTo(y.WriteOrder));

            var merged = ordered[0].Profile;
            for (var i = 1; i < ordered.Count; i++)
                merged = CardPoolAssetProfiles.Merge(merged, ordered[i].Profile);

            mergedProfile = merged;
            return true;
        }

        private static long NextCardPoolAssetReplacementWriteOrder()
        {
            _cardPoolAssetReplacementWriteOrder++;
            return _cardPoolAssetReplacementWriteOrder;
        }

        private sealed record CardPoolAssetReplacementLayer(
            CardPoolAssetProfile Profile,
            long WriteOrder);
    }
}
