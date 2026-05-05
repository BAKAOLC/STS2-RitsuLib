using System.Reflection;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.ActSequence.Patches
{
    /// <summary>
    ///     Applies act-sequence rules during new-run setup, before the first <see cref="RunManager.GenerateRooms" /> call.
    ///     Must be strict no-op when no rules are registered.
    /// </summary>
    public sealed class ActSequenceRunSetupPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "act_sequence_run_setup";

        /// <inheritdoc />
        public static string Description => "Apply act-sequence rules during new-run setup (before GenerateRooms)";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer),
                    [typeof(RunState), typeof(bool), typeof(DateTimeOffset?)]),
                new(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Harmony prefix: applies any matching <see cref="ActSequenceTrigger.OnRunSetupBeforeGenerateRooms" /> rules.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static void Prefix(MethodBase __originalMethod, RunManager __instance, RunState state)
        {
            if (!ModActSequenceRegistry.HasAnyRegistration)
                return;

            var isMultiplayer = __originalMethod.Name == nameof(RunManager.SetUpNewMultiPlayer);

            ModActSequenceRegistry.TryApplyRules(
                __instance,
                state,
                ActSequenceTrigger.OnRunSetupBeforeGenerateRooms,
                0,
                isMultiplayer
            );
        }
    }
}
