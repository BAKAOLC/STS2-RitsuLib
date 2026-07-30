using System.Text.Json.Nodes;

namespace STS2RitsuLib.Utils.Persistence.Interop
{
    /// <summary>
    ///     <para xml:lang="en">Wraps a JSON DOM for persistence through <see cref="STS2RitsuLib.Data.ModDataStore" />. <see cref="Root" /> contains the logical document, while the serialized wrapper may also contain schema-version metadata.</para>
    ///     <para xml:lang="zh-CN">封装通过 <see cref="STS2RitsuLib.Data.ModDataStore" /> 持久化的 JSON DOM。<see cref="Root" /> 保存逻辑文档，序列化后的包装器还可包含架构版本元数据。</para>
    /// </summary>
    public sealed class ModDataInteropJsonDocument
    {
        /// <summary>
        ///     <para xml:lang="en">Logical JSON DOM migrated and synchronized with interoperability providers.</para>
        ///     <para xml:lang="zh-CN">参与迁移并与互操作提供程序同步的逻辑 JSON DOM。</para>
        /// </summary>
        public JsonNode? Root { get; set; } = new JsonObject();
    }
}
