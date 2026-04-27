using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Content
{
    public sealed partial class ModContentRegistry
    {
        private static readonly List<CardLibraryCompendiumSharedPoolFilterRegistration>
            CardLibraryCompendiumSharedPoolFilters =
                [];

        private static readonly HashSet<string> CardLibraryCompendiumSharedPoolFilterStableIds =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Registers an extra card-library compendium pool filter for <typeparamref name="TPool" />. Unregistered
        ///     shared card pools do not get a filter button; call this only when you want that row, and supply
        ///     <paramref name="iconTexturePath" /> and a unique <paramref name="stableId" />.
        /// </summary>
        public void RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(string stableId, string iconTexturePath)
            where TPool : CardPoolModel
        {
            RegisterCardLibraryCompendiumSharedPoolFilter(stableId, iconTexturePath, typeof(TPool), null);
        }

        /// <summary>
        ///     Registers an extra card-library compendium pool filter with optional ordered placement rules.
        /// </summary>
        public void RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(
            string stableId,
            string iconTexturePath,
            IReadOnlyList<CardLibraryCompendiumPlacementRule>? placementRules)
            where TPool : CardPoolModel
        {
            RegisterCardLibraryCompendiumSharedPoolFilter(stableId, iconTexturePath, typeof(TPool), placementRules);
        }

        /// <inheritdoc cref="RegisterCardLibraryCompendiumSharedPoolFilter{TPool}(string,string)" />
        public void RegisterCardLibraryCompendiumSharedPoolFilter(string stableId, string iconTexturePath,
            Type cardPoolType)
        {
            RegisterCardLibraryCompendiumSharedPoolFilter(stableId, iconTexturePath, cardPoolType, null);
        }

        /// <inheritdoc
        ///     cref="RegisterCardLibraryCompendiumSharedPoolFilter{TPool}(string,string,IReadOnlyList{CardLibraryCompendiumPlacementRule}?)" />
        public void RegisterCardLibraryCompendiumSharedPoolFilter(string stableId, string iconTexturePath,
            Type cardPoolType,
            IReadOnlyList<CardLibraryCompendiumPlacementRule>? placementRules)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
            ArgumentException.ThrowIfNullOrWhiteSpace(iconTexturePath);
            ArgumentNullException.ThrowIfNull(cardPoolType);
            ThrowIfInvalidCompendiumSharedPoolFilterStableId(stableId);
            EnsureModelType(cardPoolType, typeof(CardPoolModel), nameof(cardPoolType));
            CardLibraryCompendiumPlacementRule.ThrowIfInvalidRules(placementRules);

            lock (SyncRoot)
            {
                EnsureMutable("register card library compendium shared pool filter");
                if (!CardLibraryCompendiumSharedPoolFilterStableIds.Add(stableId))
                    throw new InvalidOperationException(
                        $"Duplicate card library compendium shared pool filter stable id: '{stableId}'.");

                CardLibraryCompendiumSharedPoolFilters.Add(new()
                {
                    OwningModId = ModId,
                    StableId = stableId,
                    IconTexturePath = iconTexturePath,
                    CardPoolType = cardPoolType,
                    PlacementRules = placementRules is { Count: > 0 } ? placementRules : null,
                });
            }

            _logger.Info(
                $"[Content] Registered card library compendium shared pool filter '{stableId}' -> {cardPoolType.Name}");
        }

        internal static IReadOnlyList<CardLibraryCompendiumSharedPoolFilterRegistration>
            GetCardLibraryCompendiumSharedPoolFilters()
        {
            lock (SyncRoot)
            {
                return CardLibraryCompendiumSharedPoolFilters.ToArray();
            }
        }

        private static void ThrowIfInvalidCompendiumSharedPoolFilterStableId(string stableId)
        {
            if (stableId.Any(c => !char.IsAsciiLetter(c) && !char.IsAsciiDigit(c) && c != '_'))
                throw new ArgumentException(
                    "Stable id must contain only ASCII letters, digits, or underscores (safe for Godot node names).",
                    nameof(stableId));
        }
    }
}
