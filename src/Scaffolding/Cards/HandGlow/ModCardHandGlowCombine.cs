using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     Combinators for <see cref="ModCardHandGlowRules" /> predicates (<c>Func&lt;CardModel, bool&gt;</c>).
    ///     <see cref="ModCardHandGlowRules" /> 谓词（<c>Func&lt;CardModel, bool&gt;</c>）的组合器。
    /// </summary>
    public static class ModCardHandGlowCombine
    {
        /// <summary>
        ///     Logical OR of any non-null predicates.
        ///     任意非 null 谓词的逻辑 OR。
        /// </summary>
        public static Func<CardModel, bool> Or(params Func<CardModel, bool>?[] parts)
        {
            return card => { return parts.OfType<Func<CardModel, bool>>().Any(p => p(card)); };
        }

        /// <summary>
        ///     Logical AND of any non-null predicates; if all parts are null, returns <c>_ => true</c>.
        ///     对任意非 null 谓词做逻辑与；如果所有部分均为 null，则返回 <c>_ => true</c>。
        /// </summary>
        public static Func<CardModel, bool> And(params Func<CardModel, bool>?[] parts)
        {
            var filtered = parts.Where(static p => p != null).Cast<Func<CardModel, bool>>().ToArray();
            if (filtered.Length == 0)
                return static _ => true;

            return card => filtered.All(p => p(card));
        }
    }
}
