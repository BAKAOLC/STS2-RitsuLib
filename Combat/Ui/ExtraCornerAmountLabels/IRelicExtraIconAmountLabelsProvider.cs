namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.Models.RelicModel" /> subclasses to render additional badges
    ///     on <see cref="MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder" />.
    ///     由 <see cref="MegaCrit.Sts2.Core.Models.RelicModel" /> 子类实现，用于在
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder" /> 上渲染额外徽标。
    ///     在 <c>MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder</c> 上。
    /// </summary>
    public interface IRelicExtraIconAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        ///     每个带有非空白 <see cref="ExtraIconAmountLabelSlot.Text" /> 的条目都会在
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" />（或 <see cref="ExtraIconAmountLabelCorner.Custom" /> 边界）处生成一个徽标。
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots();
    }

    /// <summary>
    ///     Optional invalidation when only relic extra slots change without
    ///     <see cref="MegaCrit.Sts2.Core.Models.RelicModel.DisplayAmountChanged" />.
    ///     可选的失效通知：仅遗物额外槽位发生变化，且没有
    ///     <see cref="MegaCrit.Sts2.Core.Models.RelicModel.DisplayAmountChanged" /> 时使用。
    /// </summary>
    public interface IRelicExtraIconAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IRelicExtraIconAmountLabelsProvider.GetRelicExtraIconAmountLabelSlots" /> may
        ///     have changed.
        ///     当 <see cref="IRelicExtraIconAmountLabelsProvider.GetRelicExtraIconAmountLabelSlots" /> 可能
        ///     已变化时引发。
        /// </summary>
        event Action? RelicExtraIconAmountLabelsInvalidated;
    }
}
