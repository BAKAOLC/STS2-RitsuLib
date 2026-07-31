using System.Text.Json.Nodes;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Represents a serializable document of model capabilities.</para>
    ///     <para xml:lang="zh-CN">表示模型能力的可序列化文档。</para>
    /// </summary>
    public sealed class ModelCapabilitySaveDocument
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or sets capability entries in display and execution order.</para>
        ///     <para xml:lang="zh-CN">获取或设置按显示与执行顺序排列的能力条目。</para>
        /// </summary>
        public List<ModelCapabilitySaveEntry> Capabilities { get; set; } = [];
    }

    /// <summary>
    ///     <para xml:lang="en">Represents the serializable state of one model capability.</para>
    ///     <para xml:lang="zh-CN">表示单个模型能力的可序列化状态。</para>
    /// </summary>
    public sealed class ModelCapabilitySaveEntry
    {
        /// <summary>
        ///     <para xml:lang="en">Gets or sets the stable capability ID.</para>
        ///     <para xml:lang="zh-CN">获取或设置稳定的能力 ID。</para>
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the capability-state schema version.</para>
        ///     <para xml:lang="zh-CN">获取或设置能力状态的架构版本。</para>
        /// </summary>
        public int Schema { get; set; } = 1;

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the serialized capability state.</para>
        ///     <para xml:lang="zh-CN">获取或设置序列化后的能力状态。</para>
        /// </summary>
        public JsonNode? Data { get; set; }
    }
}
