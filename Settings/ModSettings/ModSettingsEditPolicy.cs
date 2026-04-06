namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Where a setting may be edited in the RitsuLib mod settings UI.
    /// </summary>
    public enum ModSettingsEditPolicy
    {
        /// <summary>
        ///     Editable in main menu and during a run (subject to other gates such as run overlay authority).
        /// </summary>
        Anywhere,

        /// <summary>
        ///     Read-only while <see cref="ModSettingsSessionState.IsInActiveRun" /> is true.
        /// </summary>
        OutOfRunOnly,

        /// <summary>
        ///     Read-only while not in an active run (e.g. tuning that only applies mid-run).
        /// </summary>
        InRunOnly,
    }

    /// <summary>
    ///     Who may change run-overlay values in multiplayer; actual replication is left to consuming mods.
    /// </summary>
    public enum ModSettingsRunOverlayAuthority
    {
        /// <summary>
        ///     Each peer keeps a local overlay (default).
        /// </summary>
        Independent,

        /// <summary>
        ///     Local writes are ignored on multiplayer clients; host (or singleplayer) may write.
        ///     Apply remote host payloads with <see cref="ModSettingsRunSession.TryApplyHostOverlayValue{TValue}" />.
        /// </summary>
        HostAuthoritative,
    }

    /// <summary>
    ///     What happens to in-memory run overlay values when the run ends.
    /// </summary>
    public enum ModSettingsRunOverlayCommitMode
    {
        /// <summary>
        ///     On run end, write overlay back to the inner persisted binding and save once.
        /// </summary>
        CommitToPersistenceOnRunEnd,

        /// <summary>
        ///     Drop overlay at run end; persisted store keeps pre-run values.
        /// </summary>
        DiscardOnRunEnd,
    }
}
