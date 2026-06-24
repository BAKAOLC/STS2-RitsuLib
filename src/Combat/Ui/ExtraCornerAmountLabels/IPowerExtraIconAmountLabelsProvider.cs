namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> subclasses to render additional
    ///     numeric/text badges on <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" /> (separate from the vanilla
    ///     counter label).
    ///     由 <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> 子类实现，用于在
    ///     <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" /> 上渲染额外的数字/文本徽标（独立于原版
    ///     计数器标签）。
    /// </summary>
    public interface IPowerExtraIconAmountLabelsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSlot.Text" /> becomes one badge at
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" /> (or <see cref="ExtraIconAmountLabelCorner.Custom" /> bounds).
        ///     Order only affects z-order (later draws on top).
        ///     每个带有非空白 <see cref="ExtraIconAmountLabelSlot.Text" /> 的条目都会在
        ///     <see cref="ExtraIconAmountLabelSlot.Corner" />（或 <see cref="ExtraIconAmountLabelCorner.Custom" /> 边界）处生成一个徽标。
        ///     顺序只影响 z 顺序（后绘制的在上方）。
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots();
    }

    /// <summary>
    ///     Implemented by <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> subclasses to render additional
    ///     badges with per-slot plain/rich text mode. Takes precedence over
    ///     <see cref="IPowerExtraIconAmountLabelsProvider" /> when both are implemented.
    ///     由 <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> 子类实现，用于渲染带有逐槽位普通/富文本
    ///     模式的额外徽标。当同时实现 <see cref="IPowerExtraIconAmountLabelsProvider" /> 时优先使用此接口。
    /// </summary>
    public interface IPowerExtraIconAmountLabelSpecsProvider
    {
        /// <summary>
        ///     Each entry with non-whitespace <see cref="ExtraIconAmountLabelSpec.Text" /> becomes one badge.
        ///     每个带有非空白 <see cref="ExtraIconAmountLabelSpec.Text" /> 的条目都会生成一个徽标。
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs();
    }

    /// <summary>
    ///     Optional invalidation signal when only <see cref="IPowerExtraIconAmountLabelsProvider" /> slots change
    ///     without <see cref="MegaCrit.Sts2.Core.Models.PowerModel.DisplayAmountChanged" /> firing.
    ///     可选的失效信号：仅 <see cref="IPowerExtraIconAmountLabelsProvider" /> 槽位发生变化，
    ///     且未触发 <see cref="MegaCrit.Sts2.Core.Models.PowerModel.DisplayAmountChanged" /> 时使用。
    /// </summary>
    public interface IPowerExtraIconAmountLabelsChangeSource
    {
        /// <summary>
        ///     Raised when <see cref="IPowerExtraIconAmountLabelsProvider.GetPowerExtraIconAmountLabelSlots" /> may
        ///     have changed.
        ///     当 <see cref="IPowerExtraIconAmountLabelsProvider.GetPowerExtraIconAmountLabelSlots" /> 可能
        ///     已变化时引发。
        /// </summary>
        event Action? PowerExtraIconAmountLabelsInvalidated;
    }
}
