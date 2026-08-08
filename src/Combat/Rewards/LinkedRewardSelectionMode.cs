namespace STS2RitsuLib.Combat.Rewards
{
    /// <summary>
    ///     <para xml:lang="en">Defines how a linked reward set resolves after one of its child rewards is selected.</para>
    ///     <para xml:lang="zh-CN">定义关联奖励集合中的一个子奖励被选择后，整个集合的结算方式。</para>
    /// </summary>
    public enum LinkedRewardSelectionMode
    {
        /// <summary>
        ///     <para xml:lang="en">Take the selected child reward and skip every other child reward.</para>
        ///     <para xml:lang="zh-CN">领取选中的子奖励，并跳过其他所有子奖励。</para>
        /// </summary>
        ChooseOne,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Attempt to take every child reward, starting with the selected child. Before any child is taken,
        ///         cancelling or failing a choice leaves the set available; after the first success, unavailable
        ///         children are skipped and the remaining children are still offered.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         尝试领取全部子奖励，并优先处理选中的子奖励。在尚未领取任何子奖励时，取消选择或领取失败会保留该集合；
        ///         首次成功领取后，无法领取的子奖励会被跳过，其余子奖励仍会继续提供。
        ///     </para>
        /// </summary>
        TakeAll,
    }
}
