using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal static class LifecyclePatchTaskBridge
    {
        public static async Task After(Task originalTask, Action continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            await originalTask;
            try
            {
                continuation();
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_lifecycle_patch_task_bridge");
                throw;
            }
        }

        public static async Task<T> After<T>(Task<T> originalTask, Action<T> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            var result = await originalTask;
            try
            {
                continuation(result);
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_lifecycle_patch_task_bridge");
                throw;
            }

            return result;
        }

        public static async Task<T> After<T>(Task<T> originalTask, Func<T, Task> continuation)
        {
            ArgumentNullException.ThrowIfNull(originalTask);
            ArgumentNullException.ThrowIfNull(continuation);

            var result = await originalTask;
            try
            {
                await continuation(result);
            }
            catch (Exception ex)
            {
                DiagnosticsTelemetryCollector.CaptureExceptionForAuthorizedApplicants(
                    ex,
                    "ritsulib_lifecycle_patch_task_bridge");
                throw;
            }

            return result;
        }
    }
}
