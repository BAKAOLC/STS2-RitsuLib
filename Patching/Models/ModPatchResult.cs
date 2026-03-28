namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Outcome of applying a single <see cref="ModPatchInfo" />.
    /// </summary>
    /// <param name="modPatchInfo">Patch metadata.</param>
    /// <param name="success">True when applied or intentionally ignored.</param>
    /// <param name="errorMessage">Failure or ignore explanation.</param>
    /// <param name="exception">Exception when patch application threw.</param>
    /// <param name="ignored">True when target was missing and patch was marked ignorable.</param>
    public class ModPatchResult(
        ModPatchInfo modPatchInfo,
        bool success,
        string errorMessage = "",
        Exception? exception = null,
        bool ignored = false)
    {
        /// <summary>
        ///     Patch that was attempted.
        /// </summary>
        public ModPatchInfo ModPatchInfo { get; } = modPatchInfo;

        /// <summary>
        ///     True when the patch applied or was ignored as allowed.
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        ///     Error or informational message.
        /// </summary>
        public string ErrorMessage { get; } = errorMessage;

        /// <summary>
        ///     Exception from Harmony or reflection when present.
        /// </summary>
        public Exception? Exception { get; } = exception;

        /// <summary>
        ///     True when the patch was skipped because the target was missing and
        ///     <see cref="ModPatchInfo.IgnoreIfTargetMissing" /> was set.
        /// </summary>
        public bool Ignored { get; } = ignored;

        /// <summary>
        ///     Successful application (not ignored).
        /// </summary>
        public static ModPatchResult CreateSuccess(ModPatchInfo modPatchInfo)
        {
            return new(modPatchInfo, true);
        }

        /// <summary>
        ///     Failed application with optional exception.
        /// </summary>
        public static ModPatchResult CreateFailure(ModPatchInfo modPatchInfo, string errorMessage,
            Exception? exception = null)
        {
            return new(modPatchInfo, false, errorMessage, exception);
        }

        /// <summary>
        ///     Target missing but patch marked ignorable — treated as success with <see cref="Ignored" /> true.
        /// </summary>
        public static ModPatchResult CreateIgnored(ModPatchInfo modPatchInfo, string message)
        {
            return new(modPatchInfo, true, message, null, true);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Success
                ? Ignored ? $"- {ModPatchInfo.Id}: {ErrorMessage}" : $"✓ {ModPatchInfo.Id}"
                : $"✗ {ModPatchInfo.Id}: {ErrorMessage}";
        }
    }
}
