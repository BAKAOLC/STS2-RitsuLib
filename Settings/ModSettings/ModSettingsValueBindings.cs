using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Value binding that reads/writes a field of persisted model <typeparamref name="TModel" /> via the mod data store.
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
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Key of the persisted model blob.
        /// </summary>
        public string DataKey { get; } = dataKey;

        /// <summary>
        ///     Persistence scope for the backing store entry.
        /// </summary>
        public SaveScope Scope { get; } = scope;

        /// <summary>
        ///     Reads the current value from the model in the store.
        /// </summary>
        public TValue Read()
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            return getter(store.Get<TModel>(DataKey));
        }

        /// <summary>
        ///     Mutates the model in memory (call <see cref="Save" /> to flush).
        /// </summary>
        public void Write(TValue value)
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Modify<TModel>(DataKey, model => setter(model, value));
        }

        /// <summary>
        ///     Persists the data key for this mod.
        /// </summary>
        public void Save()
        {
            RitsuLibFramework.GetDataStore(ModId).Save(DataKey);
        }
    }

    /// <summary>
    ///     In-memory binding for previews, tests, or non-persisted UI; uses JSON adapter for structured clipboard.
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
        /// </summary>
        public string ModId { get; } = modId;

        /// <summary>
        ///     Logical data key segment.
        /// </summary>
        public string DataKey { get; } = dataKey;

        /// <summary>
        ///     Always <see cref="SaveScope.Global" />; <see cref="Save" /> is a no-op.
        /// </summary>
        public SaveScope Scope => SaveScope.Global;

        /// <summary>
        ///     JSON round-trip adapter for clone and clipboard.
        /// </summary>
        public IStructuredModSettingsValueAdapter<TValue> Adapter { get; } = ModSettingsStructuredData.Json<TValue>();

        /// <summary>
        ///     Returns the current in-memory value.
        /// </summary>
        public TValue Read()
        {
            return _value;
        }

        /// <summary>
        ///     Sets the in-memory value.
        /// </summary>
        public void Write(TValue value)
        {
            _value = value;
        }

        /// <inheritdoc />
        public void Save()
        {
        }
    }

    /// <summary>
    ///     Wraps an inner binding and attaches a structured adapter without changing persistence behavior.
    /// </summary>
    public sealed class StructuredModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        IStructuredModSettingsValueAdapter<TValue> adapter)
        : IStructuredModSettingsValueBinding<TValue>
    {
        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <summary>
        ///     Adapter used for serialization and clipboard.
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
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }

    /// <summary>
    ///     Binding that projects a child value out of a parent binding (e.g. one field of a settings record).
    /// </summary>
    public sealed class ProjectedModSettingsValueBinding<TSource, TValue>(
        IModSettingsValueBinding<TSource> parent,
        string dataKey,
        Func<TSource, TValue> getter,
        Func<TSource, TValue, TSource> setter,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>
    {
        /// <inheritdoc />
        public string ModId => parent.ModId;

        /// <summary>
        ///     Composite key <c>parent.DataKey.{segment}</c> when the constructor segment is non-empty; otherwise the parent
        ///     data key.
        /// </summary>
        public string DataKey => string.IsNullOrWhiteSpace(dataKey) ? parent.DataKey : $"{parent.DataKey}.{dataKey}";

        /// <inheritdoc />
        public SaveScope Scope => parent.Scope;

        /// <summary>
        ///     Adapter for the projected type; defaults to JSON when the parent is not structured.
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
        }

        /// <inheritdoc />
        public void Save()
        {
            parent.Save();
        }
    }

    /// <summary>
    ///     Decorates a binding with default-value factory and structured adapter resolution for reset and clipboard.
    /// </summary>
    public sealed class DefaultModSettingsValueBinding<TValue>(
        IModSettingsValueBinding<TValue> inner,
        Func<TValue> defaultValueFactory,
        IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        : IStructuredModSettingsValueBinding<TValue>, IDefaultModSettingsValueBinding<TValue>
    {
        /// <inheritdoc />
        public TValue CreateDefaultValue()
        {
            return defaultValueFactory();
        }

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <summary>
        ///     Adapter from the inner structured binding when present; otherwise the optional constructor adapter or JSON
        ///     default.
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
