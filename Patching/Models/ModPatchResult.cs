namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Outcome of applying a single <see cref="ModPatchInfo" />.
    ///     应用单个 <see cref="ModPatchInfo" /> 的结果。
    /// </summary>
    /// <param name="modPatchInfo">
    ///     Patch metadata.
    ///     patch 元数据。
    /// </param>
    /// <param name="success">
    ///     True when applied or intentionally ignored.
    ///     已应用或被有意忽略时为 True。
    /// </param>
    /// <param name="errorMessage">
    ///     Failure or ignore explanation.
    ///     失败或忽略的说明。
    /// </param>
    /// <param name="exception">
    ///     Exception when patch application threw.
    ///     patch 应用抛出时的异常。
    /// </param>
    /// <param name="ignored">
    ///     True when target was missing and patch was marked ignorable.
    ///     目标缺失且 patch 标记为可忽略时为 True。
    /// </param>
    public class ModPatchResult(
        ModPatchInfo modPatchInfo,
        bool success,
        string errorMessage = "",
        Exception? exception = null,
        bool ignored = false)
    {
        /// <summary>
        ///     Patch that was attempted.
        ///     尝试应用的 patch。
        /// </summary>
        public ModPatchInfo ModPatchInfo { get; } = modPatchInfo;

        /// <summary>
        ///     True when the patch applied or was ignored as allowed.
        ///     patch 已应用或按允许被忽略时为 True。
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        ///     Error or informational message.
        ///     错误或信息性消息。
        /// </summary>
        public string ErrorMessage { get; } = errorMessage;

        /// <summary>
        ///     Exception from Harmony or reflection when present.
        ///     存在时为来自 Harmony 或反射的异常。
        /// </summary>
        public Exception? Exception { get; } = exception;

        /// <summary>
        ///     True when the patch was skipped because the target was missing and
        ///     <see cref="ModPatchInfo.IgnoreIfTargetMissing" /> was set.
        ///     patch 因目标缺失且
        ///     已设置 <see cref="ModPatchInfo.IgnoreIfTargetMissing" /> 而被跳过时为 True。
        /// </summary>
        public bool Ignored { get; } = ignored;

        /// <summary>
        ///     Successful application (not ignored).
        ///     成功应用（未忽略）。
        /// </summary>
        public static ModPatchResult CreateSuccess(ModPatchInfo modPatchInfo)
        {
            return new(modPatchInfo, true);
        }

        /// <summary>
        ///     Failed application with optional exception.
        ///     应用失败，可带可选异常。
        /// </summary>
        public static ModPatchResult CreateFailure(ModPatchInfo modPatchInfo, string errorMessage,
            Exception? exception = null)
        {
            return new(modPatchInfo, false, errorMessage, exception);
        }

        /// <summary>
        ///     Target missing but patch marked ignorable — treated as success with <see cref="Ignored" /> true.
        ///     目标缺失但 patch 标记为可忽略，视为成功且 <see cref="Ignored" /> 为 true。
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
