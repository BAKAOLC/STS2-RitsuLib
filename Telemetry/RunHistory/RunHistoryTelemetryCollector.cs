using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Compat;

namespace STS2RitsuLib.Telemetry.RunHistory
{
    internal static class RunHistoryTelemetryCollector
    {
        internal static JsonArray BuildModInventoryList()
        {
            var mods = new JsonArray();
            foreach (var mod in Sts2ModManagerCompat.BuildModInventoryEntries())
                mods.Add(new JsonObject
                {
                    ["id"] = mod.Id,
                    ["name"] = mod.Name,
                    ["version"] = mod.Version,
                    ["state"] = mod.State,
                    ["source"] = mod.Source,
                    ["affects_gameplay"] = mod.AffectsGameplay,
                    ["assembly"] = mod.AssemblyName,
                    ["assembly_version"] = mod.AssemblyVersion,
                    ["error_count"] = mod.Errors.Count,
                    ["errors"] = BuildModErrors(mod.Errors),
                });

            return mods;
        }

        private static object? ReadMemberValue(object? source, string name)
        {
            if (source == null)
                return null;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;
            var type = source.GetType();
            var field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue(source);

            var property = type.GetProperty(name, flags);
            return property?.GetValue(source);
        }

        internal static JsonArray BuildLoadedModList()
        {
            return FilterLoadedMods(BuildModInventoryList());
        }

        internal static JsonArray FilterLoadedMods(JsonArray mods)
        {
            var loaded = new JsonArray();
            foreach (var node in mods)
            {
                if (node is not JsonObject obj ||
                    !string.Equals(obj["state"]?.GetValue<string>(), "Loaded",
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                loaded.Add(obj.DeepClone());
            }

            return loaded;
        }

        private static JsonArray BuildModErrors(IEnumerable<LocString>? errors)
        {
            var nodes = new JsonArray();
            if (errors == null)
                return nodes;

            foreach (var error in errors)
            {
                var variables = new JsonObject();
                foreach (var variable in error.Variables)
                {
                    if (MayContainLocalPath(variable.Key, variable.Value))
                        continue;

                    variables[variable.Key] = BuildSafeVariableNode(variable.Value);
                }

                nodes.Add(new JsonObject
                {
                    ["table"] = error.LocTable,
                    ["key"] = error.LocEntryKey,
                    ["variables"] = variables,
                });
            }

            return nodes;
        }

        private static JsonNode? BuildSafeVariableNode(object? value)
        {
            return value switch
            {
                null => null,
                JsonNode node => node.DeepClone(),
                IEnumerable<string> strings => new JsonArray(strings.Select(static s => JsonValue.Create(s))
                    .ToArray<JsonNode?>()),
                _ => value switch
                {
                    string s => JsonValue.Create(s),
                    bool b => JsonValue.Create(b),
                    byte n => JsonValue.Create(n),
                    sbyte n => JsonValue.Create(n),
                    short n => JsonValue.Create(n),
                    ushort n => JsonValue.Create(n),
                    int n => JsonValue.Create(n),
                    uint n => JsonValue.Create(n),
                    long n => JsonValue.Create(n),
                    ulong n => JsonValue.Create(n),
                    float n when float.IsFinite(n) => JsonValue.Create(n),
                    double n when double.IsFinite(n) => JsonValue.Create(n),
                    decimal n => JsonValue.Create(n),
                    Enum e => JsonValue.Create(e.ToString()),
                    _ => SerializeVariableNode(value),
                },
            };
        }

        private static JsonNode? SerializeVariableNode(object value)
        {
            try
            {
                return JsonSerializer.SerializeToNode(value, value.GetType(), TelemetryJson.Options);
            }
            catch
            {
                return JsonValue.Create(value.ToString());
            }
        }

        private static bool MayContainLocalPath(string key, object? value)
        {
            if (key.Contains("path", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("folder", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("directory", StringComparison.OrdinalIgnoreCase))
                return true;

            return value is string text &&
                   (text.Contains(":\\", StringComparison.Ordinal) ||
                    text.Contains(":/", StringComparison.Ordinal) ||
                    text.Contains(@"\Users\", StringComparison.OrdinalIgnoreCase));
        }

        public static void CaptureVanillaRunHistory(
            string applicantId,
            JsonNode runHistory,
            JsonNode? applicantPayload = null,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(applicantId);
            ArgumentNullException.ThrowIfNull(runHistory);

            var payload = new JsonObject
            {
                ["run_history"] = runHistory.DeepClone(),
            };

            if (applicantPayload != null)
                payload["mod_payload"] = applicantPayload.DeepClone();

            var props = properties == null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new(properties, StringComparer.OrdinalIgnoreCase);
            props["payload_kind"] = "vanilla_run_history";
            props["json_indented"] = false;

            new TelemetryClient(applicantId).CapturePayload(
                "run_history.completed",
                "run_history",
                payload,
                props);
        }

        internal static void CaptureEndedRun(RunEndedEvent evt)
        {
            JsonNode? runHistory;
            try
            {
                runHistory = JsonSerializer.SerializeToNode(
                    evt.Run,
                    JsonSerializationUtility.GetTypeInfo<SerializableRun>());
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn($"[Telemetry] Failed to serialize ended run history: {ex.Message}");
                return;
            }

            if (runHistory == null)
            {
                RitsuLibFramework.Logger.Warn("[Telemetry] Failed to serialize ended run history: result is null.");
                return;
            }

            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["is_victory"] = evt.IsVictory,
                ["is_abandoned"] = evt.IsAbandoned,
                ["occurred_at_utc"] = evt.OccurredAtUtc.ToString("O"),
                ["run_game_mode"] = evt.Run.GameMode.ToString(),
                ["run_is_daily"] = evt.Run.GameMode == GameMode.Daily || evt.Run.DailyTime.HasValue,
                ["run_player_count"] = evt.Run.Players.Count,
                ["run_floor_reached"] = ReadMemberValue(evt.Run, "FloorReached"),
                ["run_ascension"] = evt.Run.Ascension,
                ["run_time_seconds"] = evt.Run.RunTime,
                ["run_win_time_seconds"] = evt.Run.WinTime,
                ["run_reload_count"] = ReadMemberValue(evt.Run, "NumReloads"),
                ["run_character_ids"] = evt.Run.Players
                    .Select(player => player.CharacterId?.ToString() ?? "<unknown>")
                    .ToArray(),
            };

            var capturedApplicants = new List<string>();
            foreach (var applicant in TelemetryRegistry.GetApplicants())
            {
                if (!TelemetryRegistry.TryGetRequest(applicant, "run_history", out var request) ||
                    !TelemetryConsentStore.IsRequestGranted(applicant, request))
                    continue;

                if (!ShouldCaptureForRequest(request, evt, applicant.ApplicantId))
                    continue;

                CaptureVanillaRunHistory(applicant.ApplicantId, runHistory, null, properties);
                capturedApplicants.Add(applicant.ApplicantId);
            }

            RitsuLibFramework.Logger.Info(
                $"[Telemetry] Captured ended run history for {capturedApplicants.Count} authorized applicant(s); abandoned={evt.IsAbandoned}, victory={evt.IsVictory}.");
            foreach (var applicantId in capturedApplicants)
                TelemetryTaskRunner.Forget(
                    TelemetryQueue.FlushApplicantAsync(applicantId),
                    "flush_applicant_after_run_history");
        }

        private static bool ShouldCaptureForRequest(
            TelemetryRequest request,
            RunEndedEvent evt,
            string applicantId)
        {
            if (request.RunHistoryCaptureFilter == null)
                return true;

            try
            {
                return request.RunHistoryCaptureFilter(evt);
            }
            catch (Exception ex)
            {
                RitsuLibFramework.Logger.Warn(
                    $"[Telemetry] Run-history capture filter failed for applicant '{applicantId}': {ex.Message}");
                return false;
            }
        }
    }
}
