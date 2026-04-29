namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Declares which bindings receive a direct <c>Save()</c> call when this binding's <see cref="IModSettingsBinding" />
    ///     persistence runs. Used to deduplicate deferred flush work across decorator stacks.
    /// </summary>
    internal interface IModSettingsBindingSaveDispatch
    {
        /// <summary>
        ///     Non-recursive targets: bindings invoked immediately by this instance's <c>Save()</c> (typically one inner or
        ///     parent).
        /// </summary>
        IReadOnlyList<IModSettingsBinding> ImmediateSaveTargets { get; }
    }
}
