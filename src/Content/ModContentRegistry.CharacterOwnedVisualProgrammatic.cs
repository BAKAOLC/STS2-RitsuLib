using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static long _characterOwnedVisualProgrammaticWriteOrder;

        private static readonly Dictionary<string, Dictionary<string, CharacterOwnedVisualProgrammaticLayer>>
            CharacterOwnedVisualProgrammaticByCharacterEntry =
                new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to build the programmatic owned-visual profile registered for
        ///         <paramref name="characterEntry" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试构建为 <paramref name="characterEntry" /> 注册的编程式所属视觉配置。
        ///     </para>
        /// </summary>
        internal static bool TryBuildProgrammaticCharacterOwnedVisualProfile(
            string characterEntry,
            out CharacterAssetProfile assetProfile)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            var key = NormalizeCharacterAssetEntryKey(characterEntry);

            lock (SyncRoot)
            {
                if (CharacterOwnedVisualProgrammaticByCharacterEntry.TryGetValue(key, out var byMod) &&
                    byMod.Count != 0)
                    return TryMergeCharacterOwnedVisualProgrammaticLayers(byMod.Values, out assetProfile);
                assetProfile = CharacterAssetProfile.Empty;
                return false;
            }
        }

        private static bool TryMergeCharacterOwnedVisualProgrammaticLayers(
            IEnumerable<CharacterOwnedVisualProgrammaticLayer> layers,
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

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers relic visuals used when <paramref name="characterEntry" /> owns the specified relic.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="characterEntry" /> 拥有指定遗物时使用的遗物视觉资源。
        ///     </para>
        /// </summary>
        public void RegisterCharacterOwnedRelicVisualOverride(
            string characterEntry,
            string relicModelIdEntry,
            RelicAssetProfile assets)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(relicModelIdEntry);
            ArgumentNullException.ThrowIfNull(assets);

            var fragment = new CharacterAssetProfile(
                VanillaRelicVisualOverrides:
                [
                    new(NormalizeOwnedModelIdEntry(relicModelIdEntry), assets),
                ]);

            RegisterCharacterOwnedVisualProgrammaticLayer(characterEntry, fragment);
        }

        /// <inheritdoc cref="RegisterCharacterOwnedRelicVisualOverride(string,string,RelicAssetProfile)" />
        public void RegisterCharacterOwnedRelicVisualOverride<TCharacter, TRelic>(RelicAssetProfile assets)
            where TCharacter : CharacterModel
            where TRelic : RelicModel
        {
            RegisterCharacterOwnedRelicVisualOverride(
                ModelDb.GetId<TCharacter>().Entry,
                ModelDb.GetId<TRelic>().Entry,
                assets);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers potion visuals used when <paramref name="characterEntry" /> owns the specified potion.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="characterEntry" /> 拥有指定药水时使用的药水视觉资源。
        ///     </para>
        /// </summary>
        public void RegisterCharacterOwnedPotionVisualOverride(
            string characterEntry,
            string potionModelIdEntry,
            PotionAssetProfile assets)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(potionModelIdEntry);
            ArgumentNullException.ThrowIfNull(assets);

            var fragment = new CharacterAssetProfile(
                VanillaPotionVisualOverrides:
                [
                    new(NormalizeOwnedModelIdEntry(potionModelIdEntry), assets),
                ]);

            RegisterCharacterOwnedVisualProgrammaticLayer(characterEntry, fragment);
        }

        /// <inheritdoc cref="RegisterCharacterOwnedPotionVisualOverride(string,string,PotionAssetProfile)" />
        public void RegisterCharacterOwnedPotionVisualOverride<TCharacter, TPotion>(PotionAssetProfile assets)
            where TCharacter : CharacterModel
            where TPotion : PotionModel
        {
            RegisterCharacterOwnedPotionVisualOverride(
                ModelDb.GetId<TCharacter>().Entry,
                ModelDb.GetId<TPotion>().Entry,
                assets);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers card visuals used when <paramref name="characterEntry" /> owns the specified card.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册 <paramref name="characterEntry" /> 拥有指定卡牌时使用的卡牌视觉资源。
        ///     </para>
        /// </summary>
        public void RegisterCharacterOwnedCardVisualOverride(
            string characterEntry,
            string cardModelIdEntry,
            CardAssetProfile assets)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterEntry);
            ArgumentException.ThrowIfNullOrWhiteSpace(cardModelIdEntry);
            ArgumentNullException.ThrowIfNull(assets);

            var fragment = new CharacterAssetProfile(
                VanillaCardVisualOverrides:
                [
                    new(NormalizeOwnedModelIdEntry(cardModelIdEntry), assets),
                ]);

            RegisterCharacterOwnedVisualProgrammaticLayer(characterEntry, fragment);
        }

        /// <inheritdoc cref="RegisterCharacterOwnedCardVisualOverride(string,string,CardAssetProfile)" />
        public void RegisterCharacterOwnedCardVisualOverride<TCharacter, TCard>(CardAssetProfile assets)
            where TCharacter : CharacterModel
            where TCard : CardModel
        {
            RegisterCharacterOwnedCardVisualOverride(
                ModelDb.GetId<TCharacter>().Entry,
                ModelDb.GetId<TCard>().Entry,
                assets);
        }

        private void RegisterCharacterOwnedVisualProgrammaticLayer(
            string characterEntry,
            CharacterAssetProfile fragment)
        {
            var key = NormalizeCharacterAssetEntryKey(characterEntry);

            lock (SyncRoot)
            {
                if (!CharacterOwnedVisualProgrammaticByCharacterEntry.TryGetValue(key, out var byMod))
                {
                    byMod = new(StringComparer.OrdinalIgnoreCase);
                    CharacterOwnedVisualProgrammaticByCharacterEntry[key] = byMod;
                }

                _characterOwnedVisualProgrammaticWriteOrder++;
                var write = _characterOwnedVisualProgrammaticWriteOrder;

                if (byMod.TryGetValue(ModId, out var existing))
                {
                    var merged = CharacterAssetProfiles.Merge(existing.Profile, fragment);
                    byMod[ModId] = new(merged, write);
                }
                else
                {
                    byMod[ModId] = new(fragment, write);
                }
            }

            ModCharacterOwnedVisualOverrideHelper.InvalidateCachesForCharacterEntry(key);
            RuntimeAssetRefreshCoordinator.Request(
                RuntimeAssetRefreshScope.Cards | RuntimeAssetRefreshScope.Relics | RuntimeAssetRefreshScope.Potions);
            _logger.Info($"[Content] Programmatic owned visual override for character '{key}' ({ModId}).");
        }

        private sealed record CharacterOwnedVisualProgrammaticLayer(
            CharacterAssetProfile Profile,
            long WriteOrder);
    }
}
