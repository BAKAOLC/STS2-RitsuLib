using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides helpers for applying runtime keywords and creating their hover tips. Operations on
    ///         <see cref="CardModel" /> use deterministic <see cref="CardKeyword" /> values directly in the native
    ///         <c>CardModel.Keywords</c> set, so native add, remove, clone, and canonical-seeding paths carry mod
    ///         keywords without parallel state. Non-card objects use an ad hoc
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" /> store that is neither cloned nor persisted.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供应用运行时关键词和创建对应悬停提示的辅助方法。对 <see cref="CardModel" /> 的操作会使用确定性的
    ///         <see cref="CardKeyword" /> 值直接读写原版 <c>CardModel.Keywords</c> 集合，因此原版的添加、移除、
    ///         克隆和初始关键词填充流程均可携带模组关键词，无需并行状态。非卡牌对象使用临时的
    ///         <see cref="ConditionalWeakTable{TKey,TValue}" /> 存储，不会随对象克隆或写入存档。
    ///     </para>
    /// </summary>
    public static class ModKeywordExtensions
    {
        private static readonly Lock SyncRoot = new();
        private static readonly ConditionalWeakTable<object, HashSet<string>> FallbackKeywords = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Adds a runtime keyword ID to <paramref name="target" />, using case-insensitive deduplication. For a
        ///         <see cref="CardModel" />, the deterministic <see cref="CardKeyword" /> value is added to the native
        ///         keyword set. The ID does not need to be registered, but only registered IDs provide metadata.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         向 <paramref name="target" /> 添加运行时关键词 ID，并以不区分大小写的方式去重。目标为
        ///         <see cref="CardModel" /> 时，会将确定性生成的 <see cref="CardKeyword" /> 值加入原版关键词集合。
        ///         ID 无需预先注册，但只有已注册 ID 才能提供元数据。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Adds a pre-minted mod <see cref="CardKeyword" /> value to the native card keyword set. The set is
        ///         materialized through its native getter before <see cref="CardModel.AddKeyword" /> runs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将预先生成的模组 <see cref="CardKeyword" /> 值加入原版卡牌关键词集合。调用
        ///         <see cref="CardModel.AddKeyword" /> 前会先通过原版 getter 创建该集合。
        ///     </para>
        /// </summary>
        public static void AddModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            _ = card.Keywords;
            card.AddKeyword(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Removes a previously added runtime keyword ID. For a <see cref="CardModel" />, the corresponding
        ///         deterministic value is removed from the native keyword set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         移除先前添加的运行时关键词 ID。目标为 <see cref="CardModel" /> 时，会从原版关键词集合中移除
        ///         对应的确定性枚举值。
        ///     </para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when the ID was present and removed; otherwise
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         该 ID 原本存在并已移除时返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Removes <paramref name="value" /> from the native card keyword set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从原版卡牌关键词集合中移除 <paramref name="value" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="target" /> currently contains the specified runtime keyword ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="target" /> 当前是否包含指定的运行时关键词 ID。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="card" /> currently contains <paramref name="value" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="card" /> 当前是否包含 <paramref name="value" />。
        ///     </para>
        /// </summary>
        public static bool HasModKeyword(this CardModel card, CardKeyword value)
        {
            ArgumentNullException.ThrowIfNull(card);
            return card.Keywords.Contains(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the target's effective runtime mod keyword IDs in stable order. For a
        ///         <see cref="CardModel" />, this reverse-maps registered minted values from the native keyword set and
        ///         skips native or unregistered values.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         按确定性顺序返回目标当前生效的运行时模组关键词 ID。目标为 <see cref="CardModel" /> 时，此方法
        ///         会从原版关键词集合反向映射已注册的动态值，并忽略原版值和未注册值。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Returns hover tips for all effective runtime keyword IDs on <paramref name="target" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="target" /> 上所有生效运行时关键词 ID 对应的悬停提示。
        ///     </para>
        /// </summary>
        public static IEnumerable<IHoverTip> GetModKeywordHoverTips(this object target)
        {
            ArgumentNullException.ThrowIfNull(target);
            return target.GetModKeywordIds().ToHoverTips();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Checks whether the sequence contains <paramref name="keywordId" />, ignoring case.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         检查序列是否包含 <paramref name="keywordId" />；比较时不区分大小写。
        ///     </para>
        /// </summary>
        public static bool ContainsModKeyword(this IEnumerable<string> keywords, string keywordId)
        {
            ArgumentNullException.ThrowIfNull(keywords);
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);

            return keywords.Any(id =>
                string.Equals(id?.Trim(), keywordId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Converts each distinct, non-empty keyword ID into an <see cref="IHoverTip" />. Registered mod
        ///         keywords honor <see cref="ModKeywordDefinition.IncludeInCardHoverTip" />; other resolvable values use
        ///         the native hover-tip factory.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将每个不重复的非空关键词 ID 转换为 <see cref="IHoverTip" />。已注册模组关键词会遵循
        ///         <see cref="ModKeywordDefinition.IncludeInCardHoverTip" />；其他可解析值使用原版悬停提示工厂。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Gets the registered keyword ID's inline card BBCode.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册关键词 ID 对应的内联卡牌 BBCode。
        ///     </para>
        /// </summary>
        public static string GetModKeywordCardText(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardText(keywordId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod <see cref="CardKeyword" /> value's inline card BBCode.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组 <see cref="CardKeyword" /> 值对应的内联卡牌 BBCode。
        ///     </para>
        /// </summary>
        public static string GetModKeywordCardText(this CardKeyword value)
        {
            return ModKeywordRegistry.GetCardText(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod <see cref="CardKeyword" /> value's title as a <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组 <see cref="CardKeyword" /> 值的标题 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetModKeywordTitle(this CardKeyword value)
        {
            return ModKeywordRegistry.GetTitle(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the registered mod <see cref="CardKeyword" /> value's description as a
        ///         <see cref="LocString" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取已注册模组 <see cref="CardKeyword" /> 值的描述 <see cref="LocString" />。
        ///     </para>
        /// </summary>
        public static LocString GetModKeywordDescription(this CardKeyword value)
        {
            return ModKeywordRegistry.GetDescription(value);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the deterministic <see cref="CardKeyword" /> value for <paramref name="keywordId" /> for use
        ///         with native keyword APIs.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 <paramref name="keywordId" /> 对应的确定性 <see cref="CardKeyword" /> 值，供原版关键词 API
        ///         使用。
        ///     </para>
        /// </summary>
        public static CardKeyword GetModCardKeyword(this string keywordId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keywordId);
            return ModKeywordRegistry.GetCardKeyword(keywordId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Provides a compatibility alias for <see cref="GetModCardKeyword" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         提供 <see cref="GetModCardKeyword" /> 的兼容别名。
        ///     </para>
        /// </summary>
        public static CardKeyword GetModKeywordCardKeyword(this string keywordId)
        {
            return keywordId.GetModCardKeyword();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Tries to reverse-map a minted mod <see cref="CardKeyword" /> value to its registered string ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试将动态生成的模组 <see cref="CardKeyword" /> 值反向映射为已注册的字符串 ID。
        ///     </para>
        /// </summary>
        public static bool TryGetModKeywordId(this CardKeyword value, out string id)
        {
            return ModKeywordRegistry.TryGetId(value, out id);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Reverse-maps a minted mod <see cref="CardKeyword" /> value to its registered string ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将动态生成的模组 <see cref="CardKeyword" /> 值反向映射为已注册的字符串 ID。
        ///     </para>
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        ///     <para xml:lang="en">
        ///         The value is not a registered mod keyword.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         该值不是已注册的模组关键词。
        ///     </para>
        /// </exception>
        public static string GetModKeywordId(this CardKeyword value)
        {
            return ModKeywordRegistry.TryGetId(value, out var id)
                ? id
                : throw new KeyNotFoundException($"CardKeyword '0x{(int)value:X8}' is not a registered mod keyword.");
        }
    }
}
