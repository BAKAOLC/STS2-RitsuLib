namespace STS2RitsuLib.Diagnostics.DevConsole
{
    /// <summary>
    ///     <para xml:lang="en">Binds autocomplete enhancements to a console-command argument.</para>
    ///     <para xml:lang="zh-CN">将自动补全增强功能绑定到控制台命令参数。</para>
    /// </summary>
    public sealed class DevConsoleAutocompleteBinding
    {
        /// <summary>
        ///     <para xml:lang="en">The developer-console command name, such as <c>card</c>.</para>
        ///     <para xml:lang="zh-CN">开发者控制台命令名称，例如 <c>card</c>。</para>
        /// </summary>
        public required string CommandName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The argument index to enhance, or <see langword="null" /> for predicate-only matching.</para>
        ///     <para xml:lang="zh-CN">要增强的参数索引；为 <see langword="null" /> 时仅使用谓词匹配。</para>
        /// </summary>
        public int? ArgumentIndex { get; init; }

        /// <summary>
        ///     <para xml:lang="en">An optional predicate evaluated against the current completion context.</para>
        ///     <para xml:lang="zh-CN">根据当前补全上下文计算的可选谓词。</para>
        /// </summary>
        public Func<DevConsoleAutocompleteContext, bool>? AppliesWhen { get; init; }

        /// <summary>
        ///     <para xml:lang="en">The enhancements applied when the binding matches.</para>
        ///     <para xml:lang="zh-CN">绑定匹配时应用的增强功能。</para>
        /// </summary>
        public DevConsoleAutocompleteEnhancements Enhancements { get; init; }
    }
}
