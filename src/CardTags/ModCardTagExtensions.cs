using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     Extension helpers for working with minted mod <see cref="CardTag" /> values on <see cref="CardModel" />.
    ///     用于在 <see cref="CardModel" /> 上处理已生成 mod <see cref="CardTag" /> 值的扩展辅助方法。
    /// </summary>
    public static class ModCardTagExtensions
    {
        /// <summary>
        ///     Adds a minted mod tag resolved from <paramref name="tagId" /> into the card’s materialized tag set.
        ///     将从 <paramref name="tagId" /> 解析出的已生成 mod 标签加入卡牌的实体化标签集合。
        /// </summary>
        [Obsolete(
            "Resolve the id once with ModCardTagRegistry.GetCardTag or string.GetModCardTag(), then use AddModCardTag(CardTag).")]
        public static void AddModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            var value = ModCardTagRegistry.GetCardTag(tagId);
            card.AddModCardTag(value);
        }

        /// <summary>
        ///     Adds a pre-minted mod <see cref="CardTag" /> into the card’s materialized tag set.
        ///     将预先生成的 mod <see cref="CardTag" /> 加入卡牌的实体化标签集合。
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
        ///     如果存在，则从卡牌标签集合中移除从 <paramref name="tagId" /> 解析出的已生成 mod 标签。
        /// </summary>
        [Obsolete(
            "Resolve the id once with ModCardTagRegistry.GetCardTag or string.GetModCardTag(), then use RemoveModCardTag(CardTag).")]
        public static bool RemoveModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            return ModCardTagRegistry.TryGetCardTag(tagId, out var value) && card.RemoveModCardTag(value);
        }

        /// <summary>
        ///     Removes a pre-minted mod <see cref="CardTag" /> from the card’s tag set when present.
        ///     如果存在，则从卡牌标签集合中移除预先生成的 mod <see cref="CardTag" />。
        /// </summary>
        public static bool RemoveModCardTag(this CardModel card, CardTag value)
        {
            ArgumentNullException.ThrowIfNull(card);

            return card.Tags is HashSet<CardTag> storage && storage.Remove(value);
        }

        /// <summary>
        ///     Whether the card’s tag set contains the minted value for <paramref name="tagId" />.
        ///     判断卡牌标签集合是否包含 <paramref name="tagId" /> 对应的已生成值。
        /// </summary>
        [Obsolete(
            "Resolve the id once with ModCardTagRegistry.GetCardTag or string.GetModCardTag(), then use HasModCardTag(CardTag).")]
        public static bool HasModCardTag(this CardModel card, string tagId)
        {
            ArgumentNullException.ThrowIfNull(card);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

            return ModCardTagRegistry.TryGetCardTag(tagId, out var value) && card.Tags.Contains(value);
        }

        /// <summary>
        ///     Convenience: minted <see cref="CardTag" /> for <paramref name="qualifiedTagId" />.
        ///     便捷方法：获取 <paramref name="qualifiedTagId" /> 对应的已生成 <see cref="CardTag" />。
        /// </summary>
        public static CardTag GetModCardTag(this string qualifiedTagId)
        {
            return ModCardTagRegistry.GetCardTag(qualifiedTagId);
        }

        /// <summary>
        ///     Tries to reverse-map a minted mod <see cref="CardTag" /> value to its registered string id.
        ///     尝试将已生成的 mod <see cref="CardTag" /> 值反向映射到其注册字符串 ID。
        /// </summary>
        public static bool TryGetModCardTagId(this CardTag value, out string id)
        {
            return ModCardTagRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     Reverse-maps a minted mod <see cref="CardTag" /> value to its registered string id.
        ///     将已生成的 mod <see cref="CardTag" /> 值反向映射到其注册字符串 ID。
        /// </summary>
        public static string GetModCardTagId(this CardTag value)
        {
            return ModCardTagRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"CardTag '0x{(int)value:X8}' is not a registered mod card tag.");
        }
    }
}
