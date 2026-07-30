namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Describes a telemetry contribution presented to users and subscribing applicants.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         描述向用户和订阅申请方展示的遥测数据贡献。
    ///     </para>
    /// </summary>
    public sealed class TelemetryContributionDefinition
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns this contribution.</para>
        ///     <para xml:lang="zh-CN">获取拥有此数据贡献的模组 ID。</para>
        /// </summary>
        public required string ContributorModId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the contribution's stable ID within the contributing mod.</para>
        ///     <para xml:lang="zh-CN">获取此数据贡献在提供方模组内的稳定 ID。</para>
        /// </summary>
        public required string ContributionId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the data category in which this contribution can be used.</para>
        ///     <para xml:lang="zh-CN">获取可使用此数据贡献的数据类别。</para>
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the contribution's visibility and routing policy.</para>
        ///     <para xml:lang="zh-CN">获取此数据贡献的可见性和路由策略。</para>
        /// </summary>
        public required TelemetryContributionVisibility Visibility { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets a human-readable explanation of the contribution.</para>
        ///     <para xml:lang="zh-CN">获取此数据贡献的可读说明。</para>
        /// </summary>
        public required string Description { get; init; }
    }
}
