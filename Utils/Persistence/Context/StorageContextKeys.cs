namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Built-in <see cref="StorageContextKey{TValue}" /> values used by RitsuLib persistence.
    /// </summary>
    public static class StorageContextKeys
    {
        /// <summary>
        ///     Overrides the active game profile id for a persistence operation.
        /// </summary>
        public static StorageContextKey<int> ProfileId { get; } = new("sts2ritsulib.profileId");

        /// <summary>
        ///     Stable per-run fingerprint stem used by run sidecar storage.
        /// </summary>
        /// <remarks>
        ///     This is expected to match <c>ModRunSidecarFingerprint.ComputeFileStem()</c> output.
        /// </remarks>
        public static StorageContextKey<string> RunFingerprintStem { get; } = new("sts2ritsulib.runFingerprintStem");
    }
}
