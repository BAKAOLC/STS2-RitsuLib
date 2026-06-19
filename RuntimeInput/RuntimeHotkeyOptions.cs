namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Controls optional runtime hotkey router behavior for a single registration.
    ///     控制单个注册的可选运行时热键路由器行为。
    /// </summary>
    public sealed class RuntimeHotkeyOptions
    {
        /// <summary>
        ///     Stable identifier for this hotkey registration.
        ///     此热键注册的稳定标识符。
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        ///     Optional human-readable display name for UI or help surfaces.
        ///     用于 UI 或帮助界面的可选便于阅读显示名。
        /// </summary>
        public RuntimeHotkeyText? DisplayName { get; init; }

        /// <summary>
        ///     Optional human-readable description explaining what the hotkey does.
        ///     说明热键用途的可选便于阅读描述。
        /// </summary>
        public RuntimeHotkeyText? Description { get; init; }

        /// <summary>
        ///     Optional short semantic purpose string used for grouping or formatting.
        ///     用于分组或格式化的可选短语义用途字符串。
        /// </summary>
        public string? Purpose { get; init; }

        /// <summary>
        ///     Optional UI-facing category used to group related hotkeys.
        ///     用于对相关热键分组的可选 UI 面向类别。
        /// </summary>
        public RuntimeHotkeyText? Category { get; init; }

        /// <summary>
        ///     When true, <c>action:&lt;name&gt;</c> bindings in this registration are exposed as optional Steam Input
        ///     digital actions when the game is running through Steam.
        ///     为 true 时，此注册中的 <c>action:&lt;name&gt;</c> 绑定会在游戏通过 Steam 运行时作为可选
        ///     Steam Input 数字动作暴露。
        /// </summary>
        public bool ExposeToSteamInput { get; init; }

        /// <summary>
        ///     When true, marks the input event as handled after the hotkey callback runs.
        ///     为 true 时，在热键回调运行后将输入事件标记为已处理。
        /// </summary>
        public bool MarkInputHandled { get; init; }

        /// <summary>
        ///     When true, suppresses the hotkey while a text input control is actively editing.
        ///     为 true 时，在文本输入控件正在编辑时抑制该热键。
        /// </summary>
        public bool SuppressWhenTextInputFocused { get; init; } = true;

        /// <summary>
        ///     When true, suppresses the hotkey while the developer console is visible.
        ///     为 true 时，在开发者控制台可见时抑制该热键。
        /// </summary>
        public bool SuppressWhenDevConsoleVisible { get; init; } = true;

        /// <summary>
        ///     Optional debug name included in registration logs.
        ///     注册日志中包含的可选调试名称。
        /// </summary>
        public string? DebugName { get; init; }
    }
}
