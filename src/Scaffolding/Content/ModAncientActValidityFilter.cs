using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Applies <see cref="IModAncientActValidity" /> when building per-act ancient candidate pools.
    /// </summary>
    public static class ModAncientActValidityFilter
    {
        /// <summary>
        ///     Returns whether <paramref name="ancient" /> may appear for <paramref name="act" />.
        ///     Ancients that do not implement <see cref="IModAncientActValidity" /> are always allowed.
        /// </summary>
        public static bool IsValidForAct(ActModel act, AncientEventModel ancient)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(ancient);

            return ancient is not IModAncientActValidity validity || validity.IsValidForAct(act);
        }

        /// <summary>
        ///     Keeps only ancients that pass <see cref="IsValidForAct" /> for <paramref name="act" />.
        /// </summary>
        public static IEnumerable<AncientEventModel> FilterForAct(
            ActModel act,
            IEnumerable<AncientEventModel> ancients)
        {
            ArgumentNullException.ThrowIfNull(act);
            ArgumentNullException.ThrowIfNull(ancients);

            return ancients.Where(ancient => IsValidForAct(act, ancient));
        }
    }
}
