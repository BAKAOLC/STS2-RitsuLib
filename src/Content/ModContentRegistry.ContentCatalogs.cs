using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Catalog table for patched <see cref="ModelDb" /> getters: registration source, warm path, and
        ///         merge mode per row.
        ///     </para>
        ///     <para xml:lang="zh-CN">patched <see cref="ModelDb" /> getter 的目录表：每行绑定注册源、预热路径与合并模式。</para>
        /// </summary>
        private static readonly IReadOnlyList<ContentCatalogEntry> ModelCatalogs;

        private static readonly Dictionary<ContentCatalogId, ContentCatalogEntry> CatalogById;

        static ModContentRegistry()
        {
            ModelCatalogs =
            [
                GlobalEntry<CharacterModel>(ContentCatalogId.Characters, static () => RegisteredCharacters),
                GlobalEntry<ActModel>(ContentCatalogId.Acts, static () => RegisteredActs),
                GlobalEntry<MonsterModel>(ContentCatalogId.Monsters, static () => RegisteredMonsters,
                    ContentMergeMode.MergeDistinctById),
                GlobalEntry<PowerModel>(ContentCatalogId.Powers, static () => RegisteredPowers),
                GlobalEntry<OrbModel>(ContentCatalogId.Orbs, static () => RegisteredOrbs),
                GlobalEntry<EventModel>(ContentCatalogId.SharedEvents, static () => RegisteredSharedEvents),
                GlobalEntry<AncientEventModel>(ContentCatalogId.SharedAncients, static () => RegisteredSharedAncients),
                GlobalEntry<EnchantmentModel>(ContentCatalogId.Enchantments, static () => RegisteredEnchantments),
                GlobalEntry<AfflictionModel>(ContentCatalogId.Afflictions, static () => RegisteredAfflictions),
                GlobalEntry<AchievementModel>(ContentCatalogId.Achievements, static () => RegisteredAchievements),
                GlobalEntry<RelicPoolModel>(ContentCatalogId.SharedRelicPools, static () => RegisteredSharedRelicPools),
                GlobalEntry<PotionPoolModel>(ContentCatalogId.SharedPotionPools,
                    static () => RegisteredSharedPotionPools),
                GlobalEntry<CardPoolModel>(ContentCatalogId.SharedCardPools, static () => RegisteredSharedCardPools),
                GlobalEntry<EncounterModel>(ContentCatalogId.GlobalEncounters, static () => RegisteredGlobalEncounters),
                ScopedEntry<EventModel>(ContentCatalogId.ActEvents, static () => RegisteredActEvents),
                ScopedEntry<EncounterModel>(ContentCatalogId.ActEncounters, static () => RegisteredActEncounters),
                ScopedEntry<AncientEventModel>(ContentCatalogId.ActAncients, static () => RegisteredActAncients),
            ];
            CatalogById = ModelCatalogs.ToDictionary(static entry => entry.Id);
            ResolvedModelCache.Configure(ModelCatalogs);
        }

        internal static ContentCatalogEntry GetCatalog(ContentCatalogId id)
        {
            return CatalogById[id];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Warms resolved model caches once after <see cref="ModelDb.Init" /> to avoid repeated
        ///         <see cref="ModelDb.GetId" /> during Preload.
        ///     </para>
        ///     <para xml:lang="zh-CN">在 <see cref="ModelDb.Init" /> 后一次性预热已解析模型缓存，避免 Preload 期间重复 <see cref="ModelDb.GetId" />。</para>
        /// </summary>
        internal static void WarmResolvedModelCaches()
        {
            ResolvedModelCache.Warm();
        }

        private static ContentCatalogEntry GlobalEntry<TModel>(
            ContentCatalogId id,
            Func<IEnumerable<Type>> globalTypes,
            ContentMergeMode mergeMode = ContentMergeMode.AppendDistinctById)
            where TModel : AbstractModel
        {
            return GlobalCatalog(
                id,
                globalTypes,
                static types => ResolvedModelCache.ResolveUncached<TModel>(types),
                mergeMode);
        }

        private static ContentCatalogEntry ScopedEntry<TModel>(
            ContentCatalogId id,
            Func<Dictionary<Type, HashSet<Type>>> scopedRegistry)
            where TModel : AbstractModel
        {
            return ScopedCatalog(
                id,
                scopedRegistry,
                static registry => ResolvedModelCache.ResolveScopedUncached<TModel>(registry));
        }

        private static ContentCatalogEntry GlobalCatalog(
            ContentCatalogId id,
            Func<IEnumerable<Type>> globalTypes,
            Func<IEnumerable<Type>, object> warmGlobal,
            ContentMergeMode mergeMode = ContentMergeMode.AppendDistinctById)
        {
            return new()
            {
                Id = id,
                GlobalTypes = globalTypes,
                WarmGlobal = warmGlobal,
                MergeMode = mergeMode,
            };
        }

        private static ContentCatalogEntry ScopedCatalog(
            ContentCatalogId id,
            Func<Dictionary<Type, HashSet<Type>>> scopedRegistry,
            Func<Dictionary<Type, HashSet<Type>>, Dictionary<Type, object>> warmScoped)
        {
            return new()
            {
                Id = id,
                ScopedRegistry = scopedRegistry,
                WarmScoped = warmScoped,
            };
        }
    }
}
