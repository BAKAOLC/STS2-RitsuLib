using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Diagnostics.Logging;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Telemetry.Diagnostics
{
    internal static partial class DiagnosticsTelemetryCollector
    {
        private const string EngineEventName = "godot_engine_error";
        private const string EngineSource = "godot_logger";
        private const int EngineQueueCapacity = 64;
        private const int EngineErrorsPerMinute = 20;

        private static readonly Channel<EngineErrorCapture> EngineErrors = Channel.CreateBounded<EngineErrorCapture>(
            new BoundedChannelOptions(EngineQueueCapacity)
                { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

        private static readonly CancellationTokenSource EngineCancellation = new();
        private static readonly CancellationToken EngineToken = EngineCancellation.Token;
        private static readonly Dictionary<string, long> RecentEngineErrors = new(StringComparer.Ordinal);
        private static readonly Queue<long> EngineErrorTimes = new();

        private static readonly Dictionary<string, int> EngineTextLimits = new()
        {
            ["message"] = 4096,
            ["code"] = 2048,
            ["function"] = 512,
            ["file"] = 1024,
            ["script_backtrace"] = 8192,
        };

        private static readonly string[] EngineFingerprintFields =
            ["type", "message", "code", "function", "file", "line", "script_backtrace"];

        private static Task? _engineWorker;
        private static bool _engineStopping;

        private static void InitializeEngineErrors()
        {
            if (_engineWorker != null)
                return;
            RitsuDebugLogPipeline.GodotErrorRecorded += OnEngineError;
            AppDomain.CurrentDomain.ProcessExit += StopEngineErrors;
            _engineWorker = Task.Run(ProcessEngineErrorsAsync);
        }

        private static void StopEngineErrors(object? sender, EventArgs args)
        {
            lock (Sync)
            {
                if (_engineStopping)
                    return;
                _engineStopping = true;
                RitsuDebugLogPipeline.GodotErrorRecorded -= OnEngineError;
                AppDomain.CurrentDomain.ProcessExit -= StopEngineErrors;
                EngineErrors.Writer.TryComplete();
                EngineCancellation.Cancel();
            }
        }

        private static void OnEngineError(RitsuDebugLogRecord record)
        {
            if (TelemetryDiagnosticsScope.IsActive || EngineToken.IsCancellationRequested)
                return;
            using var scope = new TelemetryDiagnosticsScope();
            var applicants = TelemetryConsentStore.GetLoadedDiagnosticsApplicantIds();
            if (applicants.Length == 0)
                return;
            var truncated = false;
            var error = new JsonObject
            {
                ["type"] = record.Attributes.GetValueOrDefault("godot.error.type") is int type ? type : 0,
                ["message"] = Bound(record.Attributes.GetValueOrDefault("godot.error.rationale") as string, "message"),
                ["code"] = Bound(record.Attributes.GetValueOrDefault("godot.error.code") as string, "code"),
                ["function"] = Bound(record.CodeFunctionName, "function"),
                ["file"] = Bound(record.CodeFilePath, "file"),
                ["line"] = record.CodeLineNumber,
                ["script_backtrace"] =
                    Bound(record.Attributes.GetValueOrDefault("godot.error.script_backtrace") as string,
                        "script_backtrace"),
                ["timestamp_utc"] = record.Timestamp,
                ["truncated"] = truncated,
            };
            EngineErrors.Writer.TryWrite(new(error, applicants));
            return;

            string Bound(string? value, string field)
            {
                if (value == null)
                    return "";
                var maximum = EngineTextLimits[field];
                if (value.Length <= maximum)
                    return value;
                truncated = true;
                return value[..maximum];
            }
        }

        private static async Task ProcessEngineErrorsAsync()
        {
            var token = EngineToken;
            using var scope = new TelemetryDiagnosticsScope();
            try
            {
                await foreach (var capture in EngineErrors.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    try
                    {
                        if (!TryMarkEngineError(capture.Error))
                            continue;
                        foreach (var (field, maximum) in EngineTextLimits)
                        {
                            var value = EngineErrorSanitizer.Sanitize(capture.Error[field]!.GetValue<string>());
                            if (value.Length > maximum)
                            {
                                value = value[..maximum];
                                capture.Error["truncated"] = true;
                            }

                            capture.Error[field] = value;
                        }

                        foreach (var applicantId in capture.ApplicantIds)
                        {
                            var pending = await RitsuMainThread.InvokeAsync(() => CaptureEngineErrorAsync(applicantId,
                                capture.Error, token), token).ConfigureAwait(false);
                            await pending.ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception) when (RitsuLibExceptionPolicy.IsRecoverable(exception))
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Telemetry] Engine diagnostics capture failed: {exception.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                lock (Sync)
                {
                    _engineStopping = true;
                    RitsuDebugLogPipeline.GodotErrorRecorded -= OnEngineError;
                    AppDomain.CurrentDomain.ProcessExit -= StopEngineErrors;
                    EngineCancellation.Cancel();
                    EngineCancellation.Dispose();
                }

                while (EngineErrors.Reader.TryRead(out _))
                {
                }

                RecentEngineErrors.Clear();
                EngineErrorTimes.Clear();
            }
        }

        private static async Task CaptureEngineErrorAsync(string applicantId, JsonObject error, CancellationToken token)
        {
            using var scope = new TelemetryDiagnosticsScope();
            if (!NGame.IsMainThread() || !TelemetryRegistry.TryGetApplicant(applicantId, out var applicant) ||
                !TelemetryRegistry.TryGetRequest(applicant, "diagnostics", out var request) ||
                request.Category != TelemetryDataCategory.Diagnostics ||
                !TelemetryConsentStore.IsRequestGranted(applicant, request))
                return;
            var context = new TelemetryCaptureContext(EngineEventName, "diagnostics", request.Category, EngineSource,
                error.DeepClone());
            if (!TelemetryCaptureFilter.ShouldCapture(request, context, applicantId))
                return;
            var payload = new JsonObject
            {
                ["engine_error"] = error.DeepClone(),
                ["framework_runtime"] = BuildFrameworkRuntimeNode(),
            };
            await new TelemetryClient(applicantId).TryCapturePayloadAsync(EngineEventName, "diagnostics", payload,
                new Dictionary<string, object?>
                    { ["payload_kind"] = "engine_error", ["capture_source"] = EngineSource },
                context, true, false, token).ConfigureAwait(false);
        }

        private static bool TryMarkEngineError(JsonObject error)
        {
            var now = Stopwatch.GetTimestamp();
            while (EngineErrorTimes.TryPeek(out var first) &&
                   Stopwatch.GetElapsedTime(first, now) >= TimeSpan.FromMinutes(1))
                EngineErrorTimes.Dequeue();
            if (EngineErrorTimes.Count >= EngineErrorsPerMinute)
                return false;
            var fingerprint = string.Join('\n', EngineFingerprintFields
                .Select(field => error[field]?.ToString() ?? ""));
            if (RecentEngineErrors.TryGetValue(fingerprint, out var last) &&
                Stopwatch.GetElapsedTime(last, now) < TimeSpan.FromSeconds(30))
                return false;
            if (RecentEngineErrors.Count >= MaxRecentFingerprints)
                RecentEngineErrors.Remove(RecentEngineErrors.MinBy(pair => pair.Value).Key);
            RecentEngineErrors[fingerprint] = now;
            EngineErrorTimes.Enqueue(now);
            return true;
        }

        private sealed record EngineErrorCapture(JsonObject Error, string[] ApplicantIds);
    }
}
