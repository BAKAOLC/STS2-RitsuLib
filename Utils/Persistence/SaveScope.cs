namespace STS2RitsuLib.Utils.Persistence
{
    /// <summary>
    ///     Defines the scope of save data storage
    /// </summary>
    public enum SaveScope
    {
        /// <summary>
        ///     Global scope - data is shared across all profiles
        /// </summary>
        Global,

        /// <summary>
        ///     Profile scope - data is specific to the current profile
        /// </summary>
        Profile,

        /// <summary>
        ///     Per-run sidecar scope under the current profile (client-local).
        /// </summary>
        /// <remarks>
        ///     This scope requires a run fingerprint stem context to resolve a durable path.
        /// </remarks>
        RunSidecar,

        /// <summary>
        ///     In-memory scope (not persisted).
        /// </summary>
        InMemory,
    }
}
