using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Binds a mod settings control to arbitrary getters/setters and a custom <see cref="Save" /> implementation
    ///     (for example BaseLib JSON configs or third-party stores) without using
    ///     <see cref="RitsuLibFramework.GetDataStore" />.
    /// </summary>
    public sealed class ModSettingsCallbackValueBinding<T>(
        string modId,
        string dataKey,
        SaveScope scope,
        Func<T> read,
        Action<T> write,
        Action save) : IModSettingsValueBinding<T>
    {
        /// <inheritdoc />
        public string ModId { get; } = modId;

        /// <inheritdoc />
        public string DataKey { get; } = dataKey;

        /// <inheritdoc />
        public SaveScope Scope { get; } = scope;

        /// <inheritdoc />
        public T Read()
        {
            return read();
        }

        /// <inheritdoc />
        public void Write(T value)
        {
            write(value);
        }

        /// <inheritdoc />
        public void Save()
        {
            save();
        }
    }
}
