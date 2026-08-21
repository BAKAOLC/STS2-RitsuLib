using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Captures process-level telemetry facts once and replays them to applicants after explicit consent.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         采集一次进程级遥测信息，并在用户明确授权后将其回放给各申请方。
    ///     </para>
    /// </summary>
    internal static class TelemetryRuntime
    {
        private static readonly Lock Sync = new();
        private static readonly HashSet<string> DeliveredStartupKeys = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ConfirmedStartupKeys = new(StringComparer.OrdinalIgnoreCase);
        private static StartupTelemetrySnapshot? _startupSnapshot;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures the startup snapshot once and publishes a replayable lifecycle event for other tasks.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         采集一次启动快照，并发布可重放的生命周期事件供其他任务使用。
        ///     </para>
        /// </summary>
        internal static void CaptureStartupSnapshot()
        {
            lock (Sync)
            {
                if (_startupSnapshot != null)
                    return;

                _startupSnapshot = new(
                    DateTimeOffset.UtcNow,
                    RunHistoryTelemetryCollector.BuildModInventoryList());
            }

            RitsuLibFramework.Logger.Debug("[Telemetry] Captured persistent startup telemetry snapshot.");
            RitsuLibFramework.PublishLifecycleEvent(
                new TelemetryStartupSnapshotReadyEvent(_startupSnapshot.CapturedAtUtc, DateTimeOffset.UtcNow),
                nameof(TelemetryStartupSnapshotReadyEvent));
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replays cached startup events to every currently authorized applicant.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将缓存的启动事件回放给当前已获授权的所有申请方。
        ///     </para>
        /// </summary>
        internal static void ReplayStartupSnapshotToAuthorizedApplicants()
        {
            foreach (var applicant in TelemetryRegistry.GetApplicants())
                ReplayStartupSnapshotToApplicant(applicant.ApplicantId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Refreshes the cached mod inventory after the host finishes mod initialization.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         在宿主完成模组初始化后刷新缓存的模组清单。
        ///     </para>
        /// </summary>
        internal static void RefreshStartupModInventorySnapshot(string reason)
        {
            lock (Sync)
            {
                if (_startupSnapshot == null)
                    return;

                _startupSnapshot = _startupSnapshot with
                {
                    Mods = RunHistoryTelemetryCollector.BuildModInventoryList(),
                };
            }

            RitsuLibFramework.Logger.Debug($"[Telemetry] Refreshed startup mod inventory snapshot ({reason}).");
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replays cached startup events to one applicant when the user has authorized the matching requests.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         用户已授权对应申请项时，将缓存的启动事件回放给一个申请方。
        ///     </para>
        /// </summary>
        internal static void ReplayStartupSnapshotToApplicant(string applicantId)
        {
            StartupTelemetrySnapshot snapshot;

            lock (Sync)
            {
                if (_startupSnapshot == null)
                    return;

                snapshot = _startupSnapshot;
            }

            if (!TelemetryRegistry.TryGetApplicant(applicantId, out var applicant))
                return;

            TryReplayStartupEvent(
                applicant,
                "basic_usage",
                "session_start",
                snapshot.BuildSessionStartPayload,
                snapshot.BuildSessionStartProperties);
            TryReplayStartupEvent(
                applicant,
                "mod_inventory",
                "mod_inventory",
                snapshot.BuildModInventoryPayload,
                snapshot.BuildModInventoryProperties);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Clears delivery markers for queued startup events discarded before transmission.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         清除排队后在发送前被丢弃的启动事件投递标记。
        ///     </para>
        /// </summary>
        internal static void ResetStartupDeliveryForDiscardedEvents(IEnumerable<TelemetryEnvelope> events)
        {
            var discardedKeys = events
                .Select(BuildStartupDeliveryKey)
                .Where(key => key != null)
                .ToArray();
            if (discardedKeys.Length == 0)
                return;

            lock (Sync)
            {
                foreach (var key in discardedKeys)
                    if (!ConfirmedStartupKeys.Contains(key!))
                        DeliveredStartupKeys.Remove(key!);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Marks startup events as successfully delivered to the applicant's backend.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         将启动事件标记为已成功送达申请方后端。
        ///     </para>
        /// </summary>
        internal static void MarkStartupDeliveryConfirmed(IEnumerable<TelemetryEnvelope> events)
        {
            var confirmedKeys = events
                .Select(BuildStartupDeliveryKey)
                .Where(key => key != null)
                .ToArray();
            if (confirmedKeys.Length == 0)
                return;

            lock (Sync)
            {
                foreach (var key in confirmedKeys)
                {
                    ConfirmedStartupKeys.Add(key!);
                    DeliveredStartupKeys.Add(key!);
                }
            }
        }

        private static void TryReplayStartupEvent(
            TelemetryApplicant applicant,
            string requestId,
            string eventName,
            Func<JsonNode> buildPayload,
            Func<IReadOnlyDictionary<string, object?>?> buildProperties)
        {
            if (!TelemetryRegistry.TryGetRequest(applicant, requestId, out var request))
                return;

            if (!TelemetryConsentStore.IsRequestGranted(applicant, request))
                return;

            var context = new TelemetryCaptureContext(
                eventName,
                requestId,
                request.Category,
                "startup_snapshot");
            if (!TelemetryCaptureFilter.ShouldCapture(request, context, applicant.ApplicantId))
                return;

            var deliveryKey = BuildStartupDeliveryKey(applicant.ApplicantId, requestId, eventName);
            lock (Sync)
            {
                if (!DeliveredStartupKeys.Add(deliveryKey))
                    return;
            }

            RitsuLibFramework.Logger.Debug(
                $"[Telemetry] Replaying startup event '{eventName}' to applicant '{applicant.ApplicantId}'.");
            var client = new TelemetryClient(applicant.ApplicantId);
            if (client.TryCapturePayload(
                    eventName,
                    requestId,
                    buildPayload(),
                    buildProperties(),
                    context,
                    true))
                return;

            lock (Sync)
            {
                DeliveredStartupKeys.Remove(deliveryKey);
            }

            RitsuLibFramework.Logger.Warn(
                $"[Telemetry] Failed to queue startup event '{eventName}' for applicant '{applicant.ApplicantId}'.");
        }

        private static string? BuildStartupDeliveryKey(TelemetryEnvelope envelope)
        {
            return envelope.EventName is "session_start" or "mod_inventory"
                ? BuildStartupDeliveryKey(envelope.ApplicantId, envelope.RequestId, envelope.EventName)
                : null;
        }

        private static string BuildStartupDeliveryKey(string applicantId, string requestId, string eventName)
        {
            return $"{applicantId}\n{requestId}\n{eventName}";
        }

        private sealed record StartupTelemetrySnapshot(DateTimeOffset CapturedAtUtc, JsonArray Mods)
        {
            /// <summary>
            ///     <para xml:lang="en">
            ///         Builds the lightweight session-start payload. Common envelope properties carry version,
            ///         platform, and identity fields.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         构建轻量的会话启动负载。版本、平台和身份字段由信封的通用属性携带。
            ///     </para>
            /// </summary>
            public JsonObject BuildSessionStartPayload()
            {
                return new()
                {
                    ["captured_at_utc"] = CapturedAtUtc.ToString("O"),
                };
            }

            /// <summary>
            ///     <para xml:lang="en">
            ///         Builds query-friendly session-start metadata from the captured startup snapshot.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         根据采集的启动快照构建便于查询的会话启动元数据。
            ///     </para>
            /// </summary>
            public Dictionary<string, object?> BuildSessionStartProperties()
            {
                return new(StringComparer.OrdinalIgnoreCase)
                {
                    ["capture_source"] = "startup_snapshot",
                    ["startup_snapshot_at_utc"] = CapturedAtUtc.ToString("O"),
                    ["registered_mod_count"] = CountMods(),
                    ["loaded_mod_count"] = CountMods("Loaded"),
                    ["gameplay_mod_count"] = CountGameplayLoadedMods(),
                };
            }

            /// <summary>
            ///     <para xml:lang="en">
            ///         Builds the mod-inventory event payload from the captured mod inventory.
            ///     </para>
            ///     <para xml:lang="zh-CN">
            ///         根据采集的模组清单构建模组清单事件负载。
            ///     </para>
            /// </summary>
            public JsonObject BuildModInventoryPayload()
            {
                var basePayload = new JsonObject
                {
                    ["mods"] = Mods.DeepClone(),
                    ["loaded_mods"] = RunHistoryTelemetryCollector.FilterLoadedMods(Mods),
                };

                return new()
                {
                    ["captured_at_utc"] = CapturedAtUtc.ToString("O"),
                    [TelemetryEnvelopeFactory.BasePayloadOverrideKey] = basePayload,
                };
            }

            /// <summary>
            ///     <para xml:lang="en">Builds query-friendly mod-inventory metadata.</para>
            ///     <para xml:lang="zh-CN">构建便于查询的模组清单元数据。</para>
            /// </summary>
            public Dictionary<string, object?> BuildModInventoryProperties()
            {
                return new(StringComparer.OrdinalIgnoreCase)
                {
                    ["capture_source"] = "startup_snapshot",
                    ["startup_snapshot_at_utc"] = CapturedAtUtc.ToString("O"),
                    ["registered_mod_count"] = CountMods(),
                    ["loaded_mod_count"] = CountMods("Loaded"),
                    ["disabled_mod_count"] = CountMods("Disabled"),
                    ["failed_mod_count"] = CountMods("Failed"),
                    ["added_at_runtime_mod_count"] = CountMods("AddedAtRuntime"),
                    ["gameplay_mod_count"] = CountGameplayLoadedMods(),
                };
            }

            private int CountMods(string? loadState = null)
            {
                var count = 0;
                foreach (var node in Mods)
                {
                    if (node is not JsonObject obj)
                        continue;

                    if (loadState != null &&
                        !string.Equals(obj["state"]?.GetValue<string>(), loadState,
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    count++;
                }

                return count;
            }

            private int CountGameplayLoadedMods()
            {
                var count = 0;
                foreach (var node in Mods)
                {
                    if (node is not JsonObject obj ||
                        !string.Equals(obj["state"]?.GetValue<string>(), "Loaded",
                            StringComparison.OrdinalIgnoreCase) ||
                        obj["affects_gameplay"]?.GetValue<bool>() == false)
                        continue;

                    count++;
                }

                return count;
            }
        }
    }
}
