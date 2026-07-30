using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Binds a mod setting to custom read, write, and save callbacks instead of
    ///         <see cref="RitsuLibFramework.GetDataStore" />, for example when using a BaseLib JSON configuration or a
    ///         third-party store.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过自定义读取、写入和保存回调绑定模组设置，而不使用
    ///         <see cref="RitsuLibFramework.GetDataStore" />；例如可用于 BaseLib JSON 配置或第三方存储。
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The setting value type.</para>
    ///     <para xml:lang="zh-CN">设置值类型。</para>
    /// </typeparam>
    /// <param name="modId">
    ///     <para xml:lang="en">The ID of the mod that owns the setting.</para>
    ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
    /// </param>
    /// <param name="dataKey">
    ///     <para xml:lang="en">The stable key that identifies the setting within the mod.</para>
    ///     <para xml:lang="zh-CN">在模组内标识该设置的稳定键。</para>
    /// </param>
    /// <param name="scope">
    ///     <para xml:lang="en">The save scope represented by the binding.</para>
    ///     <para xml:lang="zh-CN">该绑定所表示的保存作用域。</para>
    /// </param>
    /// <param name="read">
    ///     <para xml:lang="en">The callback that reads the current value.</para>
    ///     <para xml:lang="zh-CN">读取当前值的回调。</para>
    /// </param>
    /// <param name="write">
    ///     <para xml:lang="en">The callback that writes a new value.</para>
    ///     <para xml:lang="zh-CN">写入新值的回调。</para>
    /// </param>
    /// <param name="save">
    ///     <para xml:lang="en">The callback that persists the current value.</para>
    ///     <para xml:lang="zh-CN">持久化当前值的回调。</para>
    /// </param>
    public sealed class ModSettingsCallbackValueBinding<T>(
        string modId,
        string dataKey,
        SaveScope scope,
        Func<T> read,
        Action<T> write,
        Action save) : IModSettingsValueBinding<T>
    {
        private readonly Func<T> _read = ModSettingsBindingValidation.RequireNonNull(read, nameof(read));
        private readonly Action<T> _write = ModSettingsBindingValidation.RequireNonNull(write, nameof(write));
        private readonly Action _save = ModSettingsBindingValidation.RequireNonNull(save, nameof(save));

        /// <inheritdoc />
        public string ModId { get; } = ModSettingsBindingValidation.RequireNonEmpty(modId, nameof(modId));

        /// <inheritdoc />
        public string DataKey { get; } = ModSettingsBindingValidation.RequireNonEmpty(dataKey, nameof(dataKey));

        /// <inheritdoc />
        public SaveScope Scope { get; } = scope;

        /// <inheritdoc />
        public T Read()
        {
            return _read();
        }

        /// <inheritdoc />
        public void Write(T value)
        {
            _write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _save();
        }
    }
}
