using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Visuals.StateMachine
{
    /// <summary>
    ///     Top-level convenience factories for animation state machines. Mirrors the semantics of
    ///     baselib's <c>CustomCharacterModel.SetupAnimationState</c> but usable against any
    ///     <see cref="IAnimationBackend" /> (Spine, Godot animation player / animated sprite, cue frame sequences).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Standard" /> produces a vanilla <see cref="CreatureAnimator" /> so callers can return it
    ///         directly from <c>CharacterModel.GenerateAnimator</c>; this is the closest drop-in replacement for the
    ///         baselib helper.
    ///     </para>
    ///     <para>
    ///         <see cref="StandardCue" /> produces a backend-agnostic <see cref="ModAnimStateMachine" /> for
    ///         non-Spine visuals rooted at a <see cref="Node" /> (cue frame sequences, Godot animation player,
    ///         animated sprite).
    ///     </para>
    ///     <para>
    ///         Terminal states (<c>Dead</c>) leave <see cref="ModAnimState.NextState" /> / <c>AnimState.NextState</c>
    ///         unset so completion does not auto-return to idle, matching the vanilla behaviour.
    ///     </para>
    /// </remarks>
    public static class ModAnimStateMachines
    {
        /// <summary>
        ///     Builds a vanilla Spine <see cref="CreatureAnimator" /> matching the standard idle / dead / hit /
        ///     attack / cast / relaxed shape. Null animation names collapse to the idle state (vanilla behaviour).
        /// </summary>
        public static CreatureAnimator Standard(MegaSprite controller,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true)
        {
            ArgumentNullException.ThrowIfNull(controller);

            var idle = new AnimState(idleName, true);
            var dead = deadName == null ? idle : new(deadName, deadLoop);
            var hit = hitName == null
                ? idle
                : new(hitName, hitLoop) { NextState = idle };
            var attack = attackName == null
                ? idle
                : new(attackName, attackLoop) { NextState = idle };
            var cast = castName == null
                ? idle
                : new(castName, castLoop) { NextState = idle };

            AnimState relaxed;
            if (relaxedName == null)
            {
                relaxed = idle;
            }
            else
            {
                relaxed = new(relaxedName, relaxedLoop);
                relaxed.AddBranch("Idle", idle);
            }

            var animator = new CreatureAnimator(idle, controller);
            animator.AddAnyState("Idle", idle);
            animator.AddAnyState("Dead", dead);
            animator.AddAnyState("Hit", hit);
            animator.AddAnyState("Attack", attack);
            animator.AddAnyState("Cast", cast);
            animator.AddAnyState("Relaxed", relaxed);
            return animator;
        }

        /// <summary>
        ///     Builds a non-Spine <see cref="ModAnimStateMachine" /> over <paramref name="visualsRoot" /> matching
        ///     the standard idle / dead / hit / attack / cast / relaxed shape; null names fall back to idle.
        /// </summary>
        /// <param name="visualsRoot">Visuals root used by <see cref="CompositeBackendFactory" />.</param>
        /// <param name="character">Optional character model used to discover cue sets.</param>
        /// <param name="idleName">Idle animation id (always required; looping).</param>
        /// <param name="deadName">Optional die animation id; <see langword="null" /> falls back to idle.</param>
        /// <param name="deadLoop">Loop flag for the die animation.</param>
        /// <param name="hitName">Optional hit animation id; <see langword="null" /> falls back to idle.</param>
        /// <param name="hitLoop">Loop flag for the hit animation.</param>
        /// <param name="attackName">Optional attack animation id; <see langword="null" /> falls back to idle.</param>
        /// <param name="attackLoop">Loop flag for the attack animation.</param>
        /// <param name="castName">Optional cast animation id; <see langword="null" /> falls back to idle.</param>
        /// <param name="castLoop">Loop flag for the cast animation.</param>
        /// <param name="relaxedName">Optional relaxed animation id; <see langword="null" /> falls back to idle.</param>
        /// <param name="relaxedLoop">Loop flag for the relaxed animation.</param>
        /// <param name="cueSet">Optional explicit cue set, overriding the character-derived one.</param>
        public static ModAnimStateMachine StandardCue(Node visualsRoot, CharacterModel? character,
            string idleName,
            string? deadName = null, bool deadLoop = false,
            string? hitName = null, bool hitLoop = false,
            string? attackName = null, bool attackLoop = false,
            string? castName = null, bool castLoop = false,
            string? relaxedName = null, bool relaxedLoop = true,
            VisualCueSet? cueSet = null)
        {
            ArgumentNullException.ThrowIfNull(visualsRoot);
            ArgumentException.ThrowIfNullOrWhiteSpace(idleName);

            var builder = ModAnimStateMachineBuilder.Create()
                .AddState(idleName, true).AsInitial().Done();

            AddOptional(builder, deadName, deadLoop, idleName, false);
            AddOptional(builder, hitName, hitLoop, idleName, true);
            AddOptional(builder, attackName, attackLoop, idleName, true);
            AddOptional(builder, castName, castLoop, idleName, true);

            var relaxedTarget = idleName;
            if (relaxedName != null && !string.Equals(relaxedName, idleName, StringComparison.Ordinal))
            {
                builder.AddState(relaxedName, relaxedLoop).Done();
                builder.AddBranch(relaxedName, "Idle", idleName);
                relaxedTarget = relaxedName;
            }

            builder.AddAnyState("Idle", idleName);
            builder.AddAnyState("Dead", deadName ?? idleName);
            builder.AddAnyState("Hit", hitName ?? idleName);
            builder.AddAnyState("Attack", attackName ?? idleName);
            builder.AddAnyState("Cast", castName ?? idleName);
            builder.AddAnyState("Relaxed", relaxedTarget);

            return builder.BuildForVisualsRoot(visualsRoot, character, cueSet);
        }

        private static void AddOptional(ModAnimStateMachineBuilder builder, string? name, bool loop, string idleName,
            bool hasNext)
        {
            if (name == null || string.Equals(name, idleName, StringComparison.Ordinal))
                return;

            var scope = builder.AddState(name, loop);
            if (hasNext)
                scope.WithNext(idleName);
        }
    }
}
