using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Repairs saved boss selections that were generated before
    ///     <see cref="IModEncounterActValidity.IsValidForAct" /> rules changed.
    /// </summary>
    internal class ModEncounterActValidityPatch : IPatchMethod
    {
        public static string PatchId => "mod_encounter_act_validity";

        public static string Description =>
            "Validate saved boss encounters against IModEncounterActValidity after run load";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad), [typeof(Rng)]),
            ];
        }

        public static void Postfix(ActModel __instance, Rng rng)
        {
            ArgumentNullException.ThrowIfNull(__instance);
            ArgumentNullException.ThrowIfNull(rng);

            RepairBoss(__instance, rng);
            RepairSecondBoss(__instance, rng);
        }

        private static void RepairBoss(ActModel act, Rng rng)
        {
            if (ModEncounterActValidityFilter.IsValidForAct(act, act.BossEncounter))
                return;

            var candidates = GetValidBossCandidates(act)
                .Where(encounter => encounter.Id != act.SecondBossEncounter?.Id)
                .ToArray();

            if (candidates.Length == 0)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Content] Saved boss '{act.BossEncounter.Id}' is invalid for act '{act.Id}', but no valid replacement boss was available.");
                return;
            }

            var replacement = rng.NextItem(candidates)!;
            RitsuLibFramework.Logger.Info(
                $"[Content] Replaced invalid saved boss '{act.BossEncounter.Id}' with '{replacement.Id}' for act '{act.Id}'.");
            act.SetBossEncounter(replacement);
        }

        private static void RepairSecondBoss(ActModel act, Rng rng)
        {
            if (act.SecondBossEncounter == null ||
                ModEncounterActValidityFilter.IsValidForAct(act, act.SecondBossEncounter))
                return;

            var candidates = GetValidBossCandidates(act)
                .Where(encounter => encounter.Id != act.BossEncounter.Id)
                .ToArray();

            if (candidates.Length == 0)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Content] Saved second boss '{act.SecondBossEncounter.Id}' is invalid for act '{act.Id}', but no valid replacement boss was available.");
                return;
            }

            var replacement = rng.NextItem(candidates)!;
            RitsuLibFramework.Logger.Info(
                $"[Content] Replaced invalid saved second boss '{act.SecondBossEncounter.Id}' with '{replacement.Id}' for act '{act.Id}'.");
            act.SetSecondBossEncounter(replacement);
        }

        private static EncounterModel[] GetValidBossCandidates(ActModel act)
        {
            return act.AllBossEncounters
                .Where(encounter => ModEncounterActValidityFilter.IsValidForAct(act, encounter))
                .ToArray();
        }
    }
}
