using System.Text.Json.Serialization;

namespace STS2RitsuLib.Telemetry
{
    /// <summary>
    ///     Coarse data categories used by telemetry requests and user consent.
    ///     telemetry 申请和用户授权使用的粗粒度数据类别。
    /// </summary>
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TelemetryDataCategory
    {
        /// <summary>
        ///     No data category.
        ///     无数据类别。
        /// </summary>
        None = 0,

        /// <summary>
        ///     Session start and environment metadata, such as versions, platform, and anonymous install id.
        ///     会话启动和环境元数据，例如版本、平台和匿名安装 ID。
        /// </summary>
        BasicUsage = 1 << 0,

        /// <summary>
        ///     Loaded mod inventory and mod metadata.
        ///     已加载 mod 清单和 mod 元数据。
        /// </summary>
        ModInventory = 1 << 1,

        /// <summary>
        ///     Vanilla run-history payloads, preserved without field trimming.
        ///     原版 run-history 数据；保留字段，不做裁剪。
        /// </summary>
        RunHistory = 1 << 2,

        /// <summary>
        ///     Exceptions, stack traces, runtime snapshots, and related diagnostic context.
        ///     异常、堆栈、运行时快照和相关诊断上下文。
        /// </summary>
        Diagnostics = 1 << 3,

        /// <summary>
        ///     Applicant-defined custom events or payloads.
        ///     申请方定义的自定义事件或数据。
        /// </summary>
        Custom = 1 << 4,
    }
}
