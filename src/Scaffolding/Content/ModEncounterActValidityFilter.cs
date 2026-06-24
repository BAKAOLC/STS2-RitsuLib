using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Applies <see cref="IModEncounterActValidity" /> when building per-act encounter candidate pools.
    /// </summary>
    public static class ModEncounterActValidityFilter
    {
        /// <summary>
        ///     Returns whether <paramref name="encounter" /> may appear for <paramref name="act" />.
        ///     Encounters that do not implement <see cref="IModEncounterActValidity" /> are always allowed.
        /// </summary>
        public static bool IsValidForAct(ActModel act, EncounterModel encounter)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(encounter);

            return encounter is not IModEncounterActValidity validity || validity.IsValidForAct(act);
        }

        /// <summary>
        ///     Keeps only encounters that pass <see cref="IsValidForAct" /> for <paramref name="act" />.
        /// </summary>
        public static IEnumerable<EncounterModel> FilterForAct(
            ActModel act,
            IEnumerable<EncounterModel> encounters)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(encounters);

            return encounters.Where(encounter => IsValidForAct(act, encounter));
        }
    }
}
