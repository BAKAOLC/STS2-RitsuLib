using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Optional asset paths and profile data for mod characters. Patches read these values to override
    ///     vanilla <see cref="CharacterModel" /> asset resolution (visuals, UI, audio, multiplayer arms, combat Spine).
    /// </summary>
    public interface IModCharacterAssetOverrides
    {
        /// <summary>
        ///     Structured bundle of paths and styles; individual <c>Custom*</c> properties typically resolve from this
        ///     profile unless overridden in a subclass.
        /// </summary>
        CharacterAssetProfile AssetProfile { get; }

        /// <summary>
        ///     Resource path for the character combat / scene visuals (replaces vanilla <c>VisualsPath</c> when set).
        /// </summary>
        string? CustomVisualsPath { get; }

        /// <summary>
        ///     Resource path for the energy counter UI used with this character.
        /// </summary>
        string? CustomEnergyCounterPath { get; }

        /// <summary>
        ///     Resource path for merchant-room character animation assets.
        /// </summary>
        string? CustomMerchantAnimPath { get; }

        /// <summary>
        ///     Resource path for rest-site character animation assets.
        /// </summary>
        string? CustomRestSiteAnimPath { get; }

        /// <summary>
        ///     Path to the main icon texture (atlas entry or image) for UI that uses <c>IconTexturePath</c>.
        /// </summary>
        string? CustomIconTexturePath { get; }

        /// <summary>
        ///     Path to the icon outline texture used for UI framing.
        /// </summary>
        string? CustomIconOutlineTexturePath { get; }

        /// <summary>
        ///     Path resolved as the compact icon asset (<c>IconPath</c>).
        /// </summary>
        string? CustomIconPath { get; }

        /// <summary>
        ///     Scene or resource path for the character-select background art.
        /// </summary>
        string? CustomCharacterSelectBgPath { get; }

        /// <summary>
        ///     Path for the selectable character portrait/icon on the character-select screen.
        /// </summary>
        string? CustomCharacterSelectIconPath { get; }

        /// <summary>
        ///     Path for the locked-state icon on the character-select screen.
        /// </summary>
        string? CustomCharacterSelectLockedIconPath { get; }

        /// <summary>
        ///     Path for transition art/video when confirming character selection.
        /// </summary>
        string? CustomCharacterSelectTransitionPath { get; }

        /// <summary>
        ///     Path for the world-map marker icon representing this character.
        /// </summary>
        string? CustomMapMarkerPath { get; }

        /// <summary>
        ///     Path to the trail VFX scene or resource used when playing cards.
        /// </summary>
        string? CustomTrailPath { get; }

        /// <summary>
        ///     Optional modulate/width/color overrides when reusing a vanilla trail scene (see trail style patch).
        /// </summary>
        CharacterTrailStyle? CustomTrailStyle { get; }

        /// <summary>
        ///     Path to Spine skeleton data (<c>.tres</c> / resource) for combat, when reusing vanilla visuals scenes.
        /// </summary>
        string? CustomCombatSpineSkeletonDataPath { get; }

        /// <summary>
        ///     FMOD event id or path for the sound played when this character is chosen on the select screen.
        /// </summary>
        string? CustomCharacterSelectSfx { get; }

        /// <summary>
        ///     FMOD event id or path for the transition sound when locking in this character.
        /// </summary>
        string? CustomCharacterTransitionSfx { get; }

        /// <summary>
        ///     FMOD event id or path for the basic attack sound in combat.
        /// </summary>
        string? CustomAttackSfx { get; }

        /// <summary>
        ///     FMOD event id or path for casting / card-play style combat audio.
        /// </summary>
        string? CustomCastSfx { get; }

        /// <summary>
        ///     FMOD event id or path for this character’s death sound.
        /// </summary>
        string? CustomDeathSfx { get; }

        /// <summary>
        ///     Texture path for the “pointing” arm pose in multiplayer UI.
        /// </summary>
        string? CustomArmPointingTexturePath { get; }

        /// <summary>
        ///     Texture path for the rock hand in multiplayer RPS-style UI.
        /// </summary>
        string? CustomArmRockTexturePath { get; }

        /// <summary>
        ///     Texture path for the paper hand in multiplayer RPS-style UI.
        /// </summary>
        string? CustomArmPaperTexturePath { get; }

        /// <summary>
        ///     Texture path for the scissors hand in multiplayer RPS-style UI.
        /// </summary>
        string? CustomArmScissorsTexturePath { get; }

        /// <summary>
        ///     Optional per-cue static textures and frame sequences for non-Spine combat / game-over visuals; define with
        ///     <c>ModVisualCues</c> (runtime: <c>ModCreatureVisualPlayback</c>).
        /// </summary>
        VisualCueSet? VisualCues { get; }

        /// <summary>
        ///     Optional merchant / rest-site procedural shells (no custom merchant or rest-site character <c>tscn</c>);
        ///     see <see cref="ModCharacterWorldSceneVisuals" />.
        /// </summary>
        CharacterWorldProceduralVisualSet? WorldProceduralVisuals { get; }
    }

    /// <summary>
    ///     Base <see cref="CharacterModel" /> for mods: typed card/relic/potion pools, starting loadout,
    ///     <see cref="IModCharacterAssetOverrides" />, and optional <see cref="TryCreateCreatureVisuals" />.
    /// </summary>
    /// <typeparam name="TCardPool">Concrete <see cref="CardPoolModel" /> type registered for this character.</typeparam>
    /// <typeparam name="TRelicPool">Concrete <see cref="RelicPoolModel" /> type registered for this character.</typeparam>
    /// <typeparam name="TPotionPool">Concrete <see cref="PotionPoolModel" /> type registered for this character.</typeparam>
    public abstract class ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool> : CharacterModel
        , IModCharacterAssetOverrides, IModCharacterCreatureVisualsFactory
        where TCardPool : CardPoolModel
        where TRelicPool : RelicPoolModel
        where TPotionPool : PotionPoolModel
    {
        /// <inheritdoc />
        public override string CharacterSelectSfx =>
            CustomCharacterSelectSfx ?? base.CharacterSelectSfx;

        /// <inheritdoc />
        public override string CharacterTransitionSfx =>
            CustomCharacterTransitionSfx ?? base.CharacterTransitionSfx;

        /// <inheritdoc />
        protected override string CharacterSelectIconPath =>
            CustomCharacterSelectIconPath ?? base.CharacterSelectIconPath;

        /// <inheritdoc />
        protected override string CharacterSelectLockedIconPath =>
            CustomCharacterSelectLockedIconPath ?? base.CharacterSelectLockedIconPath;

        /// <inheritdoc />
        protected override string MapMarkerPath =>
            CustomMapMarkerPath ?? base.MapMarkerPath;

        /// <summary>
        ///     Resolves this character’s card pool from <typeparamref name="TCardPool" /> via <see cref="ModelDb" />.
        /// </summary>
        public sealed override CardPoolModel CardPool =>
            ModelDb.GetById<CardPoolModel>(ModelDb.GetId<TCardPool>());

        /// <summary>
        ///     Resolves this character’s relic pool from <typeparamref name="TRelicPool" /> via <see cref="ModelDb" />.
        /// </summary>
        public sealed override RelicPoolModel RelicPool =>
            ModelDb.GetById<RelicPoolModel>(ModelDb.GetId<TRelicPool>());

        /// <summary>
        ///     Resolves this character’s potion pool from <typeparamref name="TPotionPool" /> via <see cref="ModelDb" />.
        /// </summary>
        public sealed override PotionPoolModel PotionPool =>
            ModelDb.GetById<PotionPoolModel>(ModelDb.GetId<TPotionPool>());

        /// <inheritdoc />
        public sealed override IEnumerable<CardModel> StartingDeck => ResolveModels<CardModel>(StartingDeckTypes);

        /// <inheritdoc />
        public sealed override IReadOnlyList<RelicModel> StartingRelics =>
            ResolveModels<RelicModel>(StartingRelicTypes).ToArray();

        /// <inheritdoc />
        public sealed override IReadOnlyList<PotionModel> StartingPotions =>
            ResolveModels<PotionModel>(StartingPotionTypes).ToArray();

        /// <inheritdoc />
        protected sealed override CharacterModel? UnlocksAfterRunAs => UnlocksAfterRunAsType == null
            ? null
            : ModelDb.GetById<CharacterModel>(ModelDb.GetId(UnlocksAfterRunAsType));

        /// <summary>
        ///     CLR types of cards that form the starting deck; each type must be registered as a <see cref="CardModel" />.
        /// </summary>
        protected abstract IEnumerable<Type> StartingDeckTypes { get; }

        /// <summary>
        ///     CLR types of relics granted at run start; each type must be registered as a <see cref="RelicModel" />.
        /// </summary>
        protected abstract IEnumerable<Type> StartingRelicTypes { get; }

        /// <summary>
        ///     Optional starting potion types; defaults to none.
        /// </summary>
        protected virtual IEnumerable<Type> StartingPotionTypes => [];

        /// <summary>
        ///     Optional prerequisite character type for vanilla <see cref="CharacterModel.GetUnlockText" /> (the
        ///     <c>{Prerequisite}</c> placeholder). Does not drive mod unlock logic — align with
        ///     <see cref="Unlocks.ModUnlockRegistry" /> rules (e.g. the same <c>TCharacter</c> in
        ///     <c>UnlockEpochAfterWinAs&lt;TCharacter, TEpoch&gt;</c>).
        /// </summary>
        protected virtual Type? UnlocksAfterRunAsType => null;

        /// <summary>
        ///     Placeholder vanilla character id used when merging partial <see cref="CharacterAssetProfile" /> data
        ///     (see <see cref="CharacterAssetProfiles.Resolve" />).
        /// </summary>
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public virtual string? PlaceholderCharacterId => CharacterAssetProfiles.DefaultPlaceholderCharacterId;

        /// <summary>
        ///     Effective asset profile after resolving against <see cref="PlaceholderCharacterId" />.
        /// </summary>
        protected CharacterAssetProfile ResolvedAssetProfile =>
            CharacterAssetProfiles.Resolve(AssetProfile, PlaceholderCharacterId);

        /// <inheritdoc />
        public virtual CharacterAssetProfile AssetProfile => CharacterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => ResolvedAssetProfile.Scenes?.VisualsPath;

        /// <inheritdoc />
        public virtual string? CustomEnergyCounterPath => ResolvedAssetProfile.Scenes?.EnergyCounterPath;

        /// <inheritdoc />
        public virtual string? CustomMerchantAnimPath => ResolvedAssetProfile.Scenes?.MerchantAnimPath;

        /// <inheritdoc />
        public virtual string? CustomRestSiteAnimPath => ResolvedAssetProfile.Scenes?.RestSiteAnimPath;

        /// <inheritdoc />
        public virtual string? CustomIconTexturePath => ResolvedAssetProfile.Ui?.IconTexturePath;

        /// <inheritdoc />
        public virtual string? CustomIconOutlineTexturePath => ResolvedAssetProfile.Ui?.IconOutlineTexturePath;

        /// <inheritdoc />
        public virtual string? CustomIconPath => ResolvedAssetProfile.Ui?.IconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectBgPath => ResolvedAssetProfile.Ui?.CharacterSelectBgPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectIconPath => ResolvedAssetProfile.Ui?.CharacterSelectIconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectLockedIconPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectLockedIconPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectTransitionPath =>
            ResolvedAssetProfile.Ui?.CharacterSelectTransitionPath;

        /// <inheritdoc />
        public virtual string? CustomMapMarkerPath => ResolvedAssetProfile.Ui?.MapMarkerPath;

        /// <inheritdoc />
        public virtual string? CustomTrailPath => ResolvedAssetProfile.Vfx?.TrailPath;

        /// <inheritdoc />
        public virtual CharacterTrailStyle? CustomTrailStyle => ResolvedAssetProfile.Vfx?.TrailStyle;

        /// <inheritdoc />
        public virtual string? CustomCombatSpineSkeletonDataPath => ResolvedAssetProfile.Spine?.CombatSkeletonDataPath;

        /// <inheritdoc />
        public virtual string? CustomCharacterSelectSfx => ResolvedAssetProfile.Audio?.CharacterSelectSfx;

        /// <inheritdoc />
        public virtual string? CustomCharacterTransitionSfx => ResolvedAssetProfile.Audio?.CharacterTransitionSfx;

        /// <inheritdoc />
        public virtual string? CustomAttackSfx => ResolvedAssetProfile.Audio?.AttackSfx;

        /// <inheritdoc />
        public virtual string? CustomCastSfx => ResolvedAssetProfile.Audio?.CastSfx;

        /// <inheritdoc />
        public virtual string? CustomDeathSfx => ResolvedAssetProfile.Audio?.DeathSfx;

        /// <inheritdoc />
        public virtual string? CustomArmPointingTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPointingTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmRockTexturePath => ResolvedAssetProfile.Multiplayer?.ArmRockTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmPaperTexturePath => ResolvedAssetProfile.Multiplayer?.ArmPaperTexturePath;

        /// <inheritdoc />
        public virtual string? CustomArmScissorsTexturePath => ResolvedAssetProfile.Multiplayer?.ArmScissorsTexturePath;

        /// <inheritdoc />
        public virtual VisualCueSet? VisualCues => ResolvedAssetProfile.VisualCues;

        /// <inheritdoc />
        public virtual CharacterWorldProceduralVisualSet? WorldProceduralVisuals =>
            ResolvedAssetProfile.WorldProceduralVisuals;

        NCreatureVisuals? IModCharacterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        /// <summary>
        ///     Non-null combat visuals; otherwise <see cref="IModCharacterAssetOverrides.CustomVisualsPath" /> / vanilla
        ///     paths apply.
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     Maps model CLR types to live <typeparamref name="TModel" /> instances from <see cref="ModelDb" />.
        /// </summary>
        protected static IEnumerable<TModel> ResolveModels<TModel>(IEnumerable<Type> types)
            where TModel : AbstractModel
        {
            return types
                .Select(type => ModelDb.GetById<TModel>(ModelDb.GetId(type)))
                .ToArray();
        }
    }
}
