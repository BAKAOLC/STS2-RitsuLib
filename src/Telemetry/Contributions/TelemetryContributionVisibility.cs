using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Visibility policy for a telemetry contribution provider.
    ///     telemetry contribution provider 的可见性策略。
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryContributionVisibility
    {
        /// <summary>
        ///     Contribution is private to the owning applicant and is attached to that applicant's own subscribed
        ///     requests without extra shared-source consent.
        ///     contribution 仅属于拥有它的申请方，会附加到该申请方自己订阅的申请中，
        ///     不需要额外的共享来源授权。
        /// </summary>
        PrivateToApplicant,

        /// <summary>
        ///     Contribution may be routed to applicants that subscribe to it and receive explicit user consent.
        ///     contribution 可路由给订阅它并获得用户明确授权的申请方。
        /// </summary>
        SharedToAuthorizedSubscribers,
    }
}
