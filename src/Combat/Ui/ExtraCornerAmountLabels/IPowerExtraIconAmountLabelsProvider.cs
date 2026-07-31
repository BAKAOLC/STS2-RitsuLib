namespace STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Implement this on a <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> subclass to add plain-text
    ///         badges to its <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" />, independently of the base-game
    ///         amount label.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="MegaCrit.Sts2.Core.Models.PowerModel" /> 子类上实现此接口，可在对应的
    ///         <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NPower" /> 上添加纯文本角标；这些角标独立于游戏原有
    ///         数量标签。
    ///     </para>
    /// </summary>
    public interface IPowerExtraIconAmountLabelsProvider
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a non-null badge list. Whitespace-only text, invalid corners, and invalid custom bounds are
        ///         ignored. Each built-in corner uses only its first entry; custom entries may overlap and later
        ///         entries draw on top.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回非空角标列表。仅含空白的文本、无效角落和无效自定义边界会被忽略。每个内置角落只使用
        ///         第一个条目；自定义条目可以重叠，后面的条目绘制在上层。
        ///     </para>
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides plain-text and rich-text power badges. This interface takes precedence over
    ///         <see cref="IPowerExtraIconAmountLabelsProvider" /> when both are implemented.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供纯文本和富文本能力角标。同时实现两个提供接口时，此接口优先于
    ///         <see cref="IPowerExtraIconAmountLabelsProvider" />。
    ///     </para>
    /// </summary>
    public interface IPowerExtraIconAmountLabelSpecsProvider
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a non-null badge list under the same filtering and ordering rules as
        ///         <see cref="IPowerExtraIconAmountLabelsProvider.GetPowerExtraIconAmountLabelSlots" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回非空角标列表；筛选和顺序规则与
        ///         <see cref="IPowerExtraIconAmountLabelsProvider.GetPowerExtraIconAmountLabelSlots" /> 相同。
        ///     </para>
        /// </summary>
        IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides optional invalidation for badge changes that do not raise
    ///         <see cref="MegaCrit.Sts2.Core.Models.PowerModel.DisplayAmountChanged" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为不会触发 <see cref="MegaCrit.Sts2.Core.Models.PowerModel.DisplayAmountChanged" /> 的角标变化提供
    ///         可选的主动刷新通知。
    ///     </para>
    /// </summary>
    public interface IPowerExtraIconAmountLabelsChangeSource
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Occurs on the Godot main thread when either provider's returned badges may have changed.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         任一提供接口返回的角标可能发生变化时，在 Godot 主线程上发生。
        ///     </para>
        /// </summary>
        event Action? PowerExtraIconAmountLabelsInvalidated;
    }
}
