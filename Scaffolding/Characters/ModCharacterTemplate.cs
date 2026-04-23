using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     Declares whether a character participates in vanilla epoch and timeline progression.
    /// </summary>
    public interface IModCharacterEpochTimelineRequirement
    {
        /// <summary>
        ///     When false, runtime compatibility patches skip vanilla character epoch/timeline grant paths that assume
        ///     built-in <c>*_EPOCH</c> ids exist.
        /// </summary>
        bool RequiresEpochAndTimeline { get; }
    }

    /// <summary>
    ///     Controls whether a mod character should appear in vanilla character-select and random selection flows.
    /// </summary>
    public interface IModCharacterVanillaSelectionPolicy
    {
        /// <summary>
        ///     When true, hides the character from vanilla character-select UI lists.
        /// </summary>
        bool HideFromVanillaCharacterSelect { get; }

        /// <summary>
        ///     When false, excludes the character from vanilla random character selection.
        /// </summary>
        bool AllowInVanillaRandomCharacterSelect { get; }
    }

    /// <summary>
    ///     Declarative starting-deck entry that expands one card CLR type into <see cref="Count" /> copies.
    /// </summary>
    /// <param name="CardType">Registered <see cref="CardModel" /> CLR type.</param>
    /// <param name="Count">Number of copies to add to the starting deck.</param>
    public readonly record struct StartingDeckEntry(Type CardType, int Count = 1)
    {
        /// <summary>
        ///     Typed helper for concise collection expressions.
        /// </summary>
        public static StartingDeckEntry Of<TCard>(int count = 1) where TCard : CardModel
        {
            return new(typeof(TCard), count);
        }
    }

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

        /// <summary>
        ///     When <paramref name="relic" /> is owned by a player using this character, returns icon path overrides
        ///     registered for that relic’s <c>ModelId.Entry</c>; otherwise <c>null</c>. Patches resolve this before
        ///     mod-relic <c>IModRelicAssetOverrides</c> so per-owner character art wins over relic-wide defaults.
        /// </summary>
        RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic);

        /// <summary>
        ///     When <paramref name="potion" /> is encountered or held by a player using this character, returns
        ///     image/outline overrides registered for that potion’s <c>ModelId.Entry</c>; otherwise <c>null</c>.
        ///     Patches resolve this before mod-potion <c>IModPotionAssetOverrides</c>.
        /// </summary>
        PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion);

        /// <summary>
        ///     When <paramref name="card" /> is encountered or held by a player using this character, returns
        ///     portrait/frame/banner/overlay overrides registered for that card’s <c>ModelId.Entry</c>;
        ///     otherwise <c>null</c>. Patches resolve this before mod-card <c>IModCardAssetOverrides</c>.
        /// </summary>
        CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card);
    }

    /// <summary>
    ///     Base <see cref="CharacterModel" /> for mods: typed card/relic/potion pools, starting loadout,
    ///     <see cref="IModCharacterAssetOverrides" />, and optional <see cref="TryCreateCreatureVisuals" />.
    /// </summary>
    /// <typeparam name="TCardPool">Concrete <see cref="CardPoolModel" /> type registered for this character.</typeparam>
    /// <typeparam name="TRelicPool">Concrete <see cref="RelicPoolModel" /> type registered for this character.</typeparam>
    /// <typeparam name="TPotionPool">Concrete <see cref="PotionPoolModel" /> type registered for this character.</typeparam>
#pragma warning disable CS0618
    // Template keeps the obsolete IModCharacter* visuals / animator factory interfaces wired so existing derived
    // classes and external consumers that type-check against the old interface names continue to work.
    public abstract class ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool> : CharacterModel
        , IModCharacterAssetOverrides, IModCreatureVisualsFactory, IModCharacterCreatureVisualsFactory,
        IModCreatureAnimatorFactory, IModCharacterCreatureAnimatorFactory,
        IModNonSpineAnimationStateMachineFactory, IModCharacterMerchantAnimationStateMachineFactory,
        IModCharacterEpochTimelineRequirement, IModCharacterVanillaSelectionPolicy
#pragma warning restore CS0618
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
        public sealed override IEnumerable<CardModel> StartingDeck => ResolveStartingDeck();

        /// <inheritdoc />
        public sealed override IReadOnlyList<RelicModel> StartingRelics => ResolveStartingRelics();

        /// <inheritdoc />
        public sealed override IReadOnlyList<PotionModel> StartingPotions => ResolveStartingPotions();

        /// <inheritdoc />
        protected sealed override CharacterModel? UnlocksAfterRunAs => UnlocksAfterRunAsType == null
            ? null
            : ModelDb.GetById<CharacterModel>(ModelDb.GetId(UnlocksAfterRunAsType));

        /// <summary>
        ///     Legacy local starter-deck hook. Prefer additive character-starter registration so starter content can be
        ///     appended outside the character class and remain insensitive to registration order.
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingCard(...) or "
            + "ModContentRegistry.RegisterCharacterStarterCard(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<StartingDeckEntry> StartingDeckEntries
        {
            get
            {
#pragma warning disable CS0618 // Intentional compatibility bridge from legacy StartingDeckTypes
                return StartingDeckTypes.Select(static type => new StartingDeckEntry(type));
#pragma warning restore CS0618
            }
        }

        /// <summary>
        ///     CLR types of cards that form the starting deck; each type must be registered as a <see cref="CardModel" />.
        ///     Prefer additive character-starter registration in new mods.
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration. This legacy hook requires repeating the same type for duplicate starter cards. "
            + "Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingDeckTypes => [];

        /// <summary>
        ///     Legacy local starting-relic hook. Prefer additive character-starter registration in new mods.
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingRelic(...) or "
            + "ModContentRegistry.RegisterCharacterStarterRelic(...). Override only for legacy mods; suppress CS0618 if required.")]
        protected virtual IEnumerable<Type> StartingRelicTypes => [];

        /// <summary>
        ///     Legacy local starting-potion hook. Prefer additive character-starter registration in new mods.
        /// </summary>
        [Obsolete(
            "Prefer additive character-starter registration through CharacterRegistrationEntry.AddStartingPotion(...) or "
            + "ModContentRegistry.RegisterCharacterStarterPotion(...). Override only for legacy mods; suppress CS0618 if required.")]
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
        public virtual RelicAssetProfile? TryGetVanillaRelicVisualOverrideForOwnedRelic(RelicModel relic)
        {
            var entries = ResolvedAssetProfile.VanillaRelicVisualOverrides;
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

        /// <inheritdoc />
        public virtual PotionAssetProfile? TryGetVanillaPotionVisualOverrideForContext(PotionModel potion)
        {
            var entries = ResolvedAssetProfile.VanillaPotionVisualOverrides;
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

        /// <inheritdoc />
        public virtual CardAssetProfile? TryGetVanillaCardVisualOverrideForContext(CardModel card)
        {
            var entries = ResolvedAssetProfile.VanillaCardVisualOverrides;
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

        /// <inheritdoc />
        public virtual VisualCueSet? VisualCues => ResolvedAssetProfile.VisualCues;

        /// <inheritdoc />
        public virtual CharacterWorldProceduralVisualSet? WorldProceduralVisuals =>
            ResolvedAssetProfile.WorldProceduralVisuals;

#pragma warning disable CS0618
        CreatureAnimator? IModCharacterCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }
#pragma warning restore CS0618

#pragma warning disable CS0618
        NCreatureVisuals? IModCharacterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }
#pragma warning restore CS0618

        /// <inheritdoc />
        public virtual bool RequiresEpochAndTimeline => true;

        ModAnimStateMachine? IModCharacterMerchantAnimationStateMachineFactory.
            TryCreateMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character)
        {
            return SetupCustomMerchantAnimationStateMachine(merchantRoot, character);
        }

        /// <inheritdoc />
        public virtual bool HideFromVanillaCharacterSelect => false;

        /// <inheritdoc />
        public virtual bool AllowInVanillaRandomCharacterSelect => !HideFromVanillaCharacterSelect;

        CreatureAnimator? IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }

        NCreatureVisuals? IModCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        ModAnimStateMachine? IModNonSpineAnimationStateMachineFactory.
            TryCreateNonSpineAnimationStateMachine(Node visualsRoot)
        {
            return SetupCustomNonSpineAnimationStateMachine(visualsRoot, this);
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
        ///     Optional override producing a fully wired Spine <see cref="CreatureAnimator" /> (state graph for idle /
        ///     hit / attack / cast / die / relaxed). Return <see langword="null" /> to defer to vanilla
        ///     <see cref="CharacterModel.GenerateAnimator" />. Prefer <see cref="ModAnimStateMachines.Standard" /> to
        ///     match baselib semantics.
        /// </summary>
        /// <param name="controller">Spine controller attached to the character's combat visuals.</param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a non-Spine <see cref="ModAnimStateMachine" /> for the character's combat
        ///     visuals (cue frame sequences, Godot animation player, animated sprite). Return <see langword="null" />
        ///     to defer to single-shot playback via <c>ModCreatureVisualPlayback</c>.
        /// </summary>
        /// <param name="visualsRoot">Combat visuals root node.</param>
        /// <param name="character">Character model (always <see langword="this" />, exposed for convenience).</param>
        protected virtual ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(Node visualsRoot,
            CharacterModel character)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a merchant / rest-site <see cref="ModAnimStateMachine" /> for the character.
        ///     Return <see langword="null" /> to defer to single-shot playback via <c>ModCreatureVisualPlayback</c>.
        /// </summary>
        /// <param name="merchantRoot">Merchant character root node.</param>
        /// <param name="character">Character model (always <see langword="this" />, exposed for convenience).</param>
        protected virtual ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(Node merchantRoot,
            CharacterModel character)
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

        private IEnumerable<CardModel> ResolveStartingDeck()
        {
            var characterType = GetType();
            var localEntries = GetLocalStartingDeckEntries();
            var registeredEntries = ModContentRegistry.GetRegisteredCharacterStarterCards(characterType);

            return localEntries
                .SelectMany(static entry => Enumerable.Repeat(entry.CardType, Math.Max(entry.Count, 0)))
                .Concat(registeredEntries)
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        private IReadOnlyList<RelicModel> ResolveStartingRelics()
        {
            var characterType = GetType();
            return GetLocalStartingRelicTypes()
                .Concat(ModContentRegistry.GetRegisteredCharacterStarterRelics(characterType))
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        private IReadOnlyList<PotionModel> ResolveStartingPotions()
        {
            var characterType = GetType();
            return GetLocalStartingPotionTypes()
                .Concat(ModContentRegistry.GetRegisteredCharacterStarterPotions(characterType))
                .Select(type => ModelDb.GetById<PotionModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        private IReadOnlyList<StartingDeckEntry> GetLocalStartingDeckEntries()
        {
#pragma warning disable CS0618 // Intentional legacy compatibility hooks
            return StartingDeckEntries.ToArray();
#pragma warning restore CS0618
        }

        private IReadOnlyList<Type> GetLocalStartingRelicTypes()
        {
#pragma warning disable CS0618 // Intentional legacy compatibility hooks
            return StartingRelicTypes.ToArray();
#pragma warning restore CS0618
        }

        private IReadOnlyList<Type> GetLocalStartingPotionTypes()
        {
#pragma warning disable CS0618 // Intentional legacy compatibility hooks
            return StartingPotionTypes.ToArray();
#pragma warning restore CS0618
        }
    }
}
