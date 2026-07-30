using System.Text.Json.Nodes;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides telemetry data that is private to an applicant or explicitly shared with other applicants.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供申请方私有或经明确授权与其他申请方共享的遥测数据。
    ///     </para>
    /// </summary>
    public interface ITelemetryContributionProvider
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns this contribution.</para>
        ///     <para xml:lang="zh-CN">获取拥有此数据贡献的模组 ID。</para>
        /// </summary>
        string ContributorModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the contribution's stable ID within the contributing mod.</para>
        ///     <para xml:lang="zh-CN">获取此数据贡献在提供方模组内的稳定 ID。</para>
        /// </summary>
        string ContributionId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the data category to which this contribution can be attached.</para>
        ///     <para xml:lang="zh-CN">获取可附加此数据贡献的数据类别。</para>
        /// </summary>
        TelemetryDataCategory Category { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the contribution's visibility and routing policy.</para>
        ///     <para xml:lang="zh-CN">获取此数据贡献的可见性和路由策略。</para>
        /// </summary>
        TelemetryContributionVisibility Visibility { get; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Builds this contribution's payload for the current telemetry event. Private contributions are
        ///         attached only to their owning applicant; shared contributions require explicit consent for the
        ///         source.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为当前遥测事件构建此数据贡献的负载。私有数据贡献只会附加到其所属申请方；共享数据贡献需要针对
        ///         来源的明确授权。
        ///     </para>
        /// </summary>
        JsonNode? Build(TelemetryContributionContext context);
    }

    /// <summary>
    ///     <para xml:lang="en">Provides context to a telemetry contribution provider.</para>
    ///     <para xml:lang="zh-CN">提供传递给遥测数据贡献提供程序的上下文。</para>
    /// </summary>
    public sealed class TelemetryContributionContext
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the applicant that will receive the event.</para>
        ///     <para xml:lang="zh-CN">获取将接收该事件的申请方。</para>
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the request currently being assembled.</para>
        ///     <para xml:lang="zh-CN">获取当前正在组装的申请项 ID。</para>
        /// </summary>
        public required string RequestId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the name of the event currently being assembled.</para>
        ///     <para xml:lang="zh-CN">获取当前正在组装的事件名称。</para>
        /// </summary>
        public required string EventName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the shared base payload already assembled for the event, if any.</para>
        ///     <para xml:lang="zh-CN">获取已为该事件组装的共享基础负载（如果有）。</para>
        /// </summary>
        public JsonNode? BasePayload { get; init; }
    }
}
