using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Safe wrappers for Harmony patches that replace or compose asynchronous return values.
    ///     用于 Harmony patch 替换或组合异步返回值的安全包装器。
    /// </summary>
    public static class HarmonyAsyncTaskBridge
    {
        private const string TelemetrySurface = "ritsulib_harmony_async_task_bridge";

        /// <summary>
        ///     Runs <paramref name="continuation" /> before returning the original task.
        ///     在返回原始 task 前运行 <paramref name="continuation" />。
        /// </summary>
        public static async Task Before(Task originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await InvokeContinuation(continuation);
            await originalTask;
        }

        /// <summary>
        ///     Runs <paramref name="continuation" /> before awaiting the original task and returns the original result.
        ///     在等待原始 task 前运行 <paramref name="continuation" />，并返回原始结果。
        /// </summary>
        public static async Task<T> Before<T>(Task<T> originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await InvokeContinuation(continuation);
            return await originalTask;
        }

        /// <summary>
        ///     Runs <paramref name="continuation" /> after the original task completes.
        ///     在原始 task 完成后运行 <paramref name="continuation" />。
        /// </summary>
        public static async Task After(Task originalTask, Action continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await originalTask;
            InvokeContinuation(continuation);
        }

        /// <summary>
        ///     Runs <paramref name="continuation" /> after the original task completes.
        ///     在原始 task 完成后运行 <paramref name="continuation" />。
        /// </summary>
        public static async Task After(Task originalTask, Func<Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await originalTask;
            await InvokeContinuation(continuation);
        }

        /// <summary>
        ///     Runs <paramref name="continuation" /> after the original task completes and returns the original result.
        ///     在原始 task 完成后运行 <paramref name="continuation" />，并返回原始结果。
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
        ///     Runs <paramref name="continuation" /> after the original task completes and returns the original result.
        ///     在原始 task 完成后运行 <paramref name="continuation" />，并返回原始结果。
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
        ///     Replaces the original task with <paramref name="replacement" />.
        ///     使用 <paramref name="replacement" /> 替换原始 task。
        /// </summary>
        public static async Task Replace(Task originalTask, Func<Task, Task> replacement)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(replacement);

            await InvokeContinuation(() => replacement(originalTask));
        }

        /// <summary>
        ///     Replaces the original task with <paramref name="replacement" />.
        ///     使用 <paramref name="replacement" /> 替换原始 task。
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
