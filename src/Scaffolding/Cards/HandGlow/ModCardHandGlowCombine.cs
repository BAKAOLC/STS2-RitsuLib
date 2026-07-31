using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     <para xml:lang="en">Combines <see cref="ModCardHandGlowRules" /> predicates.</para>
    ///     <para xml:lang="zh-CN">组合 <see cref="ModCardHandGlowRules" /> 使用的谓词。</para>
    /// </summary>
    public static class ModCardHandGlowCombine
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the logical OR of all non-<see langword="null" /> predicates, or a predicate that returns
        ///         <see langword="false" /> when none are supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有非 <see langword="null" /> 谓词的逻辑或；未提供此类谓词时，所得谓词返回
        ///         <see langword="false" />。
        ///     </para>
        /// </summary>
        public static Func<CardModel, bool> Or(params Func<CardModel, bool>?[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);
            var filtered = parts.OfType<Func<CardModel, bool>>().ToArray();
            return filtered.Length == 0
                ? static _ => false
                : card => filtered.Any(predicate => predicate(card));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns the logical AND of all non-<see langword="null" /> predicates, or a predicate that returns
        ///         <see langword="true" /> when none are supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回所有非 <see langword="null" /> 谓词的逻辑与；未提供此类谓词时，所得谓词返回
        ///         <see langword="true" />。
        ///     </para>
        /// </summary>
        public static Func<CardModel, bool> And(params Func<CardModel, bool>?[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);
            var filtered = parts.Where(static p => p != null).Cast<Func<CardModel, bool>>().ToArray();
            if (filtered.Length == 0)
                return static _ => true;

            return card => filtered.All(p => p(card));
        }
    }
}
