using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Declares one telemetry data applicant: who requests data, where it is sent, and which requests it exposes.
    ///     声明一个 telemetry 数据申请方：谁申请数据、发送到哪里，以及暴露哪些申请项。
    /// </summary>
    public sealed class TelemetryApplicant
    {
        /// <summary>
        ///     Stable applicant id. Usually the same value as the owning mod id.
        ///     稳定申请方 ID。通常与所属 mod id 相同。
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     Mod id that owns this applicant declaration.
        ///     拥有此申请声明的 mod id。
        /// </summary>
        public required string OwnerModId { get; init; }

        /// <summary>
        ///     Human-readable name shown in the telemetry settings UI.
        ///     telemetry 设置界面中显示的可读名称。
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        ///     Optional localized display name shown in consent and settings UI.
        ///     可选本地化显示名，用于授权弹窗和设置 UI。
        /// </summary>
        public ModSettingsText? DisplayNameText { get; init; }

        /// <summary>
        ///     Fixed adapter/endpoint used for this applicant.
        ///     此申请方使用的固定 adapter/endpoint。
        /// </summary>
        public required ITelemetryAdapter Adapter { get; init; }

        /// <summary>
        ///     Data requests presented to the user for consent.
        ///     向用户展示并请求授权的数据申请项。
        /// </summary>
        public IReadOnlyList<TelemetryRequest> Requests { get; init; } = [];

        internal string ResolveDisplayName()
        {
            return DisplayNameText?.Resolve() ?? DisplayName;
        }
    }
}
