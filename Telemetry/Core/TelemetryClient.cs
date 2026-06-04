using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.Diagnostics;

namespace STS2RitsuLib.Telemetry
{
    internal sealed class TelemetryClient(string applicantId) : ITelemetryClient
    {
        public string ApplicantId { get; } = applicantId;

        public bool IsEnabled(string requestId)
        {
            return TryResolveRequest(requestId, out var applicant, out var request) &&
                   TelemetryConsentStore.IsRequestGranted(applicant, request);
        }

        public void Capture(
            string eventName,
            string requestId,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            CapturePayload(eventName, requestId, new JsonObject(), properties);
        }

        public void CapturePayload(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(payload);

            if (!TryResolveRequest(requestId, out var applicant, out var request))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not registered.");
                return;
            }

            if (!TelemetryConsentStore.IsRequestGranted(applicant, request))
            {
                RitsuLibFramework.Logger.Info(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not authorized.");
                return;
            }

            try
            {
                var envelope = TelemetryEnvelopeFactory.Create(
                    applicant,
                    request,
                    eventName,
                    payload,
                    properties);
                TelemetryQueue.Enqueue(envelope);
                TelemetryTaskRunner.Forget(
                    TelemetryQueue.FlushApplicantAsync(applicant.ApplicantId),
                    "flush_applicant");
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Capture failed for event '{eventName}' and applicant '{ApplicantId}': {ex.Message}");
            }
        }

        public void CaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var payload = DiagnosticsTelemetryCollector.BuildExceptionPayload(exception);
            CapturePayload("exception", "diagnostics", payload, properties);
        }

        private bool TryResolveRequest(
            string requestId,
            out TelemetryApplicant applicant,
            out TelemetryRequest request)
        {
            request = null!;
            return TelemetryRegistry.TryGetApplicant(ApplicantId, out applicant!) &&
                   TelemetryRegistry.TryGetRequest(applicant, requestId, out request);
        }
    }
}
