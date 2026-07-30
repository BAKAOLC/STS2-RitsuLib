using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     <para xml:lang="en">Specifies broad data categories used by telemetry requests and user consent.</para>
    ///     <para xml:lang="zh-CN">指定遥测申请项和用户授权所使用的概括性数据类别。</para>
    /// </summary>
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryDataCategory
    {
        /// <summary>
        ///     <para xml:lang="en">No data category.</para>
        ///     <para xml:lang="zh-CN">不包含任何数据类别。</para>
        /// </summary>
        None = 0,

        /// <summary>
        ///     <para xml:lang="en">
        ///         Session-start and environment metadata, such as versions, platform, and anonymous installation ID.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         会话启动和环境元数据，例如版本、平台和匿名安装 ID。
        ///     </para>
        /// </summary>
        BasicUsage = 1 << 0,

        /// <summary>
        ///     <para xml:lang="en">Loaded-mod inventory and mod metadata.</para>
        ///     <para xml:lang="zh-CN">已加载模组的清单和模组元数据。</para>
        /// </summary>
        ModInventory = 1 << 1,

        /// <summary>
        ///     <para xml:lang="en">Base-game run-history payloads preserved without removing fields.</para>
        ///     <para xml:lang="zh-CN">保留全部字段的原版游戏历史记录负载。</para>
        /// </summary>
        RunHistory = 1 << 2,

        /// <summary>
        ///     <para xml:lang="en">Exceptions, stack traces, runtime snapshots, and related diagnostic context.</para>
        ///     <para xml:lang="zh-CN">异常、堆栈跟踪、运行时快照及相关诊断上下文。</para>
        /// </summary>
        Diagnostics = 1 << 3,

        /// <summary>
        ///     <para xml:lang="en">Applicant-defined custom events or payloads.</para>
        ///     <para xml:lang="zh-CN">申请方定义的自定义事件或负载。</para>
        /// </summary>
        Custom = 1 << 4,
    }
}
