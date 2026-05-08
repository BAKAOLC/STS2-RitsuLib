namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Strongly-typed key used to store and retrieve values from <see cref="StorageContext" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Keys have a stable string identifier so they can be logged and compared safely across assemblies.
    ///     </para>
    /// </remarks>
    public sealed class StorageContextKey<TValue>(string id)
    {
        /// <summary>
        ///     Stable identifier for this context key.
        /// </summary>
        public string Id { get; } = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Context key id must not be empty.", nameof(id))
            : id.Trim();
    }
}
