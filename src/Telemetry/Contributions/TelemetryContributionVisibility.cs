using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Specifies a telemetry contribution provider's visibility policy.</para>
    ///     <para xml:lang="zh-CN">指定遥测数据贡献提供程序的可见性策略。</para>
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryContributionVisibility
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         The contribution is private to its owning applicant and is attached to that applicant's subscribed
        ///         requests without additional consent for a shared source.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此数据贡献仅供其所属申请方使用，可附加到该申请方订阅的申请项，无需额外授予共享来源权限。
        ///     </para>
        /// </summary>
        PrivateToApplicant,

        /// <summary>
        ///     <para xml:lang="en">
        ///         The contribution may be routed to subscribing applicants that have received explicit user consent.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         此数据贡献可路由给已订阅且获得用户明确授权的申请方。
        ///     </para>
        /// </summary>
        SharedToAuthorizedSubscribers,
    }
}
