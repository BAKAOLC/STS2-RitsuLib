namespace STS2RitsuLib.Settings.RunSidecar
{
    /// <summary>
    ///     Result of validating and reading a run sidecar JSON file.
    /// </summary>
    public enum ModRunSidecarReadStatus
    {
        /// <summary>
        ///     Model loaded and fingerprint matched the active run.
        /// </summary>
        Ok,

        /// <summary>
        ///     No run in progress (main menu / post-run).
        /// </summary>
        NoActiveRun,

        /// <summary>
        ///     Sidecar file does not exist yet.
        /// </summary>
        MissingFile,

        /// <summary>
        ///     JSON could not be parsed.
        /// </summary>
        InvalidJson,

        /// <summary>
        ///     Stored fingerprint does not match the active run — data is ignored to avoid wrong-run binding.
        /// </summary>
        FingerprintMismatch,
    }
}
