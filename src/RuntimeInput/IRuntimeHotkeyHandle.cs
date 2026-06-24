namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Represents a registered runtime hotkey that can be rebound or unregistered explicitly by the caller.
    ///     表示一个已注册的运行时热键，可由调用方显式重新绑定或注销。
    /// </summary>
    public interface IRuntimeHotkeyHandle : IDisposable
    {
        /// <summary>
        ///     Gets the first current normalized binding string for this registration.
        ///     获取此注册的第一个当前规范化绑定字符串。
        /// </summary>
        string CurrentBinding { get; }

        /// <summary>
        ///     Gets all current normalized binding strings for this registration.
        ///     获取此注册的所有当前规范化绑定字符串。
        /// </summary>
        IReadOnlyList<string> CurrentBindings { get; }

        /// <summary>
        ///     Gets whether this handle is still registered with the runtime hotkey router.
        ///     获取此句柄是否仍注册在运行时热键路由器中。
        /// </summary>
        bool IsRegistered { get; }

        /// <summary>
        ///     Replaces the binding with a newly persisted binding string.
        ///     用新持久化的绑定字符串替换该绑定。
        /// </summary>
        /// <param name="bindingText">
        ///     Binding text to parse and apply.
        ///     要解析并应用的绑定文本。
        /// </param>
        /// <param name="normalizedBinding">
        ///     The normalized binding string if parsing succeeded.
        ///     解析成功时的规范化绑定字符串。
        /// </param>
        /// <returns>
        ///     <c>true</c> when the new binding was parsed and applied.
        ///     新绑定已解析并应用时为 <c>true</c>。
        /// </returns>
        bool TryRebind(string bindingText, out string normalizedBinding);

        /// <summary>
        ///     Replaces all bindings with newly persisted binding strings.
        ///     用新持久化的绑定字符串替换所有绑定。
        /// </summary>
        /// <param name="bindingTexts">
        ///     Binding texts to parse and apply.
        ///     要解析并应用的绑定文本。
        /// </param>
        /// <param name="normalizedBindings">
        ///     Normalized binding strings if parsing succeeded.
        ///     解析成功时的规范化绑定字符串。
        /// </param>
        /// <returns>
        ///     <c>true</c> when all bindings were parsed and applied.
        ///     所有绑定均已解析并应用时为 <c>true</c>。
        /// </returns>
        bool TryRebind(IEnumerable<string> bindingTexts, out IReadOnlyList<string> normalizedBindings);

        /// <summary>
        ///     Returns a read-only snapshot describing the current registration.
        ///     返回描述当前注册的只读快照。
        /// </summary>
        /// <param name="registrationInfo">
        ///     Registration snapshot when this handle is still active.
        ///     此句柄仍处于活动状态时的注册快照。
        /// </param>
        /// <returns>
        ///     <c>true</c> when this handle is still registered.
        ///     此句柄仍已注册时为 <c>true</c>。
        /// </returns>
        bool TryGetRegistrationInfo(out RuntimeHotkeyRegistrationInfo registrationInfo);

        /// <summary>
        ///     Removes this registration from the runtime hotkey router.
        ///     从运行时热键路由器中移除此注册。
        /// </summary>
        void Unregister();
    }
}
