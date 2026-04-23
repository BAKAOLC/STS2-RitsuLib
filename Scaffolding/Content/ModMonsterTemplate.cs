using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Base <see cref="MonsterModel" /> for mods: <see cref="IModMonsterAssetOverrides" /> supplies the visuals scene
    ///     path; override <see cref="TryCreateCreatureVisuals" /> to build <see cref="NCreatureVisuals" /> in code instead.
    ///     Use <see cref="RitsuGodotNodeFactories" /> for explicit <c>CreateFromResource</c> / <c>CreateFromScenePath</c>
    ///     construction. Register with <c>ModContentRegistry.RegisterMonster&lt;T&gt;()</c> or <c>Monster&lt;T&gt;()</c> on
    ///     the pack builder.
    /// </summary>
    /// <remarks>
    ///     When the monster's visuals have no Spine skeleton, override
    ///     <see cref="SetupCustomNonSpineAnimationStateMachine" /> to drive the creature with a
    ///     <see cref="ModAnimStateMachine" /> (the same state-machine pipeline used by
    ///     <see cref="STS2RitsuLib.Scaffolding.Characters.ModCharacterTemplate{TCardPool,TRelicPool,TPotionPool}" />).
    /// </remarks>
#pragma warning disable CS0618
    // Template keeps the obsolete IModMonsterCreatureVisualsFactory wired so existing derived classes and external
    // consumers that type-check against the old interface name continue to work.
    public abstract class ModMonsterTemplate : MonsterModel, IModMonsterAssetOverrides,
        IModCreatureVisualsFactory, IModMonsterCreatureVisualsFactory, IModCreatureAnimatorFactory,
        IModNonSpineAnimationStateMachineFactory
#pragma warning restore CS0618
    {
        CreatureAnimator? IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
        {
            return SetupCustomCreatureAnimator(controller);
        }

        NCreatureVisuals? IModCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }

        /// <inheritdoc />
        public virtual MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

        /// <inheritdoc />
        public virtual string? CustomVisualsPath => AssetProfile.VisualsScenePath;

#pragma warning disable CS0618
        NCreatureVisuals? IModMonsterCreatureVisualsFactory.TryCreateCreatureVisuals()
        {
            return TryCreateCreatureVisuals();
        }
#pragma warning restore CS0618

        ModAnimStateMachine? IModNonSpineAnimationStateMachineFactory.
            TryCreateNonSpineAnimationStateMachine(Node visualsRoot)
        {
            return SetupCustomNonSpineAnimationStateMachine(visualsRoot, this);
        }

        /// <summary>
        ///     Non-null value becomes combat visuals; otherwise paths (<see cref="CustomVisualsPath" /> / vanilla) apply.
        /// </summary>
        protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a fully wired Spine <see cref="CreatureAnimator" /> (state graph for idle /
        ///     hit / attack / cast / die / relaxed). Return <see langword="null" /> to defer to vanilla
        ///     <see cref="MonsterModel.GenerateAnimator" />. Prefer <see cref="ModAnimStateMachines.Standard" /> to
        ///     match baselib semantics.
        /// </summary>
        /// <param name="controller">Spine controller attached to the monster's combat visuals.</param>
        protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
        {
            return null;
        }

        /// <summary>
        ///     Optional override producing a non-Spine <see cref="ModAnimStateMachine" /> for the monster's combat
        ///     visuals (cue frame sequences, Godot animation player, animated sprite). Return
        ///     <see langword="null" /> to defer to the vanilla single-shot playback path.
        /// </summary>
        /// <param name="visualsRoot">Combat visuals root node.</param>
        /// <param name="monster">Monster model (always <see langword="this" />, exposed for convenience).</param>
        protected virtual ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(Node visualsRoot,
            MonsterModel monster)
        {
            return null;
        }
    }
}
