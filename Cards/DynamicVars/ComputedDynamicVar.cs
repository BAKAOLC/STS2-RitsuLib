using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Cards.DynamicVars
{
    /// <summary>
    ///     <see cref="DynamicVar" /> whose displayed value is produced by delegates instead of a fixed base amount.
    /// </summary>
    public sealed class ComputedDynamicVar : DynamicVar
    {
        private readonly Func<CardModel?, decimal> _currentValueFactory;
        private readonly Func<CardModel?, CardPreviewMode, Creature?, bool, decimal>? _previewValueFactory;

        /// <summary>
        ///     Creates a computed variable with optional preview-specific logic.
        /// </summary>
        /// <param name="name">
        ///     Dynamic var key.
        /// </param>
        /// <param name="baseValue">
        ///     Fallback numeric base when no preview override applies.
        /// </param>
        /// <param name="currentValueFactory">
        ///     Resolves the live value from the owning <see cref="CardModel" /> (may be null outside card context).
        /// </param>
        /// <param name="previewValueFactory">
        ///     Optional override used during card preview; when null, <paramref name="currentValueFactory" /> is used.
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

            _currentValueFactory = currentValueFactory;
            _previewValueFactory = previewValueFactory;
        }

        /// <inheritdoc />
        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            PreviewValue = _previewValueFactory?.Invoke(card, previewMode, target, runGlobalHooks)
                           ?? _currentValueFactory(card);
        }

        /// <inheritdoc />
        protected override decimal GetBaseValueForIConvertible()
        {
            return _currentValueFactory(_owner as CardModel);
        }
    }
}
