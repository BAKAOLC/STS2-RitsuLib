namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">Represents the result of applying one <see cref="ModPatchInfo" />.</para>
    ///     <para xml:lang="zh-CN">表示应用一个 <see cref="ModPatchInfo" /> 的结果。</para>
    /// </summary>
    /// <param name="modPatchInfo">
    ///     <para xml:lang="en">Patch metadata.</para>
    ///     <para xml:lang="zh-CN">补丁元数据。</para>
    /// </param>
    /// <param name="success">
    ///     <para xml:lang="en">Whether the patch was applied or intentionally ignored.</para>
    ///     <para xml:lang="zh-CN">补丁是否已应用或被有意忽略。</para>
    /// </param>
    /// <param name="errorMessage">
    ///     <para xml:lang="en">Failure or ignore explanation.</para>
    ///     <para xml:lang="zh-CN">失败或忽略的说明。</para>
    /// </param>
    /// <param name="exception">
    ///     <para xml:lang="en">Exception thrown while applying the patch, if any.</para>
    ///     <para xml:lang="zh-CN">应用补丁时抛出的异常（如果有）。</para>
    /// </param>
    /// <param name="ignored">
    ///     <para xml:lang="en">Whether the patch was ignored because its target was missing.</para>
    ///     <para xml:lang="zh-CN">补丁是否因目标缺失而被忽略。</para>
    /// </param>
    public class ModPatchResult(
        ModPatchInfo modPatchInfo,
        bool success,
        string errorMessage = "",
        Exception? exception = null,
        bool ignored = false)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the patch that was processed.</para>
        ///     <para xml:lang="zh-CN">获取已处理的补丁。</para>
        /// </summary>
        public ModPatchInfo ModPatchInfo { get; } = modPatchInfo;

        /// <summary>
        ///     <para xml:lang="en">Gets whether the patch was applied or intentionally ignored.</para>
        ///     <para xml:lang="zh-CN">获取补丁是否已应用或被有意忽略。</para>
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        ///     <para xml:lang="en">Gets the error or informational message.</para>
        ///     <para xml:lang="zh-CN">获取错误或信息消息。</para>
        /// </summary>
        public string ErrorMessage { get; } = errorMessage;

        /// <summary>
        ///     <para xml:lang="en">Gets the Harmony or reflection exception, if any.</para>
        ///     <para xml:lang="zh-CN">获取 Harmony 或反射异常（如果有）。</para>
        /// </summary>
        public Exception? Exception { get; } = exception;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets whether the patch was skipped because its target was missing and
        ///         <see cref="ModPatchInfo.IgnoreIfTargetMissing" /> was set.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取补丁是否因目标缺失且已设置 <see cref="ModPatchInfo.IgnoreIfTargetMissing" /> 而被跳过。
        ///     </para>
        /// </summary>
        public bool Ignored { get; } = ignored;

        /// <summary>
        ///     <para xml:lang="en">Creates a successful, non-ignored result.</para>
        ///     <para xml:lang="zh-CN">创建表示补丁已成功应用且未被忽略的结果。</para>
        /// </summary>
        public static ModPatchResult CreateSuccess(ModPatchInfo modPatchInfo)
        {
            return new(modPatchInfo, true);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a failure result with an optional exception.</para>
        ///     <para xml:lang="zh-CN">创建应用失败的结果，可包含异常。</para>
        /// </summary>
        public static ModPatchResult CreateFailure(ModPatchInfo modPatchInfo, string errorMessage,
            Exception? exception = null)
        {
            return new(modPatchInfo, false, errorMessage, exception);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a successful ignored result for a missing optional target.</para>
        ///     <para xml:lang="zh-CN">为缺失的可选目标创建成功且已忽略的结果。</para>
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
