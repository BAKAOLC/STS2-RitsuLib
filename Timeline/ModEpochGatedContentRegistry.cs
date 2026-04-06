using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace STS2RitsuLib.Timeline
{
    /// <summary>
    ///     Maps epoch ids to CLR types gated by that epoch (cards and/or relics only — not potions), populated from pack
    ///     flow such as <see cref="TimelineColumnPackEntry{TStory}" /> slot <c>Cards</c>/<c>Relics</c>/<c>RelicsFromPool</c>/
    ///     <c>CardsFromPool</c>.
    ///     Potions use <c>RequireAllPotionsInPool</c> / <c>Potions</c> on <c>EpochSlotBuilder&lt;TEpoch&gt;</c>
    ///     (RequireEpoch only).
    ///     Used by pack-declared unlock epoch templates and stays in sync with
    ///     <see cref="Unlocks.ModUnlockRegistry.RequireEpoch(Type,string)" />.
    /// </summary>
    public static class ModEpochGatedContentRegistry
    {
        private static readonly Lock SyncRoot = new();

        private static readonly Dictionary<string, EpochGatedContentEntry> ByEpochId =
            new(StringComparer.Ordinal);

        private static bool _isFrozen;

        /// <summary>
        ///     True after <see cref="FreezeRegistrations" />.
        /// </summary>
        public static bool IsFrozen
        {
            get
            {
                lock (SyncRoot)
                {
                    return _isFrozen;
                }
            }
        }

        internal static void FreezeRegistrations(string reason)
        {
            lock (SyncRoot)
            {
                if (_isFrozen)
                    return;

                _isFrozen = true;
            }
        }

        /// <summary>
        ///     Registers gated model types for <paramref name="epochId" /> (must be unique). At least one card or relic
        ///     type is required.
        /// </summary>
        public static void Register(string modId, string epochId, IReadOnlyList<Type>? cardTypes,
            IReadOnlyList<Type>? relicTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            var cards = cardTypes ?? [];
            var relics = relicTypes ?? [];
            if (cards.Count == 0 && relics.Count == 0)
                throw new ArgumentException(
                    $"Gated content for epoch '{epochId}' must include at least one card or relic type.",
                    nameof(cardTypes));

            lock (SyncRoot)
            {
                EnsureMutable($"register gated content for epoch '{epochId}'");
                if (ByEpochId.ContainsKey(epochId))
                    throw new InvalidOperationException(
                        $"Epoch gated content was already registered for id '{epochId}'.");

                ByEpochId[epochId] = new(modId, cards, relics);
            }
        }

        /// <summary>
        ///     Returns whether <paramref name="epochId" /> has pack-registered gated types.
        /// </summary>
        public static bool TryGet(string epochId, out EpochGatedContentEntry entry)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(epochId);

            lock (SyncRoot)
            {
                return ByEpochId.TryGetValue(epochId, out entry!);
            }
        }

        /// <summary>
        ///     Resolves <see cref="CardModel" /> instances for a gated epoch id.
        /// </summary>
        public static IReadOnlyList<CardModel> ResolveCards(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return entry.CardTypes
                .Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        /// <summary>
        ///     Resolves <see cref="RelicModel" /> instances for a gated epoch id.
        /// </summary>
        public static IReadOnlyList<RelicModel> ResolveRelics(string epochId)
        {
            if (!TryGet(epochId, out var entry))
                return [];

            return entry.RelicTypes
                .Select(type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
                .ToArray();
        }

        private static void EnsureMutable(string operation)
        {
            if (!_isFrozen)
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} after epoch gated content registration has been frozen.");
        }

        /// <summary>
        ///     Snapshot of types registered for one epoch (from the owning mod’s pack).
        /// </summary>
        public sealed record EpochGatedContentEntry(
            string ModId,
            IReadOnlyList<Type> CardTypes,
            IReadOnlyList<Type> RelicTypes);
    }
}
