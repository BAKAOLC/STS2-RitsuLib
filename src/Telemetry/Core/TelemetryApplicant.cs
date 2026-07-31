using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Declares a telemetry applicant, including who requests data, where it is sent, and which requests are
    ///         presented to users.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         声明一个遥测申请方，包括数据申请者、发送目标以及向用户展示的申请项。
    ///     </para>
    /// </summary>
    public sealed class TelemetryApplicant
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the stable applicant ID, which is usually the owning mod's ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取稳定的申请方 ID，通常与所属模组的 ID 相同。
        ///     </para>
        /// </summary>
        public required string ApplicantId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the ID of the mod that owns this applicant declaration.</para>
        ///     <para xml:lang="zh-CN">获取拥有此申请方声明的模组 ID。</para>
        /// </summary>
        public required string OwnerModId { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the human-readable name shown in the telemetry settings UI.</para>
        ///     <para xml:lang="zh-CN">获取遥测设置界面中显示的可读名称。</para>
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an optional localized display name for the consent prompt and settings UI.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取用于授权提示和设置界面的可选本地化显示名称。
        ///     </para>
        /// </summary>
        public ModSettingsText? DisplayNameText { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the fixed adapter and endpoint used for this applicant.</para>
        ///     <para xml:lang="zh-CN">获取此申请方使用的固定适配器和端点。</para>
        /// </summary>
        public required ITelemetryAdapter Adapter { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets the data requests presented to the user for consent.</para>
        ///     <para xml:lang="zh-CN">获取向用户展示以请求授权的数据申请项。</para>
        /// </summary>
        public IReadOnlyList<TelemetryRequest> Requests { get; init; } = [];

        internal string ResolveDisplayName()
        {
            return DisplayNameText?.Resolve() ?? DisplayName;
        }
    }
}
