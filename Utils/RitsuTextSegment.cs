using System.Text.Json.Serialization;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     A styled text span for UIs that render rich diagnostic text.
    /// </summary>
    public sealed record RitsuTextSegment
    {
        /// <summary>
        ///     Plain text carried by this segment.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; init; } = "";

        /// <summary>
        ///     CSS-compatible foreground color, such as <c>#ff4747</c> or <c>rgb(255, 71, 71)</c>.
        /// </summary>
        [JsonPropertyName("color")]
        public string? Color { get; init; }

        /// <summary>
        ///     Whether the segment should be rendered with stronger weight.
        /// </summary>
        [JsonPropertyName("bold")]
        public bool Bold { get; init; }

        /// <summary>
        ///     Whether the segment should be rendered as secondary text.
        /// </summary>
        [JsonPropertyName("dim")]
        public bool Dim { get; init; }

        /// <summary>
        ///     Optional semantic role for callers that need richer presentation.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }
    }
}
