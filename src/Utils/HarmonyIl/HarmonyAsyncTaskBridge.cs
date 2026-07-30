using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     <para xml:lang="en">Safe wrappers for Harmony patches that replace or compose asynchronous return values.</para>
    ///     <para xml:lang="zh-CN">用于 Harmony 补丁替换或组合异步返回值的安全包装器。</para>
    /// </summary>
    public static class HarmonyAsyncTaskBridge
    {
        private const string TelemetrySurface = "ritsulib_harmony_async_task_bridge";

        /// <summary>
        ///     <para xml:lang="en">Awaits <paramref name="continuation" /> before awaiting the original task.</para>
        ///     <para xml:lang="zh-CN">在等待原始任务前等待 <paramref name="continuation" /> 完成。</para>
        /// </summary>
        public static async Task Before(Task originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await InvokeContinuation(continuation);
            await originalTask;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs <paramref name="continuation" /> before awaiting the original task and returns the
        ///         original result.
        ///     </para>
        ///     <para xml:lang="zh-CN">在等待原始任务前运行 <paramref name="continuation" />，并返回原始结果。</para>
        /// </summary>
        public static async Task<T> Before<T>(Task<T> originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await InvokeContinuation(continuation);
            return await originalTask;
        }

        /// <summary>
        ///     <para xml:lang="en">Runs <paramref name="continuation" /> after the original task completes.</para>
        ///     <para xml:lang="zh-CN">在原始任务完成后运行 <paramref name="continuation" />。</para>
        /// </summary>
        public static async Task After(Task originalTask, Action continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await originalTask;
            InvokeContinuation(continuation);
        }

        /// <summary>
        ///     <para xml:lang="en">Runs <paramref name="continuation" /> after the original task completes.</para>
        ///     <para xml:lang="zh-CN">在原始任务完成后运行 <paramref name="continuation" />。</para>
        /// </summary>
        public static async Task After(Task originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await originalTask;
            await InvokeContinuation(continuation);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs <paramref name="continuation" /> after the original task completes and returns the
        ///         original result.
        ///     </para>
        ///     <para xml:lang="zh-CN">在原始任务完成后运行 <paramref name="continuation" />，并返回原始结果。</para>
        /// </summary>
        public static async Task<T> After<T>(Task<T> originalTask, Action<T> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            var result = await originalTask;
            InvokeContinuation(() => continuation(result));
            return result;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Runs <paramref name="continuation" /> after the original task completes and returns the
        ///         original result.
        ///     </para>
        ///     <para xml:lang="zh-CN">在原始任务完成后运行 <paramref name="continuation" />，并返回原始结果。</para>
        /// </summary>
        public static async Task<T> After<T>(Task<T> originalTask, Func<T, Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            var result = await originalTask;
            await InvokeContinuation(() => continuation(result));
            return result;
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the original task with <paramref name="replacement" />.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="replacement" /> 替换原始任务。</para>
        /// </summary>
        public static async Task Replace(Task originalTask, Func<Task, Task> replacement)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(replacement);

            await InvokeContinuation(() => replacement(originalTask));
        }

        /// <summary>
        ///     <para xml:lang="en">Replaces the original task with <paramref name="replacement" />.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="replacement" /> 替换原始任务。</para>
        /// </summary>
        public static async Task<T> Replace<T>(Task<T> originalTask, Func<Task<T>, Task<T>> replacement)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(replacement);

            return await InvokeContinuation(() => replacement(originalTask));
        }

        private static void InvokeContinuation(Action continuation)
        {
            try
            {
                continuation();
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(ex, TelemetrySurface);
                throw;
            }
        }

        private static async Task InvokeContinuation(Func<Task> continuation)
        {
            try
            {
                await continuation();
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(ex, TelemetrySurface);
                throw;
            }
        }

        private static async Task<T> InvokeContinuation<T>(Func<Task<T>> continuation)
        {
            try
            {
                return await continuation();
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(ex, TelemetrySurface);
                throw;
            }
        }
    }
}
