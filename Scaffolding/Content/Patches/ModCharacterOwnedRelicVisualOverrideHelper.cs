using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Per-owner character visuals for relic/potion/card assets; applied before model-level
    ///     <see cref="IModRelicAssetOverrides" />, <see cref="IModPotionAssetOverrides" />, and
    ///     <see cref="IModCardAssetOverrides" /> patches.
    /// </summary>
    internal static class ModCharacterOwnedVisualOverrideHelper
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, IModCharacterAssetOverrides> RegisteredProfileAdapters =
            new(StringComparer.OrdinalIgnoreCase);

        private static IModCharacterAssetOverrides? _cachedGlobalProfileAdapter;

        /// <summary>
        ///     Drops cached <see cref="RegisteredCharacterAssetOverrideAdapter" /> instances after programmatic owned
        ///     visual registrations change for <paramref name="normalizedCharacterEntry" /> (canonical uppercase id).
        /// </summary>
        internal static void InvalidateCachesForCharacterEntry(string normalizedCharacterEntry)
        {
            lock (SyncRoot)
            {
                RegisteredProfileAdapters.Remove(normalizedCharacterEntry);
            }
        }

        /// <summary>
        ///     Merge order (lowest → highest): character <see cref="IModCharacterAssetOverrides.AssetProfile" /> rows,
        ///     programmatic registry, then <see cref="ModContentRegistry.RegisterCharacterAssetReplacement" /> /
        ///     global replacement.
        /// </summary>
        internal static RelicAssetProfile? ResolveOwnedRelicVisualOverride(CharacterModel owner, RelicModel relic)
        {
            var programmatic = TryProgrammaticRelic(owner.Id.Entry, relic);
            var registry = TryRegistryRelic(owner.Id.Entry, relic);
            var inline = TryInlineRelic(owner, relic);
            return CharacterAssetProfiles.MergeRelicAssetProfilesPreferSecond(
                CharacterAssetProfiles.MergeRelicAssetProfilesPreferSecond(inline, programmatic),
                registry);
        }

        internal static PotionAssetProfile? ResolveOwnedPotionVisualOverride(CharacterModel owner, PotionModel potion)
        {
            var programmatic = TryProgrammaticPotion(owner.Id.Entry, potion);
            var registry = TryRegistryPotion(owner.Id.Entry, potion);
            var inline = TryInlinePotion(owner, potion);
            return CharacterAssetProfiles.MergePotionAssetProfilesPreferSecond(
                CharacterAssetProfiles.MergePotionAssetProfilesPreferSecond(inline, programmatic),
                registry);
        }

        internal static CardAssetProfile? ResolveOwnedCardVisualOverride(CharacterModel owner, CardModel card)
        {
            var programmatic = TryProgrammaticCard(owner.Id.Entry, card);
            var registry = TryRegistryCard(owner.Id.Entry, card);
            var inline = TryInlineCard(owner, card);
            return CharacterAssetProfiles.MergeCardAssetProfilesPreferSecond(
                CharacterAssetProfiles.MergeCardAssetProfilesPreferSecond(inline, programmatic),
                registry);
        }

        private static RelicAssetProfile? TryProgrammaticRelic(string characterEntry, RelicModel relic)
        {
            return ModContentRegistry.TryBuildProgrammaticCharacterOwnedVisualProfile(characterEntry, out var profile)
                ? SelectRelic(profile, relic)
                : null;
        }

        private static RelicAssetProfile? TryRegistryRelic(string characterEntry, RelicModel relic)
        {
            return ModContentRegistry.TryGetRegistryOnlyEffectiveCharacterAssetReplacement(characterEntry, out var p)
                ? SelectRelic(p, relic)
                : null;
        }

        private static RelicAssetProfile? TryInlineRelic(CharacterModel owner, RelicModel relic)
        {
            if (owner is not IModCharacterAssetOverrides mo)
                return null;

            var resolved = CharacterAssetProfiles.Resolve(mo.AssetProfile, mo.CharacterAssetPlaceholderCharacterId);
            return SelectRelic(resolved, relic);
        }

        private static PotionAssetProfile? TryProgrammaticPotion(string characterEntry, PotionModel potion)
        {
            return ModContentRegistry.TryBuildProgrammaticCharacterOwnedVisualProfile(characterEntry, out var profile)
                ? SelectPotion(profile, potion)
                : null;
        }

        private static PotionAssetProfile? TryRegistryPotion(string characterEntry, PotionModel potion)
        {
            return ModContentRegistry.TryGetRegistryOnlyEffectiveCharacterAssetReplacement(characterEntry, out var p)
                ? SelectPotion(p, potion)
                : null;
        }

        private static PotionAssetProfile? TryInlinePotion(CharacterModel owner, PotionModel potion)
        {
            if (owner is not IModCharacterAssetOverrides mo)
                return null;

            var resolved = CharacterAssetProfiles.Resolve(mo.AssetProfile, mo.CharacterAssetPlaceholderCharacterId);
            return SelectPotion(resolved, potion);
        }

        private static CardAssetProfile? TryProgrammaticCard(string characterEntry, CardModel card)
        {
            return ModContentRegistry.TryBuildProgrammaticCharacterOwnedVisualProfile(characterEntry, out var profile)
                ? SelectCard(profile, card)
                : null;
        }

        private static CardAssetProfile? TryRegistryCard(string characterEntry, CardModel card)
        {
            return ModContentRegistry.TryGetRegistryOnlyEffectiveCharacterAssetReplacement(characterEntry, out var p)
                ? SelectCard(p, card)
                : null;
        }

        private static CardAssetProfile? TryInlineCard(CharacterModel owner, CardModel card)
        {
            if (owner is not IModCharacterAssetOverrides mo)
                return null;

            var resolved = CharacterAssetProfiles.Resolve(mo.AssetProfile, mo.CharacterAssetPlaceholderCharacterId);
            return SelectCard(resolved, card);
        }

        private static RelicAssetProfile? SelectRelic(CharacterAssetProfile profile, RelicModel relic)
        {
            var entries = profile.VanillaRelicVisualOverrides;
            if (entries is not { Length: > 0 })
                return null;

            var id = relic.Id.Entry;
            foreach (var (relicModelIdEntry, a) in entries)
            {
                if (!id.Equals(relicModelIdEntry, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(a.IconPath) && string.IsNullOrWhiteSpace(a.IconOutlinePath) &&
                    string.IsNullOrWhiteSpace(a.BigIconPath))
                    return null;

                return a;
            }

            return null;
        }

        private static PotionAssetProfile? SelectPotion(CharacterAssetProfile profile, PotionModel potion)
        {
            var entries = profile.VanillaPotionVisualOverrides;
            if (entries is not { Length: > 0 })
                return null;

            var id = potion.Id.Entry;
            foreach (var (potionModelIdEntry, a) in entries)
            {
                if (!id.Equals(potionModelIdEntry, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(a.ImagePath) && string.IsNullOrWhiteSpace(a.OutlinePath))
                    return null;

                return a;
            }

            return null;
        }

        private static CardAssetProfile? SelectCard(CharacterAssetProfile profile, CardModel card)
        {
            var entries = profile.VanillaCardVisualOverrides;
            if (entries is not { Length: > 0 })
                return null;

            var id = card.Id.Entry;
            foreach (var (cardModelIdEntry, a) in entries)
            {
                if (!id.Equals(cardModelIdEntry, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(a.PortraitPath) && string.IsNullOrWhiteSpace(a.BetaPortraitPath) &&
                    string.IsNullOrWhiteSpace(a.FramePath) && string.IsNullOrWhiteSpace(a.PortraitBorderPath) &&
                    string.IsNullOrWhiteSpace(a.EnergyIconPath) && string.IsNullOrWhiteSpace(a.FrameMaterialPath) &&
                    string.IsNullOrWhiteSpace(a.OverlayScenePath) && string.IsNullOrWhiteSpace(a.BannerTexturePath) &&
                    string.IsNullOrWhiteSpace(a.BannerMaterialPath))
                    return null;

                return a;
            }

            return null;
        }

        internal static bool TryRelicIconPath(RelicModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryRelicIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicIconOutlinePath(RelicModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconOutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconOutlinePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryRelicIconOutlineTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.IconOutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.IconOutlinePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryRelicBigIconTexture(RelicModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);

            var profile = overrides?.TryGetVanillaRelicVisualOverrideForOwnedRelic(instance);
            if (profile == null)
                return true;

            var path = profile.BigIconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(RelicAssetProfile.BigIconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryPotionImagePath(PotionModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.ImagePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.ImagePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryPotionOutlinePath(PotionModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.OutlinePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryPotionImageTexture(PotionModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.ImagePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.ImagePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryPotionOutlineTexture(PotionModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaPotionVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OutlinePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(PotionAssetProfile.OutlinePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardPortraitPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardBetaPortraitPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BetaPortraitPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BetaPortraitPath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardFrameTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.FramePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.FramePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardPortraitBorderTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitBorderPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitBorderPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardEnergyIconTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.EnergyIconPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.EnergyIconPath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardFrameMaterial(CardModel instance, ref Material result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.FrameMaterialPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.FrameMaterialPath)))
                return true;

            result = ResourceLoader.Load<Material>(path);
            return false;
        }

        internal static bool TryCardOverlayPath(CardModel instance, ref string result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath)))
                return true;

            result = path;
            return false;
        }

        internal static bool TryCardOverlayExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath));
            return false;
        }

        internal static bool TryCardCreateOverlay(CardModel instance, ref Control result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.OverlayScenePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.OverlayScenePath)))
                return true;

            result = ResourceLoader.Load<PackedScene>(path).Instantiate<Control>();
            return false;
        }

        internal static bool TryCardBannerTexture(CardModel instance, ref Texture2D result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BannerTexturePath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BannerTexturePath)))
                return true;

            result = ResourceLoader.Load<Texture2D>(path);
            return false;
        }

        internal static bool TryCardBannerMaterial(CardModel instance, ref Material result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BannerMaterialPath;
            if (string.IsNullOrWhiteSpace(path) ||
                !AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BannerMaterialPath)))
                return true;

            result = ResourceLoader.Load<Material>(path);
            return false;
        }

        internal static bool TryCardPortraitExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.PortraitPath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.PortraitPath));
            return false;
        }

        internal static bool TryCardBetaPortraitExists(CardModel instance, ref bool result)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return true;

            var path = profile.BetaPortraitPath;
            if (string.IsNullOrWhiteSpace(path))
                return true;

            result = AssetPathDiagnostics.Exists(path, instance, nameof(CardAssetProfile.BetaPortraitPath));
            return false;
        }

        internal static string[] GetExistingCardPortraitPaths(CardModel instance)
        {
            var overrides = TryGetOwningCharacterOverrides(instance);
            var profile = overrides?.TryGetVanillaCardVisualOverrideForContext(instance);
            if (profile == null)
                return [];

            return AssetPathDiagnostics.CollectExistingPaths(
                instance,
                (profile.PortraitPath, nameof(CardAssetProfile.PortraitPath)),
                (profile.BetaPortraitPath, nameof(CardAssetProfile.BetaPortraitPath)));
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(RelicModel instance)
        {
            return ResolveOwningCharacterOverrides(instance.IsCanonical ? null : instance.Owner?.Character);
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(PotionModel instance)
        {
            return ResolveOwningCharacterOverrides(instance.IsCanonical ? null : instance.Owner?.Character);
        }

        private static IModCharacterAssetOverrides? TryGetOwningCharacterOverrides(CardModel instance)
        {
            return ResolveOwningCharacterOverrides(instance.IsCanonical ? null : instance.Owner?.Character);
        }

        private static IModCharacterAssetOverrides? ResolveOwningCharacterOverrides(CharacterModel? owner)
        {
            lock (SyncRoot)
            {
                switch (owner)
                {
                    case IModCharacterAssetOverrides direct:
                        return direct;
                    case null:
                    {
                        if (!ModContentRegistry.TryGetGlobalCharacterAssetReplacement(out var globalProfile))
                            return null;

                        return _cachedGlobalProfileAdapter ??=
                            new RegisteredCharacterAssetOverrideAdapter(globalProfile);
                    }
                }

                if (!ModContentRegistry.TryGetEffectiveCharacterAssetReplacement(owner.Id.Entry, out var profile))
                    return null;

                if (RegisteredProfileAdapters.TryGetValue(owner.Id.Entry, out var cached))
                    return cached;

                var adapter = new RegisteredCharacterAssetOverrideAdapter(profile);
                RegisteredProfileAdapters[owner.Id.Entry] = adapter;
                return adapter;
            }
        }

        private sealed class RegisteredCharacterAssetOverrideAdapter(CharacterAssetProfile profile)
            : IModCharacterAssetOverrides
        {
            public CharacterAssetProfile AssetProfile { get; } = profile;
            public string? CustomVisualsPath => AssetProfile.Scenes?.VisualsPath;
            public string? CustomEnergyCounterPath => AssetProfile.Scenes?.EnergyCounterPath;
            public string? CustomMerchantAnimPath => AssetProfile.Scenes?.MerchantAnimPath;
            public string? CustomRestSiteAnimPath => AssetProfile.Scenes?.RestSiteAnimPath;
            public string? CustomIconTexturePath => AssetProfile.Ui?.IconTexturePath;
            public string? CustomIconOutlineTexturePath => AssetProfile.Ui?.IconOutlineTexturePath;
            public string? CustomIconPath => AssetProfile.Ui?.IconPath;
            public string? CustomCharacterSelectBgPath => AssetProfile.Ui?.CharacterSelectBgPath;
            public string? CustomCharacterSelectIconPath => AssetProfile.Ui?.CharacterSelectIconPath;
            public string? CustomCharacterSelectLockedIconPath => AssetProfile.Ui?.CharacterSelectLockedIconPath;
            public string? CustomCharacterSelectTransitionPath => AssetProfile.Ui?.CharacterSelectTransitionPath;
            public string? CustomMapMarkerPath => AssetProfile.Ui?.MapMarkerPath;
            public string? CustomTrailPath => AssetProfile.Vfx?.TrailPath;
            public CharacterTrailStyle? CustomTrailStyle => AssetProfile.Vfx?.TrailStyle;
            public string? CustomCombatSpineSkeletonDataPath => AssetProfile.Spine?.CombatSkeletonDataPath;
            public string? CustomCharacterSelectSfx => AssetProfile.Audio?.CharacterSelectSfx;
            public string? CustomCharacterTransitionSfx => AssetProfile.Audio?.CharacterTransitionSfx;
            public string? CustomAttackSfx => AssetProfile.Audio?.AttackSfx;
            public string? CustomCastSfx => AssetProfile.Audio?.CastSfx;
            public string? CustomDeathSfx => AssetProfile.Audio?.DeathSfx;
            public string? CustomArmPointingTexturePath => AssetProfile.Multiplayer?.ArmPointingTexturePath;
            public string? CustomArmRockTexturePath => AssetProfile.Multiplayer?.ArmRockTexturePath;
            public string? CustomArmPaperTexturePath => AssetProfile.Multiplayer?.ArmPaperTexturePath;
            public string? CustomArmScissorsTexturePath => AssetProfile.Multiplayer?.ArmScissorsTexturePath;
            public VisualCueSet? VisualCues => AssetProfile.VisualCues;
            public CharacterWorldProceduralVisualSet? WorldProceduralVisuals => AssetProfile.WorldProceduralVisuals;

            public RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic)
            {
                var entries = AssetProfile.VanillaRelicVisualOverrides;
                if (entries is not { Length: > 0 })
                    return null;

                return (from entry in entries
                    where relic.Id.Entry.Equals(entry.RelicModelIdEntry, StringComparison.OrdinalIgnoreCase)
                    select entry.Assets).FirstOrDefault();
            }

            public PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion)
            {
                var entries = AssetProfile.VanillaPotionVisualOverrides;
                if (entries is not { Length: > 0 })
                    return null;

                return (from entry in entries
                    where potion.Id.Entry.Equals(entry.PotionModelIdEntry, StringComparison.OrdinalIgnoreCase)
                    select entry.Assets).FirstOrDefault();
            }

            public CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card)
            {
                var entries = AssetProfile.VanillaCardVisualOverrides;
                if (entries is not { Length: > 0 })
                    return null;

                return (from entry in entries
                    where card.Id.Entry.Equals(entry.CardModelIdEntry, StringComparison.OrdinalIgnoreCase)
                    select entry.Assets).FirstOrDefault();
            }
        }
    }
}
