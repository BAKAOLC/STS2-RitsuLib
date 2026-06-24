using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content.Patches;

namespace STS2RitsuLib.Models.Identity
{
    /// <summary>
    ///     Runtime model identity registry backed by deterministic vanilla lifecycle entry points.
    ///     基于确定性原版生命周期入口的运行时 model identity 注册表。
    /// </summary>
    internal static class ModModelIdentityRegistry
    {
        private const uint FirstIdentityValue = 1;

        private static readonly Lock Gate = new();
        private static readonly Dictionary<AbstractModel, uint> ObjectToIdentity = new(new ReferenceComparer());
        private static readonly Dictionary<uint, AbstractModel> IdentityToObject = [];
        private static uint _nextIdentity = FirstIdentityValue;

        public static void Clear()
        {
            lock (Gate)
            {
                ObjectToIdentity.Clear();
                IdentityToObject.Clear();
                _nextIdentity = FirstIdentityValue;
            }
        }

        public static ModModelIdentity EnsureRegistered(AbstractModel? model)
        {
            if (model is not { IsMutable: true } || IsLocalOnly(model))
                return ModModelIdentity.None;

            lock (Gate)
            {
                if (ObjectToIdentity.TryGetValue(model, out var existing))
                    return new(existing);

                var value = _nextIdentity++;
                ObjectToIdentity[model] = value;
                IdentityToObject[value] = model;
                return new(value);
            }
        }

        public static bool TryGetToken(AbstractModel? model, out ModModelIdentityToken token)
        {
            token = default;
            if (model == null || IsLocalOnly(model))
                return false;

            lock (Gate)
            {
                if (!ObjectToIdentity.TryGetValue(model, out var identity) || identity == 0)
                    return false;

                token = new(new(identity), model.Id);
                return true;
            }
        }

        private static bool IsLocalOnly(AbstractModel model)
        {
            return ModelIdSerializationCacheDynamicContentPatch.IsLocalOnlyModelId(model.Id);
        }

        public static bool TryResolve(ModModelIdentityToken token, out AbstractModel model)
        {
            model = null!;
            if (!token.IsValid)
                return false;

            lock (Gate)
            {
                if (!IdentityToObject.TryGetValue(token.Identity.Value, out var resolved))
                    return false;
                if (resolved.Id != token.ModelId)
                    return false;

                model = resolved;
                return true;
            }
        }

        public static void Unregister(AbstractModel? model)
        {
            if (model == null)
                return;

            lock (Gate)
            {
                if (!ObjectToIdentity.Remove(model, out var identity))
                    return;

                if (IdentityToObject.TryGetValue(identity, out var current) && ReferenceEquals(current, model))
                    IdentityToObject.Remove(identity);
            }
        }

        public static void RegisterCardTree(CardModel? card)
        {
            if (card == null)
                return;

            EnsureRegistered(card);
            EnsureRegistered(card.Affliction);
            EnsureRegistered(card.Enchantment);
        }

        public static void RegisterPlayerInventory(Player? player)
        {
            if (player == null)
                return;

            foreach (var card in player.Deck.Cards)
                RegisterCardTree(card);
            foreach (var relic in player.Relics)
                EnsureRegistered(relic);
            foreach (var potion in player.PotionSlots)
                EnsureRegistered(potion);
        }

        public static PlayerInventoryIdentitySnapshot CapturePlayerInventory(Player player)
        {
            return new(
                CaptureCards(player.Deck.Cards),
                CaptureModels(player.Relics),
                CaptureModels(player.PotionSlots));
        }

        public static void RestorePlayerInventory(Player player, PlayerInventoryIdentitySnapshot snapshot)
        {
            RestoreCards(snapshot.DeckCards, player.Deck.Cards);
            RestoreModels(snapshot.Relics, player.Relics);
            RestoreModels(snapshot.Potions, player.PotionSlots);
        }

        private static CardIdentitySnapshot[] CaptureCards(IReadOnlyList<CardModel> cards)
        {
            var result = new CardIdentitySnapshot[cards.Count];
            for (var i = 0; i < cards.Count; i++)
                result[i] = CaptureCardTree(cards[i]);

            return result;
        }

        private static ModelIdentitySnapshot[] CaptureModels<TModel>(IReadOnlyList<TModel> models)
            where TModel : AbstractModel?
        {
            var result = new ModelIdentitySnapshot[models.Count];
            for (var i = 0; i < models.Count; i++)
                result[i] = Capture(models[i]);

            return result;
        }

        private static ModelIdentitySnapshot Capture(AbstractModel? model)
        {
            if (model == null || !TryGetToken(model, out var token))
                return default;

            return new(model, token);
        }

        private static CardIdentitySnapshot CaptureCardTree(CardModel? card)
        {
            if (card == null)
                return default;

            return new(
                Capture(card),
                Capture(card.Affliction),
                Capture(card.Enchantment));
        }

        private static void RestoreCards(
            IReadOnlyList<CardIdentitySnapshot> previous,
            IReadOnlyList<CardModel> current)
        {
            var count = Math.Min(previous.Count, current.Count);
            for (var i = 0; i < count; i++)
                RestoreCardTree(previous[i], current[i]);

            for (var i = count; i < previous.Count; i++)
                Unregister(previous[i]);
            for (var i = count; i < current.Count; i++)
                RegisterCardTree(current[i]);
        }

        private static void RestoreModels<TModel>(
            IReadOnlyList<ModelIdentitySnapshot> previous,
            IReadOnlyList<TModel> current)
            where TModel : AbstractModel?
        {
            var count = Math.Min(previous.Count, current.Count);
            for (var i = 0; i < count; i++)
                Restore(previous[i], current[i]);

            for (var i = count; i < previous.Count; i++)
                Unregister(previous[i].Model);
            for (var i = count; i < current.Count; i++)
                EnsureRegistered(current[i]);
        }

        private static void Restore(ModelIdentitySnapshot previous, AbstractModel? current)
        {
            if (current == null)
            {
                Unregister(previous.Model);
                return;
            }

            if (previous.Token.IsValid && previous.Token.ModelId == current.Id)
            {
                BindIdentity(current, previous.Token.Identity);
                return;
            }

            Unregister(previous.Model);
            EnsureRegistered(current);
        }

        private static void RestoreCardTree(CardIdentitySnapshot previous, CardModel? current)
        {
            if (current == null)
            {
                Unregister(previous);
                return;
            }

            Restore(previous.Card, current);
            Restore(previous.Affliction, current.Affliction);
            Restore(previous.Enchantment, current.Enchantment);
        }

        private static void Unregister(CardIdentitySnapshot snapshot)
        {
            Unregister(snapshot.Card.Model);
            Unregister(snapshot.Affliction.Model);
            Unregister(snapshot.Enchantment.Model);
        }

        private static void BindIdentity(AbstractModel model, ModModelIdentity identity)
        {
            if (!identity.IsValid || !model.IsMutable)
                return;

            lock (Gate)
            {
                if (ObjectToIdentity.Remove(model, out var oldIdentity) &&
                    IdentityToObject.TryGetValue(oldIdentity, out var oldCurrent) &&
                    ReferenceEquals(oldCurrent, model))
                    IdentityToObject.Remove(oldIdentity);

                if (IdentityToObject.TryGetValue(identity.Value, out var previous))
                    ObjectToIdentity.Remove(previous);

                ObjectToIdentity[model] = identity.Value;
                IdentityToObject[identity.Value] = model;
            }

            if (model is CardModel card)
                RegisterCardTree(card);
        }

        internal readonly record struct PlayerInventoryIdentitySnapshot(
            CardIdentitySnapshot[] DeckCards,
            ModelIdentitySnapshot[] Relics,
            ModelIdentitySnapshot[] Potions);

        internal readonly record struct CardIdentitySnapshot(
            ModelIdentitySnapshot Card,
            ModelIdentitySnapshot Affliction,
            ModelIdentitySnapshot Enchantment);

        internal readonly record struct ModelIdentitySnapshot(
            AbstractModel? Model,
            ModModelIdentityToken Token);

        private sealed class ReferenceComparer : IEqualityComparer<AbstractModel>
        {
            public bool Equals(AbstractModel? x, AbstractModel? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(AbstractModel obj)
            {
                return ReferenceEqualityComparer.Instance.GetHashCode(obj);
            }
        }
    }
}
