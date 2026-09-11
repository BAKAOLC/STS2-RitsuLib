namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes one command displayed in a settings action menu or context menu. An enablement failure is
    ///         logged and disables the command; an exception from the selected action propagates after the menu closes.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述设置操作菜单或上下文菜单中显示的一项命令。启用状态计算失败时会记录异常并禁用该命令；
    ///         所选操作抛出的异常会在菜单关闭后继续传播。
    ///     </para>
    /// </summary>
    /// <param name="Id">
    ///     <para xml:lang="en">An optional stable action ID.</para>
    ///     <para xml:lang="zh-CN">可选的稳定操作 ID。</para>
    /// </param>
    /// <param name="Label">
    ///     <para xml:lang="en">The text displayed for the action.</para>
    ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
    /// </param>
    /// <param name="IsEnabled">
    ///     <para xml:lang="en">A function that determines whether the action is currently enabled.</para>
    ///     <para xml:lang="zh-CN">确定操作当前是否启用的函数。</para>
    /// </param>
    /// <param name="Action">
    ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
    ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
    /// </param>
    public sealed record ModSettingsMenuAction(string? Id, string Label, Func<bool> IsEnabled, Action Action)
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an action without an ID and with a fixed enabled state.</para>
        ///     <para xml:lang="zh-CN">创建不带 ID 且启用状态固定的操作。</para>
        /// </summary>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether the action is enabled.</para>
        ///     <para xml:lang="zh-CN">操作是否启用。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string label, bool enabled, Action action)
            : this(null, label, () => enabled, action)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an action without an ID and with a dynamically evaluated enabled state.</para>
        ///     <para xml:lang="zh-CN">创建不带 ID 且启用状态动态计算的操作。</para>
        /// </summary>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="isEnabled">
        ///     <para xml:lang="en">A function that determines whether the action is currently enabled.</para>
        ///     <para xml:lang="zh-CN">确定操作当前是否启用的函数。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string label, Func<bool> isEnabled, Action action)
            : this(null, label, isEnabled, action)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Creates an action with an optional stable ID and a fixed enabled state.</para>
        ///     <para xml:lang="zh-CN">创建带可选稳定 ID 且启用状态固定的操作。</para>
        /// </summary>
        /// <param name="id">
        ///     <para xml:lang="en">An optional stable action ID.</para>
        ///     <para xml:lang="zh-CN">可选的稳定操作 ID。</para>
        /// </param>
        /// <param name="label">
        ///     <para xml:lang="en">The text displayed for the action.</para>
        ///     <para xml:lang="zh-CN">为操作显示的文本。</para>
        /// </param>
        /// <param name="enabled">
        ///     <para xml:lang="en">Whether the action is enabled.</para>
        ///     <para xml:lang="zh-CN">操作是否启用。</para>
        /// </param>
        /// <param name="action">
        ///     <para xml:lang="en">The callback invoked when the action is selected.</para>
        ///     <para xml:lang="zh-CN">选择该操作时调用的回调。</para>
        /// </param>
        public ModSettingsMenuAction(string? id, string label, bool enabled, Action action)
            : this(id, label, () => enabled, action)
        {
        }
    }
}
