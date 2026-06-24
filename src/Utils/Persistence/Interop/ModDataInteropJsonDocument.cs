using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     JSON DOM persistence payload compatible with ModDataStore generic constraints.
    ///     The logical document lives under <see cref="Root" />; file JSON is this wrapper plus optional schema version
    ///     fields.
    ///     与 ModDataStore 泛型约束兼容的 JSON DOM 持久化载荷。
    ///     逻辑文档位于 <see cref="Root" /> 下；文件 JSON 是此包装器加上可选 schema 版本
    ///     字段。
    /// </summary>
    public sealed class ModDataInteropJsonDocument
    {
        /// <summary>
        ///     Primary JSON DOM migrated and synchronized with interop providers.
        ///     已迁移并与互操作提供程序同步的主 JSON DOM。
        /// </summary>
        public JsonNode? Root { get; set; } = new JsonObject();
    }
}
