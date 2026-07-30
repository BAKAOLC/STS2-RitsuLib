using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry.RunHistory;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Provides public helper entry points for the telemetry framework.</para>
    ///     <para xml:lang="zh-CN">提供遥测框架的公共辅助入口。</para>
    /// </summary>
    public static class TelemetryApi
    {
        /// <summary>
        ///     <para xml:lang="en">Creates an applicant-scoped telemetry client.</para>
        ///     <para xml:lang="zh-CN">创建以申请方为作用域的遥测客户端。</para>
        /// </summary>
        public static ITelemetryClient GetClient(string applicantId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(applicantId);
            return new TelemetryClient(applicantId);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Captures a complete base-game run-history JSON payload for an applicant.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为申请方采集完整的原版游戏历史记录 JSON 负载。
        ///     </para>
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
