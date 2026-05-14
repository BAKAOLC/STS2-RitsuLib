namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Immutable snapshot describing one active runtime hotkey registration.
    ///     描述一个活动运行时热键注册的不可变快照。
    /// </summary>
    public sealed record RuntimeHotkeyRegistrationInfo(
        string CurrentBinding,
        bool IsModifierOnly,
        string? Id,
        string? DisplayName,
        string? Description,
        string? Purpose,
        string? Category,
        bool MarkInputHandled,
        bool SuppressWhenTextInputFocused,
        bool SuppressWhenDevConsoleVisible,
        string? DebugName)
    {
        /// <summary>
        ///     All currently active bindings for this hotkey, in display order.
        ///     此热键当前所有活动绑定，按显示顺序排列。
        /// </summary>
        public IReadOnlyList<string> CurrentBindings { get; init; } =
            string.IsNullOrWhiteSpace(CurrentBinding) ? [] : [CurrentBinding];

        /// <summary>
        ///     Per-binding modifier-only flags aligned with <see cref="CurrentBindings" />.
        ///     与 <see cref="CurrentBindings" /> 对齐的逐绑定“仅修饰键”标志。
        /// </summary>
        public IReadOnlyList<bool> BindingModifierOnlyStates { get; init; } = [IsModifierOnly];
    }

    /// <summary>
    ///     Detailed immutable snapshot describing one active runtime hotkey registration, including all bindings.
    ///     描述一个活动运行时热键注册的详细不可变快照，包括所有绑定。
    /// </summary>
    public sealed record RuntimeHotkeyRegistrationDetails(
        IReadOnlyList<string> CurrentBindings,
        IReadOnlyList<bool> BindingModifierOnlyStates,
        string? Id,
        string? DisplayName,
        string? Description,
        string? Purpose,
        string? Category,
        bool MarkInputHandled,
        bool SuppressWhenTextInputFocused,
        bool SuppressWhenDevConsoleVisible,
        string? DebugName)
    {
        /// <summary>
        ///     First active binding, kept for compatibility with single-binding consumers.
        ///     第一个活动绑定，为兼容单绑定消费者而保留。
        /// </summary>
        public string CurrentBinding => CurrentBindings.FirstOrDefault() ?? string.Empty;

        /// <summary>
        ///     Whether the first active binding is modifier-only.
        ///     第一个活动绑定是否仅包含修饰键。
        /// </summary>
        public bool IsModifierOnly => BindingModifierOnlyStates.FirstOrDefault();

        /// <summary>
        ///     Down-converts this detailed snapshot to the legacy single-binding view.
        ///     将此详细快照降级转换为旧版单绑定视图。
        /// </summary>
        public RuntimeHotkeyRegistrationInfo ToRegistrationInfo()
        {
            return new(
                CurrentBinding,
                IsModifierOnly,
                Id,
                DisplayName,
                Description,
                Purpose,
                Category,
                MarkInputHandled,
                SuppressWhenTextInputFocused,
                SuppressWhenDevConsoleVisible,
                DebugName)
            {
                CurrentBindings = CurrentBindings,
                BindingModifierOnlyStates = BindingModifierOnlyStates,
            };
        }
    }
}
