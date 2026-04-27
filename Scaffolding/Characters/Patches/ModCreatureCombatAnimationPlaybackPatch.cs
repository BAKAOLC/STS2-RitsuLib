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
    ///     Routes <see cref="NCreature.SetAnimationTrigger" /> into a <see cref="ModAnimStateMachine" /> when the
    ///     creature's model opts in via <see cref="IModCreatureCombatAnimationStateMachineFactory" /> (or the legacy
    ///     <see cref="IModNonSpineAnimationStateMachineFactory" />), including Spine-backed visuals when the factory
    ///     returns a non-null machine (for example via <see cref="ModAnimStateMachineBuilder.BuildSpine" />). When no
    ///     state machine is registered and the creature has no Spine animator, falls back to
    ///     <see cref="ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         State machines are cached per visuals root via a
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" /> so factories run at most once per combat lifetime.
    ///     </para>
    /// </remarks>
    public class ModCreatureCombatAnimationPlaybackPatch : IPatchMethod
    {
        private static readonly ConditionalWeakTable<Node, StateMachineSlot> StateMachinesByVisuals = new();

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "mod_creature_combat_animation_playback";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Route NCreature.SetAnimationTrigger through ModAnimStateMachine when opted in (Spine or non-Spine); "
            + "otherwise cue playback for non-Spine";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))];
        }

        /// <summary>
        ///     Returns the cached combat <see cref="ModAnimStateMachine" /> for <paramref name="creature" /> when the
        ///     owning model's factory produced one; otherwise <see langword="null" />.
        /// </summary>
        internal static ModAnimStateMachine? TryGetCombatAnimationStateMachine(NCreature creature)
        {
            var visuals = creature.Visuals;
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
                return null;

            var entity = creature.Entity;
            if (entity == null)
                return null;

            var slot = StateMachinesByVisuals.GetValue(visuals, _ => new());
            slot.EnsureBuilt(entity.Player?.Character, entity.Monster, visuals);
            return slot.StateMachine;
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Returns <see langword="false" /> when the trigger was consumed (skip vanilla
        ///     <see cref="NCreature.SetAnimationTrigger" /> body).
        /// </summary>
        public static bool Prefix(NCreature __instance, string trigger)
        {
            if (TryRouteToStateMachine(__instance, trigger))
                return false;

            if (__instance.HasSpineAnimation)
                return true;

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
                if (character is IModCreatureCombatAnimationStateMachineFactory combatCharacter)
                {
                    var built = combatCharacter.TryCreateCombatAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }

#pragma warning disable CS0618
                if (character is IModNonSpineAnimationStateMachineFactory legacyCharacter)
                {
                    var built = legacyCharacter.TryCreateNonSpineAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }
#pragma warning restore CS0618

                if (monster is IModCreatureCombatAnimationStateMachineFactory combatMonster)
                {
                    var built = combatMonster.TryCreateCombatAnimationStateMachine(visuals);
                    if (built != null)
                        return built;
                }

#pragma warning disable CS0618
                if (monster is IModNonSpineAnimationStateMachineFactory legacyMonster)
                    return legacyMonster.TryCreateNonSpineAnimationStateMachine(visuals);
#pragma warning restore CS0618

                return null;
            }
        }
    }
}
