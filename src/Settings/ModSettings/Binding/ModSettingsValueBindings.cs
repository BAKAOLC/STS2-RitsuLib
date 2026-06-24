using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Value binding that reads/writes a field of persisted model <typeparamref name="TModel" /> via the mod data store.
    ///     通过 mod data store 读写持久化模型 <typeparamref name="TModel" /> 字段的值绑定。
    /// </summary>
    public sealed class ModSettingsValueBinding<TModel, TValue>(
        string modId,
        string dataKey,
        SaveScope scope,
        Func<TModel, TValue> getter,
        Action<TModel, TValue> setter)
        : IModSettingsValueBinding<TValue>
        where TModel : class, new()
    {
        /// <summary>
        ///     Mod id used to resolve <see cref="RitsuLibFramework.GetDataStore" />.
        ///     用于解析 <see cref="RitsuLibFramework.GetDataStore" /> 的 mod id。
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Key of the persisted model blob.
        ///     持久化模型 blob 的键。
        /// </summary>
        public string DataKey { get; } = dataKey;

        /// <summary>
        ///     Persistence scope for the backing store entry.
        ///     后备存储条目的持久化作用域。
        /// </summary>
        public SaveScope Scope { get; } = scope;

        /// <summary>
        ///     Reads the current value from the model in the store.
        ///     从存储中的模型读取当前值。
        /// </summary>
        public TValue Read()
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            return getter(store.Get<TModel>(DataKey));
        }

        /// <summary>
        ///     Mutates the model in memory (call <see cref="Save" /> to flush).
        ///     在内存中修改模型（调用 <see cref="Save" /> 以 flush）。
        /// </summary>
        public void Write(TValue value)
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Modify<TModel>(DataKey, model => setter(model, value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <summary>
        ///     Persists the data key for this mod.
        ///     持久化此 mod 的数据键。
        /// </summary>
        public void Save()
        {
            RitsuLibFramework.GetDataStore(ModId).Save(DataKey);
        }
    }

    /// <summary>
    ///     In-memory binding for previews, tests, or non-persisted UI; uses JSON adapter for structured clipboard.
    ///     In-memory binding 用于 previews, tests, 或 non-persisted UI; 使用 JSON adapter 用于 structured clipboard.
    /// </summary>
    public sealed class InMemoryModSettingsValueBinding<TValue>(string modId, string dataKey, TValue initialValue)
        : IStructuredModSettingsValueBinding<TValue>, ITransientModSettingsBinding,
            IDefaultModSettingsValueBinding<TValue>
    {
        private readonly TValue _defaultValue = initialValue;
        private TValue _value = initialValue;

        /// <inheritdoc />
        public TValue CreateDefaultValue()
        {
            return Adapter.Clone(_defaultValue);
        }

        /// <summary>
        ///     Logical mod id (for UI identity; not persisted by this type).
        ///     Logical mod id (用于 UI identity; not persisted 通过 this type).
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Logical data key segment.
        ///     逻辑 data key 片段。
        /// </summary>
        public string DataKey { get; } = dataKey;

        /// <summary>
        ///     Always <see cref="SaveScope.Global" />; <see cref="Save" /> is a no-op.
        ///     始终为 <see cref="SaveScope.Global" />；<see cref="Save" /> 不执行任何操作。
        /// </summary>
        public SaveScope Scope => SaveScope.Global;

        /// <summary>
        ///     JSON round-trip adapter for clone and clipboard.
        ///     JSON round-trip adapter 用于 clone 和 clipboard.
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = ModSettingsStructuredData.Json<TValue>();

        /// <summary>
        ///     Returns the current in-memory value.
        ///     返回当前内存值。
        /// </summary>
        public TValue Read()
        {
            return _value;
        }

        /// <summary>
        ///     Sets the in-memory value.
        ///     设置内存值。
        /// </summary>
        public void Write(TValue value)
        {
            _value = value;
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
        }
    }

    /// <summary>
    ///     Wraps an inner binding and attaches a structured adapter without changing persistence behavior.
    ///     包装内部绑定并附加结构化适配器，同时不改变持久化行为。
    /// </summary>
    public sealed class StructuredModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        IStructuredModSettingsValueAdapter<TValue> adapter)
        : IStructuredModSettingsValueBinding<TValue>, IModSettingsUiRefreshPropagation,
            IModSettingsUiRefreshEquivalence,
            IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        /// <inheritdoc />
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [inner];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [inner];

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <summary>
        ///     Adapter used for serialization and clipboard.
        ///     用于序列化和剪贴板的适配器。
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = adapter;

        /// <inheritdoc />
        public TValue Read()
        {
            return inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }

    /// <summary>
    ///     Binding that projects a child value out of a parent binding (e.g. one field of a settings record).
    ///     从父绑定投影出子值的绑定（例如设置记录的某个字段）。
    /// </summary>
    public sealed class ProjectedModSettingsValueBinding<TSource, TValue>(
        IModSettingsValueBinding<TSource> parent,
        string dataKey,
        Func<TSource, TValue> getter,
        Func<TSource, TValue, TSource> setter,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IModSettingsUiRefreshPropagation, IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [parent];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [parent];

        /// <inheritdoc />
        public string ModId => parent.ModId;

        /// <summary>
        ///     Composite key <c>parent.DataKey.{segment}</c> when the constructor segment is non-empty; otherwise the parent
        ///     data key.
        ///     当构造函数 segment 非空时为复合 key <c>parent.DataKey.{segment}</c>；否则为父级
        ///     data key。
        /// </summary>
        public string DataKey => string.IsNullOrWhiteSpace(dataKey) ? parent.DataKey : $"{parent.DataKey}.{dataKey}";

        /// <inheritdoc />
        public SaveScope Scope => parent.Scope;

        /// <summary>
        ///     Adapter for the projected type; defaults to JSON when the parent is not structured.
        ///     投影类型的适配器；当父级不是结构化绑定时默认为 JSON。
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            adapter ?? ModSettingsStructuredData.Json<TValue>();

        /// <inheritdoc />
        public TValue Read()
        {
            return getter(parent.Read());
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            var source = parent.Read();
            parent.Write(setter(source, value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            parent.Save();
        }
    }

    /// <summary>
    ///     Decorates a binding with default-value factory and structured adapter resolution for reset and clipboard.
    ///     Decorates a binding 带有 default-value factory 和 structured adapter resolution 用于 re设置 和 clipboard.
    /// </summary>
    public sealed class DefaultModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Func<TValue> defaultValueFactory,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IDefaultModSettingsValueBinding<TValue>,
            IModSettingsUiRefreshPropagation, IModSettingsUiRefreshEquivalence, IModSettingsBindingSaveDispatch
    {
        /// <inheritdoc />
        public TValue CreateDefaultValue()
        {
            return defaultValueFactory();
        }

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        /// <inheritdoc />
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [inner];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [inner];

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <summary>
        ///     Adapter from the inner structured binding when present; otherwise the optional constructor adapter or JSON
        ///     default.
        ///     存在内部结构化绑定时取其适配器；否则使用可选构造函数适配器或 JSON
        ///     默认适配器。
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            inner is IStructuredModSettingsValueBinding<TValue> structured
                ? structured.Adapter
                : adapter ?? ModSettingsStructuredData.Json<TValue>();

        /// <inheritdoc />
        public TValue Read()
        {
            return inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }

    internal sealed class JsonStructuredValueAdapter<TValue>(JsonSerializerOptions? options)
        : IStructuredModSettingsValueAdapter<TValue>
    {
        public TValue Clone(TValue value)
        {
            var json = JsonSerializer.Serialize(value, options);
            return JsonSerializer.Deserialize<TValue>(json, options)!;
        }

        public string Serialize(TValue value)
        {
            return JsonSerializer.Serialize(value, options);
        }

        public bool TryDeserialize(string text, out TValue value)
        {
            try
            {
                value = JsonSerializer.Deserialize<TValue>(text, options)!;
                return true;
            }
            catch
            {
                value = default!;
                return false;
            }
        }
    }

    internal sealed class ListStructuredValueAdapter<TItem>(
        IStructuredModSettingsValueAdapter<TItem>? itemAdapter,
        JsonSerializerOptions? options)
        : IStructuredModSettingsValueAdapter<List<TItem>>
    {
        public List<TItem> Clone(List<TItem> value)
        {
            return itemAdapter == null ? value.ToList() : value.Select(itemAdapter.Clone).ToList();
        }

        public string Serialize(List<TItem> value)
        {
            return JsonSerializer.Serialize(value, options);
        }

        public bool TryDeserialize(string text, out List<TItem> value)
        {
            try
            {
                value = JsonSerializer.Deserialize<List<TItem>>(text, options) ?? [];
                return true;
            }
            catch
            {
                value = [];
                return false;
            }
        }
    }
}
