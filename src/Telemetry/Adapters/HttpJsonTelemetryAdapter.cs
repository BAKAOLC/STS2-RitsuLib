using System.Text;
using System.Text.Json;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Sends telemetry batches as JSON over HTTP to a self-hosted mod endpoint.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 HTTP 将遥测批次以 JSON 格式发送到模组自行托管的端点。
    ///     </para>
    /// </summary>
    public sealed class HttpJsonTelemetryAdapter : ITelemetryAdapter
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        private readonly IReadOnlyDictionary<string, string> _headers;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates an adapter that sends batches to <paramref name="endpoint" /> with HTTP POST requests.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         创建通过 HTTP POST 请求向 <paramref name="endpoint" /> 发送批次的适配器。
        ///     </para>
        /// </summary>
        public HttpJsonTelemetryAdapter(string endpoint, IReadOnlyDictionary<string, string>? headers = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            Endpoint = new(endpoint, UriKind.Absolute);
            if (Endpoint.Scheme is not ("http" or "https"))
                throw new ArgumentException("The telemetry endpoint must use HTTP or HTTPS.", nameof(endpoint));

            _headers = headers == null
                ? []
                : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the absolute endpoint URI that receives telemetry batches.</para>
        ///     <para xml:lang="zh-CN">获取接收遥测批次的绝对端点 URI。</para>
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TelemetrySendResult.Fail(ex.Message);
            }
        }
    }
}
