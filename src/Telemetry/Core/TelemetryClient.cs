using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.Diagnostics;
using STS2RitsuLib.Utils;

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
            TryCapturePayload(eventName, requestId, payload, properties);
        }

        public void CaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            TryCaptureException(exception, properties);
        }

        internal bool TryCapturePayload(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null,
            TelemetryCaptureContext? captureContext = null,
            bool filterAlreadyApplied = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(payload);

            if (!TryResolveRequest(requestId, out var applicant, out var request))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not registered.");
                return false;
            }

            if (!TelemetryConsentStore.IsRequestGranted(applicant, request))
            {
                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not authorized.");
                return false;
            }

            captureContext ??= new(
                eventName,
                requestId,
                request.Category,
                ResolveCaptureSource(properties));
            if (!filterAlreadyApplied &&
                !TelemetryCaptureFilter.ShouldCapture(request, captureContext.Value, applicant.ApplicantId))
                return false;

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
                return true;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Capture failed for event '{eventName}' and applicant '{ApplicantId}': {ex.Message}");
                return false;
            }
        }

        internal async Task<bool> TryCapturePayloadAsync(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null,
            TelemetryCaptureContext? captureContext = null,
            bool filterAlreadyApplied = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
            ArgumentNullException.ThrowIfNull(payload);

            if (!TryResolveRequest(requestId, out var applicant, out var request))
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not registered.");
                return false;
            }

            if (!TelemetryConsentStore.IsRequestGranted(applicant, request))
            {
                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Dropped event '{eventName}' for applicant '{ApplicantId}': request '{requestId}' is not authorized.");
                return false;
            }

            captureContext ??= new(
                eventName,
                requestId,
                request.Category,
                ResolveCaptureSource(properties));
            if (!filterAlreadyApplied &&
                !TelemetryCaptureFilter.ShouldCapture(request, captureContext.Value, applicant.ApplicantId))
                return false;

            try
            {
                TelemetryEnvelope? envelope = null;
                await RitsuMainThread.InvokeAsync(
                        () => envelope = TelemetryEnvelopeFactory.Create(
                            applicant,
                            request,
                            eventName,
                            payload,
                            properties),
                        cancellationToken)
                    .ConfigureAwait(false);
                await TelemetryQueue.EnqueueAsync(envelope!, cancellationToken).ConfigureAwait(false);
                await TelemetryQueue.FlushApplicantAsync(applicant.ApplicantId, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Async capture failed for event '{eventName}' and applicant '{ApplicantId}': {ex.Message}");
                return false;
            }
        }

        internal bool TryCaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            ArgumentNullException.ThrowIfNull(exception);

            const string eventName = "exception";
            const string requestId = "diagnostics";
            if (!TryResolveRequest(requestId, out var applicant, out var request) ||
                !TelemetryConsentStore.IsRequestGranted(applicant, request))
                return false;

            var context = new TelemetryCaptureContext(
                eventName,
                requestId,
                request.Category,
                ResolveCaptureSource(properties),
                exception);
            if (!TelemetryCaptureFilter.ShouldCapture(request, context, applicant.ApplicantId))
                return false;

            var payload = DiagnosticsTelemetryCollector.BuildExceptionPayload(exception);
            return TryCapturePayload(
                eventName,
                requestId,
                payload,
                properties,
                context,
                true);
        }

        private static string ResolveCaptureSource(IReadOnlyDictionary<string, object?>? properties)
        {
            return properties != null &&
                   properties.TryGetValue("capture_source", out var value) &&
                   value is string source &&
                   !string.IsNullOrWhiteSpace(source)
                ? source
                : "applicant";
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
