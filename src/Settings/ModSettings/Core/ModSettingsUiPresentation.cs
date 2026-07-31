namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides optional global presentation overrides for the RitsuLib mod settings screen. These values may be
    ///         configured during mod loading.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供 RitsuLib 模组设置屏幕的可选全局呈现覆盖项，可在模组加载期间配置。
    ///     </para>
    /// </summary>
    public static class ModSettingsUiPresentation
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the default maximum body height for paragraph entries and info cards before they use
        ///         internal vertical scrolling. Only positive finite values impose a limit; <see langword="null" />,
        ///         zero, negative, and non-finite values leave the height unrestricted. For paragraph entries, an
        ///         entry-specific <see cref="ParagraphModSettingsEntryDefinition.MaxBodyHeight" /> takes precedence.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置段落条目与信息卡在启用内部垂直滚动前的默认正文最大高度。只有有限正值会施加限制；
        ///         <see langword="null" />、零、负值与非有限值均表示不限制高度。对于段落条目，其自身的
        ///         <see cref="ParagraphModSettingsEntryDefinition.MaxBodyHeight" /> 设置具有更高优先级。
        ///     </para>
        /// </summary>
        public static float? ParagraphMaxBodyHeight { get; set; }
    }
}
