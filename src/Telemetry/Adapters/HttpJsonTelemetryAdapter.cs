using System.Text;
using System.Text.Json;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Generic JSON-over-HTTP telemetry adapter for self-hosted mod endpoints.
    ///     面向自托管 mod endpoint 的通用 JSON-over-HTTP telemetry adapter。
    /// </summary>
    public sealed class HttpJsonTelemetryAdapter : ITelemetryAdapter
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        private readonly IReadOnlyDictionary<string, string> _headers;

        /// <summary>
        ///     Creates an adapter that POSTs batches to <paramref name="endpoint" />.
        ///     创建向 <paramref name="endpoint" /> POST 批量事件的 adapter。
        /// </summary>
        public HttpJsonTelemetryAdapter(string endpoint, IReadOnlyDictionary<string, string>? headers = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            Endpoint = new(endpoint, UriKind.Absolute);
            _headers = headers ?? new Dictionary<string, string>();
        }

        /// <summary>
        ///     Absolute endpoint URI that receives telemetry batches.
        ///     接收 telemetry 批量事件的绝对 endpoint URI。
        /// </summary>
        public Uri Endpoint { get; }

        /// <inheritdoc />
        public string AdapterId => "http_json";

        /// <inheritdoc />
        public string EndpointDescription => Endpoint.ToString();

        /// <inheritdoc />
        public async ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default)
        {
            var body = JsonSerializer.Serialize(new
            {
                schema = "ritsulib.telemetry.batch.v1",
                applicant_id = applicant.ApplicantId,
                events,
            }, TelemetryJson.Options);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                foreach (var header in _headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                using var response = await Client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return TelemetrySendResult.Ok();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var reason = string.IsNullOrWhiteSpace(responseBody)
                    ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
                    : $"{(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}";
                return TelemetrySendResult.Fail(reason);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return TelemetrySendResult.Fail($"Timed out posting telemetry to {Endpoint}.");
            }
            catch (Exception ex)
            {
                return TelemetrySendResult.Fail(ex.Message);
            }
        }
    }
}
