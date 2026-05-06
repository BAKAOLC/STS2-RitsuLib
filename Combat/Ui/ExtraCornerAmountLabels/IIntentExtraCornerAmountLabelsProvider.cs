namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent" /> subclasses to render
    ///     additional corner labels on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" />.
    /// </summary>
    public interface IIntentExtraCornerAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetIntentExtraCornerAmountLabelSlots();
    }

    /// <summary>
    ///     Optional invalidation when only intent extra slots change without
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent.UpdateVisuals" /> being driven by combat ticks.
    /// </summary>
    public interface IIntentExtraCornerAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IIntentExtraCornerAmountLabelsProvider.GetIntentExtraCornerAmountLabelSlots" />
        ///     may have changed.
        /// </summary>
        event Action? IntentExtraCornerAmountLabelsInvalidated;
    }
}
