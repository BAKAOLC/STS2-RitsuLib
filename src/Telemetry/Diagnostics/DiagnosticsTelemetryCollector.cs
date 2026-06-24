using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace STS2RitsuLib.Telemetry.Diagnostics
{
    internal static partial class DiagnosticsTelemetryCollector
    {
        private const int MaxRecentFingerprints = 128;
        private static readonly Lock Sync = new();
        private static readonly Queue<string> RecentFingerprintOrder = new();
        private static readonly HashSet<string> RecentFingerprints = new(StringComparer.Ordinal);
        private static bool _globalHandlersInitialized;

        internal static JsonObject BuildExceptionPayload(Exception exception)
        {
            return new()
            {
                ["exception"] = BuildExceptionNode(exception),
                ["framework_runtime"] = BuildFrameworkRuntimeNode(),
            };
        }

        private static JsonObject BuildExceptionNode(Exception exception)
        {
            var node = new JsonObject
            {
                ["type"] = exception.GetType().FullName ?? exception.GetType().Name,
                ["message"] = SanitizeDiagnosticText(exception.Message),
                ["stack_trace"] = SanitizeDiagnosticText(exception.StackTrace),
            };

            if (exception.InnerException != null)
                node["inner"] = BuildExceptionNode(exception.InnerException);

            return node;
        }

        private static JsonObject BuildFrameworkRuntimeNode()
        {
            var snapshot = RitsuLibFramework.CaptureRuntimeSnapshot();
            return new()
            {
                ["is_initialized"] = snapshot.IsInitialized,
                ["is_active"] = snapshot.IsActive,
                ["profile_services_initialized"] = snapshot.ProfileServicesInitialized,
                ["has_registered_mod_settings"] = snapshot.HasRegisteredModSettings,
                ["lifecycle_observer_count"] = snapshot.LifecycleObserverCount,
                ["registered_script_assembly_count"] = snapshot.RegisteredScriptAssemblyCount,
                ["patcher_areas"] = new JsonArray(snapshot.PatcherAreas.Select(area => new JsonObject
                {
                    ["area_name"] = area.AreaName,
                    ["is_registered"] = area.IsRegistered,
                    ["is_applied"] = area.IsApplied,
                    ["registered_patch_count"] = area.RegisteredPatchCount,
                    ["registered_dynamic_patch_count"] = area.RegisteredDynamicPatchCount,
                    ["applied_patch_count"] = area.AppliedPatchCount,
                }).ToArray<JsonNode?>()),
            };
        }

        internal static void CaptureExceptionForAuthorizedApplicants(Exception exception, string source)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);

            try
            {
                if (!TryMarkRecent(exception, source))
                    return;

                var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["payload_kind"] = "exception",
                    ["capture_source"] = source,
                    ["exception_type"] = exception.GetType().FullName ?? exception.GetType().Name,
                };

                var capturedApplicants = new List<string>();
                foreach (var applicant in TelemetryRegistry.GetApplicants())
                {
                    if (!TelemetryRegistry.TryGetRequest(applicant, "diagnostics", out var request) ||
                        !TelemetryConsentStore.IsRequestGranted(applicant, request))
                        continue;

                    new TelemetryClient(applicant.ApplicantId).CaptureException(exception, properties);
                    capturedApplicants.Add(applicant.ApplicantId);
                }

                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Captured exception diagnostics from '{source}' for {capturedApplicants.Count} authorized applicant(s): {exception.GetType().Name}.");
                foreach (var applicantId in capturedApplicants)
                    TelemetryTaskRunner.Forget(
                        TelemetryQueue.FlushApplicantAsync(applicantId),
                        "flush_applicant_after_diagnostics");
            }
            catch (Exception captureException)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Exception diagnostics capture failed: {captureException.Message}");
            }
        }

        internal static void InitializeGlobalExceptionHandlers()
        {
            lock (Sync)
            {
                if (_globalHandlersInitialized)
                    return;
                _globalHandlersInitialized = true;
            }

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                    CaptureExceptionForAuthorizedApplicants(exception, "dotnet_unhandled_exception");
            };
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                CaptureExceptionForAuthorizedApplicants(args.Exception, "dotnet_unobserved_task_exception");
                args.SetObserved();
            };
        }

        private static bool TryMarkRecent(Exception exception, string source)
        {
            var stackHead = SanitizeDiagnosticText(exception.StackTrace)?.Split('\n').FirstOrDefault()?.Trim() ?? "";
            var fingerprint = string.Join(
                "\n",
                source,
                exception.GetType().FullName ?? exception.GetType().Name,
                SanitizeDiagnosticText(exception.Message),
                stackHead);

            lock (Sync)
            {
                if (!RecentFingerprints.Add(fingerprint))
                    return false;

                RecentFingerprintOrder.Enqueue(fingerprint);
                while (RecentFingerprintOrder.Count > MaxRecentFingerprints)
                    RecentFingerprints.Remove(RecentFingerprintOrder.Dequeue());
                return true;
            }
        }

        private static string? SanitizeDiagnosticText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sanitized = WindowsStackPathRegex().Replace(text, " in <local-path>:line $1");
            sanitized = UnixStackPathRegex().Replace(sanitized, " in <local-path>:line $1");
            sanitized = WindowsPathRegex().Replace(sanitized, "<local-path>");
            return sanitized;
        }

        [GeneratedRegex(@"\s+in\s+[A-Za-z]:[^\r\n]*:line\s+(\d+)", RegexOptions.CultureInvariant)]
        private static partial Regex WindowsStackPathRegex();

        [GeneratedRegex(@"\s+in\s+/[^\r\n]*:line\s+(\d+)", RegexOptions.CultureInvariant)]
        private static partial Regex UnixStackPathRegex();

        [GeneratedRegex(@"[A-Za-z]:[\\/][^\r\n\t""']+", RegexOptions.CultureInvariant)]
        private static partial Regex WindowsPathRegex();
    }
}
