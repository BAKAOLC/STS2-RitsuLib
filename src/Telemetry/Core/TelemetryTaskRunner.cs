namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryTaskRunner
    {
        internal static void Forget(Task task, string operation)
        {
            ArgumentNullException.ThrowIfNull(task);
            _ = ObserveAsync(task, operation);
        }

        private static async Task ObserveAsync(Task task, string operation)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Background task '{operation}' was canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Background task '{operation}' failed: {ex.Message}");
            }
        }
    }
}
