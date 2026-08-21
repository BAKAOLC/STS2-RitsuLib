namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryCaptureFilter
    {
        internal static bool ShouldCapture(
            TelemetryRequest request,
            TelemetryCaptureContext context,
            string applicantId)
        {
            var captureFilter = request.CaptureFilter;
            if (captureFilter == null)
                return true;

            try
            {
                return captureFilter(context);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Capture filter failed for applicant '{applicantId}' and event '{context.EventName}': {ex.Message}");
                return false;
            }
        }
    }
}
