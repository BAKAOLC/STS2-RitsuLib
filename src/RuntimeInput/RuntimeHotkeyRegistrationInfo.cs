namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">Provides an immutable snapshot of an active runtime hotkey registration.</para>
    ///     <para xml:lang="zh-CN">提供活动运行时热键注册的不可变快照。</para>
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
        ///     <para xml:lang="en">Gets all active bindings for this hotkey in display order.</para>
        ///     <para xml:lang="zh-CN">按显示顺序获取此热键的所有有效绑定。</para>
        /// </summary>
        public IReadOnlyList<string> CurrentBindings { get; init; } =
            string.IsNullOrWhiteSpace(CurrentBinding) ? [] : [CurrentBinding];

        /// <summary>
        ///     <para xml:lang="en">Gets modifier-only flags corresponding by index to <see cref="CurrentBindings" />.</para>
        ///     <para xml:lang="zh-CN">获取按索引对应于 <see cref="CurrentBindings" /> 的“仅修饰键”标志。</para>
        /// </summary>
        public IReadOnlyList<bool> BindingModifierOnlyStates { get; init; } = [IsModifierOnly];
    }

    /// <summary>
    ///     <para xml:lang="en">Provides a detailed immutable snapshot of an active hotkey and all its bindings.</para>
    ///     <para xml:lang="zh-CN">提供活动热键及其所有绑定的详细不可变快照。</para>
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
        ///     <para xml:lang="en">Gets the first active binding for compatibility with single-binding consumers.</para>
        ///     <para xml:lang="zh-CN">获取第一个有效绑定，以兼容仅支持单个绑定的调用方。</para>
        /// </summary>
        public string CurrentBinding => CurrentBindings.FirstOrDefault() ?? string.Empty;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the first active binding consists only of modifier keys.</para>
        ///     <para xml:lang="zh-CN">获取第一个有效绑定是否仅由修饰键组成。</para>
        /// </summary>
        public bool IsModifierOnly => BindingModifierOnlyStates.FirstOrDefault();

        /// <summary>
        ///     <para xml:lang="en">Converts this snapshot to the compatibility view used by single-binding consumers.</para>
        ///     <para xml:lang="zh-CN">将此快照转换为仅支持单个绑定的调用方所用的兼容视图。</para>
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
