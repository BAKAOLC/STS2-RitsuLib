using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Runtime <see cref="NCreatureVisuals" /> for mod monsters. Subclass <see cref="ModMonsterTemplate" /> or implement
    ///     on the model type; non-null <see cref="TryCreateCreatureVisuals" /> replaces path-based
    ///     <c>CreateVisuals</c>.
    /// </summary>
    public interface IModMonsterCreatureVisualsFactory
    {
        /// <summary>
        ///     Combat visuals from code, or <c>null</c> to use asset paths.
        /// </summary>
        NCreatureVisuals? TryCreateCreatureVisuals();
    }

    /// <summary>
    ///     Runtime <see cref="NCreatureVisuals" /> for mod characters; same contract as
    ///     <see cref="IModMonsterCreatureVisualsFactory" />.
    /// </summary>
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
}
