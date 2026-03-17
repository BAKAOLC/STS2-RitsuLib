namespace STS2RitsuLib.Scaffolding.Characters
{
    public static class CharacterAssetProfiles
    {
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

        extension(CharacterAssetProfile profile)
        {
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
