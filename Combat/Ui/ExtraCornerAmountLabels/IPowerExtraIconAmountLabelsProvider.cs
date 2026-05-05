namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> subclasses to render additional
    ///     numeric/text badges on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" /> (separate from the vanilla
    ///     counter label).
    /// </summary>
    public interface IPowerExtraIconAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        ///     Order only affects z-order (later draws on top).
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots();
    }

    /// <summary>
    ///     Optional invalidation signal when only <see cref="IPowerExtraIconAmountLabelsProvider" /> slots change
    ///     without <see cref="MegaCrit.Sts2.Core.Models.PowerModel.DisplayAmountChanged" /> firing.
    /// </summary>
    public interface IPowerExtraIconAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IPowerExtraIconAmountLabelsProvider.GetPowerExtraIconAmountLabelSlots" /> may
        ///     have changed.
        /// </summary>
        event Action? PowerExtraIconAmountLabelsInvalidated;
    }
}
