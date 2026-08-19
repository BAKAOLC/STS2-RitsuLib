using System.Text.Json.Nodes;
using STS2RitsuLib.Networking.StateDivergence;

namespace STS2RitsuLib.Telemetry.Diagnostics
{
    internal static class StateDivergenceTelemetryCollector
    {
        private const string EventName = "state_divergence.bundle";
        private const string RequestId = "state_divergence";
        private const string Source = "state_divergence";

        internal static void CaptureForAuthorizedApplicants(
            StateDivergenceDiagnosticReport report,
            string? bundlePath,
            string trigger)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

            if (string.IsNullOrWhiteSpace(bundlePath) ||
                !StateDivergenceLogBundleWriter.IsPublishedBundlePath(bundlePath))
                return;

            var context = new TelemetryCaptureContext(
                EventName,
                RequestId,
                TelemetryDataCategory.Diagnostics,
                Source);
            var applicantIds = new List<string>();
            foreach (var applicant in TelemetryRegistry.GetApplicants())
            {
                if (!TelemetryRegistry.TryGetRequest(applicant, RequestId, out var request) ||
                    !TelemetryConsentStore.IsRequestGranted(applicant, request) ||
                    !TelemetryCaptureFilter.ShouldCapture(request, context, applicant.ApplicantId))
                    continue;

                applicantIds.Add(applicant.ApplicantId);
            }

            if (applicantIds.Count == 0)
                return;

            TelemetryTaskRunner.Forget(
                CaptureAsync(report, bundlePath, trigger, context, applicantIds),
                "capture_state_divergence_bundle");
        }

        private static async Task CaptureAsync(
            StateDivergenceDiagnosticReport report,
            string bundlePath,
            string trigger,
            TelemetryCaptureContext context,
            IReadOnlyList<string> applicantIds)
        {
            var capture = await Task.Run(() => BuildCapture(report, bundlePath, trigger)).ConfigureAwait(false);
            var capturedCount = 0;
            foreach (var applicantId in applicantIds)
            {
                var client = new TelemetryClient(applicantId);
                if (await client.TryCapturePayloadAsync(
                            EventName,
                            RequestId,
                            capture.Payload,
                            capture.Properties,
                            context,
                            true)
                        .ConfigureAwait(false))
                    capturedCount++;
            }

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] Captured state divergence bundle for {capturedCount} authorized applicant(s); bytes={capture.BundleByteCount}.");
        }

        private static StateDivergenceTelemetryCapture BuildCapture(
            StateDivergenceDiagnosticReport report,
            string bundlePath,
            string trigger)
        {
            var bundleBytes = StateDivergenceLogBundleWriter.BuildSanitizedSubmissionBundle(bundlePath);

            var payload = new JsonObject
            {
                ["bundle"] = new JsonObject
                {
                    ["file_name"] = "state-divergence-diagnostics.zip",
                    ["content_type"] = "application/zip",
                    ["content_encoding"] = "base64",
                    ["content"] = Convert.ToBase64String(bundleBytes),
                    ["byte_count"] = bundleBytes.Length,
                },
            };
            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["payload_kind"] = "state_divergence_bundle",
                ["capture_source"] = Source,
                ["capture_trigger"] = trigger,
                ["role"] = report.Role,
                ["remote_peer_id"] = report.RemotePeerId,
                ["local_checksum_id"] = report.LocalChecksum.Id,
                ["local_checksum"] = report.LocalChecksum.Checksum,
                ["remote_checksum_id"] = report.RemoteChecksum.Id,
                ["remote_checksum"] = report.RemoteChecksum.Checksum,
                ["bundle_byte_count"] = bundleBytes.Length,
            };
            return new(payload, properties, bundleBytes.Length);
        }

        private sealed record StateDivergenceTelemetryCapture(
            JsonObject Payload,
            IReadOnlyDictionary<string, object?> Properties,
            int BundleByteCount);
    }
}
