using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Identifies a mod settings value stored under a mod id, data key, and <see cref="SaveScope" />.
    /// </summary>
    public interface IModSettingsBinding
    {
        /// <summary>
        ///     Owning mod id for persistence and UI grouping.
        /// </summary>
        string ModId { get; }

        /// <summary>
        ///     Stable key within the mod’s settings store.
        /// </summary>
        string DataKey { get; }

        /// <summary>
        ///     Whether the value is profile-scoped or global.
        /// </summary>
        SaveScope Scope { get; }

        /// <summary>
        ///     Persists the current in-memory value to the active save scope.
        /// </summary>
        void Save();
    }

    /// <summary>
    ///     Read/write binding for a single settings value of type <typeparamref name="TValue" />.
    /// </summary>
    /// <typeparam name="TValue">Serialized settings payload type.</typeparam>
    public interface IModSettingsValueBinding<TValue> : IModSettingsBinding
    {
        /// <summary>
        ///     Reads the current value from the backing store (or default if unset).
        /// </summary>
        TValue Read();

        /// <summary>
        ///     Writes and optionally stages persistence depending on host policy.
        /// </summary>
        void Write(TValue value);
    }

    /// <summary>
    ///     Binding that can synthesize a default when no stored value exists.
    /// </summary>
    /// <typeparam name="TValue">Serialized settings payload type.</typeparam>
    public interface IDefaultModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     Factory for the value used when the store has no entry.
        /// </summary>
        TValue CreateDefaultValue();
    }

    /// <summary>
    ///     Marker binding that is not written to disk (preview / transient UI state).
    /// </summary>
    public interface ITransientModSettingsBinding : IModSettingsBinding;

    /// <summary>
    ///     Converts between live objects and clipboard/JSON text for structured settings.
    /// </summary>
    /// <typeparam name="TValue">Structured settings type.</typeparam>
    public interface IStructuredModSettingsValueAdapter<TValue>
    {
        /// <summary>
        ///     Deep or defensive copy for editor sessions.
        /// </summary>
        TValue Clone(TValue value);

        /// <summary>
        ///     Serializes <paramref name="value" /> to a single text blob (e.g. JSON).
        /// </summary>
        string Serialize(TValue value);

        /// <summary>
        ///     Parses <paramref name="text" /> into <paramref name="value" />.
        /// </summary>
        bool TryDeserialize(string text, out TValue value);
    }

    /// <summary>
    ///     Value binding that uses an <see cref="IStructuredModSettingsValueAdapter{TValue}" /> for serialization.
    /// </summary>
    /// <typeparam name="TValue">Structured settings type.</typeparam>
    public interface IStructuredModSettingsValueBinding<TValue> : IModSettingsValueBinding<TValue>
    {
        /// <summary>
        ///     Adapter used for clone/serialize/deserialize operations.
        /// </summary>
        IStructuredModSettingsValueAdapter<TValue> Adapter { get; }
    }
}
