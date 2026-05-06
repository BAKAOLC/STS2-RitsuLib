using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Scaffolding.Characters.Patches
{
    /// <summary>
    ///     Eagerly builds combat animation state machines once combat visuals are ready for opted-in creature models.
    /// </summary>
    public class NCreatureCombatAnimationInitialBootstrapPatch : IPatchMethod
    {
        /// <inheritdoc cref="IPatchMethod.PatchId" />
        public static string PatchId => "ncreature_combat_animation_initial_bootstrap";

        /// <inheritdoc cref="IPatchMethod.Description" />
        public static string Description =>
            "Build combat animation state machine at NCreature._Ready for opted-in models";

        /// <inheritdoc cref="IPatchMethod.IsCritical" />
        public static bool IsCritical => false;

        /// <inheritdoc cref="IPatchMethod.GetTargets" />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature._Ready))];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Builds the state machine early so initialization and initial-state playback happen as part of visuals
        ///     readiness instead of waiting for the first trigger.
        /// </summary>
        public static void Postfix(NCreature __instance)
        {
            if (!HasCombatStateMachineFactory(__instance))
                return;

            _ = ModCreatureCombatAnimationPlaybackPatch.TryGetCombatAnimationStateMachine(__instance);
        }

        private static bool HasCombatStateMachineFactory(NCreature creature)
        {
            var entity = creature.Entity;
            if (entity == null)
                return false;

            var character = entity.Player?.Character;
            if (character is IModCreatureCombatAnimationStateMachineFactory
#pragma warning disable CS0618
                or IModNonSpineAnimationStateMachineFactory
#pragma warning restore CS0618
               )
                return true;

            var monster = entity.Monster;
            return monster is IModCreatureCombatAnimationStateMachineFactory
#pragma warning disable CS0618
                or IModNonSpineAnimationStateMachineFactory;
#pragma warning restore CS0618
        }
    }
}
