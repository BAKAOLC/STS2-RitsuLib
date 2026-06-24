using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Public helper entry points for the telemetry framework.
    ///     telemetry 框架的公共辅助入口。
    /// </summary>
    public static class TelemetryApi
    {
        /// <summary>
        ///     Creates an applicant-scoped telemetry client.
        ///     创建申请方作用域的 telemetry client。
        /// </summary>
        public static ITelemetryClient GetClient(string applicantId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(applicantId);
            return new TelemetryClient(applicantId);
        }

        /// <summary>
        ///     Captures a complete vanilla run-history JSON payload for an applicant.
        ///     为申请方采集完整的原版 run-history JSON 数据。
        /// </summary>
        public static void CaptureVanillaRunHistory(
            string applicantId,
            JsonNode runHistory,
            JsonNode? applicantPayload = null,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
            RunHistoryTelemetryCollector.CaptureVanillaRunHistory(
                applicantId,
                runHistory,
                applicantPayload,
                properties);
        }
    }
}
