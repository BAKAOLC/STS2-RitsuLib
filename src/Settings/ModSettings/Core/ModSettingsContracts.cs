using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Identifies a mod setting by its owning mod, data key, and <see cref="SaveScope" /> and exposes its save
    ///         operation.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过所属模组、数据键与 <see cref="SaveScope" /> 标识模组设置，并公开其保存操作。
    ///     </para>
    /// </summary>
    public interface IModSettingsBinding
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the owning mod ID used for binding identity and UI grouping.</para>
        ///     <para xml:lang="zh-CN">获取用于绑定标识与界面分组的所属模组 ID。</para>
        /// </summary>
        string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the stable key that identifies the setting within the mod and, when applicable, its persistence
        ///         store.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取在模组内标识该设置的稳定键；适用时，该键也用于其持久化存储。
        ///     </para>
        /// </summary>
        string DataKey { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the declared global, profile, or in-memory save scope.</para>
        ///     <para xml:lang="zh-CN">获取声明的全局、档案或内存保存作用域。</para>
        /// </summary>
        SaveScope Scope { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Requests that the binding save its current state. An implementation may persist through RitsuLib,
        ///         delegate to another store, or perform no operation for transient state.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         请求绑定保存其当前状态。具体实现可通过 RitsuLib 持久化、转交其他存储，或对瞬时状态不执行操作。
        ///     </para>
        /// </summary>
        void Save();
    }

    /// <summary>
    ///     <para xml:lang="en">Reads and writes one setting value of type <typeparamref name="TValue" />.</para>
    ///     <para xml:lang="zh-CN">读写一个 <typeparamref name="TValue" /> 类型的设置值。</para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The setting value type.</para>
    ///     <para xml:lang="zh-CN">设置值类型。</para>
    /// </typeparam>
    public interface IModSettingsValueBinding<TValue> : IModSettingsBinding
    {
        /// <summary>
        ///     <para xml:lang="en">Reads the binding's current value from its backing source.</para>
        ///     <para xml:lang="zh-CN">从绑定的底层来源读取当前值。</para>
        /// </summary>
        TValue Read();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Writes the binding's current value. Persistence may be deferred or immediate depending on the
        ///         implementation; callers can request it explicitly through <see cref="IModSettingsBinding.Save" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         写入绑定的当前值。持久化可由实现延后或立即执行；调用方也可通过
        ///         <see cref="IModSettingsBinding.Save" /> 显式请求保存。
        ///     </para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to write.</para>
        ///     <para xml:lang="zh-CN">要写入的值。</para>
        /// </param>
        void Write(TValue value);
    }

    /// <summary>
    ///     <para xml:lang="en">Creates values for explicit reset-to-default operations.</para>
    ///     <para xml:lang="zh-CN">为显式恢复默认值操作创建值。</para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The setting value type.</para>
    ///     <para xml:lang="zh-CN">设置值类型。</para>
    /// </typeparam>
    public interface IDefaultModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     <para xml:lang="en">Creates a value for one explicit reset operation.</para>
        ///     <para xml:lang="zh-CN">为一次显式恢复默认值操作创建值。</para>
        /// </summary>
        TValue CreateDefaultValue();
    }

    /// <summary>
    ///     <para xml:lang="en">Marks a binding as transient and not written to persistent storage.</para>
    ///     <para xml:lang="zh-CN">将绑定标记为瞬时绑定，不写入持久化存储。</para>
    /// </summary>
    public interface ITransientModSettingsBinding : IModSettingsBinding;

    /// <summary>
    ///     <para xml:lang="en">
    ///         Clones structured setting values and converts them to and from text for editing and clipboard operations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         克隆结构化设置值，并在值与编辑、剪贴板操作所用文本之间进行转换。
    ///     </para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The structured setting value type.</para>
    ///     <para xml:lang="zh-CN">结构化设置值类型。</para>
    /// </typeparam>
    public interface IStructuredModSettingsValueAdapter<TValue>
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an independent or defensive copy for an editing session.</para>
        ///     <para xml:lang="zh-CN">为编辑会话创建独立副本或防御性副本。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to clone.</para>
        ///     <para xml:lang="zh-CN">要克隆的值。</para>
        /// </param>
        TValue Clone(TValue value);

        /// <summary>
        ///     <para xml:lang="en">Serializes a value to one text payload, such as JSON.</para>
        ///     <para xml:lang="zh-CN">将值序列化为一段文本载荷，例如 JSON。</para>
        /// </summary>
        /// <param name="value">
        ///     <para xml:lang="en">The value to serialize.</para>
        ///     <para xml:lang="zh-CN">要序列化的值。</para>
        /// </param>
        string Serialize(TValue value);

        /// <summary>
        ///     <para xml:lang="en">Attempts to parse a text payload into a structured value.</para>
        ///     <para xml:lang="zh-CN">尝试将文本载荷解析为结构化值。</para>
        /// </summary>
        /// <param name="text">
        ///     <para xml:lang="en">The text payload to parse.</para>
        ///     <para xml:lang="zh-CN">要解析的文本载荷。</para>
        /// </param>
        /// <param name="value">
        ///     <para xml:lang="en">The parsed value when successful.</para>
        ///     <para xml:lang="zh-CN">成功时解析出的值。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         <see langword="true" /> when parsing succeeds; otherwise, <see langword="false" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         解析成功时为 <see langword="true" />；否则为 <see langword="false" />。
        ///     </para>
        /// </returns>
        bool TryDeserialize(string text, out TValue value);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Exposes an <see cref="IStructuredModSettingsValueAdapter{TValue}" /> for a value binding's clone,
    ///         serialization, and clipboard operations.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为值绑定的克隆、序列化与剪贴板操作公开
    ///         <see cref="IStructuredModSettingsValueAdapter{TValue}" />。
    ///     </para>
    /// </summary>
    /// <typeparam name="TValue">
    ///     <para xml:lang="en">The structured setting value type.</para>
    ///     <para xml:lang="zh-CN">结构化设置值类型。</para>
    /// </typeparam>
    public interface IStructuredModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the adapter used for clone, serialization, and deserialization operations.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取用于克隆、序列化与反序列化操作的适配器。</para>
        /// </summary>
        IStructuredModSettingsValueAdapter<TValue> Adapter { get; }
    }
}
