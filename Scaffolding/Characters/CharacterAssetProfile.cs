using Godot;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

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
    ///     One entry in <see cref="CharacterAssetProfile.VanillaRelicVisualOverrides" />: when this mod character owns a
    ///     relic whose <c>ModelId.Entry</c> equals <paramref name="RelicModelIdEntry" /> (ordinal ignore-case), use
    ///     <paramref name="Assets" /> for icon paths.
    /// </summary>
    /// <param name="RelicModelIdEntry">Stable relic id (same string as <c>RelicModel.Id.Entry</c>).</param>
    /// <param name="Assets">
    ///     Packed icon, outline, and large art paths (same shape as mod relic
    ///     <see cref="RelicAssetProfile" />).
    /// </param>
    public sealed record CharacterVanillaRelicVisualOverride(string RelicModelIdEntry, RelicAssetProfile Assets);

    /// <summary>
    ///     One entry in <see cref="CharacterAssetProfile.VanillaPotionVisualOverrides" />: when this mod character
    ///     encounters or holds a potion whose <c>ModelId.Entry</c> equals <paramref name="PotionModelIdEntry" />
    ///     (ordinal ignore-case), use <paramref name="Assets" /> for image/outline paths.
    /// </summary>
    /// <param name="PotionModelIdEntry">Stable potion id (same string as <c>PotionModel.Id.Entry</c>).</param>
    /// <param name="Assets">
    ///     Bottle and outline paths (same shape as mod potion <see cref="PotionAssetProfile" />).
    /// </param>
    public sealed record CharacterVanillaPotionVisualOverride(string PotionModelIdEntry, PotionAssetProfile Assets);

    /// <summary>
    ///     One entry in <see cref="CharacterAssetProfile.VanillaCardVisualOverrides" />: when this mod character
    ///     encounters or holds a card whose <c>ModelId.Entry</c> equals <paramref name="CardModelIdEntry" />
    ///     (ordinal ignore-case), use <paramref name="Assets" /> for portrait/frame/banner/overlay paths.
    /// </summary>
    /// <param name="CardModelIdEntry">Stable card id (same string as <c>CardModel.Id.Entry</c>).</param>
    /// <param name="Assets">
    ///     Card portrait and frame/border/material/overlay/banner path bundle (same shape as
    ///     mod card <see cref="CardAssetProfile" />).
    /// </param>
    public sealed record CharacterVanillaCardVisualOverride(string CardModelIdEntry, CardAssetProfile Assets);

    /// <summary>
    ///     Well-known <see cref="CharacterVanillaRelicVisualOverride.RelicModelIdEntry" /> values for base-game relics
    ///     that commonly need per-character art.
    /// </summary>
    public static class CharacterOwnedVanillaRelicModelId
    {
        /// <summary>
        ///     Entry id for the vanilla <c>YummyCookie</c> relic.
        /// </summary>
        public const string YummyCookie = "yummy_cookie";
    }

    /// <summary>
    ///     Bundles optional path sets for scaffolding a mod character alongside vanilla layout conventions.
    /// </summary>
    /// <param name="Scenes">Combat / world scenes.</param>
    /// <param name="Ui">HUD and character select assets.</param>
    /// <param name="Vfx">Trails and similar.</param>
    /// <param name="Spine">Spine data.</param>
    /// <param name="Audio">FMOD event ids or paths.</param>
    /// <param name="Multiplayer">Multiplayer hand art.</param>
    /// <param name="VisualCues">Per-cue textures / frame sequences (combat, game-over, and other consumers).</param>
    /// <param name="WorldProceduralVisuals">Merchant / rest-site shells without custom character <c>tscn</c> scenes.</param>
    /// <param name="VanillaRelicVisualOverrides">
    ///     Per–relic-id icon overrides when this character is the relic owner (see
    ///     <see cref="CharacterVanillaRelicVisualOverride" />).
    /// </param>
    /// <param name="VanillaPotionVisualOverrides">
    ///     Per–potion-id image/outline overrides when this character encounters or holds that potion (see
    ///     <see cref="CharacterVanillaPotionVisualOverride" />).
    /// </param>
    /// <param name="VanillaCardVisualOverrides">
    ///     Per–card-id portrait/frame/banner/overlay overrides when this character encounters or holds that card (see
    ///     <see cref="CharacterVanillaCardVisualOverride" />).
    /// </param>
    public sealed record CharacterAssetProfile(
        CharacterSceneAssetSet? Scenes = null,
        CharacterUiAssetSet? Ui = null,
        CharacterVfxAssetSet? Vfx = null,
        CharacterSpineAssetSet? Spine = null,
        CharacterAudioAssetSet? Audio = null,
        CharacterMultiplayerAssetSet? Multiplayer = null,
        VisualCueSet? VisualCues = null,
        CharacterWorldProceduralVisualSet? WorldProceduralVisuals = null,
        CharacterVanillaRelicVisualOverride[]? VanillaRelicVisualOverrides = null,
        CharacterVanillaPotionVisualOverride[]? VanillaPotionVisualOverrides = null,
        CharacterVanillaCardVisualOverride[]? VanillaCardVisualOverrides = null)
    {
        /// <summary>
        ///     Binary compatibility constructor for assemblies compiled before
        ///     <see cref="VanillaRelicVisualOverrides" /> was added (legacy eight-parameter <c>.ctor</c>).
        /// </summary>
        public CharacterAssetProfile(
            CharacterSceneAssetSet? scenes,
            CharacterUiAssetSet? ui,
            CharacterVfxAssetSet? vfx,
            CharacterSpineAssetSet? spine,
            CharacterAudioAssetSet? audio,
            CharacterMultiplayerAssetSet? multiplayer,
            VisualCueSet? visualCues,
            CharacterWorldProceduralVisualSet? worldProceduralVisuals)
            : this(
                scenes,
                ui,
                vfx,
                spine,
                audio,
                multiplayer,
                visualCues,
                worldProceduralVisuals,
                null)
        {
        }

        /// <summary>
        ///     Profile with all components null (merge / fill helpers treat null as “missing”).
        /// </summary>
        public static CharacterAssetProfile Empty { get; } = new();
    }
}
