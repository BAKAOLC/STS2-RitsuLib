using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <para xml:lang="en">Represents a <see cref="DynamicVar" /> whose displayed value is computed by delegates.</para>
    ///     <para xml:lang="zh-CN">表示显示值由委托计算的 <see cref="DynamicVar" />。</para>
    /// </summary>
    public sealed class ComputedDynamicVar : DynamicVar, IComputedDynamicVar
    {
        private readonly ComputedDynamicVarEvaluator _evaluator;

        /// <summary>
        ///     <para xml:lang="en">Creates a computed variable with optional preview-specific evaluation.</para>
        ///     <para xml:lang="zh-CN">创建可指定预览求值逻辑的计算变量。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">Dynamic-variable key.</para>
        ///     <para xml:lang="zh-CN">动态变量键。</para>
        /// </param>
        /// <param name="baseValue">
        ///     <para xml:lang="en">
        ///         Initial stored base value. The evaluator does not return it automatically.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始存储的基础值；求值器不会自动返回此值。</para>
        /// </param>
        /// <param name="currentValueFactory">
        ///     <para xml:lang="en">Computes the current value from the owning card, which may be <see langword="null" />.</para>
        ///     <para xml:lang="zh-CN">根据所属卡牌计算当前值；卡牌可能为 <see langword="null" />。</para>
        /// </param>
        /// <param name="previewValueFactory">
        ///     <para xml:lang="en">Optional preview evaluator; when omitted, <paramref name="currentValueFactory" /> is used.</para>
        ///     <para xml:lang="zh-CN">可选的预览求值器；省略时使用 <paramref name="currentValueFactory" />。</para>
        /// </param>
        public ComputedDynamicVar(
            string name,
            decimal baseValue,
            Func<CardModel?, decimal> currentValueFactory,
            Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? previewValueFactory = null)
            : this(name, baseValue, (card, _) => currentValueFactory(card), previewValueFactory)
        {
            ArgumentNullException.ThrowIfNull(currentValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed variable with target-aware evaluation.</para>
        ///     <para xml:lang="zh-CN">创建支持目标感知求值的计算变量。</para>
        /// </summary>
        /// <param name="name">
        ///     <para xml:lang="en">Dynamic-variable key.</para>
        ///     <para xml:lang="zh-CN">动态变量键。</para>
        /// </param>
        /// <param name="baseValue">
        ///     <para xml:lang="en">
        ///         Initial stored base value. The evaluator does not return it automatically.
        ///     </para>
        ///     <para xml:lang="zh-CN">初始存储的基础值；求值器不会自动返回此值。</para>
        /// </param>
        /// <param name="currentValueFactory">
        ///     <para xml:lang="en">Computes the current value from the owning card and current target.</para>
        ///     <para xml:lang="zh-CN">根据所属卡牌和当前目标计算当前值。</para>
        /// </param>
        /// <param name="previewValueFactory">
        ///     <para xml:lang="en">Optional preview evaluator; when omitted, <paramref name="currentValueFactory" /> is used.</para>
        ///     <para xml:lang="zh-CN">可选的预览求值器；省略时使用 <paramref name="currentValueFactory" />。</para>
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

            _evaluator = new(currentValueFactory, previewValueFactory);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a computed variable from a context-aware evaluator.</para>
        ///     <para xml:lang="zh-CN">使用上下文感知求值器创建计算变量。</para>
        /// </summary>
        public ComputedDynamicVar(
            string name,
            ComputedDynamicVarFactory contextFactory,
            decimal baseValue = 0m)
            : this(name, baseValue, contextFactory)
        {
        }

        internal ComputedDynamicVar(
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
        ///     <para xml:lang="en">Computes the value for the owning card, if any, and <paramref name="target" />.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）和 <paramref name="target" /> 对应的值。</para>
        /// </summary>
        public decimal Calculate(Creature? target)
        {
            return _evaluator.Calculate(this, _owner, target);
        }

        /// <summary>
        ///     <para xml:lang="en">Computes the value for the owning card, if any, without a target.</para>
        ///     <para xml:lang="zh-CN">计算当前所属卡牌（若有）在没有目标时的值。</para>
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

        /// <summary>
        ///     <para xml:lang="en">Returns the computed value as a string.</para>
        ///     <para xml:lang="zh-CN">以字符串形式返回计算值。</para>
        /// </summary>
        public override string ToString()
        {
            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
            return Calculate(null).ToString();
        }
    }
}
