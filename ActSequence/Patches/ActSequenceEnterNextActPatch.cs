using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.ActSequence.Patches
{
    /// <summary>
    ///     Applies act-sequence append rules when advancing to the next act.
    /// </summary>
    public sealed class ActSequenceEnterNextActPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "act_sequence_enter_next_act";

        /// <inheritdoc />
        public static string Description => "Apply act-sequence rules on RunManager.EnterNextAct";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(RunManager), nameof(RunManager.EnterNextAct)),
            ];
        }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Harmony prefix: applies any matching <see cref="ActSequenceTrigger.OnEnterNextAct" /> rules.
        /// </summary>
        public static void Prefix(RunManager __instance)
        {
            if (!ModActSequenceRegistry.HasAnyRegistration)
                return;

            var state = __instance.State;
            if (state == null)
                return;

            var isMultiplayer = __instance.NetService != null && __instance.NetService.Type != NetGameType.Singleplayer;
            ModActSequenceRegistry.TryApplyRules(
                __instance,
                state,
                ActSequenceTrigger.OnEnterNextAct,
                state.CurrentActIndex,
                isMultiplayer
            );
        }
    }
}
