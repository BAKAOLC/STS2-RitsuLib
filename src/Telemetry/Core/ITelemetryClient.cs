using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Applicant-scoped telemetry client. Calls no-op when the matching request is not authorized.
    ///     申请方作用域的 telemetry client。当对应申请未授权时调用为空操作。
    /// </summary>
    public interface ITelemetryClient
    {
        /// <summary>
        ///     Applicant id this client sends on behalf of.
        ///     此 client 代表的申请方 ID。
        /// </summary>
        string ApplicantId { get; }

        /// <summary>
        ///     Returns whether <paramref name="requestId" /> is currently authorized.
        ///     返回 <paramref name="requestId" /> 当前是否已授权。
        /// </summary>
        bool IsEnabled(string requestId);

        /// <summary>
        ///     Captures a property-only event for an authorized request.
        ///     为已授权申请采集仅包含属性的事件。
        /// </summary>
        void Capture(
            string eventName,
            string requestId,
            IReadOnlyDictionary<string, object?>? properties = null);

        /// <summary>
        ///     Captures an event with structured applicant payload for an authorized request.
        ///     为已授权申请采集带结构化申请方数据的事件。
        /// </summary>
        void CapturePayload(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null);

        /// <summary>
        ///     Captures an exception under the diagnostics request.
        ///     在 diagnostics 申请下采集异常。
        /// </summary>
        void CaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null);
    }
}
