using STS2RitsuLib.Scaffolding.Characters;

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
        ///     Registers global asset overrides applied to all characters for the current <see cref="ModId" />.
        ///     Character-specific overrides still win.
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

            _logger.Info("[Content] Registered global character asset replacement.");
        }

        /// <summary>
        ///     Registers asset overrides for any character id (vanilla or mod), merged field-by-field with existing
        ///     registrations. Later calls win for non-null fields.
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

            _logger.Info($"[Content] Registered character asset replacement for '{normalizedEntry}'.");
        }

        /// <summary>
        ///     Removes global character asset overrides registered by the current <see cref="ModId" />.
        /// </summary>
        /// <returns><c>true</c> when this mod had a global override and it was removed.</returns>
        public bool ClearGlobalCharacterAssetReplacement()
        {
            bool removed;

            lock (SyncRoot)
            {
                removed = RegisteredGlobalCharacterAssetReplacementsByMod.Remove(ModId);
            }

            if (removed)
                _logger.Info("[Content] Cleared global character asset replacement.");

            return removed;
        }

        /// <summary>
        ///     Removes this mod's registered asset overrides for the specified character id.
        /// </summary>
        /// <returns><c>true</c> when this mod had an override and it was removed.</returns>
        public bool RemoveCharacterAssetReplacement(string characterEntry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            var canonical = NormalizeCharacterAssetEntryKey(characterEntry);
            bool removed;

            lock (SyncRoot)
            {
                removed = TryRemoveCharacterAssetReplacementForKey(canonical);
            }

            if (removed)
                _logger.Info($"[Content] Removed character asset replacement for '{canonical}'.");

            return removed;
        }

        /// <summary>
        ///     Returns merged registered asset overrides for <paramref name="characterEntry" />, if any.
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
        ///     Returns global asset overrides, if any.
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
        ///     Returns registry-only overrides (global + per-character <see cref="RegisterCharacterAssetReplacement" />),
        ///     without programmatic owned-visual registrations.
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
        ///     Returns effective overrides for a character: programmatic owned relic / potion / card art merged
        ///     underneath registry overrides from <see cref="RegisterGlobalCharacterAssetReplacement" /> and
        ///     <see cref="RegisterCharacterAssetReplacement" /> (registry wins on conflicts). Character
        ///     <see>
        ///         <cref>T:STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate</cref>
        ///     </see>
        ///     <c>AssetProfile</c> rows are merged in
        ///     <c>TryGetVanilla*</c> below
        ///     both registry and programmatic tiers.
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
        ///     Well-known base-game character ids for
        ///     <see cref="RegisterCharacterAssetReplacement(string,CharacterAssetProfile)" />.
        /// </summary>
        public static class VanillaCharacterIds
        {
            /// <summary>Vanilla Ironclad character id.</summary>
            public const string Ironclad = "IRONCLAD";

            /// <summary>Vanilla Silent character id.</summary>
            public const string Silent = "SILENT";

            /// <summary>Vanilla Defect character id.</summary>
            public const string Defect = "DEFECT";

            /// <summary>Vanilla Regent character id.</summary>
            public const string Regent = "REGENT";

            /// <summary>Vanilla Necrobinder character id.</summary>
            public const string Necrobinder = "NECROBINDER";
        }
    }
}
