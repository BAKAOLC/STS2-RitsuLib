using System.Text.Json.Serialization;

namespace STS2RitsuLib.Diagnostics.Logging
{
    /// <summary>
    ///     OpenTelemetry-shaped debug log event used by RitsuLib's local viewer pipeline.
    ///     RitsuLib 本地查看器管道使用的 OpenTelemetry 风格调试日志事件。
    /// </summary>
    internal sealed record RitsuDebugLogRecord
    {
        [JsonPropertyName("id")] public long Id { get; init; }

        [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; init; }

        [JsonPropertyName("timeUnixNano")] public string TimeUnixNano { get; init; } = "";

        [JsonPropertyName("severityText")] public string SeverityText { get; init; } = "";

        [JsonPropertyName("severityNumber")] public int SeverityNumber { get; init; }

        [JsonPropertyName("body")] public string Body { get; init; } = "";

        [JsonPropertyName("source")] public string? Source { get; init; }

        [JsonPropertyName("category")] public string? Category { get; init; }

        [JsonPropertyName("loggerName")] public string? LoggerName { get; init; }

        [JsonPropertyName("codeFilePath")] public string? CodeFilePath { get; init; }

        [JsonPropertyName("codeFunctionName")] public string? CodeFunctionName { get; init; }

        [JsonPropertyName("codeLineNumber")] public int? CodeLineNumber { get; init; }

        [JsonPropertyName("attributes")]
        public IReadOnlyDictionary<string, object?> Attributes { get; init; } =
            new Dictionary<string, object?>();

        [JsonPropertyName("resource")]
        public IReadOnlyDictionary<string, object?> Resource { get; init; } =
            new Dictionary<string, object?>();

        [JsonPropertyName("scope")]
        public IReadOnlyDictionary<string, object?> Scope { get; init; } =
            new Dictionary<string, object?>();
    }
}
