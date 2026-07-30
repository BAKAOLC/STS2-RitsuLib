using System.Text;
using System.Text.Json;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Sends telemetry through the PostHog batch API.</para>
    ///     <para xml:lang="zh-CN">通过 PostHog 批量 API 发送遥测数据。</para>
    /// </summary>
    public sealed class PostHogTelemetryAdapter : ITelemetryAdapter
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        /// <summary>
        ///     <para xml:lang="en">Creates a PostHog adapter for a fixed host and project API key.</para>
        ///     <para xml:lang="zh-CN">使用固定主机地址和项目 API 密钥创建 PostHog 适配器。</para>
        /// </summary>
        public PostHogTelemetryAdapter(string host, string projectApiKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            Host = new(host.TrimEnd('/'), UriKind.Absolute);
            if (Host.Scheme is not ("http" or "https"))
                throw new ArgumentException("The PostHog host must use HTTP or HTTPS.", nameof(host));

            ArgumentException.ThrowIfNullOrWhiteSpace(projectApiKey);
            ProjectApiKey = projectApiKey;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the PostHog host root, such as <c>https://us.i.posthog.com</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取 PostHog 主机根地址，例如 <c>https://us.i.posthog.com</c>。
        ///     </para>
        /// </summary>
        public Uri Host { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the PostHog project API key.</para>
        ///     <para xml:lang="zh-CN">获取 PostHog 项目 API 密钥。</para>
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
            catch (OperationCanceledException)
            {
                throw;
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
