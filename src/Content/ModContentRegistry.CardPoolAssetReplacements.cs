using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static long _cardPoolAssetReplacementWriteOrder;

        private static readonly Dictionary<CardPoolAssetTarget, Dictionary<string, CardPoolAssetReplacementLayer>>
            RegisteredCardPoolAssetReplacements = [];

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
            RegisterCardPoolAssetReplacement(new CardPoolAssetTarget(NormalizeOwnedModelIdEntry(cardPoolEntry), null),
                assetProfile);
        }

        private void RegisterCardPoolAssetReplacement(CardPoolAssetTarget target, CardPoolAssetProfile assetProfile)
        {
            ArgumentNullException.ThrowIfNull(assetProfile);

            lock (SyncRoot)
            {
                if (!RegisteredCardPoolAssetReplacements.TryGetValue(target, out var perMod))
                {
                    perMod = new(StringComparer.OrdinalIgnoreCase);
                    RegisteredCardPoolAssetReplacements[target] = perMod;
                }

                perMod[ModId] = new(assetProfile, NextCardPoolAssetReplacementWriteOrder());
            }

            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info($"[Content] Registered card-pool asset replacement for '{target.GetEntry()}'.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers asset replacements for <typeparamref name="TPool" />. The registration follows the
        ///         pool type when character or shared-pool registration assigns its model ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TPool" /> 注册资源替换。角色或共享池注册分配模型 ID 时，此注册会跟随池类型。
        ///     </para>
        /// </summary>
        /// <typeparam name="TPool">
        ///     <para xml:lang="en">The target card-pool model type.</para>
        ///     <para xml:lang="zh-CN">目标卡牌池模型类型。</para>
        /// </typeparam>
        /// <param name="assetProfile">
        ///     <para xml:lang="en">The assets to override. Non-null fields from later registrations take precedence.</para>
        ///     <para xml:lang="zh-CN">要替换的资源。后注册的非空字段优先。</para>
        /// </param>
        public void RegisterCardPoolAssetReplacement<TPool>(CardPoolAssetProfile assetProfile)
            where TPool : CardPoolModel
        {
            RegisterCardPoolAssetReplacement(new CardPoolAssetTarget(null, typeof(TPool)), assetProfile);
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
            var removed = false;

            lock (SyncRoot)
            {
                var targets = RegisteredCardPoolAssetReplacements.Keys
                    .Where(target => target.GetEntry() == canonical)
                    .ToArray();
                foreach (var target in targets)
                {
                    var perMod = RegisteredCardPoolAssetReplacements[target];
                    removed |= perMod.Remove(ModId);
                    if (perMod.Count == 0)
                        RegisteredCardPoolAssetReplacements.Remove(target);
                }
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
            CardPoolModel pool,
            out CardPoolAssetProfile assetProfile)
        {
            var canonical = NormalizeOwnedModelIdEntry(pool.Id.Entry);

            lock (SyncRoot)
            {
                RegisteredCardPoolAssetReplacements.TryGetValue(new(canonical, null), out var entryLayers);
                RegisteredCardPoolAssetReplacements.TryGetValue(new(null, pool.GetType()), out var typeLayers);
                if (entryLayers == null || typeLayers == null)
                    return TryMergeCardPoolAssetReplacementLayers(entryLayers?.Values ?? typeLayers?.Values,
                        out assetProfile);

                var combined = new Dictionary<string, CardPoolAssetReplacementLayer>(entryLayers,
                    StringComparer.OrdinalIgnoreCase);
                foreach (var (modId, layer) in typeLayers)
                    if (!combined.TryGetValue(modId, out var existing) || layer.WriteOrder > existing.WriteOrder)
                        combined[modId] = layer;
                return TryMergeCardPoolAssetReplacementLayers(combined.Values, out assetProfile);
            }
        }

        private static bool TryMergeCardPoolAssetReplacementLayers(
            IEnumerable<CardPoolAssetReplacementLayer>? layers,
            out CardPoolAssetProfile mergedProfile)
        {
            var ordered = layers?.ToList();
            if (ordered is not { Count: > 0 })
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

        private readonly record struct CardPoolAssetTarget(string? Entry, Type? PoolType)
        {
            internal string GetEntry()
            {
                return Entry ?? NormalizeOwnedModelIdEntry(ModelDb.GetId(PoolType!).Entry);
            }
        }
    }
}
