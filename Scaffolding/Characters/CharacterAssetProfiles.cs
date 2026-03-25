namespace STS2RitsuLib.Scaffolding.Characters
{
    public static class CharacterAssetProfiles
    {
        public const string DefaultPlaceholderCharacterId = "ironclad";

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

        public static CharacterAssetProfile Resolve(CharacterAssetProfile? profile, string? placeholderCharacterId)
        {
            profile ??= CharacterAssetProfile.Empty;

            if (string.IsNullOrWhiteSpace(placeholderCharacterId))
                return profile;

            return Merge(FromCharacterId(placeholderCharacterId), profile);
        }

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
                MergeMultiplayer(fallback.Multiplayer, profile.Multiplayer));
        }

        public static CharacterAssetProfile Ironclad()
        {
            return FromCharacterId("ironclad");
        }

        public static CharacterAssetProfile Silent()
        {
            return FromCharacterId("silent");
        }

        public static CharacterAssetProfile Defect()
        {
            return FromCharacterId("defect");
        }

        public static CharacterAssetProfile Regent()
        {
            return FromCharacterId("regent");
        }

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

        extension(CharacterAssetProfile profile)
        {
            public CharacterAssetProfile FillMissingFrom(CharacterAssetProfile fallback)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(fallback);
                return Merge(fallback, profile);
            }

            public CharacterAssetProfile WithPlaceholder(string characterId)
            {
                ArgumentNullException.ThrowIfNull(profile);
                return profile.FillMissingFrom(FromCharacterId(characterId));
            }

            public CharacterAssetProfile WithScenes(CharacterSceneAssetSet scenes)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(scenes);
                return profile with { Scenes = scenes };
            }

            public CharacterAssetProfile WithUi(CharacterUiAssetSet ui)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(ui);
                return profile with { Ui = ui };
            }

            public CharacterAssetProfile WithVfx(CharacterVfxAssetSet vfx)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(vfx);
                return profile with { Vfx = vfx };
            }

            public CharacterAssetProfile WithSpine(CharacterSpineAssetSet spine)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(spine);
                return profile with { Spine = spine };
            }

            public CharacterAssetProfile WithAudio(CharacterAudioAssetSet audio)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(audio);
                return profile with { Audio = audio };
            }

            public CharacterAssetProfile WithMultiplayer(CharacterMultiplayerAssetSet multiplayer)
            {
                ArgumentNullException.ThrowIfNull(profile);
                ArgumentNullException.ThrowIfNull(multiplayer);
                return profile with { Multiplayer = multiplayer };
            }
        }
    }
}
