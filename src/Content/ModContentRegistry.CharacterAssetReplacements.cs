using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static readonly CharacterAssetPathField[] CharacterAssetPathFields =
        [
            new("Scenes.VisualsPath", static p => p.Scenes?.VisualsPath),
            new("Scenes.EnergyCounterPath", static p => p.Scenes?.EnergyCounterPath),
            new("Scenes.MerchantAnimPath", static p => p.Scenes?.MerchantAnimPath),
            new("Scenes.RestSiteAnimPath", static p => p.Scenes?.RestSiteAnimPath),
            new("Ui.IconTexturePath", static p => p.Ui?.IconTexturePath),
            new("Ui.IconOutlineTexturePath", static p => p.Ui?.IconOutlineTexturePath),
            new("Ui.IconPath", static p => p.Ui?.IconPath),
            new("Ui.CharacterSelectBgPath", static p => p.Ui?.CharacterSelectBgPath),
            new("Ui.CharacterSelectIconPath", static p => p.Ui?.CharacterSelectIconPath),
            new("Ui.CharacterSelectLockedIconPath", static p => p.Ui?.CharacterSelectLockedIconPath),
            new("Ui.CharacterSelectTransitionPath", static p => p.Ui?.CharacterSelectTransitionPath),
            new("Ui.MapMarkerPath", static p => p.Ui?.MapMarkerPath),
            new("Vfx.TrailPath", static p => p.Vfx?.TrailPath),
            new("Spine.CombatSkeletonDataPath", static p => p.Spine?.CombatSkeletonDataPath),
            new("Audio.CharacterSelectSfx", static p => p.Audio?.CharacterSelectSfx),
            new("Audio.CharacterTransitionSfx", static p => p.Audio?.CharacterTransitionSfx),
            new("Audio.AttackSfx", static p => p.Audio?.AttackSfx),
            new("Audio.CastSfx", static p => p.Audio?.CastSfx),
            new("Audio.DeathSfx", static p => p.Audio?.DeathSfx),
            new("Multiplayer.ArmPointingTexturePath", static p => p.Multiplayer?.ArmPointingTexturePath),
            new("Multiplayer.ArmRockTexturePath", static p => p.Multiplayer?.ArmRockTexturePath),
            new("Multiplayer.ArmPaperTexturePath", static p => p.Multiplayer?.ArmPaperTexturePath),
            new("Multiplayer.ArmScissorsTexturePath", static p => p.Multiplayer?.ArmScissorsTexturePath),
        ];

        private static long _characterAssetReplacementWriteOrder;

        private static readonly Dictionary<string, CharacterAssetReplacementLayer>
            RegisteredGlobalCharacterAssetReplacementsByMod =
                new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Dictionary<string, CharacterAssetReplacementLayer>>
            RegisteredCharacterAssetReplacementsByEntry =
                new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers this mod's asset replacements for all characters. Character-specific replacements
        ///         take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册此模组应用于所有角色的资源替换。角色专用替换优先。
        ///     </para>
        /// </summary>
        public void RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile assetProfile)
        {
            ArgumentNullException.ThrowIfNull(assetProfile);

            lock (SyncRoot)
            {
                RegisteredGlobalCharacterAssetReplacementsByMod[ModId] = new(
                    assetProfile,
                    NextCharacterAssetReplacementWriteOrder());
            }

            ModCharacterOwnedVisualOverrideHelper.InvalidateAllCaches();
            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info("[Content] Registered global character asset replacement.");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers asset replacements for a character ID. Non-null fields from later registrations
        ///         take precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为角色 ID 注册资源替换。后注册的非空字段优先。
        ///     </para>
        /// </summary>
        public void RegisterCharacterAssetReplacement(string characterEntry, CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentNullException.ThrowIfNull(assetProfile);
            var normalizedEntry = NormalizeCharacterAssetEntryKey(characterEntry);

            lock (SyncRoot)
            {
                if (!RegisteredCharacterAssetReplacementsByEntry.TryGetValue(normalizedEntry, out var perMod))
                {
                    perMod = new(StringComparer.OrdinalIgnoreCase);
                    RegisteredCharacterAssetReplacementsByEntry[normalizedEntry] = perMod;
                }

                perMod[ModId] = new(assetProfile, NextCharacterAssetReplacementWriteOrder());
            }

            ModCharacterOwnedVisualOverrideHelper.InvalidateCachesForCharacterEntry(normalizedEntry);
            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info($"[Content] Registered character asset replacement for '{normalizedEntry}'.");
        }

        /// <summary>
        ///     <para xml:lang="en">Removes this mod's global character asset replacements.</para>
        ///     <para xml:lang="zh-CN">移除此模组的全局角色资源替换。</para>
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
        public bool ClearGlobalCharacterAssetReplacement()
        {
            bool removed;

            lock (SyncRoot)
            {
                removed = RegisteredGlobalCharacterAssetReplacementsByMod.Remove(ModId);
            }

            if (!removed) return removed;
            ModCharacterOwnedVisualOverrideHelper.InvalidateAllCaches();
            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info("[Content] Cleared global character asset replacement.");

            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">Removes this mod's asset replacements for the specified character ID.</para>
        ///     <para xml:lang="zh-CN">移除此模组为指定角色 ID 注册的资源替换。</para>
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
        public bool RemoveCharacterAssetReplacement(string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            var canonical = NormalizeCharacterAssetEntryKey(characterEntry);
            bool removed;

            lock (SyncRoot)
            {
                removed = TryRemoveCharacterAssetReplacementForKey(canonical);
            }

            if (!removed) return removed;
            ModCharacterOwnedVisualOverrideHelper.InvalidateCachesForCharacterEntry(canonical);
            RuntimeAssetRefreshCoordinator.Request();
            _logger.Info($"[Content] Removed character asset replacement for '{canonical}'.");

            return removed;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the merged character-specific replacements for
        ///         <paramref name="characterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="characterEntry" /> 合并后的角色专用替换。
        ///     </para>
        /// </summary>
        internal static bool TryGetRegisteredCharacterAssetReplacement(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            var canonical = NormalizeCharacterAssetEntryKey(characterEntry);

            lock (SyncRoot)
            {
                if (RegisteredCharacterAssetReplacementsByEntry.TryGetValue(canonical, out var layersByMod))
                    return TryMergeCharacterAssetReplacementLayers(layersByMod.Values, out assetProfile);

                assetProfile = CharacterAssetProfile.Empty;
                return false;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Tries to get the merged global character asset replacements.</para>
        ///     <para xml:lang="zh-CN">尝试获取合并后的全局角色资源替换。</para>
        /// </summary>
        internal static bool TryGetGlobalCharacterAssetReplacement(out CharacterAssetProfile assetProfile)
        {
            lock (SyncRoot)
            {
                return TryMergeCharacterAssetReplacementLayers(
                    RegisteredGlobalCharacterAssetReplacementsByMod.Values,
                    out assetProfile);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the effective global and character-specific registry replacements without
        ///         programmatic owned-visual replacements.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取全局与角色专用注册表替换的最终结果，不包含编程式所属视觉替换。
        ///     </para>
        /// </summary>
        internal static bool TryGetRegistryOnlyEffectiveCharacterAssetReplacement(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);

            var hasGlobal = TryGetGlobalCharacterAssetReplacement(out var globalProfile);
            var hasCharacter = TryGetRegisteredCharacterAssetReplacement(characterEntry, out var characterProfile);

            if (!hasGlobal && !hasCharacter)
            {
                assetProfile = CharacterAssetProfile.Empty;
                return false;
            }

            assetProfile = hasGlobal && hasCharacter
                ? CharacterAssetProfiles.Merge(globalProfile, characterProfile)
                : hasCharacter
                    ? characterProfile
                    : globalProfile;
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get effective registry and programmatic owned-visual replacements for a character.
        ///         Registry replacements take precedence on conflicts.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取角色的最终注册表与编程式所属视觉替换。发生冲突时注册表替换优先。
        ///     </para>
        /// </summary>
        internal static bool TryGetEffectiveCharacterAssetReplacement(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);

            var hasRegistry = TryGetRegistryOnlyEffectiveCharacterAssetReplacement(characterEntry, out var registry);
            var hasProgrammatic =
                TryBuildProgrammaticCharacterOwnedVisualProfile(characterEntry, out var programmatic);

            if (!hasRegistry && !hasProgrammatic)
            {
                assetProfile = CharacterAssetProfile.Empty;
                return false;
            }

            assetProfile = hasRegistry && hasProgrammatic
                ? CharacterAssetProfiles.Merge(programmatic, registry)
                : hasRegistry
                    ? registry
                    : programmatic;
            return true;
        }

        internal static IReadOnlyList<CharacterAssetReplacementLayerSnapshot>
            GetCharacterAssetReplacementLayerSnapshots()
        {
            lock (SyncRoot)
            {
                var snapshots = new List<CharacterAssetReplacementLayerSnapshot>();

                foreach (var (modId, layer) in RegisteredGlobalCharacterAssetReplacementsByMod)
                    snapshots.Add(new("global", "*", modId, layer.WriteOrder, layer.Profile));

                foreach (var (entry, perMod) in RegisteredCharacterAssetReplacementsByEntry)
                foreach (var (modId, layer) in perMod)
                    snapshots.Add(new("character", entry, modId, layer.WriteOrder, layer.Profile));

                snapshots.Sort(static (x, y) => x.WriteOrder.CompareTo(y.WriteOrder));
                return snapshots;
            }
        }

        internal static IReadOnlyList<CharacterAssetReplacementResolvedPropertySnapshot>
            GetCharacterAssetReplacementResolvedPropertySnapshots()
        {
            lock (SyncRoot)
            {
                var resolved = new List<CharacterAssetReplacementResolvedPropertySnapshot>();
                var entries = RegisteredCharacterAssetReplacementsByEntry.Keys
                    .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var entry in entries)
                    resolved.AddRange(GetResolvedPropertiesForEntry(entry));

                return resolved;
            }
        }

        private static long NextCharacterAssetReplacementWriteOrder()
        {
            _characterAssetReplacementWriteOrder++;
            return _characterAssetReplacementWriteOrder;
        }

        private bool TryRemoveCharacterAssetReplacementForKey(string dictionaryKey)
        {
            if (!RegisteredCharacterAssetReplacementsByEntry.TryGetValue(dictionaryKey, out var perMod))
                return false;

            var removed = perMod.Remove(ModId);
            if (perMod.Count == 0)
                RegisteredCharacterAssetReplacementsByEntry.Remove(dictionaryKey);

            return removed;
        }

        private static bool TryMergeCharacterAssetReplacementLayers(
            IEnumerable<CharacterAssetReplacementLayer> layers,
            out CharacterAssetProfile mergedProfile)
        {
            var ordered = layers.ToList();
            if (ordered.Count == 0)
            {
                mergedProfile = CharacterAssetProfile.Empty;
                return false;
            }

            ordered.Sort(static (x, y) => x.WriteOrder.CompareTo(y.WriteOrder));

            var merged = ordered[0].Profile;
            for (var i = 1; i < ordered.Count; i++)
                merged = CharacterAssetProfiles.Merge(merged, ordered[i].Profile);

            mergedProfile = merged;
            return true;
        }

        private static List<CharacterAssetReplacementResolvedPropertySnapshot> GetResolvedPropertiesForEntry(
            string characterEntry)
        {
            var values = new List<CharacterAssetReplacementResolvedPropertySnapshot>();

            RegisteredCharacterAssetReplacementsByEntry.TryGetValue(characterEntry, out var characterLayersByMod);
            var globalLayers = RegisteredGlobalCharacterAssetReplacementsByMod
                .Select(static kv => (ModId: kv.Key, Layer: kv.Value))
                .OrderBy(static x => x.Layer.WriteOrder)
                .ToArray();
            var characterLayers = characterLayersByMod == null
                ? []
                : characterLayersByMod
                    .Select(static kv => (ModId: kv.Key, Layer: kv.Value))
                    .OrderBy(static x => x.Layer.WriteOrder)
                    .ToArray();

            foreach (var field in CharacterAssetPathFields)
            {
                if (TryResolveFieldSource(characterEntry, field, characterLayers, "character", out var characterValue))
                {
                    values.Add(characterValue);
                    continue;
                }

                if (TryResolveFieldSource(characterEntry, field, globalLayers, "global", out var globalValue))
                    values.Add(globalValue);
            }

            return values;
        }

        private static bool TryResolveFieldSource(
            string characterEntry,
            CharacterAssetPathField field,
            IEnumerable<(string ModId, CharacterAssetReplacementLayer Layer)> orderedLayers,
            string scope,
            out CharacterAssetReplacementResolvedPropertySnapshot resolved)
        {
            foreach (var layer in orderedLayers.Reverse())
            {
                var value = field.Selector(layer.Layer.Profile);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                resolved = new(characterEntry, field.Name, value, scope, layer.ModId, layer.Layer.WriteOrder);
                return true;
            }

            resolved = default;
            return false;
        }

        private readonly record struct CharacterAssetReplacementLayer(
            CharacterAssetProfile Profile,
            long WriteOrder);

        internal readonly record struct CharacterAssetReplacementLayerSnapshot(
            string Scope,
            string CharacterEntry,
            string ModId,
            long WriteOrder,
            CharacterAssetProfile Profile);

        internal readonly record struct CharacterAssetReplacementResolvedPropertySnapshot(
            string CharacterEntry,
            string PropertyPath,
            string Value,
            string SourceScope,
            string SourceModId,
            long SourceWriteOrder);

        private readonly record struct CharacterAssetPathField(
            string Name,
            Func<CharacterAssetProfile, string?> Selector);

        /// <summary>
        ///     <para xml:lang="en">Provides well-known base-game character IDs.</para>
        ///     <para xml:lang="zh-CN">提供常用的原版角色 ID。</para>
        /// </summary>
        public static class VanillaCharacterIds
        {
            /// <summary>
            ///     <para xml:lang="en">The Ironclad character ID.</para>
            ///     <para xml:lang="zh-CN">铁甲战士角色 ID。</para>
            /// </summary>
            public const string Ironclad = "IRONCLAD";

            /// <summary>
            ///     <para xml:lang="en">The Silent character ID.</para>
            ///     <para xml:lang="zh-CN">静默猎手角色 ID。</para>
            /// </summary>
            public const string Silent = "SILENT";

            /// <summary>
            ///     <para xml:lang="en">The Defect character ID.</para>
            ///     <para xml:lang="zh-CN">故障机器人角色 ID。</para>
            /// </summary>
            public const string Defect = "DEFECT";

            /// <summary>
            ///     <para xml:lang="en">The Regent character ID.</para>
            ///     <para xml:lang="zh-CN">储君角色 ID。</para>
            /// </summary>
            public const string Regent = "REGENT";

            /// <summary>
            ///     <para xml:lang="en">The Necrobinder character ID.</para>
            ///     <para xml:lang="zh-CN">亡灵契约师角色 ID。</para>
            /// </summary>
            public const string Necrobinder = "NECROBINDER";
        }
    }
}
