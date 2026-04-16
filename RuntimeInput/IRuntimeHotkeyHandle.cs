namespace STS2RitsuLib.RuntimeInput
{
    /// <summary>
    ///     Represents a registered runtime hotkey that can be rebound or unregistered explicitly by the caller.
    /// </summary>
    public interface IRuntimeHotkeyHandle : IDisposable
    {
        /// <summary>
        ///     Gets the current normalized binding string for this registration.
        /// </summary>
        string CurrentBinding { get; }

        /// <summary>
        ///     Gets whether this handle is still registered with the runtime hotkey router.
        /// </summary>
        bool IsRegistered { get; }

        /// <summary>
        ///     Replaces the binding with a newly persisted binding string.
        /// </summary>
        /// <param name="bindingText">Binding text to parse and apply.</param>
        /// <param name="normalizedBinding">The normalized binding string if parsing succeeded.</param>
        /// <returns><c>true</c> when the new binding was parsed and applied.</returns>
        bool TryRebind(string bindingText, out string normalizedBinding);

        /// <summary>
        ///     Returns a read-only snapshot describing the current registration.
        /// </summary>
        /// <param name="registrationInfo">Registration snapshot when this handle is still active.</param>
        /// <returns><c>true</c> when this handle is still registered.</returns>
        bool TryGetRegistrationInfo(out RuntimeHotkeyRegistrationInfo registrationInfo);

        /// <summary>
        ///     Removes this registration from the runtime hotkey router.
        /// </summary>
        void Unregister();
    }
}
