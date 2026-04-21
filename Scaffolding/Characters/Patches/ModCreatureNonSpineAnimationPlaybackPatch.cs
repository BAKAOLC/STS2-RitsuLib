using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     When a creature has no Spine animator, routes <see cref="NCreature.SetAnimationTrigger" /> through
    ///     <see cref="ModCreatureVisualPlayback" /> (cue textures, Godot animators). Co-loading another library that
    ///     patches the same method may run both prefixes; prefer a single stack for creature visuals when possible.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Routing for non-Spine creatures: if the creature's model (either
    ///         <c>Entity.Player?.Character</c> or <c>Entity.Monster</c>) implements
    ///         <see cref="IModNonSpineAnimationStateMachineFactory" />, the trigger is dispatched through the
    ///         state machine's <see cref="ModAnimStateMachine.SetTrigger" /> so branches, any-state transitions
    ///         and <see cref="ModAnimState.NextState" /> apply. Otherwise the single-shot cue playback path
    ///         (<see cref="ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger" />) runs.
    ///     </para>
    ///     <para>
    ///         State machines are cached per visuals root via a
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" /> so factories run at most once per combat lifetime.
    ///     </para>
    /// </remarks>
    public class ModCreatureNonSpineAnimationPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<Node, StateMachineSlot> StateMachinesByVisuals = new();

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "mod_creature_non_spine_animation_playback";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Play non-Spine combat creature cues via ModCreatureVisualPlayback for SetAnimationTrigger";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Returns <see langword="false" /> when playback handled the trigger (skip vanilla no-op / Spine path).
        /// </summary>
        public static bool Prefix(NCreature __instance, string trigger)
        {
            if (__instance.HasSpineAnimation)
                return true;

            if (TryRouteToStateMachine(__instance, trigger))
                return false;

            return !ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger(__instance, trigger);
        }

        private static bool TryRouteToStateMachine(NCreature creature, string trigger)
        {
            var visuals = creature.Visuals;
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
                return false;

            var entity = creature.Entity;
            if (entity == null)
                return false;

            var slot = StateMachinesByVisuals.GetValue(visuals, _ => new());
            slot.EnsureBuilt(entity.Player?.Character, entity.Monster, visuals);

            if (slot.StateMachine == null)
                return false;

            slot.StateMachine.SetTrigger(trigger);
            return true;
        }

        private sealed class StateMachineSlot
        {
            private bool _built;
            public ModAnimStateMachine? StateMachine { get; private set; }

            /// <summary>
            ///     Resolves the first <see cref="IModNonSpineAnimationStateMachineFactory" /> opt-in across
            ///     <paramref name="character" /> and <paramref name="monster" /> and caches the resulting state
            ///     machine.
            /// </summary>
            public void EnsureBuilt(CharacterModel? character, MonsterModel? monster, Node visuals)
            {
                if (_built)
                    return;

                _built = true;
                StateMachine = BuildFrom(character, monster, visuals);
            }

            private static ModAnimStateMachine? BuildFrom(CharacterModel? character, MonsterModel? monster,
                Node visuals)
            {
                if (character is IModNonSpineAnimationStateMachineFactory characterFactory)
                {
                    var built = characterFactory.TryCreateNonSpineAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }

                if (monster is IModNonSpineAnimationStateMachineFactory monsterFactory)
                    return monsterFactory.TryCreateNonSpineAnimationStateMachine(visuals);

                return null;
            }
        }
    }
}
