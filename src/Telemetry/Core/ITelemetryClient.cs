using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides an applicant-scoped telemetry client. Capture calls do nothing when the matching request is not
    ///         authorized.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供以申请方为作用域的遥测客户端。对应申请项未获授权时，采集调用不会执行任何操作。
    ///     </para>
    /// </summary>
    public interface ITelemetryClient
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the applicant represented by this client.</para>
        ///     <para xml:lang="zh-CN">获取此客户端所代表的申请方 ID。</para>
        /// </summary>
        string ApplicantId { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns whether <paramref name="requestId" /> is currently authorized.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回 <paramref name="requestId" /> 当前是否已获授权。
        ///     </para>
        /// </summary>
        bool IsEnabled(string requestId);

        /// <summary>
        ///     <para xml:lang="en">Captures a properties-only event for an authorized request.</para>
        ///     <para xml:lang="zh-CN">为已获授权的申请项采集仅包含属性的事件。</para>
        /// </summary>
        void Capture(
            string eventName,
            string requestId,
            IReadOnlyDictionary<string, object?>? properties = null);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures an event with a structured applicant payload for an authorized request.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为已获授权的申请项采集带有结构化申请方负载的事件。
        ///     </para>
        /// </summary>
        void CapturePayload(
            string eventName,
            string requestId,
            JsonNode payload,
            IReadOnlyDictionary<string, object?>? properties = null);

        /// <summary>
        ///     <para xml:lang="en">Captures an exception under the <c>diagnostics</c> request.</para>
        ///     <para xml:lang="zh-CN">在 <c>diagnostics</c> 申请项下采集异常。</para>
        /// </summary>
        void CaptureException(
            Exception exception,
            IReadOnlyDictionary<string, object?>? properties = null);
    }
}
