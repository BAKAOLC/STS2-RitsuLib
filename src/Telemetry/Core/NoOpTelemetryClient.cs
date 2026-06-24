using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    internal sealed class NoOpTelemetryClient(string applicantId) : ITelemetryClient
    {
        public string ApplicantId { get; } = applicantId;

        public bool IsEnabled(string requestId)
        {
            return false;
        }

        public void Capture(
            string eventName,
            string requestId,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
        }

        public void CapturePayload(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
        }

        public void CaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
        }
    }
}
