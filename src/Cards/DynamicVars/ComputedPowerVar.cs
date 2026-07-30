using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">Represents a <see cref="PowerVar{T}" /> whose displayed amount is computed by delegates.</para>
    ///     <para xml:lang="zh-CN">表示显示层数由委托计算的 <see cref="PowerVar{T}" />。</para>
    /// </summary>
    public sealed class ComputedPowerVar<T> : PowerVar<T>, IComputedDynamicVar where T : PowerModel
    {
        private readonly ComputedDynamicVarEvaluator _evaluator;

        /// <summary>
        ///     <para xml:lang="en">Creates a computed power variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名的计算型能力层数变量。</para>
        /// </summary>
        public ComputedPowerVar(
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : this(typeof(T).Name, baseValue, currentValueFactory, previewBaseValueFactory)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named computed power variable with optional preview-specific evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可指定预览求值逻辑的具名计算型能力层数变量。</para>
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
        ///     <para xml:lang="en">Creates a target-aware computed power variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名且支持目标感知求值的计算型能力层数变量。</para>
        /// </summary>
        public ComputedPowerVar(
            decimal baseValue,
            Func<CardModel?, Creature?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewBaseValueFactory = null)
            : this(typeof(T).Name, baseValue, currentValueFactory, previewBaseValueFactory)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named, target-aware computed power variable.</para>
        ///     <para xml:lang="zh-CN">创建具名且支持目标感知求值的计算型能力层数变量。</para>
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

            _evaluator = new(currentValueFactory, previewBaseValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed power variable named after <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh-CN">创建以 <typeparamref name="T" /> 命名的上下文感知计算型能力层数变量。</para>
        /// </summary>
        public ComputedPowerVar(
            ComputedDynamicVarFactory contextFactory,
            decimal baseValue = 0m)
            : this(typeof(T).Name, baseValue, contextFactory)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a named, context-aware computed power variable.</para>
        ///     <para xml:lang="zh-CN">创建具名的上下文感知计算型能力层数变量。</para>
        /// </summary>
        public ComputedPowerVar(
            string name,
            ComputedDynamicVarFactory contextFactory,
            decimal baseValue = 0m)
            : this(name, baseValue, contextFactory)
        {
        }

        internal ComputedPowerVar(
            string name,
            decimal baseValue,
            ComputedDynamicVarFactory contextFactory)
            : base(name, baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(contextFactory);

            _evaluator = new(contextFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Computes the power amount for the owning card, if any, and <paramref name="target" />.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）和 <paramref name="target" /> 对应的能力层数。</para>
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _evaluator.Calculate(this, _owner, target);
        }

        /// <summary>
        ///     <para xml:lang="en">Computes the power amount for the owning card, if any, without a target.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）在没有目标时的能力层数。</para>
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
            PreviewValue = _evaluator.CalculatePreview(
                this,
                _owner,
                card,
                previewMode,
                target,
                runGlobalHooks);
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
