using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Factory methods for <see cref="IModSettingsValueBinding{TValue}" /> and related wrappers.
    /// </summary>
    public static class ModSettingsBindings
    {
        /// <summary>
        ///     Creates a binding against <typeparamref name="TModel" /> with an explicit <see cref="SaveScope" />.
        /// </summary>
        public static ModSettingsValueBinding<TModel, TValue> Create<TModel, TValue>(
            string modId,
            string dataKey,
            SaveScope scope,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return new(modId, dataKey, scope, getter, setter);
        }

        /// <summary>
        ///     Shorthand for <see cref="SaveScope.Global" />.
        /// </summary>
        public static ModSettingsValueBinding<TModel, TValue> Global<TModel, TValue>(
            string modId,
            string dataKey,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return Create(modId, dataKey, SaveScope.Global, getter, setter);
        }

        /// <summary>
        ///     Shorthand for <see cref="SaveScope.Profile" />.
        /// </summary>
        public static ModSettingsValueBinding<TModel, TValue> Profile<TModel, TValue>(
            string modId,
            string dataKey,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            return Create(modId, dataKey, SaveScope.Profile, getter, setter);
        }

        /// <summary>
        ///     Non-persisted binding for previews and debug UI.
        /// </summary>
        public static InMemoryModSettingsValueBinding<TValue> InMemory<TValue>(
            string modId,
            string dataKey,
            TValue initialValue)
        {
            return new(modId, dataKey, initialValue);
        }

        /// <summary>
        ///     Value binding backed by caller-supplied read/write/save delegates (external persistence, legacy configs,
        ///     etc.). Uses <see cref="SaveScope.Global" /> by default.
        /// </summary>
        public static ModSettingsCallbackValueBinding<TValue> Callback<TValue>(
            string modId,
            string dataKey,
            Func<TValue> read,
            Action<TValue> write,
            Action save,
            SaveScope scope = SaveScope.Global)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataKey);
            ArgumentNullException.ThrowIfNull(read);
            ArgumentNullException.ThrowIfNull(write);
            ArgumentNullException.ThrowIfNull(save);

            return new(modId, dataKey, scope, read, write, save);
        }

        /// <summary>
        ///     Attaches a structured adapter for clipboard / JSON round-trip.
        /// </summary>
        public static StructuredModSettingsValueBinding<TValue> WithAdapter<TValue>(
            IModSettingsValueBinding<TValue> inner,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            return new(inner, adapter);
        }

        /// <summary>
        ///     Supplies a default when the store is empty; optional adapter for structured types.
        /// </summary>
        public static DefaultModSettingsValueBinding<TValue> WithDefault<TValue>(
            IModSettingsValueBinding<TValue> inner,
            Func<TValue> defaultValueFactory,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return new(inner, defaultValueFactory, adapter);
        }

        /// <summary>
        ///     Derives a child binding from a parent object graph.
        /// </summary>
        public static ProjectedModSettingsValueBinding<TSource, TValue> Project<TSource, TValue>(
            IModSettingsValueBinding<TSource> parent,
            string dataKey,
            Func<TSource, TValue> getter,
            Func<TSource, TValue, TSource> setter,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            return new(parent, dataKey, getter, setter, adapter);
        }
    }

    /// <summary>
    ///     Built-in <see cref="IStructuredModSettingsValueAdapter{TValue}" /> implementations.
    /// </summary>
    public static class ModSettingsStructuredData
    {
        /// <summary>
        ///     JSON adapter using optional custom <see cref="JsonSerializerOptions" />.
        /// </summary>
        public static IStructuredModSettingsValueAdapter<TValue> Json<TValue>(JsonSerializerOptions? options = null)
        {
            return new JsonStructuredValueAdapter<TValue>(options);
        }

        /// <summary>
        ///     List adapter; items use <paramref name="itemAdapter" /> or default JSON per element.
        /// </summary>
        public static IStructuredModSettingsValueAdapter<List<TItem>> List<TItem>(
            IStructuredModSettingsValueAdapter<TItem>? itemAdapter = null,
            JsonSerializerOptions? options = null)
        {
            return new ListStructuredValueAdapter<TItem>(itemAdapter, options);
        }
    }
}
