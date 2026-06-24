using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <see cref="DynamicVar" /> whose displayed value is produced by delegates instead of a fixed base amount.
    ///     显示值由委托生成、而不是使用固定基础数值的 <see cref="DynamicVar" />。
    /// </summary>
    public sealed class ComputedDynamicVar : DynamicVar
    {
        private readonly Func<CardModel?, Creature?, decimal> _currentValueFactory;
        private readonly Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? _previewValueFactory;

        /// <summary>
        ///     Creates a computed variable with optional preview-specific logic.
        ///     创建带可选预览专用逻辑的计算变量。
        /// </summary>
        /// <param name="name">
        ///     Dynamic var key.
        ///     动态变量 key。
        /// </param>
        /// <param name="baseValue">
        ///     Fallback numeric base when no preview override applies.
        ///     没有预览覆盖时使用的后备基础数值。
        /// </param>
        /// <param name="currentValueFactory">
        ///     Resolves the live value from the owning <see cref="CardModel" /> (may be null outside card context).
        ///     从所属 <see cref="CardModel" /> 解析实时值（在卡牌上下文外可为 null）。
        /// </param>
        /// <param name="previewValueFactory">
        ///     Optional override used during card preview; when null, <paramref name="currentValueFactory" /> is used.
        ///     卡牌预览期间使用的可选覆盖；为 null 时使用 <paramref name="currentValueFactory" />。
        /// </param>
        public ComputedDynamicVar(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            _currentValueFactory = (card, _) => currentValueFactory(card);
            _previewValueFactory = previewValueFactory;
        }

        /// <summary>
        ///     Creates a computed variable with target-aware evaluation.
        ///     创建支持目标感知求值的计算变量。
        /// </summary>
        /// <param name="name">
        ///     Dynamic var key.
        ///     动态变量 key。
        /// </param>
        /// <param name="baseValue">
        ///     Fallback numeric base when no preview override applies.
        ///     没有预览覆盖时使用的后备基础数值。
        /// </param>
        /// <param name="currentValueFactory">
        ///     Resolves the live value from the owning <see cref="CardModel" /> and current target.
        ///     从所属 <see cref="CardModel" /> 和当前目标解析实时值。
        /// </param>
        /// <param name="previewValueFactory">
        ///     Optional override used during card preview; when null, <paramref name="currentValueFactory" /> is used.
        ///     卡牌预览期间使用的可选覆盖；为 null 时使用 <paramref name="currentValueFactory" />。
        /// </param>
        public ComputedDynamicVar(
            string name,
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(currentValueFactory);

            _currentValueFactory = currentValueFactory;
            _previewValueFactory = previewValueFactory;
        }

        /// <summary>
        ///     Computes the dynamic value for the current owner and target.
        ///     计算当前拥有者和目标对应的动态值。
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _currentValueFactory(_owner as CardModel, target);
        }

        /// <summary>
        ///     Computes the dynamic value for the current owner.
        ///     计算当前拥有者对应的动态值。
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

        /// <summary>
        ///     Returns the computed value as a string.
        ///     以字符串形式返回计算值。
        /// </summary>
        public override string ToString()
        {
            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
            return Calculate(null).ToString();
        }
    }
}
