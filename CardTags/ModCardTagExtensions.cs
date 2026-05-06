using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Extension helpers for working with minted mod <see cref="CardTag" /> values on <see cref="CardModel" />.
    /// </summary>
    public static class ModCardTagExtensions
    {
        /// <summary>
        ///     Adds a minted mod tag resolved from <paramref name="tagId" /> into the card’s materialized tag set.
        /// </summary>
        public static void AddModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            var value = ModCardTagRegistry.GetCardTag(tagId);
            card.AddModCardTag(value);
        }

        /// <summary>
        ///     Adds a pre-minted mod <see cref="CardTag" /> into the card’s materialized tag set.
        /// </summary>
        public static void AddModCardTag(this CardModel card, CardTag value)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (card.Tags is not HashSet<CardTag> storage)
                throw new InvalidOperationException(
                    "CardModel.Tags is not backed by a mutable HashSet<CardTag>; cannot add mod tags at runtime.");

            storage.Add(value);
        }

        /// <summary>
        ///     Removes a minted mod tag resolved from <paramref name="tagId" /> from the card’s tag set when present.
        /// </summary>
        public static bool RemoveModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            return ModCardTagRegistry.TryGetCardTag(tagId, out var value) && card.RemoveModCardTag(value);
        }

        /// <summary>
        ///     Removes a pre-minted mod <see cref="CardTag" /> from the card’s tag set when present.
        /// </summary>
        public static bool RemoveModCardTag(this CardModel card, CardTag value)
        {
            ArgumentNullException.ThrowIfNull(card);

            return card.Tags is HashSet<CardTag> storage && storage.Remove(value);
        }

        /// <summary>
        ///     Whether the card’s tag set contains the minted value for <paramref name="tagId" />.
        /// </summary>
        public static bool HasModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            return ModCardTagRegistry.TryGetCardTag(tagId, out var value) && card.Tags.Contains(value);
        }

        /// <summary>
        ///     Convenience: minted <see cref="CardTag" /> for <paramref name="qualifiedTagId" />.
        /// </summary>
        public static CardTag GetModCardTag(this string qualifiedTagId)
        {
            return ModCardTagRegistry.GetCardTag(qualifiedTagId);
        }
    }
}
