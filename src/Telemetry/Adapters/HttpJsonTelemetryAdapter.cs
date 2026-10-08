using System.Net;
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
        private const int MaxBatchBytes = 4 * 1024 * 1024;

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
        /// <remarks>
        ///     <para xml:lang="en">Batches larger than 4 MiB of UTF-8 JSON fail without an HTTP request. Queued delivery splits oversized batches and discards a single event that cannot fit.</para>
        ///     <para xml:lang="zh-CN">UTF-8 JSON 超过 4 MiB 的批次会直接失败，不发起 HTTP 请求。队列投递会拆分过大的批次，并丢弃仍无法容纳的单个事件。</para>
        /// </remarks>
        public async ValueTask<TelemetrySendResult> SendAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken = default)
        {
            var (result, _) = await SendBatchAsync(applicant, events, cancellationToken).ConfigureAwait(false);
            return result;
        }

        internal async ValueTask<(TelemetrySendResult Result, bool PayloadTooLarge)> SendBatchAsync(
            TelemetryApplicant applicant,
            IReadOnlyList<TelemetryEnvelope> events,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(applicant);
            ArgumentNullException.ThrowIfNull(events);
            cancellationToken.ThrowIfCancellationRequested();
            var body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schema = "ritsulib.telemetry.batch.v1",
                applicant_id = applicant.ApplicantId,
                events,
            }, TelemetryJson.Options);
            if (body.Length > MaxBatchBytes)
                return (TelemetrySendResult.Fail("Telemetry batch exceeds the 4 MiB JSON limit."), true);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };

                foreach (var header in _headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

                using var response = await Client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return (TelemetrySendResult.Ok(), false);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var reason = string.IsNullOrWhiteSpace(responseBody)
                    ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
                    : $"{(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}";
                return (TelemetrySendResult.Fail(reason), response.StatusCode == HttpStatusCode.RequestEntityTooLarge);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (TelemetrySendResult.Fail($"Timed out posting telemetry to {Endpoint}."), false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (TelemetrySendResult.Fail(ex.Message), false);
            }
        }
    }
}
