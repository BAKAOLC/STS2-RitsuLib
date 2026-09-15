namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">Identifies the semantic role and visual hierarchy of a settings-sidebar row.</para>
    ///     <para xml:lang="zh-CN">标识设置侧栏行的语义角色和视觉层级。</para>
    /// </summary>
    public enum ModSettingsSidebarItemKind
    {
        /// <summary>
        ///     <para xml:lang="en">A top-level row representing a mod.</para>
        ///     <para xml:lang="zh-CN">代表一个模组的顶层行。</para>
        /// </summary>
        ModGroup,

        /// <summary>
        ///     <para xml:lang="en">A settings-page navigation row.</para>
        ///     <para xml:lang="zh-CN">设置页面导航行。</para>
        /// </summary>
        Page,

        /// <summary>
        ///     <para xml:lang="en">A navigation row for a section within a settings page.</para>
        ///     <para xml:lang="zh-CN">设置页面内节的导航行。</para>
        /// </summary>
        Section,

        /// <summary>
        ///     <para xml:lang="en">A utility or secondary-action row.</para>
        ///     <para xml:lang="zh-CN">工具或次级操作行。</para>
        /// </summary>
        Utility,
    }
}
