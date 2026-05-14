namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Built-in <see cref="StorageContextKey{TValue}" /> values used by RitsuLib persistence.
    ///     RitsuLib 持久化使用的内置 <see cref="StorageContextKey{TValue}" /> 值。
    /// </summary>
    public static class StorageContextKeys
    {
        /// <summary>
        ///     Overrides the active game profile id for a persistence operation.
        ///     覆盖持久化操作的活动游戏档案 id。
        /// </summary>
        public static StorageContextKey<int> ProfileId { get; } = new("sts2ritsulib.profileId");

        /// <summary>
        ///     Stable per-run fingerprint stem used by run sidecar storage.
        ///     稳定 per-跑局 fingerprint stem used by 跑局 sidecar 存储.
        /// </summary>
        /// <remarks>
        ///     This is expected to match <c>ModRunSidecarFingerprint.ComputeFileStem()</c> output.
        /// </remarks>
        public static StorageContextKey<string> RunFingerprintStem { get; } = new("sts2ritsulib.runFingerprintStem");
    }
}
