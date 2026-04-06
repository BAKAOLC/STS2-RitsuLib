namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Read-only snapshot of whether mod settings UI is opened in an active run and basic multiplayer role hints.
    ///     Updated by <see cref="ModSettingsRunSessionCoordinator" /> from run lifecycle.
    /// </summary>
    public static class ModSettingsSessionState
    {
        /// <summary>
        ///     True between run start/load and run end (victory, defeat, or abandon).
        /// </summary>
        public static bool IsInActiveRun { get; internal set; }

        /// <summary>
        ///     True when the current run uses a non-singleplayer net session.
        /// </summary>
        public static bool IsMultiplayerRun { get; internal set; }

        /// <summary>
        ///     True when this machine is a multiplayer client (not host / not singleplayer).
        ///     Host-authoritative overlay bindings ignore local writes while this is true.
        /// </summary>
        public static bool IsNetClient { get; internal set; }

        internal static void ClearRunFlags()
        {
            IsInActiveRun = false;
            IsMultiplayerRun = false;
            IsNetClient = false;
        }
    }
}
