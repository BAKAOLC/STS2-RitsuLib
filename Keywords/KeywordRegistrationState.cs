namespace STS2RitsuLib.Keywords
{
    /// <summary>
    ///     Whether <see cref="ModKeywordRegistry" /> still accepts new keyword registrations from mods.
    /// </summary>
    public enum KeywordRegistrationState
    {
        /// <summary>
        ///     Registrations are allowed until the framework freezes them (with other model registries).
        /// </summary>
        Open = 0,

        /// <summary>
        ///     Further registration throws; the global keyword table is considered sealed.
        /// </summary>
        Frozen = 1,
    }
}
