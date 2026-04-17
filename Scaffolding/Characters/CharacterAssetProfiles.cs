using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Factory and merge helpers for <see cref="CharacterAssetProfile" /> using vanilla path conventions.
    /// </summary>
    public static class CharacterAssetProfiles
    {
        /// <summary>
        ///     Default character id used when no placeholder is specified (<c>ironclad</c>).
        /// </summary>
        public const string DefaultPlaceholderCharacterId = "ironclad";

        /// <summary>
        ///     Builds a profile with <c>res://</c> paths matching base-game layout for <paramref name="characterId" />.
        /// </summary>
        public static CharacterAssetProfile FromCharacterId(string characterId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterId);

            var id = characterId.ToLowerInvariant();

            return new(
                new(
                    $"res://scenes/creature_visuals/{id}.tscn",
                    $"res://scenes/combat/energy_counters/{id}_energy_counter.tscn",
                    $"res://scenes/merchant/characters/{id}_merchant.tscn",
                    $"res://scenes/rest_site/characters/{id}_rest_site.tscn"),
                new(
                    $"res://images/ui/top_panel/character_icon_{id}.png",
                    $"res://images/ui/top_panel/character_icon_{id}_outline.png",
                    $"res://scenes/ui/character_icons/{id}_icon.tscn",
                    $"res://scenes/screens/char_select/char_select_bg_{id}.tscn",
                    $"res://images/packed/character_select/char_select_{id}.png",
                    $"res://images/packed/character_select/char_select_{id}_locked.png",
                    $"res://materials/transitions/{id}_transition_mat.tres",
                    $"res://images/packed/map/icons/map_marker_{id}.png"),
                new(
                    $"res://scenes/vfx/card_trail_{id}.tscn"),
                Audio: new(
                    $"event:/sfx/characters/{id}/{id}_select",
                    $"event:/sfx/ui/wipe_{id}",
                    $"event:/sfx/characters/{id}/{id}_attack",
                    $"event:/sfx/characters/{id}/{id}_cast",
                    $"event:/sfx/characters/{id}/{id}_die"),
                Multiplayer: new(
                    $"res://images/ui/hands/multiplayer_hand_{id}_point.png",
                    $"res://images/ui/hands/multiplayer_hand_{id}_rock.png",
                    $"res://images/ui/hands/multiplayer_hand_{id}_paper.png",
                    $"res://images/ui/hands/multiplayer_hand_{id}_scissors.png"));
        }

        /// <summary>
        ///     Returns <paramref name="profile" /> or empty; if <paramref name="placeholderCharacterId" /> is set, merges
        ///     missing fields from that vanilla character.
        /// </summary>
        public static CharacterAssetProfile Resolve(CharacterAssetProfile? profile, string? placeholderCharacterId)
        {
            profile ??= CharacterAssetProfile.Empty;

            return string.IsNullOrWhiteSpace(placeholderCharacterId)
                ? profile
                : Merge(FromCharacterId(placeholderCharacterId), profile);
        }

        /// <summary>
        ///     Per-field prefer-<paramref name="profile" /> / fallback-<paramref name="fallback" /> merge.
        /// </summary>
        public static CharacterAssetProfile Merge(CharacterAssetProfile? fallback, CharacterAssetProfile? profile)
        {
            fallback ??= CharacterAssetProfile.Empty;
            profile ??= CharacterAssetProfile.Empty;

            return new(
                MergeScenes(fallback.Scenes, profile.Scenes),
                MergeUi(fallback.Ui, profile.Ui),
                MergeVfx(fallback.Vfx, profile.Vfx),
                MergeSpine(fallback.Spine, profile.Spine),
                MergeAudio(fallback.Audio, profile.Audio),
                MergeMultiplayer(fallback.Multiplayer, profile.Multiplayer),
                MergeVisualCues(fallback.VisualCues, profile.VisualCues),
                MergeWorldProceduralVisuals(fallback.WorldProceduralVisuals, profile.WorldProceduralVisuals),
                MergeVanillaRelicVisualOverrides(fallback.VanillaRelicVisualOverrides,
                    profile.VanillaRelicVisualOverrides),
                MergeVanillaPotionVisualOverrides(fallback.VanillaPotionVisualOverrides,
                    profile.VanillaPotionVisualOverrides),
                MergeVanillaCardVisualOverrides(fallback.VanillaCardVisualOverrides,
                    profile.VanillaCardVisualOverrides));
        }

        /// <summary>
        ///     Shortcut for <see cref="FromCharacterId" /> with id <c>ironclad</c>.
        /// </summary>
        public static CharacterAssetProfile Ironclad()
        {
            return FromCharacterId("ironclad");
        }

        /// <summary>
        ///     Shortcut for <see cref="FromCharacterId" /> with id <c>silent</c>.
        /// </summary>
        public static CharacterAssetProfile Silent()
        {
            return FromCharacterId("silent");
        }

        /// <summary>
        ///     Shortcut for <see cref="FromCharacterId" /> with id <c>defect</c>.
        /// </summary>
        public static CharacterAssetProfile Defect()
        {
            return FromCharacterId("defect");
        }

        /// <summary>
        ///     Shortcut for <see cref="FromCharacterId" /> with id <c>regent</c>.
        /// </summary>
        public static CharacterAssetProfile Regent()
        {
            return FromCharacterId("regent");
        }

        /// <summary>
        ///     Shortcut for <see cref="FromCharacterId" /> with id <c>necrobinder</c>.
        /// </summary>
        public static CharacterAssetProfile Necrobinder()
        {
            return FromCharacterId("necrobinder");
        }

        private static CharacterSceneAssetSet? MergeScenes(CharacterSceneAssetSet? fallback,
            CharacterSceneAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            if (profile == null)
                return fallback;

            return new(profile.VisualsPath ?? fallback.VisualsPath,
                profile.EnergyCounterPath ?? fallback.EnergyCounterPath,
                profile.MerchantAnimPath ?? fallback.MerchantAnimPath,
                profile.RestSiteAnimPath ?? fallback.RestSiteAnimPath);
        }

        private static CharacterUiAssetSet? MergeUi(CharacterUiAssetSet? fallback, CharacterUiAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            if (profile == null)
                return fallback;

            return new(profile.IconTexturePath ?? fallback.IconTexturePath,
                profile.IconOutlineTexturePath ?? fallback.IconOutlineTexturePath,
                profile.IconPath ?? fallback.IconPath, profile.CharacterSelectBgPath ?? fallback.CharacterSelectBgPath,
                profile.CharacterSelectIconPath ?? fallback.CharacterSelectIconPath,
                profile.CharacterSelectLockedIconPath ?? fallback.CharacterSelectLockedIconPath,
                profile.CharacterSelectTransitionPath ?? fallback.CharacterSelectTransitionPath,
                profile.MapMarkerPath ?? fallback.MapMarkerPath);
        }

        private static CharacterVfxAssetSet? MergeVfx(CharacterVfxAssetSet? fallback, CharacterVfxAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            return profile == null
                ? fallback
                : new(profile.TrailPath ?? fallback.TrailPath, profile.TrailStyle ?? fallback.TrailStyle);
        }

        private static CharacterSpineAssetSet? MergeSpine(CharacterSpineAssetSet? fallback,
            CharacterSpineAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            return profile == null ? fallback : new(profile.CombatSkeletonDataPath ?? fallback.CombatSkeletonDataPath);
        }

        private static CharacterAudioAssetSet? MergeAudio(CharacterAudioAssetSet? fallback,
            CharacterAudioAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            return profile == null
                ? fallback
                : new(profile.CharacterSelectSfx ?? fallback.CharacterSelectSfx,
                    profile.CharacterTransitionSfx ?? fallback.CharacterTransitionSfx,
                    profile.AttackSfx ?? fallback.AttackSfx, profile.CastSfx ?? fallback.CastSfx,
                    profile.DeathSfx ?? fallback.DeathSfx);
        }

        private static CharacterMultiplayerAssetSet? MergeMultiplayer(
            CharacterMultiplayerAssetSet? fallback,
            CharacterMultiplayerAssetSet? profile)
        {
            if (fallback == null)
                return profile;

            return profile == null
                ? fallback
                : new(profile.ArmPointingTexturePath ?? fallback.ArmPointingTexturePath,
                    profile.ArmRockTexturePath ?? fallback.ArmRockTexturePath,
                    profile.ArmPaperTexturePath ?? fallback.ArmPaperTexturePath,
                    profile.ArmScissorsTexturePath ?? fallback.ArmScissorsTexturePath);
        }

        private static CharacterVanillaRelicVisualOverride[]? MergeVanillaRelicVisualOverrides(
            CharacterVanillaRelicVisualOverride[]? fallback,
            CharacterVanillaRelicVisualOverride[]? profile)
        {
            if (fallback is not { Length: > 0 })
                return profile is { Length: > 0 } ? profile : null;

            if (profile is not { Length: > 0 })
                return fallback;

            var map = new Dictionary<string, CharacterVanillaRelicVisualOverride>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in fallback)
                map[e.RelicModelIdEntry] = e;

            foreach (var e in profile)
                if (map.TryGetValue(e.RelicModelIdEntry, out var existing))
                    map[e.RelicModelIdEntry] = e with { Assets = MergeRelicAssetProfiles(existing.Assets, e.Assets) };
                else
                    map[e.RelicModelIdEntry] = e;

            var merged = new CharacterVanillaRelicVisualOverride[map.Count];
            var i = 0;
            foreach (var kv in map.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
                merged[i++] = kv.Value;

            return merged;
        }

        private static RelicAssetProfile MergeRelicAssetProfiles(RelicAssetProfile fallback, RelicAssetProfile profile)
        {
            return new(
                profile.IconPath ?? fallback.IconPath,
                profile.IconOutlinePath ?? fallback.IconOutlinePath,
                profile.BigIconPath ?? fallback.BigIconPath);
        }

        private static CharacterVanillaPotionVisualOverride[]? MergeVanillaPotionVisualOverrides(
            CharacterVanillaPotionVisualOverride[]? fallback,
            CharacterVanillaPotionVisualOverride[]? profile)
        {
            if (fallback is not { Length: > 0 })
                return profile is { Length: > 0 } ? profile : null;

            if (profile is not { Length: > 0 })
                return fallback;

            var map = new Dictionary<string, CharacterVanillaPotionVisualOverride>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in fallback)
                map[e.PotionModelIdEntry] = e;

            foreach (var e in profile)
                if (map.TryGetValue(e.PotionModelIdEntry, out var existing))
                    map[e.PotionModelIdEntry] = e with
                    {
                        Assets = MergePotionAssetProfiles(existing.Assets, e.Assets),
                    };
                else
                    map[e.PotionModelIdEntry] = e;

            var merged = new CharacterVanillaPotionVisualOverride[map.Count];
            var i = 0;
            foreach (var kv in map.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
                merged[i++] = kv.Value;

            return merged;
        }

        private static PotionAssetProfile MergePotionAssetProfiles(PotionAssetProfile fallback,
            PotionAssetProfile profile)
        {
            return new(
                profile.ImagePath ?? fallback.ImagePath,
                profile.OutlinePath ?? fallback.OutlinePath);
        }

        private static CharacterVanillaCardVisualOverride[]? MergeVanillaCardVisualOverrides(
            CharacterVanillaCardVisualOverride[]? fallback,
            CharacterVanillaCardVisualOverride[]? profile)
        {
            if (fallback is not { Length: > 0 })
                return profile is { Length: > 0 } ? profile : null;

            if (profile is not { Length: > 0 })
                return fallback;

            var map = new Dictionary<string, CharacterVanillaCardVisualOverride>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in fallback)
                map[e.CardModelIdEntry] = e;

            foreach (var e in profile)
                if (map.TryGetValue(e.CardModelIdEntry, out var existing))
                    map[e.CardModelIdEntry] = e with
                    {
                        Assets = MergeCardAssetProfiles(existing.Assets, e.Assets),
                    };
                else
                    map[e.CardModelIdEntry] = e;

            var merged = new CharacterVanillaCardVisualOverride[map.Count];
            var i = 0;
            foreach (var kv in map.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
                merged[i++] = kv.Value;

            return merged;
        }

        private static CardAssetProfile MergeCardAssetProfiles(CardAssetProfile fallback, CardAssetProfile profile)
        {
            return new(
                profile.PortraitPath ?? fallback.PortraitPath,
                profile.BetaPortraitPath ?? fallback.BetaPortraitPath,
                profile.FramePath ?? fallback.FramePath,
                profile.PortraitBorderPath ?? fallback.PortraitBorderPath,
                profile.EnergyIconPath ?? fallback.EnergyIconPath,
                profile.FrameMaterialPath ?? fallback.FrameMaterialPath,
                profile.OverlayScenePath ?? fallback.OverlayScenePath,
                profile.BannerTexturePath ?? fallback.BannerTexturePath,
                profile.BannerMaterialPath ?? fallback.BannerMaterialPath);
        }

        private static CharacterWorldProceduralVisualSet? MergeWorldProceduralVisuals(
            CharacterWorldProceduralVisualSet? fallback,
            CharacterWorldProceduralVisualSet? profile)
        {
            if (fallback == null)
                return profile;

            if (profile == null)
                return fallback;

            var merchant = profile.Merchant ?? fallback.Merchant;
            var restSite = profile.RestSite ?? fallback.RestSite;

            if (merchant == null && restSite == null)
                return null;

            return new(merchant, restSite);
        }

        private static VisualCueSet? MergeVisualCues(
            VisualCueSet? fallback,
            VisualCueSet? profile)
        {
            if (fallback == null)
                return profile;

            if (profile == null)
                return fallback;

            var mergedTex = MergeCueTextureMap(fallback.TexturePathByCue, profile.TexturePathByCue);
            var mergedSeq = MergeCueFrameSequenceMap(fallback.FrameSequenceByCue, profile.FrameSequenceByCue);

            if (mergedTex == null && mergedSeq == null)
                return new();

            return new(mergedTex, mergedSeq);
        }

        private static IReadOnlyDictionary<string, string>? MergeCueTextureMap(
            IReadOnlyDictionary<string, string>? fallback,
            IReadOnlyDictionary<string, string>? profile)
        {
            if (profile is not { Count: > 0 }) return fallback is { Count: > 0 } ? fallback : null;
            if (fallback is not { Count: > 0 })
                return profile;

            var merged = new Dictionary<string, string>(fallback, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in profile)
                merged[kv.Key] = kv.Value;

            return merged;
        }

        private static IReadOnlyDictionary<string, VisualFrameSequence>? MergeCueFrameSequenceMap(
            IReadOnlyDictionary<string, VisualFrameSequence>? fallback,
            IReadOnlyDictionary<string, VisualFrameSequence>? profile)
        {
            if (profile is not { Count: > 0 }) return fallback is { Count: > 0 } ? fallback : null;
            if (fallback is not { Count: > 0 })
                return profile;

            var merged = new Dictionary<string, VisualFrameSequence>(fallback,
                StringComparer.OrdinalIgnoreCase);
            foreach (var kv in profile)
                merged[kv.Key] = kv.Value;

            return merged;
        }

        /// <summary>
        ///     Merges <paramref name="fallback" /> into <paramref name="profile" /> for any null component or field.
        /// </summary>
        public static CharacterAssetProfile FillMissingFrom(this CharacterAssetProfile profile,
            CharacterAssetProfile fallback)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(fallback);
            return Merge(fallback, profile);
        }

        /// <summary>
        ///     Fills missing entries using <see cref="FromCharacterId" />.
        /// </summary>
        public static CharacterAssetProfile WithPlaceholder(this CharacterAssetProfile profile, string characterId)
        {
            ArgumentNullException.ThrowIfNull(profile);
            return profile.FillMissingFrom(FromCharacterId(characterId));
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Scenes" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithScenes(this CharacterAssetProfile profile,
            CharacterSceneAssetSet scenes)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(scenes);
            return profile with { Scenes = scenes };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Ui" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithUi(this CharacterAssetProfile profile, CharacterUiAssetSet ui)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(ui);
            return profile with { Ui = ui };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Vfx" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithVfx(this CharacterAssetProfile profile, CharacterVfxAssetSet vfx)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(vfx);
            return profile with { Vfx = vfx };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Spine" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithSpine(this CharacterAssetProfile profile, CharacterSpineAssetSet spine)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(spine);
            return profile with { Spine = spine };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Audio" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithAudio(this CharacterAssetProfile profile, CharacterAudioAssetSet audio)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(audio);
            return profile with { Audio = audio };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.Multiplayer" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithMultiplayer(this CharacterAssetProfile profile,
            CharacterMultiplayerAssetSet multiplayer)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(multiplayer);
            return profile with { Multiplayer = multiplayer };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.VisualCues" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithVisualCues(this CharacterAssetProfile profile, VisualCueSet visualCues)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(visualCues);
            return profile with { VisualCues = visualCues };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.WorldProceduralVisuals" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithWorldProceduralVisuals(this CharacterAssetProfile profile,
            CharacterWorldProceduralVisualSet worldVisuals)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(worldVisuals);
            return profile with { WorldProceduralVisuals = worldVisuals };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.VanillaRelicVisualOverrides" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithVanillaRelicVisualOverrides(this CharacterAssetProfile profile,
            CharacterVanillaRelicVisualOverride[] vanillaRelicVisualOverrides)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(vanillaRelicVisualOverrides);
            return profile with { VanillaRelicVisualOverrides = vanillaRelicVisualOverrides };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.VanillaPotionVisualOverrides" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithVanillaPotionVisualOverrides(this CharacterAssetProfile profile,
            CharacterVanillaPotionVisualOverride[] vanillaPotionVisualOverrides)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(vanillaPotionVisualOverrides);
            return profile with { VanillaPotionVisualOverrides = vanillaPotionVisualOverrides };
        }

        /// <summary>
        ///     Returns a copy with <see cref="CharacterAssetProfile.VanillaCardVisualOverrides" /> replaced.
        /// </summary>
        public static CharacterAssetProfile WithVanillaCardVisualOverrides(this CharacterAssetProfile profile,
            CharacterVanillaCardVisualOverride[] vanillaCardVisualOverrides)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(vanillaCardVisualOverrides);
            return profile with { VanillaCardVisualOverrides = vanillaCardVisualOverrides };
        }
    }
}
