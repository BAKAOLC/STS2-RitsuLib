using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Reads and writes one value in a persisted <typeparamref name="TModel" /> through the owning mod's data
    ///         store.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过所属模组的数据存储读写持久化 <typeparamref name="TModel" /> 中的一个值。
    ///     </para>
    /// </summary>
    /// <typeparam name="TModel">
    ///     <para xml:lang="en">The persisted model type.</para>
    ///     <para xml:lang="zh-CN">持久化模型类型。</para>
    /// </typeparam>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The bound value type.</para>
    ///     <para xml:lang="zh-CN">绑定值类型。</para>
    /// </typeparam>
    /// <param name="modId">
    ///     <para xml:lang="en">The ID of the mod that owns the model.</para>
    ///     <para xml:lang="zh-CN">所属模组的 ID。</para>
    /// </param>
    /// <param name="dataKey">
    ///     <para xml:lang="en">The persisted model's data key.</para>
    ///     <para xml:lang="zh-CN">持久化模型的数据键。</para>
    /// </param>
    /// <param name="scope">
    ///     <para xml:lang="en">The model's save scope.</para>
    ///     <para xml:lang="zh-CN">模型的保存作用域。</para>
    /// </param>
    /// <param name="getter">
    ///     <para xml:lang="en">The function that reads the value from the model.</para>
    ///     <para xml:lang="zh-CN">从模型读取值的函数。</para>
    /// </param>
    /// <param name="setter">
    ///     <para xml:lang="en">The callback that writes the value into the model.</para>
    ///     <para xml:lang="zh-CN">将值写入模型的回调。</para>
    /// </param>
    public sealed class ModSettingsValueBinding<TModel, TValue>(
        string modId,
        string dataKey,
        SaveScope scope,
        Func<TModel, TValue> getter,
        Action<TModel, TValue> setter)
        : IModSettingsValueBinding<TValue>
        where TModel : class, new()
    {
        private readonly Func<TModel, TValue> _getter =
            ModSettingsBindingValidation.RequireNonNull(getter, nameof(getter));

        private readonly Action<TModel, TValue> _setter =
            ModSettingsBindingValidation.RequireNonNull(setter, nameof(setter));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the mod ID used to resolve <see cref="RitsuLibFramework.GetDataStore" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用于解析 <see cref="RitsuLibFramework.GetDataStore" /> 的模组 ID。
        ///     </para>
        /// </summary>
        public string ModId { get; } = ModSettingsBindingValidation.RequireNonEmpty(modId, nameof(modId));

        /// <summary>
        ///     <para xml:lang="en">Gets the persisted model's data key.</para>
        ///     <para xml:lang="zh-CN">获取持久化模型的数据键。</para>
        /// </summary>
        public string DataKey { get; } = ModSettingsBindingValidation.RequireNonEmpty(dataKey, nameof(dataKey));

        /// <summary>
        ///     <para xml:lang="en">Gets the save scope of the backing data-store entry.</para>
        ///     <para xml:lang="zh-CN">获取底层数据存储条目的保存作用域。</para>
        /// </summary>
        public SaveScope Scope { get; } = scope;

        /// <summary>
        ///     <para xml:lang="en">Reads the current value from the stored model.</para>
        ///     <para xml:lang="zh-CN">从已存储的模型读取当前值。</para>
        /// </summary>
        public TValue Read()
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            return _getter(store.Get<TModel>(DataKey));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes the value to the in-memory model. Call <see cref="Save" /> to persist the model's data key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将值写入内存中的模型。调用 <see cref="Save" /> 可持久化该模型的数据键。
        ///     </para>
        /// </summary>
        public void Write(TValue value)
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Modify<TModel>(DataKey, model => _setter(model, value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <summary>
        ///     <para xml:lang="en">Persists this binding's data key through the owning mod's data store.</para>
        ///     <para xml:lang="zh-CN">通过所属模组的数据存储持久化该绑定的数据键。</para>
        /// </summary>
        public void Save()
        {
            RitsuLibFramework.GetDataStore(ModId).Save(DataKey);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Stores a transient setting in memory for previews, tests, or temporary UI and provides a JSON adapter for
    ///         cloning and clipboard operations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为预览、测试或临时界面在内存中保存瞬时设置，并提供用于克隆与剪贴板操作的 JSON 适配器。
    ///     </para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The setting value type.</para>
    ///     <para xml:lang="zh-CN">设置值类型。</para>
    /// </typeparam>
    /// <param name="modId">
    ///     <para xml:lang="en">The logical owning mod ID used for UI identity.</para>
    ///     <para xml:lang="zh-CN">用于界面标识的逻辑所属模组 ID。</para>
    /// </param>
    /// <param name="dataKey">
    ///     <para xml:lang="en">The logical data key used for UI identity.</para>
    ///     <para xml:lang="zh-CN">用于界面标识的逻辑数据键。</para>
    /// </param>
    /// <param name="initialValue">
    ///     <para xml:lang="en">The initial value and source for future default-value clones.</para>
    ///     <para xml:lang="zh-CN">初始值，也是之后创建默认值副本的来源。</para>
    /// </param>
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
        ///     <para xml:lang="en">Gets the logical mod ID used for UI identity; it is not persisted.</para>
        ///     <para xml:lang="zh-CN">获取用于界面标识的逻辑模组 ID；该 ID 不会被持久化。</para>
        /// </summary>
        public string ModId { get; } = ModSettingsBindingValidation.RequireNonEmpty(modId, nameof(modId));

        /// <summary>
        ///     <para xml:lang="en">Gets the logical data key used for UI identity.</para>
        ///     <para xml:lang="zh-CN">获取用于界面标识的逻辑数据键。</para>
        /// </summary>
        public string DataKey { get; } = ModSettingsBindingValidation.RequireNonEmpty(dataKey, nameof(dataKey));

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets <see cref="SaveScope.Global" /> for interface compatibility. <see cref="Save" /> performs no
        ///         operation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为满足接口约定而返回 <see cref="SaveScope.Global" />；<see cref="Save" /> 不执行任何操作。
        ///     </para>
        /// </summary>
        public SaveScope Scope => SaveScope.Global;

        /// <summary>
        ///     <para xml:lang="en">Gets the JSON adapter used for cloning and clipboard operations.</para>
        ///     <para xml:lang="zh-CN">获取用于克隆与剪贴板操作的 JSON 适配器。</para>
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = ModSettingsStructuredData.Json<TValue>();

        /// <summary>
        ///     <para xml:lang="en">Reads the current in-memory value.</para>
        ///     <para xml:lang="zh-CN">读取当前内存值。</para>
        /// </summary>
        public TValue Read()
        {
            return _value;
        }

        /// <summary>
        ///     <para xml:lang="en">Writes the current in-memory value and publishes a value-written notification.</para>
        ///     <para xml:lang="zh-CN">写入当前内存值并发布值写入通知。</para>
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
    ///     <para xml:lang="en">
    ///         Adds a structured value adapter to an existing binding while forwarding its reads, writes, and
    ///         persistence.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为现有绑定添加结构化值适配器，同时转发其读取、写入与持久化操作。
    ///     </para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The bound value type.</para>
    ///     <para xml:lang="zh-CN">绑定值类型。</para>
    /// </typeparam>
    /// <param name="inner">
    ///     <para xml:lang="en">The binding to decorate.</para>
    ///     <para xml:lang="zh-CN">要装饰的绑定。</para>
    /// </param>
    /// <param name="adapter">
    ///     <para xml:lang="en">The structured value adapter to expose.</para>
    ///     <para xml:lang="zh-CN">要公开的结构化值适配器。</para>
    /// </param>
    public sealed class StructuredModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        IStructuredModSettingsValueAdapter<TValue> adapter)
        : IStructuredModSettingsValueBinding<TValue>, IModSettingsUiRefreshPropagation,
            IModSettingsUiRefreshEquivalence,
            IModSettingsBindingSaveDispatch
    {
        private readonly IModSettingsValueBinding<TValue> _inner =
            ModSettingsBindingValidation.RequireNonNull(inner, nameof(inner));

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_inner];

        /// <inheritdoc />
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [_inner];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [_inner];

        /// <inheritdoc />
        public string ModId => _inner.ModId;

        /// <inheritdoc />
        public string DataKey => _inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => _inner.Scope;

        /// <summary>
        ///     <para xml:lang="en">Gets the adapter used for cloning, serialization, and clipboard operations.</para>
        ///     <para xml:lang="zh-CN">获取用于克隆、序列化与剪贴板操作的适配器。</para>
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            ModSettingsBindingValidation.RequireNonNull(adapter, nameof(adapter));

        /// <inheritdoc />
        public TValue Read()
        {
            return _inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            _inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _inner.Save();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Projects a child value from a parent binding, such as one field of a settings record, and writes changes
    ///         back by replacing the parent value.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从父绑定投影子值（例如设置记录中的一个字段），并通过替换父值写回更改。
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">
    ///     <para xml:lang="en">The parent value type.</para>
    ///     <para xml:lang="zh-CN">父值类型。</para>
    /// </typeparam>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The projected value type.</para>
    ///     <para xml:lang="zh-CN">投影值类型。</para>
    /// </typeparam>
    /// <param name="parent">
    ///     <para xml:lang="en">The parent binding.</para>
    ///     <para xml:lang="zh-CN">父绑定。</para>
    /// </param>
    /// <param name="dataKey">
    ///     <para xml:lang="en">The optional child segment appended to the parent data key.</para>
    ///     <para xml:lang="zh-CN">追加到父数据键的可选子片段。</para>
    /// </param>
    /// <param name="getter">
    ///     <para xml:lang="en">The function that reads the projected value.</para>
    ///     <para xml:lang="zh-CN">读取投影值的函数。</para>
    /// </param>
    /// <param name="setter">
    ///     <para xml:lang="en">The function that returns a parent value containing the projected change.</para>
    ///     <para xml:lang="zh-CN">返回包含投影更改的父值的函数。</para>
    /// </param>
    /// <param name="adapter">
    ///     <para xml:lang="en">An optional adapter for the projected value type.</para>
    ///     <para xml:lang="zh-CN">投影值类型的可选适配器。</para>
    /// </param>
    public sealed class ProjectedModSettingsValueBinding<TSource, TValue>(
        IModSettingsValueBinding<TSource> parent,
        string dataKey,
        Func<TSource, TValue> getter,
        Func<TSource, TValue, TSource> setter,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IModSettingsUiRefreshPropagation, IModSettingsBindingSaveDispatch
    {
        private readonly Func<TSource, TValue> _getter =
            ModSettingsBindingValidation.RequireNonNull(getter, nameof(getter));

        private readonly IModSettingsValueBinding<TSource> _parent =
            ModSettingsBindingValidation.RequireNonNull(parent, nameof(parent));

        private readonly Func<TSource, TValue, TSource> _setter =
            ModSettingsBindingValidation.RequireNonNull(setter, nameof(setter));

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_parent];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [_parent];

        /// <inheritdoc />
        public string ModId => _parent.ModId;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets <c>parent.DataKey.{segment}</c> when the child segment is not blank; otherwise, gets the parent's
        ///         data key.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         子片段非空白时获取 <c>parent.DataKey.{segment}</c>；否则获取父绑定的数据键。
        ///     </para>
        /// </summary>
        public string DataKey =>
            string.IsNullOrWhiteSpace(dataKey) ? _parent.DataKey : $"{_parent.DataKey}.{dataKey}";

        /// <inheritdoc />
        public SaveScope Scope => _parent.Scope;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the projected value adapter, using the built-in JSON adapter when none was supplied.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取投影值适配器；未提供时使用内置 JSON 适配器。
        ///     </para>
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            adapter ?? ModSettingsStructuredData.Json<TValue>();

        /// <inheritdoc />
        public TValue Read()
        {
            return _getter(_parent.Read());
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            var source = _parent.Read();
            _parent.Write(_setter(source, value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _parent.Save();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Adds a reset default-value factory to a binding and exposes the structured adapter used by clipboard
    ///         operations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为绑定添加供重置使用的默认值工厂，并公开剪贴板操作使用的结构化适配器。
    ///     </para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The bound value type.</para>
    ///     <para xml:lang="zh-CN">绑定值类型。</para>
    /// </typeparam>
    /// <param name="inner">
    ///     <para xml:lang="en">The binding to decorate.</para>
    ///     <para xml:lang="zh-CN">要装饰的绑定。</para>
    /// </param>
    /// <param name="defaultValueFactory">
    ///     <para xml:lang="en">The function that creates a value for each reset operation.</para>
    ///     <para xml:lang="zh-CN">为每次重置操作创建值的函数。</para>
    /// </param>
    /// <param name="adapter">
    ///     <para xml:lang="en">
    ///         The fallback structured adapter when the inner binding does not already provide one.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         内部绑定尚未提供结构化适配器时使用的回退适配器。
    ///     </para>
    /// </param>
    public sealed class DefaultModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Func<TValue> defaultValueFactory,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IDefaultModSettingsValueBinding<TValue>,
            IModSettingsUiRefreshPropagation, IModSettingsUiRefreshEquivalence, IModSettingsBindingSaveDispatch
    {
        private readonly Func<TValue> _defaultValueFactory =
            ModSettingsBindingValidation.RequireNonNull(defaultValueFactory, nameof(defaultValueFactory));

        private readonly IModSettingsValueBinding<TValue> _inner =
            ModSettingsBindingValidation.RequireNonNull(inner, nameof(inner));

        /// <inheritdoc />
        public TValue CreateDefaultValue()
        {
            return _defaultValueFactory();
        }

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_inner];

        /// <inheritdoc />
        public IReadOnlyList<IModSettingsBinding> UiRefreshAlsoTreatAsDirty => [_inner];

        /// <inheritdoc />
        public IEnumerable<IModSettingsBinding> ExtraBindingsToMarkDirtyForUi => [_inner];

        /// <inheritdoc />
        public string ModId => _inner.ModId;

        /// <inheritdoc />
        public string DataKey => _inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => _inner.Scope;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the inner binding's structured adapter when available; otherwise, gets the supplied fallback or
        ///         the built-in JSON adapter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         内部绑定提供结构化适配器时获取该适配器；否则获取所提供的回退适配器或内置 JSON 适配器。
        ///     </para>
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } =
            inner is IStructuredModSettingsValueBinding<TValue> structured
                ? structured.Adapter
                : adapter ?? ModSettingsStructuredData.Json<TValue>();

        /// <inheritdoc />
        public TValue Read()
        {
            return _inner.Read();
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            _inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _inner.Save();
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
            catch (JsonException)
            {
                value = default!;
                return false;
            }
            catch (NotSupportedException)
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
            if (itemAdapter != null)
                return [.. value.Select(itemAdapter.Clone)];

            var json = JsonSerializer.Serialize(value, options);
            return JsonSerializer.Deserialize<List<TItem>>(json, options) ?? [];
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
            catch (JsonException)
            {
                value = [];
                return false;
            }
            catch (NotSupportedException)
            {
                value = [];
                return false;
            }
        }
    }
}
