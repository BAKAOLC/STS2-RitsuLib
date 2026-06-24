namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Metadata for a telemetry contribution advertised to users and subscribers.
    ///     向用户和订阅方展示的 telemetry contribution 元数据。
    /// </summary>
    public sealed class TelemetryContributionDefinition
    {
        /// <summary>
        ///     Mod id that owns this contribution.
        ///     拥有此 contribution 的 mod id。
        /// </summary>
        public required string ContributorModId { get; init; }

        /// <summary>
        ///     Stable contribution id within the contributor mod.
        ///     contributor mod 内稳定的 contribution ID。
        /// </summary>
        public required string ContributionId { get; init; }

        /// <summary>
        ///     Data category where this contribution can be used.
        ///     此 contribution 可用于的数据类别。
        /// </summary>
        public required TelemetryDataCategory Category { get; init; }

        /// <summary>
        ///     Visibility and routing policy.
        ///     可见性和路由策略。
        /// </summary>
        public required TelemetryContributionVisibility Visibility { get; init; }

        /// <summary>
        ///     Human-readable explanation of the contribution.
        ///     contribution 的可读说明。
        /// </summary>
        public required string Description { get; init; }
    }
}
