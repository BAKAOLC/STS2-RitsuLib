using Godot;

namespace STS2RitsuLib.Scaffolding.Characters
{
    public sealed record CharacterSceneAssetSet(
        string? VisualsPath = null,
        string? EnergyCounterPath = null,
        string? MerchantAnimPath = null,
        string? RestSiteAnimPath = null);

    public sealed record CharacterUiAssetSet(
        string? IconTexturePath = null,
        string? IconOutlineTexturePath = null,
        string? IconPath = null,
        string? CharacterSelectBgPath = null,
        string? CharacterSelectIconPath = null,
        string? CharacterSelectLockedIconPath = null,
        string? CharacterSelectTransitionPath = null,
        string? MapMarkerPath = null);

    public sealed record CharacterVfxAssetSet(
        string? TrailPath = null,
        CharacterTrailStyle? TrailStyle = null);

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

    public sealed record CharacterSpineAssetSet(
        string? CombatSkeletonDataPath = null);

    public sealed record CharacterAudioAssetSet(
        string? CharacterSelectSfx = null,
        string? CharacterTransitionSfx = null,
        string? AttackSfx = null,
        string? CastSfx = null,
        string? DeathSfx = null);

    public sealed record CharacterMultiplayerAssetSet(
        string? ArmPointingTexturePath = null,
        string? ArmRockTexturePath = null,
        string? ArmPaperTexturePath = null,
        string? ArmScissorsTexturePath = null);

    public sealed record CharacterAssetProfile(
        CharacterSceneAssetSet? Scenes = null,
        CharacterUiAssetSet? Ui = null,
        CharacterVfxAssetSet? Vfx = null,
        CharacterSpineAssetSet? Spine = null,
        CharacterAudioAssetSet? Audio = null,
        CharacterMultiplayerAssetSet? Multiplayer = null)
    {
        public static CharacterAssetProfile Empty { get; } = new();
    }
}
