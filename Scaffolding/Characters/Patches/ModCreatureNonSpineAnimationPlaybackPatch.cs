using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     When a creature has no Spine animator, routes <see cref="NCreature.SetAnimationTrigger" /> through
    ///     <see cref="ModCreatureVisualPlayback" /> (cue textures, Godot animators). Co-loading another library that
    ///     patches the same method may run both prefixes; prefer a single stack for creature visuals when possible.
    /// </summary>
    public class ModCreatureNonSpineAnimationPlaybackPatch : IPatchMethod
    {
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

            return !ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger(__instance, trigger);
        }
    }
}
