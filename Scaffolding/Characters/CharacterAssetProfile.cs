using Godot;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Scene paths for combat visuals, energy counter, merchant, and rest site animations.
    /// </summary>
    /// <param name="VisualsPath">Creature visuals scene.</param>
    /// <param name="EnergyCounterPath">Energy counter scene.</param>
    /// <param name="MerchantAnimPath">Merchant character scene.</param>
    /// <param name="RestSiteAnimPath">Rest site character scene.</param>
    public sealed record CharacterSceneAssetSet(
        string? VisualsPath = null,
        string? EnergyCounterPath = null,
        string? MerchantAnimPath = null,
        string? RestSiteAnimPath = null);

    /// <summary>
    ///     UI textures and scenes: HUD icon, character select, map marker, transitions.
    /// </summary>
    /// <param name="IconTexturePath">Top-panel icon texture.</param>
    /// <param name="IconOutlineTexturePath">Outlined variant for HUD.</param>
    /// <param name="IconPath">Optional icon scene.</param>
    /// <param name="CharacterSelectBgPath">Character select background scene.</param>
    /// <param name="CharacterSelectIconPath">Portrait when unlocked.</param>
    /// <param name="CharacterSelectLockedIconPath">Portrait when locked.</param>
    /// <param name="CharacterSelectTransitionPath">Transition material resource.</param>
    /// <param name="MapMarkerPath">Run map marker texture.</param>
    public sealed record CharacterUiAssetSet(
        string? IconTexturePath = null,
        string? IconOutlineTexturePath = null,
        string? IconPath = null,
        string? CharacterSelectBgPath = null,
        string? CharacterSelectIconPath = null,
        string? CharacterSelectLockedIconPath = null,
        string? CharacterSelectTransitionPath = null,
        string? MapMarkerPath = null);

    /// <summary>
    ///     Card trail scene and optional style overrides.
    /// </summary>
    /// <param name="TrailPath">Trail VFX scene path.</param>
    /// <param name="TrailStyle">Trail color/width tuning.</param>
    public sealed record CharacterVfxAssetSet(
        string? TrailPath = null,
        CharacterTrailStyle? TrailStyle = null);

    /// <summary>
    ///     Tunable trail / sparkle parameters applied by trail override patches.
    /// </summary>
    /// <param name="OuterTrailModulate">Outer ribbon tint.</param>
    /// <param name="OuterTrailWidth">Outer ribbon width scale.</param>
    /// <param name="InnerTrailModulate">Inner ribbon tint.</param>
    /// <param name="InnerTrailWidth">Inner ribbon width scale.</param>
    /// <param name="BigSparksColor">Large spark color.</param>
    /// <param name="LittleSparksColor">Small spark color.</param>
    /// <param name="PrimarySpriteModulate">Primary trail sprite tint.</param>
    /// <param name="PrimarySpriteScale">Primary trail sprite scale.</param>
    /// <param name="SecondarySpriteModulate">Secondary trail sprite tint.</param>
    /// <param name="SecondarySpriteScale">Secondary trail sprite scale.</param>
    public sealed record CharacterTrailStyle(
        Color? OuterTrailModulate = null,
        float? OuterTrailWidth = null,
        Color? InnerTrailModulate = null,
        float? InnerTrailWidth = null,
        Color? BigSparksColor = null,
        Color? LittleSparksColor = null,
        Color? PrimarySpriteModulate = null,
        Vector2? PrimarySpriteScale = null,
        Color? SecondarySpriteModulate = null,
        Vector2? SecondarySpriteScale = null);

    /// <summary>
    ///     Spine skeleton data used in combat.
    /// </summary>
    /// <param name="CombatSkeletonDataPath">Spine skeleton resource path.</param>
    public sealed record CharacterSpineAssetSet(
        string? CombatSkeletonDataPath = null);

    /// <summary>
    ///     FMOD-style event paths for character feedback audio.
    /// </summary>
    /// <param name="CharacterSelectSfx">Select / confirm on character screen.</param>
    /// <param name="CharacterTransitionSfx">Screen transition sting.</param>
    /// <param name="AttackSfx">Basic attack cue.</param>
    /// <param name="CastSfx">Card cast cue.</param>
    /// <param name="DeathSfx">Player death cue.</param>
    public sealed record CharacterAudioAssetSet(
        string? CharacterSelectSfx = null,
        string? CharacterTransitionSfx = null,
        string? AttackSfx = null,
        string? CastSfx = null,
        string? DeathSfx = null);

    /// <summary>
    ///     RPS hand textures for multiplayer UI.
    /// </summary>
    /// <param name="ArmPointingTexturePath">Pointing hand.</param>
    /// <param name="ArmRockTexturePath">Rock hand.</param>
    /// <param name="ArmPaperTexturePath">Paper hand.</param>
    /// <param name="ArmScissorsTexturePath">Scissors hand.</param>
    public sealed record CharacterMultiplayerAssetSet(
        string? ArmPointingTexturePath = null,
        string? ArmRockTexturePath = null,
        string? ArmPaperTexturePath = null,
        string? ArmScissorsTexturePath = null);

    /// <summary>
    ///     Bundles optional path sets for scaffolding a mod character alongside vanilla layout conventions.
    /// </summary>
    /// <param name="Scenes">Combat / world scenes.</param>
    /// <param name="Ui">HUD and character select assets.</param>
    /// <param name="Vfx">Trails and similar.</param>
    /// <param name="Spine">Spine data.</param>
    /// <param name="Audio">FMOD event ids or paths.</param>
    /// <param name="Multiplayer">Multiplayer hand art.</param>
    public sealed record CharacterAssetProfile(
        CharacterSceneAssetSet? Scenes = null,
        CharacterUiAssetSet? Ui = null,
        CharacterVfxAssetSet? Vfx = null,
        CharacterSpineAssetSet? Spine = null,
        CharacterAudioAssetSet? Audio = null,
        CharacterMultiplayerAssetSet? Multiplayer = null)
    {
        /// <summary>
        ///     Profile with all components null (merge / fill helpers treat null as “missing”).
        /// </summary>
        public static CharacterAssetProfile Empty { get; } = new();
    }
}
