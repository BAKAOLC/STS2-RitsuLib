using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Runtime <see cref="NCreatureVisuals" /> factory for any combat creature model (player characters and
    ///     monsters). Implement on the model type — typically by subclassing
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///     or <see cref="ModMonsterTemplate" />, though the templates are convenience and not required. Non-null
    ///     <see cref="TryCreateCreatureVisuals" /> replaces the path-based <c>CreateVisuals</c> on both
    ///     <see cref="CharacterModel" /> and <see cref="MonsterModel" />.
    /// </summary>
    public interface IModCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to fall through to asset paths.
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Obsolete monster-specific alias of <see cref="IModCreatureVisualsFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureVisualsFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="MonsterModel" />.
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModMonsterCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to use asset paths.
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Obsolete character-specific alias of <see cref="IModCreatureVisualsFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureVisualsFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="CharacterModel" />.
    /// </summary>
    [Obsolete(
        "Implement IModCreatureVisualsFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to use asset paths.
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Runtime encounter combat root <see cref="Control" />. Set <see cref="SuppliesEncounterCombatSceneFromFactory" />
    ///     when using a factory without <c>CustomEncounterScenePath</c> so <c>HasScene</c> stays correct.
    /// </summary>
    public interface IModEncounterCombatSceneFactory
    {
        /// <summary>
        ///     <c>true</c> when this encounter provides a scene only via the factory (no path).
        /// </summary>
        bool SuppliesEncounterCombatSceneFromFactory { get; }

        /// <summary>
        ///     Combat UI root from code, or <c>null</c> to load the default encounter scene.
        /// </summary>
        Control? TryCreateEncounterCombatScene();
    }

    /// <summary>
    ///     Runtime layout <see cref="PackedScene" /> for <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateScene" />.
    /// </summary>
    public interface IModEventLayoutPackedSceneFactory
    {
        /// <summary>
        ///     Layout scene from code, or <c>null</c> to resolve <c>LayoutScenePath</c>.
        /// </summary>
        PackedScene? TryCreateLayoutPackedScene();
    }

    /// <summary>
    ///     Runtime background <see cref="PackedScene" /> for
    ///     <see cref="MegaCrit.Sts2.Core.Models.EventModel.CreateBackgroundScene" />.
    /// </summary>
    public interface IModEventBackgroundPackedSceneFactory
    {
        /// <summary>
        ///     Background scene from code, or <c>null</c> to use path resolution.
        /// </summary>
        PackedScene? TryCreateBackgroundPackedScene();
    }

    /// <summary>
    ///     Runtime event VFX <see cref="Node2D" />. Use <see cref="SuppliesCustomEventVfx" /> when VFX is code-built and
    ///     there is no VFX scene file on disk.
    /// </summary>
    public interface IModEventVfxFactory
    {
        /// <summary>
        ///     <c>true</c> when <see cref="TryCreateEventVfx" /> should run instead of loading the default VFX path.
        /// </summary>
        bool SuppliesCustomEventVfx { get; }

        /// <summary>
        ///     VFX root from code, or <c>null</c> to fall through to path-based loading.
        /// </summary>
        Node2D? TryCreateEventVfx();
    }

    /// <summary>
    ///     Runtime orb presentation <see cref="Node2D" /> for <c>OrbModel.CreateSprite</c>. Match the node shape and animation
    ///     setup that vanilla expects (e.g. Spine idle) if other systems assume it.
    /// </summary>
    public interface IModOrbSpriteFactory
    {
        /// <summary>
        ///     Orb sprite node from code, or <c>null</c> to instantiate from the visuals scene path.
        /// </summary>
        Node2D? TryCreateOrbSprite();
    }

    /// <summary>
    ///     Runtime Spine <see cref="CreatureAnimator" /> factory for any combat creature model (player characters
    ///     and monsters). Overrides the default vanilla <c>GenerateAnimator</c> so mods can wire custom
    ///     <see cref="AnimState" /> graphs (idle / hit / attack / cast / die / relaxed) without subclassing
    ///     <c>NCreature</c>. Prefer
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachines.Standard" /> for the
    ///     standard shape; return <see langword="null" /> to fall through to vanilla behaviour.
    /// </summary>
    public interface IModCreatureAnimatorFactory
    {
        /// <summary>
        ///     Returns a fully wired <see cref="CreatureAnimator" />, or <see langword="null" /> to defer to vanilla.
        /// </summary>
        /// <param name="controller">Spine controller attached to the creature's combat visuals.</param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     Obsolete character-specific alias of <see cref="IModCreatureAnimatorFactory" /> kept for backward
    ///     compatibility with existing mods. New code should implement
    ///     <see cref="IModCreatureAnimatorFactory" /> — which works for both monsters and player characters —
    ///     instead. The routing patch still honours this interface when present on a <see cref="CharacterModel" />.
    /// </summary>
    [Obsolete(
        "Implement IModCreatureAnimatorFactory instead; the replacement applies to both monsters and player characters.",
        false)]
    public interface IModCharacterCreatureAnimatorFactory
    {
        /// <summary>
        ///     Returns a fully wired <see cref="CreatureAnimator" />, or <see langword="null" /> to defer to vanilla.
        /// </summary>
        /// <param name="controller">Spine controller attached to the character's combat visuals.</param>
        CreatureAnimator? TryCreateCreatureAnimator(MegaSprite controller);
    }

    /// <summary>
    ///     Runtime non-Spine <see cref="ModAnimStateMachine" /> factory for any combat creature model (player
    ///     characters, monsters, or any other <see cref="AbstractModel" />) whose visuals are driven without a
    ///     Spine skeleton. Implementers bind a state machine to the supplied visuals root using any
    ///     <see cref="IAnimationBackend" />-backed driver (cue frame sequences, cue textures, Godot
    ///     <see cref="AnimationPlayer" />, <see cref="AnimatedSprite2D" />, or a
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends.CompositeAnimationBackend" />).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Typical implementers subclass
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />
    ///         or <see cref="ModMonsterTemplate" />, but the templates are convenience. The contract is opt-in via the
    ///         interface itself: any model type implementing this interface is routed through
    ///         <see cref="STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureNonSpineAnimationPlaybackPatch" /> —
    ///         template subclassing is <b>not</b> required.
    ///     </para>
    ///     <para>
    ///         <see cref="ModAnimStateMachine.SetTrigger" /> receives the same trigger names that vanilla would
    ///         dispatch to a Spine animator (<c>Idle</c>, <c>Attack</c>, <c>Cast</c>, <c>Hit</c>, <c>Dead</c>,
    ///         <c>Revive</c>, …).
    ///     </para>
    /// </remarks>
    public interface IModNonSpineAnimationStateMachineFactory
    {
        /// <summary>
        ///     Builds a state machine bound to <paramref name="visualsRoot" />, or <see langword="null" /> to defer
        ///     to the single-shot cue playback path. Called at most once per combat visuals lifetime (cached by the
        ///     routing patch via a <see cref="ConditionalWeakTable{TKey,TValue}" /> keyed on <paramref name="visualsRoot" />).
        /// </summary>
        /// <param name="visualsRoot">Combat visuals root (typically an <see cref="NCreatureVisuals" />).</param>
        ModAnimStateMachine? TryCreateNonSpineAnimationStateMachine(Node visualsRoot);
    }

    /// <summary>
    ///     Runtime <see cref="ModAnimStateMachine" /> factory for mod characters in merchant / rest-site contexts.
    ///     Implement when the merchant visuals need state transitions rather than single-shot playback.
    /// </summary>
    public interface IModCharacterMerchantAnimationStateMachineFactory
    {
        /// <summary>
        ///     Builds a merchant-context state machine, or <see langword="null" /> to defer to the single-shot path.
        /// </summary>
        /// <param name="merchantRoot">Merchant character root.</param>
        /// <param name="character">Owning character model for cue lookup.</param>
        ModAnimStateMachine? TryCreateMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character);
    }
}
