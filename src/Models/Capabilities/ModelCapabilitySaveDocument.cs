using System.Text.Json.Nodes;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Serializable document for model capabilities.
    ///     模型能力的可序列化文档。
    /// </summary>
    public sealed class ModelCapabilitySaveDocument
    {
        /// <summary>
        ///     Capability entries in display/execution order.
        ///     按显示/执行顺序排列的能力条目。
        /// </summary>
        public List<ModelCapabilitySaveEntry> Capabilities { get; set; } = [];
    }

    /// <summary>
    ///     Serializable state for one model capability.
    ///     单个模型能力的可序列化状态。
    /// </summary>
    public sealed class ModelCapabilitySaveEntry
    {
        /// <summary>
        ///     Stable capability id.
        ///     稳定能力 ID。
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        ///     Capability state schema version.
        ///     能力状态 schema 版本。
        /// </summary>
        public int Schema { get; set; } = 1;

        /// <summary>
        ///     Serialized capability state.
        ///     序列化能力状态。
        /// </summary>
        public JsonNode? Data { get; set; }
    }
}
