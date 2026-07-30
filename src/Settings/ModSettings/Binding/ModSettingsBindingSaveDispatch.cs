namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes the bindings saved directly by another binding, allowing deferred persistence to avoid
    ///         duplicate work across decorator chains.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述一个绑定在保存时会直接保存的其他绑定，以便延迟持久化在装饰器链中避免重复工作。
    ///     </para>
    /// </summary>
    internal interface IModSettingsBindingSaveDispatch
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the non-recursive set of bindings whose <see cref="IModSettingsBinding.Save" /> method this
        ///         instance calls directly, typically an inner or parent binding.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取此实例会直接调用 <see cref="IModSettingsBinding.Save" /> 的非递归绑定集合，
        ///         通常为内部绑定或父绑定。
        ///     </para>
        /// </summary>
        IReadOnlyList<IModSettingsBinding> ImmediateSaveTargets { get; }
    }
}
