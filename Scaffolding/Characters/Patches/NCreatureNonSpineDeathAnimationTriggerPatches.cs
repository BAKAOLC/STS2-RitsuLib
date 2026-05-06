using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Fires the <c>Dead</c> animation trigger for RitsuLib-managed creatures without a Spine animator after
    ///     <see cref="NCreature.StartDeathAnim" /> runs. Vanilla gates the entire trigger dispatch (including death
    ///     SFX) behind <c>_spineAnimator != null</c>, so mod creatures using <c>AnimatedSprite2D</c>, Godot
    ///     <c>AnimationPlayer</c>, or cue-frame-sequence backends never receive the trigger — the most visible symptom
    ///     for players is that the death animation does not play when the run is abandoned or the player dies in
    ///     combat.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Scope:</b> the postfix only fires when all of the following hold, so foreign creatures that do not
    ///         opt into the RitsuLib visuals pipeline are untouched:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>the creature has no Spine animator;</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     the creature's model (either <c>Entity.Player?.Character</c> or <c>Entity.Monster</c>)
    ///                     opts into RitsuLib visuals by implementing
    ///                     <see cref="IModCreatureCombatAnimationStateMachineFactory" /> (or the legacy
    ///                     <see cref="IModNonSpineAnimationStateMachineFactory" />), or — for players only —
    ///                     <see cref="IModCharacterAssetOverrides" /> (which pulls the cue-playback path).
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When all guards pass, the patch calls <see cref="NCreature.SetAnimationTrigger" />, which
    ///         <see cref="ModCreatureCombatAnimationPlaybackPatch" /> routes through the model's
    ///         <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" /> (when registered)
    ///         or the legacy cue playback
    ///         (<see cref="STS2RitsuLib.Scaffolding.Characters.Visuals.ModCreatureVisualPlayback" />).
    ///     </para>
    ///     <para>
    ///         This patch does not attempt to backfill the death-animation length returned from
    ///         <see cref="NCreature.StartDeathAnim" /> — vanilla already returns <c>0f</c> for non-Spine creatures
    ///         unless a monster sets <see cref="MonsterModel.DeathAnimLengthOverride" />.
    ///     </para>
    /// </remarks>
    public class NCreatureNonSpineDeathAnimationTriggerPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ncreature_non_spine_death_animation_trigger";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Dispatch the Dead animation trigger for RitsuLib-managed non-Spine creatures so StartDeathAnim animates correctly";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.StartDeathAnim))];
        }

        /// <summary>
        ///     Dispatches <c>Dead</c> through <see cref="NCreature.SetAnimationTrigger" /> for RitsuLib-managed
        ///     non-Spine creatures only; returns silently otherwise.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Postfix(NCreature __instance)
        {
            if (!CombatAnimationStateMachineTriggerScope.AppliesToDeathPostfix(__instance))
                return;

            __instance.SetAnimationTrigger("Dead");
        }
    }

    /// <summary>
    ///     Fires the <c>Revive</c> animation trigger for RitsuLib-managed creatures after
    ///     <see cref="NCreature.StartReviveAnim" /> when vanilla would not dispatch it. Vanilla only dispatches the
    ///     trigger when a Spine animator exists and <see cref="CreatureAnimator.HasTrigger" /> reports
    ///     <c>Revive</c>; otherwise it falls back to <c>AnimTempRevive</c> (a fade-out / fade-in tween on the visuals
    ///     root), which silently swallows any <c>Revive</c> state the mod creature registered on a
    ///     <see cref="STS2RitsuLib.Scaffolding.Visuals.StateMachine.ModAnimStateMachine" /> without a matching
    ///     <c>Revive</c> branch on the vanilla <see cref="CreatureAnimator" />.
    /// </summary>
    /// <remarks>
    ///     Scope mirrors <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" /> for non-Spine creatures. For
    ///     Spine-backed creatures with a combat state machine that declares <c>Revive</c>, the postfix may dispatch
    ///     <c>Revive</c> when the vanilla animator does not expose that trigger (see interface remarks on keeping both
    ///     in sync). The vanilla fade tween still runs alongside the triggered animation when <c>AnimTempRevive</c>
    ///     also ran; mods that want a clean revive animation should treat the brief fade as expected behaviour.
    /// </remarks>
    public class NCreatureNonSpineReviveAnimationTriggerPatch : IPatchMethod
    {
        private static readonly AccessTools.FieldRef<NCreature, CreatureAnimator?> SpineAnimatorRef =
            AccessTools.FieldRefAccess<NCreature, CreatureAnimator?>("_spineAnimator");

        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ncreature_non_spine_revive_animation_trigger";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Dispatch the Revive animation trigger for RitsuLib-managed creatures so StartReviveAnim animates correctly";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.StartReviveAnim))];
        }

        /// <summary>
        ///     Dispatches <c>Revive</c> through <see cref="NCreature.SetAnimationTrigger" /> when in scope; skips when
        ///     vanilla already dispatched <c>Revive</c> on the Spine <see cref="CreatureAnimator" />.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Postfix(NCreature __instance)
        {
            if (!CombatAnimationStateMachineTriggerScope.AppliesToRevivePostfix(__instance))
                return;

            if (__instance.HasSpineAnimation)
            {
                var animator = SpineAnimatorRef(__instance);
                if (animator != null && animator.HasTrigger("Revive"))
                    return;
            }

            __instance.SetAnimationTrigger("Revive");
        }
    }

    /// <summary>
    ///     Shared gate used by combat animation lifecycle postfixes so scope stays consistent across
    ///     <see cref="NCreature.StartDeathAnim" /> / <see cref="NCreature.StartReviveAnim" />.
    /// </summary>
    internal static class CombatAnimationStateMachineTriggerScope
    {
        public static bool AppliesToDeathPostfix(NCreature creature)
        {
            return !creature.HasSpineAnimation && AppliesToRitsuLibVisuals(creature);
        }

        public static bool AppliesToRevivePostfix(NCreature creature)
        {
            if (!creature.HasSpineAnimation)
                return AppliesToRitsuLibVisuals(creature);

            var sm = ModCreatureCombatAnimationPlaybackPatch.TryGetCombatAnimationStateMachine(creature);
            return sm != null && sm.HasTrigger("Revive");
        }

        private static bool AppliesToRitsuLibVisuals(NCreature creature)
        {
            var entity = creature.Entity;
            if (entity == null)
                return false;

            var character = entity.Player?.Character;
            var monster = entity.Monster;

            switch (character)
            {
                case IModCreatureCombatAnimationStateMachineFactory:
#pragma warning disable CS0618
                case IModNonSpineAnimationStateMachineFactory:
#pragma warning restore CS0618
                    return true;
            }

            switch (monster)
            {
                case IModCreatureCombatAnimationStateMachineFactory:
#pragma warning disable CS0618
                case IModNonSpineAnimationStateMachineFactory:
#pragma warning restore CS0618
                    return true;
            }

            return character is IModCharacterAssetOverrides;
        }
    }
}
