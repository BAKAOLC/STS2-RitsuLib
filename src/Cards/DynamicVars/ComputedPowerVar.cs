using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <see cref="PowerVar{T}" /> whose displayed amount is produced by delegates.
    ///     由委托生成显示层数的 <see cref="PowerVar{T}" />。
    /// </summary>
    public sealed class ComputedPowerVar<T> : PowerVar<T> where T : PowerModel
    {
        private readonly Func<CardModel?, Creature?, decimal> _currentValueFactory;
        private readonly Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? _previewBaseValueFactory;

        /// <summary>
        ///     Creates a computed power variable named after <typeparamref name="T" />.
        ///     创建以 <typeparamref name="T" /> 命名的计算型能力层数变量。
        /// </summary>
        public ComputedPowerVar(
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : this(typeof(T).Name, baseValue, currentValueFactory, previewBaseValueFactory)
        {
        }

        /// <summary>
        ///     Creates a computed power variable with optional preview-specific base amount logic.
        ///     创建带可选预览专用基础层数逻辑的计算型能力层数变量。
        /// </summary>
        public ComputedPowerVar(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : this(name, baseValue, (card, _) => currentValueFactory(card), previewBaseValueFactory)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
        }

        /// <summary>
        ///     Creates a target-aware computed power variable named after <typeparamref name="T" />.
        ///     创建以 <typeparamref name="T" /> 命名、支持目标感知求值的计算型能力层数变量。
        /// </summary>
        public ComputedPowerVar(
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : this(typeof(T).Name, baseValue, currentValueFactory, previewBaseValueFactory)
        {
        }

        /// <summary>
        ///     Creates a target-aware computed power variable.
        ///     创建支持目标感知求值的计算型能力层数变量。
        /// </summary>
        public ComputedPowerVar(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            _currentValueFactory = currentValueFactory;
            _previewBaseValueFactory = previewBaseValueFactory;
        }

        /// <summary>
        ///     Computes the live power amount for the current owner and target.
        ///     计算当前拥有者和目标对应的实时能力层数。
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _currentValueFactory(_owner as CardModel, target);
        }

        /// <summary>
        ///     Computes the live power amount for the current owner.
        ///     计算当前拥有者对应的实时能力层数。
        /// </summary>
        public decimal Calculate()
        {
            return Calculate(null);
        }

        /// <inheritdoc />
        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            var value = _previewBaseValueFactory?.Invoke(card, previewMode, target, runGlobalHooks)
                        ?? _currentValueFactory(card, target);

            PreviewValue = value;
        }

        /// <inheritdoc />
        protected override decimal GetBaseValueForIConvertible()
        {
            return Calculate(null);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
            return Calculate(null).ToString();
        }
    }
}
