using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Extension methods for attaching runtime keyword ids to arbitrary objects and for hover-tip helpers.
    ///     Every <see cref="CardModel" /> operation is routed straight through vanilla <c>CardModel.Keywords</c>
    ///     using the pre-minted <see cref="ModKeywordDefinition.CardKeywordValue" /> (so the mod keyword rides
    ///     vanilla <c>AddKeyword</c> / <c>RemoveKeyword</c> / <c>DeepCloneFields</c> / canonical seeding without
    ///     any side-loaded state). Non-card objects fall back to a <see cref="ConditionalWeakTable{TKey,TValue}" />
    ///     for ad-hoc usage (no clone / save persistence).
    ///     <c>AddKeyword</c> / <c>RemoveKeyword</c> / <c>DeepCloneFields</c> / canonical seeding，
    ///     用于将运行时关键词 id 附加到任意对象以及处理悬停提示 helper 的扩展方法。
    ///     每个 <see cref="CardModel" /> 操作都会直接路由到原版 <c>CardModel.Keywords</c>，
    ///     使用预先铸造的 <see cref="ModKeywordDefinition.CardKeywordValue" />（因此 mod 关键词可以沿用
    ///     原版 <c>AddKeyword</c>、<c>RemoveKeyword</c>、<c>DeepCloneFields</c> 和规范种入，且无需
    ///     任何 side-loaded 状态）。非卡牌对象会回退到 <see cref="ConditionalWeakTable{TKey,TValue}" />
    ///     做临时用途（无克隆/存档持久化）。
    ///     <c>AddKeyword</c>
    ///     <c>RemoveKeyword</c>
    ///     <c>DeepCloneFields</c>
    ///     canonical seeding，
    /// </summary>
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> FallbackKeywords = new();

        /// <summary>
        ///     Adds a runtime keyword id to the extended target (deduplicated, case-insensitive).
        ///     For every <see cref="CardModel" /> (vanilla or modded) the minted
        ///     <see cref="ModKeywordDefinition.CardKeywordValue" /> is pushed into vanilla
        ///     <c>CardModel.Keywords</c>; the id does not need to be registered, but registered ids can provide
        ///     hover-tip metadata.
        ///     向扩展目标添加 runtime keyword id（去重、大小写不敏感）。
        ///     对每个 <see cref="CardModel" />（原版或 modded），minted 的
        ///     <see cref="ModKeywordDefinition.CardKeywordValue" /> 会被推入原版
        ///     <c>CardModel.Keywords</c>；id 不需要已注册，但已注册 id 可提供 hover-tip 元数据。
        /// </summary>
        [Obsolete(
            "Resolve the id once with ModKeywordRegistry.GetCardKeyword or string.GetModCardKeyword(), then use AddModKeyword(CardKeyword).")]
        public static void AddModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
            {
                card.AddModKeyword(ModKeywordRegistry.GetCardKeyword(keywordId));
                return;
            }

            lock (SyncRoot)
            {
                FallbackKeywords.GetValue(target, static _ => new(StringComparer.OrdinalIgnoreCase))
                    .Add(keywordId.Trim());
            }
        }

        /// <summary>
        ///     Adds a pre-minted mod <see cref="CardKeyword" /> value directly to vanilla
        ///     <c>CardModel.Keywords</c>, enabling native-style call sites like
        ///     <c>card.AddModKeyword(ModKeywordRegistry.GetCardKeyword("mymod.blazed"))</c>. The card's keyword
        ///     set is materialized first (mirroring the vanilla getter) so the underlying
        ///     <c>_keywords</c> field is never null when <see cref="CardModel.AddKeyword" /> runs.
        ///     将预先铸造的 mod <see cref="CardKeyword" /> 值直接添加到原版
        ///     <c>CardModel.Keywords</c>，使
        ///     <c>card.AddModKeyword(ModKeywordRegistry.GetCardKeyword("mymod.blazed"))</c> 这类原生风格调用点可用。该卡牌的关键词
        ///     集合会先实体化（对应原版 getter），因此底层
        ///     <c>_keywords</c> 字段在 <see cref="CardModel.AddKeyword" /> 运行时绝不会为 null。
        /// </summary>
        public static void AddModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            _ = card.Keywords;
            card.AddKeyword(value);
        }

        /// <summary>
        ///     Removes a previously added runtime keyword id.
        ///     For every <see cref="CardModel" /> the corresponding minted value is removed from vanilla
        ///     <c>CardModel.Keywords</c>; unregistered ids return <c>false</c> without touching the card.
        ///     移除先前添加的 runtime keyword id。
        ///     对每个 <see cref="CardModel" />，对应的 minted 值会从原版
        ///     <c>CardModel.Keywords</c> 中移除；未注册 id 返回 <c>false</c>，且不触碰卡牌。
        /// </summary>
        /// <returns>
        ///     True when the id was present and removed.
        ///     id 曾存在并已移除时为 true。
        /// </returns>
        [Obsolete(
            "Resolve the id once with ModKeywordRegistry.GetCardKeyword or string.GetModCardKeyword(), then use RemoveModKeyword(CardKeyword).")]
        public static bool RemoveModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return ModKeywordRegistry.TryGetCardKeyword(keywordId, out var value) &&
                       card.RemoveModKeyword(value);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Remove(keywordId.Trim());
            }
        }

        /// <summary>
        ///     Removes <paramref name="value" /> from vanilla <c>CardModel.Keywords</c>. Returns <c>true</c> when
        ///     the keyword was present.
        ///     从原版 <c>CardModel.Keywords</c> 中移除 <paramref name="value" />。当
        ///     该 keyword 曾存在时返回 <c>true</c>。
        /// </summary>
        public static bool RemoveModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            if (!card.Keywords.Contains(value))
                return false;

            card.RemoveKeyword(value);
            return true;
        }

        /// <summary>
        ///     Returns whether the target has the given runtime keyword id currently in effect.
        ///     返回目标当前是否具有给定 runtime keyword id。
        /// </summary>
        [Obsolete(
            "Resolve the id once with ModKeywordRegistry.GetCardKeyword or string.GetModCardKeyword(), then use HasModKeyword(CardKeyword).")]
        public static bool HasModKeyword(this object target, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            if (target is CardModel card)
                return ModKeywordRegistry.TryGetCardKeyword(keywordId, out var value) &&
                       card.Keywords.Contains(value);

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set) &&
                       set.Contains(keywordId.Trim());
            }
        }

        /// <summary>
        ///     Whether <paramref name="card" /> currently carries the minted mod keyword <paramref name="value" />.
        ///     <paramref name="card" /> 当前是否携带 minted mod keyword <paramref name="value" />。
        /// </summary>
        public static bool HasModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            return card.Keywords.Contains(value);
        }

        /// <summary>
        ///     Sorted list of effective runtime mod-keyword ids on the target. For every
        ///     <see cref="CardModel" /> this enumerates vanilla <c>CardModel.Keywords</c> and reverse-maps minted
        ///     values back to their registered ids (skipping vanilla and unregistered entries).
        ///     目标上生效的 runtime mod-keyword id 排序列表。对每个
        ///     <see cref="CardModel" />，此方法枚举原版 <c>CardModel.Keywords</c>，并将 minted
        ///     值反向映射为其已注册 id（跳过原版和未注册条目）。
        /// </summary>
        public static IReadOnlyList<string> GetModKeywordIds(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is CardModel card)
            {
                var ids = new List<string>();
                foreach (var keyword in card.Keywords)
                    if (ModKeywordRegistry.TryGetByCardKeyword(keyword, out var def))
                        ids.Add(def.Id);

                ids.Sort(StringComparer.Ordinal);
                return ids;
            }

            lock (SyncRoot)
            {
                return FallbackKeywords.TryGetValue(target, out var set)
                    ? [.. set.OrderBy(static x => x, StringComparer.Ordinal)]
                    : [];
            }
        }

        /// <summary>
        ///     Hover tips for all runtime keyword ids on the target.
        ///     目标上所有 runtime keyword id 的 hover tip。
        /// </summary>
        public static IEnumerable<IHoverTip> GetModKeywordHoverTips(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);
            return target.GetModKeywordIds().ToHoverTips();
        }

        /// <summary>
        ///     Case-insensitive containment check for a keyword id in the sequence.
        ///     在序列中对 keyword id 执行大小写不敏感的包含检查。
        /// </summary>
        public static bool ContainsModKeyword(this IEnumerable<string> keywords, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(keywords);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            return keywords.Any(id =>
                string.Equals(id?.Trim(), keywordId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Maps each non-empty keyword id to a registered <see cref="IHoverTip" /> when
        ///     <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> is true.
        ///     当 <see cref="ModKeywordDefinition.IncludeInCardHoverTip" /> 为 true 时，将每个非空 keyword id
        ///     映射到已注册的 <see cref="IHoverTip" />。
        /// </summary>
        public static IEnumerable<IHoverTip> ToHoverTips(this IEnumerable<string> keywords)
        {
            ArgumentNullException.ThrowIfNull(keywords);

            var tips = new List<IHoverTip>();
            foreach (var id in keywords
                         .Where(static id => !string.IsNullOrWhiteSpace(id))
                         .Select(static id => id.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!ModKeywordRegistry.TryResolveCardKeyword(id, out var value) || value == CardKeyword.None)
                    continue;

                if (ModKeywordRegistry.TryGetByCardKeyword(value, out var def))
                {
                    if (def.IncludeInCardHoverTip)
                        tips.Add(ModKeywordRegistry.CreateHoverTip(def.Id));
                    continue;
                }

                tips.Add(HoverTipFactory.FromKeyword(value));
            }

            return tips;
        }

        /// <summary>
        ///     Card BBCode for the extended keyword id string via <see cref="ModKeywordRegistry.GetCardText" />.
        ///     通过 <see cref="ModKeywordRegistry.GetCardText" /> 获取扩展关键词 id 字符串对应的卡牌 BBCode。
        /// </summary>
        public static string GetModKeywordCardText(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardText(keywordId);
        }

        /// <summary>
        ///     Convenience: minted <see cref="CardKeyword" /> value for <paramref name="keywordId" />, intended
        ///     for call sites that want to use the native vanilla keyword API directly
        ///     (<c>card.AddKeyword(id.GetModCardKeyword())</c>).
        ///     （<c>card.AddKeyword(id.GetModCardKeyword())</c>）。
        ///     便捷方法：返回 <paramref name="keywordId" /> 的 minted <see cref="CardKeyword" /> 值，供
        ///     希望直接使用 native 原版 keyword API 的调用点使用
        ///     （<c>card.AddKeyword(id.GetModCardKeyword())</c>）。
        ///     （<c>card.AddKeyword(id.GetModCardKeyword())</c>）。
        /// </summary>
        public static CardKeyword GetModCardKeyword(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardKeyword(keywordId);
        }

        /// <summary>
        ///     Compatibility alias for <see cref="GetModCardKeyword" />.
        ///     <see cref="GetModCardKeyword" /> 的兼容别名。
        /// </summary>
        public static CardKeyword GetModKeywordCardKeyword(this string keywordId)
        {
            return keywordId.GetModCardKeyword();
        }

        /// <summary>
        ///     Tries to reverse-map a minted mod <see cref="CardKeyword" /> value to its registered string id.
        ///     尝试将 minted mod <see cref="CardKeyword" /> 值反向映射到其已注册字符串 id。
        /// </summary>
        public static bool TryGetModKeywordId(this CardKeyword value, out string id)
        {
            return ModKeywordRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     Reverse-maps a minted mod <see cref="CardKeyword" /> value to its registered string id.
        ///     将 minted mod <see cref="CardKeyword" /> 值反向映射到其已注册字符串 id。
        /// </summary>
        public static string GetModKeywordId(this CardKeyword value)
        {
            return ModKeywordRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"CardKeyword '0x{(int)value:X8}' is not a registered mod keyword.");
        }
    }
}
