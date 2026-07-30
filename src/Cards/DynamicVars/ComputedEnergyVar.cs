using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">Represents an <see cref="EnergyVar" /> whose displayed icon count is computed by delegates.</para>
    ///     <para xml:lang="zh-CN">表示显示图标数量由委托计算的 <see cref="EnergyVar" />。</para>
    /// </summary>
    public sealed class ComputedEnergyVar : EnergyVar, IComputedDynamicVar
    {
        private readonly ComputedDynamicVarEvaluator _evaluator;

        /// <summary>
        ///     <para xml:lang="en">Creates a computed energy variable with optional preview-specific evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可指定预览求值逻辑的计算型能量变量。</para>
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
        ///     <para xml:lang="en">Creates a target-aware computed energy variable.</para>
        ///     <para xml:lang="zh-CN">创建支持目标感知求值的计算型能量变量。</para>
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
            _evaluator = new(currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a context-aware computed energy variable.</para>
        ///     <para xml:lang="zh-CN">创建上下文感知的计算型能量变量。</para>
        /// </summary>
        public ComputedEnergyVar(
            string name,
            ComputedDynamicVarFactory contextFactory,
            decimal baseValue = 0m)
            : this(name, baseValue, contextFactory)
        {
        }

        internal ComputedEnergyVar(
            string name,
            decimal baseValue,
            ComputedDynamicVarFactory contextFactory)
            : base(name, (int)baseValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(contextFactory);

            BaseValue = baseValue;
            _evaluator = new(contextFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Computes the icon count for the owning card, if any, and <paramref name="target" />.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）和 <paramref name="target" /> 对应的图标数量。</para>
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _evaluator.Calculate(this, _owner, target);
        }

        /// <summary>
        ///     <para xml:lang="en">Computes the icon count for the owning card, if any, without a target.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）在没有目标时的图标数量。</para>
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
