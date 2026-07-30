using System.Text.Json.Serialization;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Represents a styled text span for UI code that renders rich diagnostic text.</para>
    ///     <para xml:lang="zh-CN">表示供界面代码渲染富格式诊断文本的带样式文本片段。</para>
    /// </summary>
    public sealed record RitsuTextSegment
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the plain text carried by this segment.</para>
        ///     <para xml:lang="zh-CN">获取此片段包含的纯文本。</para>
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets an optional CSS-compatible foreground color, such as <c>#ff4747</c> or <c>rgb(255, 71, 71)</c>.</para>
        ///     <para xml:lang="zh-CN">获取可选的 CSS 兼容前景色，例如 <c>#ff4747</c> 或 <c>rgb(255, 71, 71)</c>。</para>
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the segment should be rendered with stronger weight.</para>
        ///     <para xml:lang="zh-CN">获取该片段是否应以更粗的字重渲染。</para>
        /// </summary>
        [JsonPropertyName("bold")]
        public bool Bold { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets whether the segment should be rendered as secondary text.</para>
        ///     <para xml:lang="zh-CN">获取该片段是否应以次要文本渲染。</para>
        /// </summary>
        [JsonPropertyName("dim")]
        public bool Dim { get; init; }

        /// <summary>
        ///     <para xml:lang="en">Gets an optional semantic role for callers that need richer presentation.</para>
        ///     <para xml:lang="zh-CN">获取供需要更丰富呈现的调用方使用的可选语义角色。</para>
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }
    }
}
