namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     <para xml:lang="en">Represents a runtime hotkey registration that can be rebound or unregistered.</para>
    ///     <para xml:lang="zh-CN">表示可重新绑定或注销的运行时热键注册。</para>
    /// </summary>
    public interface IRuntimeHotkeyHandle : IDisposable
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the first normalized binding in this registration.</para>
        ///     <para xml:lang="zh-CN">获取此注册中的第一个规范化绑定。</para>
        /// </summary>
        string CurrentBinding { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets all normalized bindings in this registration.</para>
        ///     <para xml:lang="zh-CN">获取此注册中的所有规范化绑定。</para>
        /// </summary>
        IReadOnlyList<string> CurrentBindings { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether this handle remains registered with the runtime hotkey router.</para>
        ///     <para xml:lang="zh-CN">获取此句柄是否仍注册在运行时热键路由器中。</para>
        /// </summary>
        bool IsRegistered { get; }

        /// <summary>
        ///     <para xml:lang="en">Replaces the current bindings with one parsed binding.</para>
        ///     <para xml:lang="zh-CN">使用一个解析后的绑定替换当前全部绑定。</para>
        /// </summary>
        /// <param name="bindingText">
        ///     <para xml:lang="en">The binding text to parse and apply.</para>
        ///     <para xml:lang="zh-CN">要解析并应用的绑定文本。</para>
        /// </param>
        /// <param name="normalizedBinding">
        ///     <para xml:lang="en">Receives the normalized binding when parsing succeeds.</para>
        ///     <para xml:lang="zh-CN">解析成功时接收规范化绑定。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if the binding was parsed and applied; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若绑定已成功解析并应用，则返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        bool TryRebind(string bindingText, out string normalizedBinding);

        /// <summary>
        ///     <para xml:lang="en">Replaces all current bindings with the parsed bindings.</para>
        ///     <para xml:lang="zh-CN">使用解析后的绑定替换当前全部绑定。</para>
        /// </summary>
        /// <param name="bindingTexts">
        ///     <para xml:lang="en">The binding texts to parse and apply.</para>
        ///     <para xml:lang="zh-CN">要解析并应用的绑定文本。</para>
        /// </param>
        /// <param name="normalizedBindings">
        ///     <para xml:lang="en">Receives the normalized bindings when parsing succeeds.</para>
        ///     <para xml:lang="zh-CN">解析成功时接收规范化绑定。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if every binding was parsed and applied; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若所有绑定均已成功解析并应用，则返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        bool TryRebind(IEnumerable<string> bindingTexts, out IReadOnlyList<string> normalizedBindings);

        /// <summary>
        ///     <para xml:lang="en">Tries to obtain a read-only snapshot of the current registration.</para>
        ///     <para xml:lang="zh-CN">尝试获取当前注册的只读快照。</para>
        /// </summary>
        /// <param name="registrationInfo">
        ///     <para xml:lang="en">Receives the registration snapshot when this handle is active.</para>
        ///     <para xml:lang="zh-CN">此句柄仍有效时接收注册快照。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> if this handle remains registered; otherwise,
        ///         <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         若此句柄仍已注册，则返回 <see langword="true" />；否则返回
        ///         <see langword="false" />。
        ///     </para>
        /// </returns>
        bool TryGetRegistrationInfo(out RuntimeHotkeyRegistrationInfo registrationInfo);

        /// <summary>
        ///     <para xml:lang="en">Removes this registration from the runtime hotkey router.</para>
        ///     <para xml:lang="zh-CN">从运行时热键路由器中移除此注册。</para>
        /// </summary>
        void Unregister();
    }
}
