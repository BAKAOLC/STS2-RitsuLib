using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Diagnostics;

namespace STS2RitsuLib.Relics
{
    /// <summary>
    ///     Holds mod-supplied Dusty Tome card candidates, applied before the vanilla unlocked ancient-card fallback.
    ///     保存 mod 提供的 Dusty Tome 卡牌候选，并在原版已解锁 ancient 卡牌兜底前应用。
    /// </summary>
    internal static class DustyTomeCardRegistry
    {
        private static readonly Lock Sync = new();

        private static readonly List<DustyTomeCardMapping> ExplicitMappings = [];
        private static Dictionary<ModelId, IReadOnlyList<CardModel>>? _cacheByCharacter;
        private static long _nextRegistrationOrder;

        internal static bool TryGetPreferredCards(ModelId characterId,
            [NotNullWhen(true)] out IReadOnlyList<CardModel>? cards)
        {
            var cache = GetCache();
            if (cache.TryGetValue(characterId, out cards) && cards.Count > 0)
                return true;

            cards = null;
            return false;
        }

        internal static void Register(ModelId characterId, Type ancientCardType, string? modIdForLog)
        {
            EnsureModelType(ancientCardType, typeof(CardModel), nameof(ancientCardType));

            lock (Sync)
            {
                ExplicitMappings.Add(new(characterId, null, ancientCardType, modIdForLog,
                    _nextRegistrationOrder++));
                _cacheByCharacter = null;
            }
        }

        internal static void Register(Type characterType, Type ancientCardType, string? modIdForLog)
        {
            EnsureModelType(characterType, typeof(CharacterModel), nameof(characterType));
            EnsureModelType(ancientCardType, typeof(CardModel), nameof(ancientCardType));

            lock (Sync)
            {
                ExplicitMappings.Add(new(null, characterType, ancientCardType, modIdForLog,
                    _nextRegistrationOrder++));
                _cacheByCharacter = null;
            }
        }

        internal static void ValidateFrozenRegistrations()
        {
            DustyTomeCardMapping[] mappings;
            lock (Sync)
            {
                mappings = [.. ExplicitMappings];
            }

            foreach (var mapping in mappings)
            {
                if (mapping.CharacterType != null)
                    RegistrationFreezeDiagnostics.WarnMissingModelType(
                        "DustyTomeCards",
                        mapping.ModId,
                        "Dusty Tome character",
                        mapping.CharacterType,
                        typeof(CharacterModel));
                else if (mapping.CharacterId is { } characterId)
                    RegistrationFreezeDiagnostics.WarnMissingModelId(
                        "DustyTomeCards",
                        mapping.ModId,
                        "Dusty Tome character",
                        characterId,
                        typeof(CharacterModel));

                RegistrationFreezeDiagnostics.WarnMissingModelType(
                    "DustyTomeCards",
                    mapping.ModId,
                    $"Dusty Tome card for {mapping.CharacterDescription}",
                    mapping.CardType,
                    typeof(CardModel));
            }
        }

        private static Dictionary<ModelId, IReadOnlyList<CardModel>> GetCache()
        {
            lock (Sync)
            {
                return _cacheByCharacter ??= BuildCacheLocked();
            }
        }

        private static Dictionary<ModelId, IReadOnlyList<CardModel>> BuildCacheLocked()
        {
            var candidates = new Dictionary<ModelId, List<DustyTomeCardCandidate>>();

            foreach (var mapping in ExplicitMappings)
            {
                var card = ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(mapping.CardType));
                if (card == null)
                    continue;

                AddCandidate(
                    candidates,
                    mapping.ResolveCharacterId(),
                    card,
                    mapping.RegistrationOrder,
                    mapping.ModId ?? mapping.CardType.FullName ?? mapping.CardType.Name);
            }

            return candidates.ToDictionary(
                static pair => pair.Key,
                static IReadOnlyList<CardModel> (pair) =>
                {
                    return pair.Value
                        .GroupBy(static candidate => candidate.Card.Id)
                        .Select(static group => group.OrderBy(static candidate => candidate.RegistrationOrder)
                            .First().Card)
                        .OrderBy(static card => card.Id.ToString(), StringComparer.Ordinal)
                        .ToArray();
                });
        }

        private static void AddCandidate(
            Dictionary<ModelId, List<DustyTomeCardCandidate>> candidates,
            ModelId characterId,
            CardModel card,
            long registrationOrder,
            string source)
        {
            if (card.Rarity != CardRarity.Ancient)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[DustyTomeCards] Ignoring non-Ancient Dusty Tome candidate {card.Id} from {source}.");
                return;
            }

            if (!candidates.TryGetValue(characterId, out var list))
            {
                list = [];
                candidates[characterId] = list;
            }

            list.Add(new(card, registrationOrder));
        }

        private static void EnsureModelType(Type modelType, Type requiredBase, string paramName)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            if (!requiredBase.IsAssignableFrom(modelType))
                throw new ArgumentException($"{modelType.Name} must derive from {requiredBase.Name}.", paramName);
        }

        private sealed record DustyTomeCardMapping(
            ModelId? CharacterId,
            Type? CharacterType,
            Type CardType,
            string? ModId,
            long RegistrationOrder)
        {
            public string CharacterDescription => CharacterType?.FullName ?? CharacterId?.ToString() ?? "<unknown>";

            public ModelId ResolveCharacterId()
            {
                return CharacterId ?? ModelDb.GetId(CharacterType!);
            }
        }

        private sealed record DustyTomeCardCandidate(CardModel Card, long RegistrationOrder);
    }
}
