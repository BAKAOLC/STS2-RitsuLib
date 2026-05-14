using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Identifies a mod settings value stored under a mod id, data key, and <see cref="SaveScope" />.
    ///     标识存储在 mod id、data key 和 <see cref="SaveScope" /> 下的 mod 设置值。
    /// </summary>
    public interface IModSettingsBinding
    {
        /// <summary>
        ///     Owning mod id for persistence and UI grouping.
        ///     用于持久化和 UI 分组的所属 Mod id。
        /// </summary>
        string ModId { get; }

        /// <summary>
        ///     Stable key within the mod’s settings store.
        ///     Mod 设置存储中的稳定键。
        /// </summary>
        string DataKey { get; }

        /// <summary>
        ///     Whether the value is profile-scoped or global.
        ///     该值是档案作用域还是全局作用域。
        /// </summary>
        SaveScope Scope { get; }

        /// <summary>
        ///     Persists the current in-memory value to the active save scope.
        ///     将当前内存值持久化到激活的保存作用域。
        /// </summary>
        void Save();
    }

    /// <summary>
    ///     Read/write binding for a single settings value of type <typeparamref name="TValue" />.
    ///     类型为 <typeparamref name="TValue" /> 的单个设置值的读 / 写绑定。
    /// </summary>
    /// <typeparam name="TValue">
    ///     Serialized settings payload type.
    ///     序列化设置载荷类型。
    /// </typeparam>
    public interface IModSettingsValueBinding<TValue> : IModSettingsBinding
    {
        /// <summary>
        ///     Reads the current value from the backing store (or default if unset).
        ///     从后端存储读取当前值（未设置时读取默认值）。
        /// </summary>
        TValue Read();

        /// <summary>
        ///     Writes and optionally stages persistence depending on host policy.
        ///     写入值，并根据宿主策略选择是否暂存持久化。
        /// </summary>
        void Write(TValue value);
    }

    /// <summary>
    ///     Binding that can synthesize a default when no stored value exists.
    ///     当不存在已存储值时，可合成默认值的绑定。
    /// </summary>
    /// <typeparam name="TValue">
    ///     Serialized settings payload type.
    ///     序列化设置载荷类型。
    /// </typeparam>
    public interface IDefaultModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     Factory for the value used when the store has no entry.
        ///     当存储中没有条目时使用的值工厂。
        /// </summary>
        TValue CreateDefaultValue();
    }

    /// <summary>
    ///     Marker binding that is not written to disk (preview / transient UI state).
    ///     不写入磁盘的标记绑定（预览/临时 UI 状态）。
    /// </summary>
    public interface ITransientModSettingsBinding : IModSettingsBinding;

    /// <summary>
    ///     Converts between live objects and clipboard/JSON text for structured settings.
    ///     为结构化设置在运行时对象和剪贴板/JSON 文本之间转换。
    /// </summary>
    /// <typeparam name="TValue">
    ///     Structured settings type.
    ///     结构化设置类型。
    /// </typeparam>
    public interface IStructuredModSettingsValueAdapter<TValue>
    {
        /// <summary>
        ///     Deep or defensive copy for editor sessions.
        ///     为编辑会话创建深拷贝或防御性副本。
        /// </summary>
        TValue Clone(TValue value);

        /// <summary>
        ///     Serializes <paramref name="value" /> to a single text blob (e.g. JSON).
        ///     将 <paramref name="value" /> 序列化为单个文本 blob（例如 JSON）。
        /// </summary>
        string Serialize(TValue value);

        /// <summary>
        ///     Parses <paramref name="text" /> into <paramref name="value" />.
        ///     将 <paramref name="text" /> 解析到 <paramref name="value" /> 中。
        /// </summary>
        bool TryDeserialize(string text, out TValue value);
    }

    /// <summary>
    ///     Value binding that uses an <see cref="IStructuredModSettingsValueAdapter{TValue}" /> for serialization.
    ///     使用 <see cref="IStructuredModSettingsValueAdapter{TValue}" /> 进行序列化的值绑定。
    /// </summary>
    /// <typeparam name="TValue">
    ///     Structured settings type.
    ///     结构化设置类型。
    /// </typeparam>
    public interface IStructuredModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     Adapter used for clone/serialize/deserialize operations.
        ///     用于克隆、序列化和反序列化操作的适配器。
        /// </summary>
        IStructuredModSettingsValueAdapter<TValue> Adapter { get; }
    }

    /// <summary>
    ///     Marker for bindings backed by <see cref="T:STS2RitsuLib.Settings.RunSidecar.ModRunSidecarStore" />
    ///     (client-local JSON, never written into vanilla multiplayer packets).
    ///     由 <see cref="T:STS2RitsuLib.Settings.RunSidecar.ModRunSidecarStore" /> 支持的绑定标记
    ///     （客户端本地 JSON，永不写入原版多人数据包）。
    /// </summary>
    public interface IRunSidecarModSettingsBinding : IModSettingsBinding;
}
