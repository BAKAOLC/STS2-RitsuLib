using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.CardTags
{
    /// <summary>
    ///     <para xml:lang="en">Provides helpers for using dynamic mod card tags with cards and tag IDs.</para>
    ///     <para xml:lang="zh-CN">提供在卡牌与标签 ID 上使用动态模组卡牌标签的辅助方法。</para>
    /// </summary>
    public static class ModCardTagExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Adds the tag represented by <paramref name="tagId" /> to the card.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="tagId" /> 所表示的标签添加到卡牌。</para>
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
        ///     <para xml:lang="en">Adds <paramref name="value" /> to the card's mutable tag set.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="value" /> 添加到卡牌的可变标签集合。</para>
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
        ///     <para xml:lang="en">Removes the tag represented by <paramref name="tagId" /> from the card.</para>
        ///     <para xml:lang="zh-CN">从卡牌中移除 <paramref name="tagId" /> 所表示的标签。</para>
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
        ///     <para xml:lang="en">Removes <paramref name="value" /> from the card's tag set.</para>
        ///     <para xml:lang="zh-CN">从卡牌标签集合中移除 <paramref name="value" />。</para>
        /// </summary>
        public static bool RemoveModCardTag(this CardModel card, CardTag value)
        {
            ArgumentNullException.ThrowIfNull(card);

            return card.Tags is HashSet<CardTag> storage && storage.Remove(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Determines whether the card contains the tag represented by <paramref name="tagId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定卡牌是否包含 <paramref name="tagId" /> 所表示的标签。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Determines whether a card's tag set contains a registered dynamic mod <see cref="CardTag" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         确定卡牌的标签集合是否包含已注册的动态模组 <see cref="CardTag" />。
        ///     </para>
        /// </summary>
        /// <param name="card">
        ///     <para xml:lang="en">The card to inspect.</para>
        ///     <para xml:lang="zh-CN">要检查的卡牌。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The registered dynamic tag value to find.</para>
        ///     <para xml:lang="zh-CN">要查找的已注册动态标签值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when the card contains the tag; otherwise, <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">卡牌包含该标签时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public static bool HasModCardTag(this CardModel card, CardTag value)
        {
            ArgumentNullException.ThrowIfNull(card);

            return card.Tags.Contains(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deterministic <see cref="CardTag" /> for <paramref name="qualifiedTagId" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="qualifiedTagId" /> 对应的确定性 <see cref="CardTag" />。
        ///     </para>
        /// </summary>
        public static CardTag GetModCardTag(this string qualifiedTagId)
        {
            return ModCardTagRegistry.GetCardTag(qualifiedTagId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to get the registered ID represented by <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试获取 <paramref name="value" /> 所表示的已注册 ID。
        ///     </para>
        /// </summary>
        public static bool TryGetModCardTagId(this CardTag value, out string id)
        {
            return ModCardTagRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registered ID represented by <paramref name="value" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="value" /> 所表示的已注册 ID。</para>
        /// </summary>
        public static string GetModCardTagId(this CardTag value)
        {
            return ModCardTagRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"CardTag '0x{(int)value:X8}' is not a registered mod card tag.");
        }
    }
}
