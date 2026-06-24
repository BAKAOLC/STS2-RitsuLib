using System.Text;
using System.Text.Json;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     PostHog batch API telemetry adapter.
    ///     PostHog batch API telemetry adapter。
    /// </summary>
    public sealed class PostHogTelemetryAdapter : ITelemetryAdapter
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        /// <summary>
        ///     Creates a PostHog adapter for a fixed host and project API key.
        ///     使用固定 host 和项目 API key 创建 PostHog adapter。
        /// </summary>
        public PostHogTelemetryAdapter(string host, string projectApiKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            Host = new(host.TrimEnd('/'), UriKind.Absolute);
            ProjectApiKey = projectApiKey;
        }

        /// <summary>
        ///     PostHog host root, for example <c>https://us.i.posthog.com</c>.
        ///     PostHog host 根地址，例如 <c>https://us.i.posthog.com</c>。
        /// </summary>
        public Uri Host { get; }

        /// <summary>
        ///     PostHog project API key.
        ///     PostHog 项目 API key。
        /// </summary>
        public string ProjectApiKey { get; }

        /// <inheritdoc />
        public string AdapterId => "posthog";

        /// <inheritdoc />
        public string EndpointDescription => $"{Host}/batch";

        /// <inheritdoc />
        public async ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ProjectApiKey))
                return TelemetrySendResult.Fail("PostHog project API key is not configured.");

            var batch = events.Select(evt => new
            {
                @event = evt.EventName,
                distinct_id = evt.Properties.GetValueOrDefault("anonymous_install_id"),
                properties = BuildProperties(evt),
                timestamp = evt.TimestampUtc,
            }).ToArray();

            var body = JsonSerializer.Serialize(new
            {
                api_key = ProjectApiKey,
                batch,
            }, TelemetryJson.Options);

            try
            {
                using var response = await Client.PostAsync(
                    new Uri(Host, "/batch/"),
                    new StringContent(body, Encoding.UTF8, "application/json"),
                    cancellationToken);

                return response.IsSuccessStatusCode
                    ? TelemetrySendResult.Ok()
                    : TelemetrySendResult.Fail($"{(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return TelemetrySendResult.Fail($"Timed out posting telemetry to {Host}.");
            }
            catch (Exception ex)
            {
                return TelemetrySendResult.Fail(ex.Message);
            }
        }

        private static Dictionary<string, object?> BuildProperties(TelemetryEnvelope evt)
        {
            var props = new Dictionary<string, object?>(evt.Properties, StringComparer.OrdinalIgnoreCase)
            {
                ["schema"] = evt.Schema,
                ["applicant_id"] = evt.ApplicantId,
                ["request_id"] = evt.RequestId,
                ["category"] = evt.Category.ToString(),
            };

            if (evt.Payload != null)
                props["payload"] = evt.Payload;

            return props;
        }
    }
}
