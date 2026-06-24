using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Handle for a typed model-saved data slot.
    ///     类型化模型保存数据槽位的句柄。
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
        ///     Gets the current value, creating it from the default factory if necessary.
        ///     获取当前值；必要时通过默认工厂创建。
        /// </summary>
        public TPayload Get(TTarget model)
        {
            return _slot.GetOrCreate(model);
        }

        /// <summary>
        ///     Attempts to get an existing value without creating one.
        ///     尝试获取已有值，但不创建新值。
        /// </summary>
        public bool TryGet(TTarget model, out TPayload value)
        {
            return _slot.TryGet(model, out value);
        }

        /// <summary>
        ///     Sets the value for <paramref name="model" />.
        ///     设置 <paramref name="model" /> 的值。
        /// </summary>
        public void Set(TTarget model, TPayload value)
        {
            _slot.Set(model, value);
        }

        /// <summary>
        ///     Marks the current value dirty after an in-place mutation.
        ///     在原地修改后将当前值标记为已变更。
        /// </summary>
        public void MarkDirty(TTarget model)
        {
            _slot.MarkDirty(model);
        }

        /// <summary>
        ///     Removes the saved value from <paramref name="model" />.
        ///     从 <paramref name="model" /> 移除保存值。
        /// </summary>
        public bool Remove(TTarget model)
        {
            return _slot.Remove(model);
        }

        /// <summary>
        ///     Mutates the value and marks it dirty.
        ///     修改值并标记为已变更。
        /// </summary>
        public TPayload Modify(TTarget model, Action<TPayload> mutate)
        {
            ArgumentNullException.ThrowIfNull(mutate);
            var value = _slot.GetOrCreate(model);
            mutate(value);
            _slot.Set(model, value);
            return value;
        }
    }
}
