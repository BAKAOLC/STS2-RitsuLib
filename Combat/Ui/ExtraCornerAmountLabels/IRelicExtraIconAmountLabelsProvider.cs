namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.Models.RelicModel" /> subclasses to render additional badges
    ///     on <see cref="MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder" />.
    /// </summary>
    public interface IRelicExtraIconAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots();
    }

    /// <summary>
    ///     Optional invalidation when only relic extra slots change without
    ///     <see cref="MegaCrit.Sts2.Core.Models.RelicModel.DisplayAmountChanged" />.
    /// </summary>
    public interface IRelicExtraIconAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IRelicExtraIconAmountLabelsProvider.GetRelicExtraIconAmountLabelSlots" /> may
        ///     have changed.
        /// </summary>
        event Action? RelicExtraIconAmountLabelsInvalidated;
    }
}
