using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Optional gate for whether a mod <see cref="EncounterModel" /> may enter an act's encounter pool for a given
    ///     <see cref="ActModel" /> during room generation. Implement on
    ///     <see cref="ModEncounterTemplate" /> or your encounter type; the framework applies it in
    ///     <see cref="ModEncounterActValidityFilter" />.
    /// </summary>
    public interface IModEncounterActValidity
    {
        /// <summary>
        ///     When false, this encounter is excluded from <see cref="ActModel.GenerateAllEncounters" /> results for
        ///     <paramref name="act" />. This covers monster, elite, and boss encounter pools.
        /// </summary>
        bool IsValidForAct(ActModel act);
    }
}
