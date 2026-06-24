using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Provides applicant-private or explicitly shared telemetry context.
    ///     提供申请方私有或显式共享的 telemetry 上下文。
    /// </summary>
    public interface ITelemetryContributionProvider
    {
        /// <summary>
        ///     Mod id that owns this contribution.
        ///     拥有此 contribution 的 mod id。
        /// </summary>
        string ContributorModId { get; }

        /// <summary>
        ///     Stable contribution id within the contributor mod.
        ///     contributor mod 内稳定的 contribution ID。
        /// </summary>
        string ContributionId { get; }

        /// <summary>
        ///     Data category where this contribution can be attached.
        ///     此 contribution 可附加到的数据类别。
        /// </summary>
        TelemetryDataCategory Category { get; }

        /// <summary>
        ///     Visibility and routing policy.
        ///     可见性和路由策略。
        /// </summary>
        TelemetryContributionVisibility Visibility { get; }

        /// <summary>
        ///     Builds the contribution payload for the current telemetry event. Private contributions are only attached
        ///     to the owning applicant; shared contributions require explicit source consent.
        ///     为当前 telemetry 事件构建 contribution 数据。私有 contribution 仅附加到拥有者自己的申请；
        ///     共享 contribution 需要额外的来源授权。
        /// </summary>
        JsonNode? Build(TelemetryContributionContext context);
    }

    /// <summary>
    ///     Context passed to a telemetry contribution provider.
    ///     传递给 telemetry contribution provider 的上下文。
    /// </summary>
    public sealed class TelemetryContributionContext
    {
        /// <summary>
        ///     Applicant that will receive the event.
        ///     将接收该事件的申请方。
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     Request currently being assembled.
        ///     当前正在组装的申请 ID。
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     Event currently being assembled.
        ///     当前正在组装的事件。
        /// </summary>
        public required string EventName { get; init; }

        /// <summary>
        ///     Shared base payload already assembled for the event, if any.
        ///     已为事件组装的共享基础数据（如果存在）。
        /// </summary>
        public JsonNode? BasePayload { get; init; }
    }
}
