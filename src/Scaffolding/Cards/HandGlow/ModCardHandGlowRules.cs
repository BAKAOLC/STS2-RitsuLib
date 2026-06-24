using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Cards.HandGlow
{
    /// <summary>
    ///     Declarative hand-highlight rules that mirror vanilla <see cref="CardModel.ShouldGlowGold" /> (gold border when a
    ///     bonus / stronger line is active while the card is playable) and <see cref="CardModel.ShouldGlowRed" /> (red border
    ///     for warning states such as companion missing). Applied either by overriding the protected
    ///     <c>ShouldGlow*Internal</c>
    ///     members, or by registering with <see cref="ModCardHandGlowRegistry" /> /
    ///     <see cref="STS2RitsuLib.Content.ModContentRegistry" /> (see
    ///     <c>RegisterCardHandGlow&lt;TCard&gt;</c> on the content registry).
    ///     <see cref="ModCardHandGlowRegistry" /> / <see cref="STS2RitsuLib.Content.ModContentRegistry" />
    ///     声明式手牌高亮规则，镜像原版 <see cref="CardModel.ShouldGlowGold" />（卡牌可打出且
    ///     奖励/更强效果行生效时显示金色边框）和 <see cref="CardModel.ShouldGlowRed" />（用于伙伴缺失等警告状态的红色边框）。
    ///     可通过 override 受保护的 <c>ShouldGlow*Internal</c>
    ///     成员应用，也可通过 <see cref="ModCardHandGlowRegistry" /> /
    ///     <see cref="STS2RitsuLib.Content.ModContentRegistry" /> 注册（见内容注册表上的
    ///     <c>RegisterCardHandGlow&lt;TCard&gt;</c>）。
    /// </summary>
    public readonly record struct ModCardHandGlowRules
    {
        /// <summary>
        ///     When this returns true for a card instance, the hand UI may show the gold highlight (same channel as
        ///     <see cref="CardModel.ShouldGlowGoldInternal" />).
        ///     当此项对某个卡牌实例返回 true 时，手牌 UI 可显示金色高亮（与
        ///     <see cref="CardModel.ShouldGlowGoldInternal" /> 相同通道）。
        /// </summary>
        public Func<CardModel, bool>? GoldWhenBonusActive { get; init; }

        /// <summary>
        ///     When this returns true, the hand UI may show the red highlight (same channel as
        ///     <see cref="CardModel.ShouldGlowRedInternal" />).
        ///     当此项返回 true 时，手牌 UI 可显示红色高亮（与
        ///     <see cref="CardModel.ShouldGlowRedInternal" /> 相同通道）。
        /// </summary>
        public Func<CardModel, bool>? RedWhenHandWarning { get; init; }

        /// <summary>
        ///     Gold only (e.g. Evil Eye–style “stronger effect active”).
        ///     仅金色（例如 Evil Eye 风格的“更强效果生效”）。
        /// </summary>
        public static ModCardHandGlowRules Gold(Func<CardModel, bool> whenBonusActive)
        {
            return new() { GoldWhenBonusActive = whenBonusActive };
        }

        /// <summary>
        ///     Red only (e.g. Osty-missing attack cards).
        ///     仅红色（例如 Osty 缺失的攻击卡牌）。
        /// </summary>
        public static ModCardHandGlowRules Red(Func<CardModel, bool> whenHandWarning)
        {
            return new() { RedWhenHandWarning = whenHandWarning };
        }

        /// <summary>
        ///     Both channels in one rule set.
        ///     在一个规则集中同时包含两个通道。
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
        ///     Merges with <paramref name="other" /> by OR-ing each channel (useful when multiple mods register the same
        ///     card type, or you split rules across calls).
        ///     通过对每个通道执行 OR 与 <paramref name="other" /> 合并（多个 mod 注册同一
        ///     卡牌类型，或你将规则拆分到多次调用时很有用）。
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
