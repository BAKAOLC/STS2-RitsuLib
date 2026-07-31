namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">Configures runtime hotkey routing and presentation for one registration.</para>
    ///     <para xml:lang="zh-CN">配置单个运行时热键注册的路由与显示方式。</para>
    /// </summary>
    public sealed class RuntimeHotkeyOptions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the stable identifier for this registration.</para>
        ///     <para xml:lang="zh-CN">获取此注册的稳定标识符。</para>
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional display name used by UI and help text.</para>
        ///     <para xml:lang="zh-CN">获取供界面和帮助文本使用的可选显示名称。</para>
        /// </summary>
        public RuntimeHotkeyText? DisplayName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional description of the hotkey's action.</para>
        ///     <para xml:lang="zh-CN">获取说明热键作用的可选描述。</para>
        /// </summary>
        public RuntimeHotkeyText? Description { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional semantic purpose used for grouping or formatting.</para>
        ///     <para xml:lang="zh-CN">获取用于分组或格式化的可选语义用途。</para>
        /// </summary>
        public string? Purpose { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the optional UI category used to group related hotkeys.</para>
        ///     <para xml:lang="zh-CN">获取用于在界面中归类相关热键的可选类别。</para>
        /// </summary>
        public RuntimeHotkeyText? Category { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether <c>action:&lt;name&gt;</c> bindings are exposed as optional Steam Input digital actions when
        ///         the game runs through Steam.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取游戏通过 Steam 运行时，是否将 <c>action:&lt;name&gt;</c> 绑定公开为可选的 Steam Input 数字动作。
        ///     </para>
        /// </summary>
        public bool ExposeToSteamInput { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the input event is marked as handled after the callback runs.</para>
        ///     <para xml:lang="zh-CN">获取回调执行后是否将输入事件标记为已处理。</para>
        /// </summary>
        public bool MarkInputHandled { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the hotkey is suppressed while a text control is being edited.</para>
        ///     <para xml:lang="zh-CN">获取文本控件正在编辑时是否禁用此热键。</para>
        /// </summary>
        public bool SuppressWhenTextInputFocused { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the hotkey is suppressed while the developer console is visible.</para>
        ///     <para xml:lang="zh-CN">获取开发者控制台可见时是否禁用此热键。</para>
        /// </summary>
        public bool SuppressWhenDevConsoleVisible { get; init; } = true;

        /// <summary>
        ///     <para xml:lang="en">Gets the optional debug name included in registration logs.</para>
        ///     <para xml:lang="zh-CN">获取注册日志中包含的可选调试名称。</para>
        /// </summary>
        public string? DebugName { get; init; }
    }
}
