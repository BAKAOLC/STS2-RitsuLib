using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Factory methods for <see cref="IModSettingsValueBinding{TValue}" /> and related wrappers.
    ///     <see cref="IModSettingsValueBinding{TValue}" /> 及相关 wrapper 的工厂方法。
    /// </summary>
    public static class ModSettingsBindings
    {
        /// <summary>
        ///     Creates a binding against <typeparamref name="TModel" /> with an explicit <see cref="SaveScope" />.
        ///     使用显式 <see cref="SaveScope" /> 创建面向 <typeparamref name="TModel" /> 的绑定。
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
        ///     <see cref="SaveScope.Global" /> 的简写。
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
        ///     <see cref="SaveScope.Profile" /> 的简写。
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
        ///     用于预览和调试 UI 的非持久化绑定。
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
        ///     由调用方提供的读 / 写 / 保存委托支持的值绑定（外部持久化、旧版配置
        ///     等）。默认使用 <see cref="SaveScope.Global" />。
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
        ///     附加用于剪贴板 / JSON 往返的结构化适配器。
        /// </summary>
        public static StructuredModSettingsValueBinding<TValue> WithAdapter<TValue>(
            IModSettingsValueBinding<TValue> inner,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            return new(inner, adapter);
        }

        /// <summary>
        ///     Supplies a default when the store is empty; optional adapter for structured types.
        ///     在存储为空时提供默认值；可为结构化类型提供可选适配器。
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
        ///     从父对象图派生子绑定。
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
    ///     内置 <see cref="IStructuredModSettingsValueAdapter{TValue}" /> 实现。
    /// </summary>
    public static class ModSettingsStructuredData
    {
        /// <summary>
        ///     JSON adapter using optional custom <see cref="JsonSerializerOptions" />.
        ///     使用可选自定义 <see cref="JsonSerializerOptions" /> 的 JSON 适配器。
        /// </summary>
        public static IStructuredModSettingsValueAdapter<TValue> Json<TValue>(JsonSerializerOptions? options = null)
        {
            return new JsonStructuredValueAdapter<TValue>(options);
        }

        /// <summary>
        ///     List adapter; items use <paramref name="itemAdapter" /> or default JSON per element.
        ///     列表适配器；条目使用 <paramref name="itemAdapter" />，或每个元素使用默认 JSON。
        /// </summary>
        public static IStructuredModSettingsValueAdapter<List<TItem>> List<TItem>(
            IStructuredModSettingsValueAdapter<TItem>? itemAdapter = null,
            JsonSerializerOptions? options = null)
        {
            return new ListStructuredValueAdapter<TItem>(itemAdapter, options);
        }
    }
}
