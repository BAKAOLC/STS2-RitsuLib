using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Telemetry
{
    internal static class TelemetryQueue
    {
        private const int MaxEventsPerApplicant = 2000;
        private const int MaxEventsPerFlush = 1000;
        private static readonly Lock Sync = new();
        private static readonly HashSet<string> FlushingApplicants = new(StringComparer.OrdinalIgnoreCase);

        internal static void Enqueue(TelemetryEnvelope envelope)
        {
            lock (Sync)
            {
                var doc = ReadQueue(envelope.ApplicantId);
                doc.Events.Add(envelope);
                if (doc.Events.Count > MaxEventsPerApplicant)
                    doc.Events.RemoveRange(0, doc.Events.Count - MaxEventsPerApplicant);
                WriteQueue(envelope.ApplicantId, doc);
                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Queued event '{envelope.EventName}' for applicant '{envelope.ApplicantId}'. Queue size: {doc.Events.Count}.");
            }
        }

        public static async Task FlushApplicantAsync(string applicantId, CancellationToken cancellationToken = default)
        {
            TelemetryApplicant applicant;

            lock (Sync)
            {
                if (!TelemetryRegistry.TryGetApplicant(applicantId, out applicant))
                {
                    RitsuLibFramework.Logger.Warn($"[Telemetry] Flush skipped: unknown applicant '{applicantId}'.");
                    return;
                }

                if (!FlushingApplicants.Add(applicantId))
                {
                    RitsuLibFramework.Logger.Debug(
                        $"[Telemetry] Flush skipped for '{applicantId}': another send is already in progress.");
                    return;
                }
            }

            try
            {
                while (true)
                {
                    var batch = await RitsuMainThread.InvokeAsync(
                        () => PrepareBatch(applicantId, applicant),
                        cancellationToken);
                    if (batch.Length == 0)
                        return;

                    RitsuLibFramework.Logger.Debug(
                        $"[Telemetry] Sending {batch.Length} queued event(s) for applicant '{applicantId}' via {applicant.Adapter.AdapterId}.");

                    TelemetrySendResult result;
                    try
                    {
                        result = await applicant.Adapter.SendAsync(applicant, batch, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        result = TelemetrySendResult.Fail(ex.Message);
                    }

                    var shouldContinue = await RitsuMainThread.InvokeAsync(
                        () => CommitBatchResult(applicantId, batch, result),
                        cancellationToken);
                    if (!shouldContinue)
                        return;
                }
            }
            catch (Exception ex)
            {
                await RitsuMainThread.InvokeAsync(
                    () => RecordFlushFailure(applicantId, ex),
                    CancellationToken.None);
            }
            finally
            {
                lock (Sync)
                {
                    FlushingApplicants.Remove(applicantId);
                }
            }
        }

        public static async Task FlushAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var applicant in TelemetryRegistry.GetApplicants())
                await FlushApplicantAsync(applicant.ApplicantId, cancellationToken);
        }

        public static void ClearApplicant(string applicantId)
        {
            lock (Sync)
            {
                var doc = ReadQueue(applicantId);
                TelemetryRuntime.ResetStartupDeliveryForDiscardedEvents(doc.Events);
                WriteQueue(applicantId, new());
                RitsuLibFramework.Logger.Info($"[Telemetry] Cleared queue for applicant '{applicantId}'.");
            }
        }

        public static int GetQueuedEventCount(string applicantId)
        {
            lock (Sync)
            {
                return ReadQueue(applicantId).Events.Count;
            }
        }

        private static TelemetryEnvelope[] PrepareBatch(string applicantId, TelemetryApplicant applicant)
        {
            lock (Sync)
            {
                var doc = ReadQueue(applicantId);
                var dropped = DropUnauthorizedEvents(applicant, doc);
                if (dropped.Count > 0)
                {
                    TelemetryRuntime.ResetStartupDeliveryForDiscardedEvents(dropped);
                    WriteQueue(applicantId, doc);
                    RitsuLibFramework.Logger.Info(
                        $"[Telemetry] Dropped {dropped.Count} unauthorized queued event(s) for applicant '{applicantId}'.");
                }

                if (doc.Events.Count != 0) return [.. doc.Events.Take(MaxEventsPerFlush)];
                RitsuLibFramework.Logger.Debug(
                    $"[Telemetry] Flush skipped for '{applicantId}': queue is empty.");
                return [];
            }
        }

        private static bool CommitBatchResult(
            string applicantId,
            IReadOnlyList<TelemetryEnvelope> batch,
            TelemetrySendResult result)
        {
            lock (Sync)
            {
                var state = ReadState(applicantId);
                state.LastSendUtc = DateTimeOffset.UtcNow;

                if (result.Success)
                {
                    TelemetryRuntime.MarkStartupDeliveryConfirmed(batch);
                    var queue = ReadQueue(applicantId);
                    var remainingCount = queue.Events.Count;
                    if (TryRemoveSentPrefix(queue, batch))
                    {
                        WriteQueue(applicantId, queue);
                        remainingCount = queue.Events.Count;
                    }
                    else
                    {
                        RitsuLibFramework.Logger.Warn(
                            $"[Telemetry] Sent {batch.Count} event(s) for applicant '{applicantId}', but queue changed unexpectedly. Keeping queued events to avoid data loss.");
                    }

                    state.LastError = null;
                    state.FailureCount = 0;
                    RitsuLibFramework.Logger.Debug(
                        $"[Telemetry] Sent {batch.Count} event(s) for applicant '{applicantId}'. Remaining queue size: {remainingCount}.");
                    WriteState(applicantId, state);
                    return true;
                }

                state.LastError = result.ErrorMessage;
                state.FailureCount++;
                WriteState(applicantId, state);
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Send failed for applicant '{applicantId}': {result.ErrorMessage}");
                return false;
            }
        }

        private static TelemetryQueueDocument ReadQueue(string applicantId)
        {
            var result = FileOperations.ReadJson<TelemetryQueueDocument>(
                TelemetryPaths.QueuePath(applicantId),
                TelemetryJson.Options,
                "TelemetryQueue");
            return result is { Success: true, Data: not null } ? result.Data : new();
        }

        private static void WriteQueue(string applicantId, TelemetryQueueDocument doc)
        {
            FileOperations.WriteJson(TelemetryPaths.QueuePath(applicantId), doc, TelemetryJson.Options,
                "TelemetryQueue");
        }

        private static TelemetryQueueState ReadState(string applicantId)
        {
            var result = FileOperations.ReadJson<TelemetryQueueState>(
                TelemetryPaths.StatePath(applicantId),
                TelemetryJson.Options,
                "TelemetryQueueState");
            return result is { Success: true, Data: not null } ? result.Data : new();
        }

        private static void WriteState(string applicantId, TelemetryQueueState state)
        {
            FileOperations.WriteJson(TelemetryPaths.StatePath(applicantId), state, TelemetryJson.Options,
                "TelemetryQueueState");
        }

        private static void RecordFlushFailure(string applicantId, Exception exception)
        {
            RitsuLibFramework.Logger.Warn(
                $"[Telemetry] Flush failed for applicant '{applicantId}': {exception.Message}");

            try
            {
                lock (Sync)
                {
                    var state = ReadState(applicantId);
                    state.LastSendUtc = DateTimeOffset.UtcNow;
                    state.LastError = exception.Message;
                    state.FailureCount++;
                    WriteState(applicantId, state);
                }
            }
            catch (Exception stateException)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Failed to record flush failure for applicant '{applicantId}': {stateException.Message}");
            }
        }

        private static List<TelemetryEnvelope> DropUnauthorizedEvents(
            TelemetryApplicant applicant,
            TelemetryQueueDocument queue)
        {
            var dropped = new List<TelemetryEnvelope>();
            for (var i = queue.Events.Count - 1; i >= 0; i--)
            {
                var evt = queue.Events[i];
                if (TelemetryRegistry.TryGetRequest(applicant, evt.RequestId, out var request) &&
                    TelemetryConsentStore.IsRequestGranted(applicant, request))
                    continue;

                dropped.Add(evt);
                queue.Events.RemoveAt(i);
            }

            dropped.Reverse();
            return dropped;
        }

        private static bool TryRemoveSentPrefix(TelemetryQueueDocument queue, IReadOnlyList<TelemetryEnvelope> sent)
        {
            if (sent.Count > queue.Events.Count)
                return false;

            if (sent.Where((t, i) => !IsSameQueuedEvent(queue.Events[i], t)).Any()) return false;

            queue.Events.RemoveRange(0, sent.Count);
            return true;
        }

        private static bool IsSameQueuedEvent(TelemetryEnvelope left, TelemetryEnvelope right)
        {
            return left.Schema == right.Schema &&
                   left.ApplicantId == right.ApplicantId &&
                   left.EventName == right.EventName &&
                   left.RequestId == right.RequestId &&
                   left.Category == right.Category &&
                   left.TimestampUtc == right.TimestampUtc;
        }
    }
}
