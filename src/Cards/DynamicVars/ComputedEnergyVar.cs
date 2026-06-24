using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <see cref="EnergyVar" /> whose displayed icon count is produced by delegates.
    ///     由委托生成显示图标数量的 <see cref="EnergyVar" />。
    /// </summary>
    public sealed class ComputedEnergyVar : EnergyVar
    {
        private readonly Func<CardModel?, Creature?, decimal> _currentValueFactory;
        private readonly Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? _previewValueFactory;

        /// <summary>
        ///     Creates a computed energy variable with optional preview-specific logic.
        ///     创建带可选预览专用逻辑的计算型能量变量。
        /// </summary>
        public ComputedEnergyVar(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
            : this(name, baseValue, (card, _) => currentValueFactory(card), previewValueFactory)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
        }

        /// <summary>
        ///     Creates a target-aware computed energy variable.
        ///     创建支持目标感知求值的计算型能量变量。
        /// </summary>
        public ComputedEnergyVar(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
            : base(name, (int)baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            BaseValue = baseValue;
            _currentValueFactory = currentValueFactory;
            _previewValueFactory = previewValueFactory;
        }

        /// <summary>
        ///     Computes the live icon count for the current owner and target.
        ///     计算当前拥有者和目标对应的实时图标数量。
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _currentValueFactory(_owner as CardModel, target);
        }

        /// <summary>
        ///     Computes the live icon count for the current owner.
        ///     计算当前拥有者对应的实时图标数量。
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
            PreviewValue = _previewValueFactory?.Invoke(card, previewMode, target, runGlobalHooks)
                           ?? _currentValueFactory(card, target);
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
