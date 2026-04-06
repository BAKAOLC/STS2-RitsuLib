namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Optional marker on a binding that participates in session-based read-only UI rules
    ///     (<see cref="ModSettingsEditPolicy" />).
    /// </summary>
    public interface IModSettingsSessionEditGate : IModSettingsBinding
    {
        /// <summary>
        ///     When true, the settings row should not accept edits in the current session context.
        /// </summary>
        bool IsReadOnlyInCurrentSession { get; }
    }

    /// <summary>
    ///     Marker for bindings that keep a per-run overlay; used for UI chips and host-value application.
    /// </summary>
    public interface IModSettingsRunOverlayBinding : IModSettingsBinding
    {
        /// <summary>
        ///     Stable slot id: <c>modId|dataKey</c> of the inner binding.
        /// </summary>
        string OverlaySlotKey { get; }
    }
}
