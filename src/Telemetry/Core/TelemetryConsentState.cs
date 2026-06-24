using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     User consent state for one telemetry applicant.
    ///     单个 telemetry 申请方的用户授权状态。
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryConsentState
    {
        /// <summary>
        ///     The user has not made a decision yet; telemetry is not sent.
        ///     用户尚未作出决定；不会发送 telemetry。
        /// </summary>
        Unknown,

        /// <summary>
        ///     The user denied this applicant; telemetry is not sent.
        ///     用户拒绝了此申请方；不会发送 telemetry。
        /// </summary>
        Denied,

        /// <summary>
        ///     The user granted at least one request for this applicant.
        ///     用户已授权此申请方的至少一项申请。
        /// </summary>
        Granted,
    }
}
