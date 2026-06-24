namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Declares which bindings receive a direct <c>Save()</c> call when this binding's <see cref="IModSettingsBinding" />
    ///     persistence runs. Used to deduplicate deferred flush work across decorator stacks.
    ///     声明当此绑定的 <see cref="IModSettingsBinding" /> 持久化运行时，哪些绑定会收到直接的
    ///     <c>Save()</c> 调用。用于在装饰器 stack 之间去重延迟 flush 工作。
    /// </summary>
    internal interface IModSettingsBindingSaveDispatch
    {
        /// <summary>
        ///     Non-recursive targets: bindings invoked immediately by this instance's <c>Save()</c> (typically one inner or
        ///     parent).
        ///     非递归目标：由此实例的 <c>Save()</c> 立即调用的绑定（通常是一个内部绑定或
        ///     父绑定）。
        /// </summary>
        IReadOnlyList<IModSettingsBinding> ImmediateSaveTargets { get; }
    }
}
