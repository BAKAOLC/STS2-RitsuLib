namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent" /> subclasses to render
    ///     additional corner labels on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" />.
    ///     由 <see cref="MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent" /> 子类实现，用于在
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent" /> 上渲染额外的角落标签。
    /// </summary>
    public interface IIntentExtraCornerAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        ///     每个带有非空白 <see cref="ExtraIconAmountLabelSlot.Text" /> 的条目都会在
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" />（或 <see cref="ExtraIconAmountLabelCorner.Custom" /> 边界）处生成一个徽标。
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetIntentExtraCornerAmountLabelSlots();
    }

    /// <summary>
    ///     Optional invalidation when only intent extra slots change without
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent.UpdateVisuals" /> being driven by combat ticks.
    ///     可选的失效通知：仅意图额外槽位发生变化，且没有由战斗 tick 驱动
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NIntent.UpdateVisuals" /> 时使用。
    /// </summary>
    public interface IIntentExtraCornerAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IIntentExtraCornerAmountLabelsProvider.GetIntentExtraCornerAmountLabelSlots" />
        ///     may have changed.
        ///     当 <see cref="IIntentExtraCornerAmountLabelsProvider.GetIntentExtraCornerAmountLabelSlots" />
        ///     可能已变化时引发。
        /// </summary>
        event Action? IntentExtraCornerAmountLabelsInvalidated;
    }
}
