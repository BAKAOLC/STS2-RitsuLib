using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Relics;

namespace STS2RitsuLib
{
    public static partial class RitsuLibFramework
    {
        /// <summary>
        ///     Registers an <see cref="ArchaicTooth" /> transcendence pair: when the player’s deck contains
        ///     <typeparamref name="TStarterCard" />, obtaining the relic transforms it into <typeparamref name="TAncientCard" />
        ///     (preserving upgrade state and enchantments, same as vanilla starters).
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores <typeparamref name="TAncientCard" /> as a
        ///     type for lazy <see cref="ModelDb" /> resolution so this is safe during content-pack <c>Apply()</c>.
        /// </remarks>
        /// <param name="registeringModId">Optional mod id for log messages when mappings are replaced.</param>
        public static void RegisterArchaicToothTranscendenceMapping<TStarterCard, TAncientCard>(
            string? registeringModId = null)
            where TStarterCard : CardModel
            where TAncientCard : CardModel
        {
            RegisterArchaicToothTranscendenceMapping(
                ModelDb.GetId<TStarterCard>(),
                typeof(TAncientCard),
                registeringModId);
        }

        /// <summary>
        ///     Registers an <see cref="ArchaicTooth" /> transcendence mapping using an explicit starter id and ancient card
        ///     type.
        /// </summary>
        /// <param name="starterCardId">Deck card model id to match.</param>
        /// <param name="ancientCardType">Concrete card type; resolved via <see cref="ModelDb" /> when the blessing runs.</param>
        /// <param name="registeringModId">Optional mod id for log messages when mappings are replaced.</param>
        public static void RegisterArchaicToothTranscendenceMapping(ModelId starterCardId, Type ancientCardType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterTranscendence(starterCardId, ancientCardType, registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="TouchOfOrobas" /> refinement pair: when the player’s starter relic is
        ///     <typeparamref name="TStarterRelic" />, the blessing replaces it with <typeparamref name="TUpgradedRelic" />.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="ModelDb.GetId{T}" /> for the starter key and stores the upgraded relic as a type for lazy
        ///     <see cref="ModelDb" /> resolution so this is safe during content-pack <c>Apply()</c>.
        /// </remarks>
        /// <param name="registeringModId">Optional mod id for log messages when mappings are replaced.</param>
        public static void RegisterTouchOfOrobasRefinementMapping<TStarterRelic, TUpgradedRelic>(
            string? registeringModId = null)
            where TStarterRelic : RelicModel
            where TUpgradedRelic : RelicModel
        {
            RegisterTouchOfOrobasRefinementMapping(
                ModelDb.GetId<TStarterRelic>(),
                typeof(TUpgradedRelic),
                registeringModId);
        }

        /// <summary>
        ///     Registers a <see cref="TouchOfOrobas" /> refinement mapping using explicit starter id and upgraded relic type.
        /// </summary>
        /// <param name="starterRelicId">Starter relic instance id to match.</param>
        /// <param name="upgradedRelicType">Concrete relic type; resolved via <see cref="ModelDb" /> when the blessing runs.</param>
        /// <param name="registeringModId">Optional mod id for log messages when mappings are replaced.</param>
        public static void RegisterTouchOfOrobasRefinementMapping(ModelId starterRelicId, Type upgradedRelicType,
            string? registeringModId = null)
        {
            OrobasAncientUpgradeRegistry.RegisterRefinement(starterRelicId, upgradedRelicType, registeringModId);
        }
    }
}
