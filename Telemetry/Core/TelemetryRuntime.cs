using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Captures process-level telemetry facts once, then replays them to applicants after explicit consent.
    ///     只采集一次进程级 telemetry 事实，并在用户明确授权后回放给各申请方。
    /// </summary>
    internal static class TelemetryRuntime
    {
        private static readonly Lock Sync = new();
        private static readonly HashSet<string> DeliveredStartupKeys = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ConfirmedStartupKeys = new(StringComparer.OrdinalIgnoreCase);
        private static StartupTelemetrySnapshot? _startupSnapshot;

        /// <summary>
        ///     Captures the startup snapshot once and publishes a replayable lifecycle event for other tasks.
        ///     采集一次启动快照，并发布可重放生命周期事件供其他任务复用。
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

            RitsuLibFramework.Logger.Info("[Telemetry] Captured persistent startup telemetry snapshot.");
            RitsuLibFramework.PublishLifecycleEvent(
                new TelemetryStartupSnapshotReadyEvent(_startupSnapshot.CapturedAtUtc, DateTimeOffset.UtcNow),
                nameof(TelemetryStartupSnapshotReadyEvent));
        }

        /// <summary>
        ///     Replays cached startup events to every currently authorized applicant.
        ///     将已缓存的启动事件回放给当前已授权的所有申请方。
        /// </summary>
        internal static void ReplayStartupSnapshotToAuthorizedApplicants()
        {
            foreach (var applicant in TelemetryRegistry.GetApplicants())
                ReplayStartupSnapshotToApplicant(applicant.ApplicantId);
        }

        /// <summary>
        ///     Refreshes the cached mod inventory after the host has finished mod initialization.
        ///     在宿主完成 mod 初始化后刷新缓存的 mod 清单。
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

            RitsuLibFramework.Logger.Info($"[Telemetry] Refreshed startup mod inventory snapshot ({reason}).");
        }

        /// <summary>
        ///     Replays cached startup events to a single applicant, if the user authorized matching requests.
        ///     如果用户授权了对应申请，则将已缓存的启动事件回放给单个申请方。
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
                snapshot.BuildSessionStartPayload(),
                snapshot.BuildSessionStartProperties());
            TryReplayStartupEvent(
                applicant,
                "mod_inventory",
                "mod_inventory",
                snapshot.BuildModInventoryPayload(),
                snapshot.BuildModInventoryProperties());
        }

        /// <summary>
        ///     Clears startup delivery markers for queued events that were discarded before being sent.
        ///     清除尚未发送就被丢弃的启动事件投递标记。
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
        ///     Marks startup events as successfully handed to the applicant backend.
        ///     将启动事件标记为已成功交给申请方后端。
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
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties)
        {
            if (!TelemetryRegistry.TryGetRequest(applicant, requestId, out var request))
                return;

            if (!TelemetryConsentStore.IsRequestGranted(applicant, request))
                return;

            var deliveryKey = BuildStartupDeliveryKey(applicant.ApplicantId, requestId, eventName);
            lock (Sync)
            {
                if (!DeliveredStartupKeys.Add(deliveryKey))
                    return;
            }

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] Replaying startup event '{eventName}' to applicant '{applicant.ApplicantId}'.");
            try
            {
                new TelemetryClient(applicant.ApplicantId).CapturePayload(eventName, requestId, payload, properties);
            }
            catch (Exception ex)
            {
                lock (Sync)
                {
                    DeliveredStartupKeys.Remove(deliveryKey);
                }

                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Failed to replay startup event '{eventName}' to applicant '{applicant.ApplicantId}': {ex.Message}");
            }
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
            ///     Builds the lightweight session-start payload. Common envelope properties carry version/platform ids.
            ///     构建轻量 session-start 载荷。版本、平台等通用字段由 envelope properties 携带。
            /// </summary>
            public JsonObject BuildSessionStartPayload()
            {
                return new()
                {
                    ["captured_at_utc"] = CapturedAtUtc.ToString("O"),
                };
            }

            /// <summary>
            ///     Builds query-friendly session-start metadata from the captured startup snapshot.
            ///     从启动快照构建便于查询的 session-start 元数据。
            /// </summary>
            public Dictionary<string, object?> BuildSessionStartProperties()
            {
                return new(StringComparer.OrdinalIgnoreCase)
                {
                    ["startup_snapshot_at_utc"] = CapturedAtUtc.ToString("O"),
                    ["registered_mod_count"] = CountMods(),
                    ["loaded_mod_count"] = CountMods("Loaded"),
                    ["gameplay_mod_count"] = CountGameplayLoadedMods(),
                };
            }

            /// <summary>
            ///     Builds the mod-inventory trigger payload with the captured mod inventory.
            ///     使用已采集的 mod 清单构建 mod-inventory 触发载荷。
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
            ///     Builds query-friendly mod inventory metadata.
            ///     构建便于查询的 mod 清单元数据。
            /// </summary>
            public Dictionary<string, object?> BuildModInventoryProperties()
            {
                return new(StringComparer.OrdinalIgnoreCase)
                {
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
