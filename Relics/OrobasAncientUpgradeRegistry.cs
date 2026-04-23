using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace STS2RitsuLib.Relics
{
    /// <summary>
    ///     Holds mod-supplied mappings for <see cref="ArchaicTooth" /> transcendence and
    ///     <see cref="TouchOfOrobas" /> refinement, applied via framework Harmony patches.
    /// </summary>
    /// <remarks>
    ///     Target models are stored as CLR <see cref="Type" /> and resolved through <see cref="ModelDb.GetByIdOrNull{T}" />
    ///     at patch time so registration can run during mod <c>Apply()</c> before <see cref="ModelDb" /> has injected mod
    ///     content into <c>_contentById</c>. Starter keys use <see cref="ModelDb.GetId{T}" /> (metadata only).
    /// </remarks>
    internal static class OrobasAncientUpgradeRegistry
    {
        private static readonly Lock Sync = new();

        private static readonly Dictionary<ModelId, Type> TranscendenceAncientTypeByStarter = [];

        private static readonly Dictionary<ModelId, Type> RefinementUpgradedTypeByStarter = [];

        internal static bool TryGetTranscendenceAncient(ModelId starterCardId,
            [NotNullWhen(true)] out CardModel? ancientTemplate)
        {
            Type ancientType;
            lock (Sync)
            {
                if (!TranscendenceAncientTypeByStarter.TryGetValue(starterCardId, out var t))
                {
                    ancientTemplate = null;
                    return false;
                }

                ancientType = t;
            }

            ancientTemplate = ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(ancientType));
            return ancientTemplate != null;
        }

        internal static bool TryGetRefinementUpgrade(ModelId starterRelicId,
            [NotNullWhen(true)] out RelicModel? upgradedTemplate)
        {
            Type upgradedType;
            lock (Sync)
            {
                if (!RefinementUpgradedTypeByStarter.TryGetValue(starterRelicId, out var t))
                {
                    upgradedTemplate = null;
                    return false;
                }

                upgradedType = t;
            }

            upgradedTemplate = ModelDb.GetByIdOrNull<RelicModel>(ModelDb.GetId(upgradedType));
            return upgradedTemplate != null;
        }

        internal static bool HasTranscendenceStarter(ModelId starterCardId)
        {
            lock (Sync)
            {
                return TranscendenceAncientTypeByStarter.ContainsKey(starterCardId);
            }
        }

        /// <summary>
        ///     Distinct ancient card templates registered by mods (for <see cref="ArchaicTooth.TranscendenceCards" />).
        /// </summary>
        internal static IReadOnlyList<CardModel> GetRegisteredTranscendenceAncientTemplates()
        {
            Type[] types;
            lock (Sync)
            {
                types = TranscendenceAncientTypeByStarter.Values
                    .Distinct()
                    .OrderBy(static t => t.FullName ?? t.Name, StringComparer.Ordinal)
                    .ToArray();
            }

            var seen = new HashSet<ModelId>();
            List<CardModel> list = [];
            list.AddRange(types.Select(ancientType => ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(ancientType)))
                .OfType<CardModel>().Where(card => seen.Add(card.Id)));

            return list;
        }

        internal static void RegisterTranscendence(ModelId starterCardId, Type ancientCardType, string? modIdForLog)
        {
            EnsureModelType(ancientCardType, typeof(CardModel), nameof(ancientCardType));

            lock (Sync)
            {
                if (TranscendenceAncientTypeByStarter.TryGetValue(starterCardId, out var previous) &&
                    previous != ancientCardType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Transcendence mapping for starter card {starterCardId} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                TranscendenceAncientTypeByStarter[starterCardId] = ancientCardType;
            }
        }

        internal static void RegisterRefinement(ModelId starterRelicId, Type upgradedRelicType, string? modIdForLog)
        {
            EnsureModelType(upgradedRelicType, typeof(RelicModel), nameof(upgradedRelicType));

            lock (Sync)
            {
                if (RefinementUpgradedTypeByStarter.TryGetValue(starterRelicId, out var previous) &&
                    previous != upgradedRelicType)
                    RitsuLibFramework.Logger.Warn(
                        $"[OrobasAncientUpgrades] Refinement mapping for starter relic {starterRelicId} " +
                        $"was replaced{(string.IsNullOrEmpty(modIdForLog) ? "" : $" (mod {modIdForLog})")}.");

                RefinementUpgradedTypeByStarter[starterRelicId] = upgradedRelicType;
            }
        }

        private static void EnsureModelType(Type modelType, Type requiredBase, string paramName)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            if (!requiredBase.IsAssignableFrom(modelType))
                throw new ArgumentException($"{modelType.Name} must derive from {requiredBase.Name}.", paramName);
        }
    }
}
