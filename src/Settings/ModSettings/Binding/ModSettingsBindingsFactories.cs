using System.Text.Json;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates settings value bindings and composes their persistence, default-value, projection, and structured
    ///         data decorators.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         创建设置值绑定，并组合其持久化、默认值、投影与结构化数据装饰器。
    ///     </para>
    /// </summary>
    public static class ModSettingsBindings
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a data-store binding that reads and writes a value within a persisted
        ///         <typeparamref name="TModel" /> using an explicit <see cref="SaveScope" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建数据存储绑定，通过显式 <see cref="SaveScope" /> 读写持久化
        ///         <typeparamref name="TModel" /> 中的值。
        ///     </para>
        /// </summary>
        public static ModSettingsValueBinding<TModel, TValue> Create<TModel, TValue>(
            string modId,
            string dataKey,
            SaveScope scope,
            Func<TModel, TValue> getter,
            Action<TModel, TValue> setter)
            where TModel : class, new()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataKey);
            ArgumentNullException.ThrowIfNull(getter);
            ArgumentNullException.ThrowIfNull(setter);
            return new(modId, dataKey, scope, getter, setter);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a data-store binding in <see cref="SaveScope.Global" />.</para>
        ///     <para xml:lang="zh-CN">创建使用 <see cref="SaveScope.Global" /> 的数据存储绑定。</para>
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
        ///     <para xml:lang="en">Creates a data-store binding in <see cref="SaveScope.Profile" />.</para>
        ///     <para xml:lang="zh-CN">创建使用 <see cref="SaveScope.Profile" /> 的数据存储绑定。</para>
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
        ///     <para xml:lang="en">
        ///         Creates a non-persisted in-memory binding for previews, tests, or transient UI.
        ///     </para>
        ///     <para xml:lang="zh-CN">为预览、测试或临时界面创建不持久化的内存绑定。</para>
        /// </summary>
        public static InMemoryModSettingsValueBinding<TValue> InMemory<TValue>(
            string modId,
            string dataKey,
            TValue initialValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataKey);
            return new(modId, dataKey, initialValue);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a value binding backed by caller-provided read, write, and save callbacks, such as for legacy
        ///         configurations or external persistence. The default scope is <see cref="SaveScope.Global" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建由调用方提供的读取、写入和保存回调支持的值绑定，可用于旧版配置或外部持久化。
        ///         默认作用域为 <see cref="SaveScope.Global" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Wraps a binding with a structured value adapter used by cloning and clipboard operations.
        ///     </para>
        ///     <para xml:lang="zh-CN">使用结构化值适配器包装绑定，供克隆与剪贴板操作使用。</para>
        /// </summary>
        public static StructuredModSettingsValueBinding<TValue> WithAdapter<TValue>(
            IModSettingsValueBinding<TValue> inner,
            IStructuredModSettingsValueAdapter<TValue> adapter)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(adapter);
            return new(inner, adapter);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Wraps a binding with a default-value factory for reset operations and, when needed, a structured value
        ///         adapter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         使用供重置操作使用的默认值工厂包装绑定，并可在需要时提供结构化值适配器。
        ///     </para>
        /// </summary>
        public static DefaultModSettingsValueBinding<TValue> WithDefault<TValue>(
            IModSettingsValueBinding<TValue> inner,
            Func<TValue> defaultValueFactory,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(defaultValueFactory);
            return new(inner, defaultValueFactory, adapter);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a child binding that projects one value from a parent binding and writes changes back through
        ///         a caller-provided transformation.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建从父绑定投影单个值的子绑定，并通过调用方提供的转换将更改写回父绑定。
        ///     </para>
        /// </summary>
        public static ProjectedModSettingsValueBinding<TSource, TValue> Project<TSource, TValue>(
            IModSettingsValueBinding<TSource> parent,
            string dataKey,
            Func<TSource, TValue> getter,
            Func<TSource, TValue, TSource> setter,
            IStructuredModSettingsValueAdapter<TValue>? adapter = null)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(getter);
            ArgumentNullException.ThrowIfNull(setter);
            return new(parent, dataKey, getter, setter, adapter);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Creates the built-in structured value adapters used for cloning, serialization, and clipboard operations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         创建用于克隆、序列化与剪贴板操作的内置结构化值适配器。
    ///     </para>
    /// </summary>
    public static class ModSettingsStructuredData
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a JSON adapter using optional custom <see cref="JsonSerializerOptions" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建使用可选自定义 <see cref="JsonSerializerOptions" /> 的 JSON 适配器。
        ///     </para>
        /// </summary>
        public static IStructuredModSettingsValueAdapter<TValue> Json<TValue>(JsonSerializerOptions? options = null)
        {
            return new JsonStructuredValueAdapter<TValue>(options);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a list adapter. Cloning uses <paramref name="itemAdapter" /> for each item when supplied;
        ///         otherwise, it performs one JSON round trip for the entire list.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建列表适配器。提供 <paramref name="itemAdapter" /> 时逐项克隆；否则对整个列表执行一次
        ///         JSON 往返。
        ///     </para>
        /// </summary>
        public static IStructuredModSettingsValueAdapter<List<TItem>> List<TItem>(
            IStructuredModSettingsValueAdapter<TItem>? itemAdapter = null,
            JsonSerializerOptions? options = null)
        {
            return new ListStructuredValueAdapter<TItem>(itemAdapter, options);
        }
    }
}
