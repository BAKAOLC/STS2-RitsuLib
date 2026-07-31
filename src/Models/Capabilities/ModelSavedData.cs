using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Provides a handle for a typed model-saved-data slot.</para>
    ///     <para xml:lang="zh-CN">提供类型化模型保存数据槽位的句柄。</para>
    /// </summary>
    public sealed class ModelSavedData<TTarget, TPayload>
        where TTarget : AbstractModel
        where TPayload : class, new()
    {
        private readonly StoredModelSavedDataSlot<TTarget, TPayload> _slot;

        internal ModelSavedData(StoredModelSavedDataSlot<TTarget, TPayload> slot)
        {
            _slot = slot;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the current value, creating it with the default factory when necessary.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取当前值；必要时使用默认工厂创建。
        ///     </para>
        /// </summary>
        public TPayload Get(TTarget model)
        {
            return _slot.GetOrCreate(model);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get an existing value without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取已有值，但不创建新值。</para>
        /// </summary>
        public bool TryGet(TTarget model, out TPayload value)
        {
            return _slot.TryGet(model, out value);
        }

        /// <summary>
        ///     <para xml:lang="en">Sets the value for <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">设置 <paramref name="model" /> 的值。</para>
        /// </summary>
        public void Set(TTarget model, TPayload value)
        {
            _slot.Set(model, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Marks the current value dirty after an in-place mutation.</para>
        ///     <para xml:lang="zh-CN">在原地修改后将当前值标记为脏。</para>
        /// </summary>
        public void MarkDirty(TTarget model)
        {
            _slot.MarkDirty(model);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the saved value from <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">从 <paramref name="model" /> 移除保存值。</para>
        /// </summary>
        public bool Remove(TTarget model)
        {
            return _slot.Remove(model);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Invokes <paramref name="mutate" /> and writes the value back as dirty even if the callback throws.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         调用 <paramref name="mutate" />；即使回调抛出异常，也会将该值写回并标记为脏。
        ///     </para>
        /// </summary>
        public TPayload Modify(TTarget model, Action<TPayload> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = _slot.GetOrCreate(model);
            try
            {
                mutate(value);
            }
            finally
            {
                _slot.Set(model, value);
            }

            return value;
        }
    }
}
