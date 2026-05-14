using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     Factory helpers for common mod card <see cref="DynamicVar" /> shapes.
    ///     常见 mod 卡牌 <see cref="DynamicVar" /> 形态的工厂辅助方法。
    /// </summary>
    public static class ModCardVars
    {
        /// <summary>
        ///     Creates an integer-backed dynamic var named <paramref name="name" /> with amount
        ///     <paramref name="amount" />.
        ///     创建名为 <paramref name="name" />、数值为 <paramref name="amount" /> 的整数型动态变量。
        /// </summary>
        public static IntVar Int(string name, decimal amount)
        {
            return new(name, amount);
        }

        /// <summary>
        ///     Creates a string dynamic var named <paramref name="name" />.
        ///     创建名为 <paramref name="name" /> 的字符串动态变量。
        /// </summary>
        public static StringVar String(string name, string value = "")
        {
            return new(name, value);
        }

        /// <summary>
        ///     Creates a <see cref="ComputedDynamicVar" /> with optional preview-specific computation.
        ///     创建带可选预览专用计算的 <see cref="ComputedDynamicVar" />。
        /// </summary>
        public static ComputedDynamicVar Computed(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     Creates a <see cref="ComputedDynamicVar" /> with target-aware computation.
        ///     创建支持目标感知计算的 <see cref="ComputedDynamicVar" />。
        /// </summary>
        public static ComputedDynamicVar Computed(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
        {
            return new(name, baseValue, currentValueFactory, previewValueFactory);
        }
    }
}
