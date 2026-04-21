using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Fires the <c>Dead</c> animation trigger for RitsuLib-managed non-Spine creatures (player characters and
    ///     monsters) after <see cref="NCreature.StartDeathAnim" /> runs. Vanilla gates the entire trigger dispatch
    ///     (including death SFX) behind <c>_spineAnimator != null</c>, so mod creatures using
    ///     <c>AnimatedSprite2D</c>, Godot <c>AnimationPlayer</c>, or cue-frame-sequence backends never receive the
    ///     trigger — the most visible symptom for players is that the death animation does not play when the run
    ///     is abandoned or the player dies in combat.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Scope:</b> the postfix only fires when all of the following hold, so foreign creatures that do
    ///         not opt into the RitsuLib visuals pipeline are untouched:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>the creature has no Spine animator;</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     the creature's model (either <c>Entity.Player?.Character</c> or <c>Entity.Monster</c>)
    ///                     opts into RitsuLib visuals by implementing
    ///                     <see cref="IModNonSpineAnimationStateMachineFactory" />, or — for players only —
    ///                     <see cref="IModCharacterAssetOverrides" /> (which pulls the cue-playback path).
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         When all guards pass, the patch calls <see cref="NCreature.SetAnimationTrigger" />, which
    ///         <see cref="ModCreatureNonSpineAnimationPlaybackPatch" /> routes through the model's
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

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Dispatches <c>Dead</c> through <see cref="NCreature.SetAnimationTrigger" /> for RitsuLib-managed
        ///     non-Spine creatures only; returns silently otherwise.
        /// </summary>
        public static void Postfix(NCreature __instance)
        {
            if (!NonSpineAnimationTriggerScope.AppliesTo(__instance))
                return;

            __instance.SetAnimationTrigger("Dead");
        }
    }

    /// <summary>
    ///     Fires the <c>Revive</c> animation trigger for RitsuLib-managed non-Spine creatures after
    ///     <see cref="NCreature.StartReviveAnim" /> runs. Vanilla only dispatches the trigger when a Spine
    ///     animator exists and otherwise falls back to <c>AnimTempRevive</c> (a fade-out / fade-in tween on the
    ///     visuals root), which silently swallows any <c>Revive</c> state the mod creature registered.
    /// </summary>
    /// <remarks>
    ///     Scope mirrors <see cref="NCreatureNonSpineDeathAnimationTriggerPatch" /> — only RitsuLib-managed
    ///     non-Spine creatures are affected. The vanilla fade tween still runs alongside the triggered
    ///     animation; mods that want a clean revive animation should treat the brief fade as expected behaviour.
    /// </remarks>
    public class NCreatureNonSpineReviveAnimationTriggerPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ncreature_non_spine_revive_animation_trigger";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Dispatch the Revive animation trigger for RitsuLib-managed non-Spine creatures so StartReviveAnim animates correctly";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.StartReviveAnim))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Dispatches <c>Revive</c> through <see cref="NCreature.SetAnimationTrigger" /> for RitsuLib-managed
        ///     non-Spine creatures only; returns silently otherwise.
        /// </summary>
        public static void Postfix(NCreature __instance)
        {
            if (!NonSpineAnimationTriggerScope.AppliesTo(__instance))
                return;

            __instance.SetAnimationTrigger("Revive");
        }
    }

    /// <summary>
    ///     Shared gate used by the non-Spine lifecycle triggers so scope remains consistent across
    ///     <see cref="NCreature.StartDeathAnim" /> / <see cref="NCreature.StartReviveAnim" /> postfixes.
    /// </summary>
    internal static class NonSpineAnimationTriggerScope
    {
        /// <summary>
        ///     Returns <see langword="true" /> only for non-Spine creatures whose owning model opted into
        ///     RitsuLib visuals:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Any <see cref="AbstractModel" /> implementing
        ///                 <see cref="IModNonSpineAnimationStateMachineFactory" /> (players or monsters).
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 A <see cref="CharacterModel" /> implementing <see cref="IModCharacterAssetOverrides" />
        ///                 (cue-playback fallback, player-only).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        public static bool AppliesTo(NCreature creature)
        {
            if (creature.HasSpineAnimation)
                return false;

            var entity = creature.Entity;
            if (entity == null)
                return false;

            var character = entity.Player?.Character;
            var monster = entity.Monster;

            if (character is IModNonSpineAnimationStateMachineFactory)
                return true;

            if (monster is IModNonSpineAnimationStateMachineFactory)
                return true;

            return character is IModCharacterAssetOverrides;
        }
    }
}
