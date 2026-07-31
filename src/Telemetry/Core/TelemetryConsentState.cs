using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Specifies the user's consent state for one telemetry applicant.</para>
    ///     <para xml:lang="zh-CN">指定用户对一个遥测申请方的授权状态。</para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryConsentState
    {
        /// <summary>
        ///     <para xml:lang="en">The user has not made a decision; telemetry is not sent.</para>
        ///     <para xml:lang="zh-CN">用户尚未作出决定；不会发送遥测数据。</para>
        /// </summary>
        Unknown,

        /// <summary>
        ///     <para xml:lang="en">The user denied this applicant; telemetry is not sent.</para>
        ///     <para xml:lang="zh-CN">用户拒绝了此申请方；不会发送遥测数据。</para>
        /// </summary>
        Denied,

        /// <summary>
        ///     <para xml:lang="en">The user granted at least one request for this applicant.</para>
        ///     <para xml:lang="zh-CN">用户已授权此申请方的至少一个申请项。</para>
        /// </summary>
        Granted,
    }
}
