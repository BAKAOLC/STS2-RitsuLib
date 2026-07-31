using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines optional predicates for the game's gold and red card-glow channels.
    ///     </para>
    ///     <para xml:lang="zh-CN">定义游戏金色与红色卡牌发光通道使用的可选谓词。</para>
    /// </summary>
    public readonly record struct ModCardHandGlowRules
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Predicate that contributes to <see cref="CardModel.ShouldGlowGold" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         参与计算 <see cref="CardModel.ShouldGlowGold" /> 的谓词。
        ///     </para>
        /// </summary>
        public Func<CardModel, bool>? GoldWhenBonusActive { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Predicate that contributes to <see cref="CardModel.ShouldGlowRed" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         参与计算 <see cref="CardModel.ShouldGlowRed" /> 的谓词。
        ///     </para>
        /// </summary>
        public Func<CardModel, bool>? RedWhenHandWarning { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set with only a gold-glow predicate.</para>
        ///     <para xml:lang="zh-CN">创建仅包含金色发光谓词的规则集。</para>
        /// </summary>
        public static ModCardHandGlowRules Gold(Func<CardModel, bool> whenBonusActive)
        {
            ArgumentNullException.ThrowIfNull(whenBonusActive);
            return new() { GoldWhenBonusActive = whenBonusActive };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set with only a red-glow predicate.</para>
        ///     <para xml:lang="zh-CN">创建仅包含红色发光谓词的规则集。</para>
        /// </summary>
        public static ModCardHandGlowRules Red(Func<CardModel, bool> whenHandWarning)
        {
            ArgumentNullException.ThrowIfNull(whenHandWarning);
            return new() { RedWhenHandWarning = whenHandWarning };
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a rule set for both glow channels.</para>
        ///     <para xml:lang="zh-CN">创建同时包含两个发光通道的规则集。</para>
        /// </summary>
        public static ModCardHandGlowRules GoldAndRed(
            Func<CardModel, bool>? goldWhenBonusActive,
            Func<CardModel, bool>? redWhenHandWarning)
        {
            return new()
            {
                GoldWhenBonusActive = goldWhenBonusActive,
                RedWhenHandWarning = redWhenHandWarning,
            };
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a rule set that combines corresponding predicates with logical OR.
        ///     </para>
        ///     <para xml:lang="zh-CN">返回将对应通道谓词按逻辑或组合后的规则集。</para>
        /// </summary>
        public ModCardHandGlowRules Or(ModCardHandGlowRules other)
        {
            return new()
            {
                GoldWhenBonusActive = CombineOr(GoldWhenBonusActive, other.GoldWhenBonusActive),
                RedWhenHandWarning = CombineOr(RedWhenHandWarning, other.RedWhenHandWarning),
            };
        }

        private static Func<CardModel, bool>? CombineOr(Func<CardModel, bool>? a, Func<CardModel, bool>? b)
        {
            if (a == null)
                return b;
            return b == null ? a : c => a(c) || b(c);
        }
    }
}
