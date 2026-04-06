namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Public entry points for run overlay settings (multiplayer host push, introspection).
    /// </summary>
    public static class ModSettingsRunSession
    {
        /// <summary>
        ///     Applies a host-provided value to a host-authoritative run overlay binding.
        ///     Typical use: deserialize a network message on clients and call this with the same
        ///     <paramref name="modId" /> / <paramref name="dataKey" /> as the inner binding.
        /// </summary>
        /// <returns>True when a registered overlay accepted the value.</returns>
        public static bool TryApplyHostOverlayValue<TValue>(string modId, string dataKey, TValue value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataKey);

            var key = ModSettingsRunSessionCoordinator.MakeOverlaySlotKey(modId, dataKey);
            return ModSettingsRunSessionCoordinator.TryApplyHostOverlay(key, value!);
        }
    }
}
