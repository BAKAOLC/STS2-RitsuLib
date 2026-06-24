using System.Text.Json;
using System.Text.Json.Serialization;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     Deserialized W3C Design Tokens Format Module document for a shell theme. Group nodes are nested
    ///     dictionaries; leaf tokens carry <c>$value</c>, <c>$type</c>, and optional <c>$description</c>.
    ///     shell 主题的反序列化 W3C Design Tokens Format Module 文档。分组节点是嵌套
    ///     字典；叶令牌携带 <c>$value</c>、<c>$type</c> 和可选的 <c>$description</c>。
    /// </summary>
    public sealed class RitsuShellThemeDocument
    {
        private static readonly Lazy<JsonSerializerOptions> DefaultJsonOptions = new(() => new()
        {
            PropertyNameCaseInsensitive = true,
        });

        /// <summary>
        ///     Optional <c>$schema</c> URL for editors.
        ///     可选 <c>$schema</c> 供编辑器使用的 URL。
        /// </summary>
        [JsonPropertyName("$schema")]
        public string? SchemaReference { get; set; }

        /// <summary>
        ///     Theme format version (currently <c>1</c>).
        ///     Theme 格式版本 (当前 <c>1</c>)。
        /// </summary>
        [JsonPropertyName("themeFormatVersion")]
        public int? ThemeFormatVersion { get; set; }

        /// <summary>
        ///     Theme content revision for disk auto-upgrade. Higher values replace older disk copies.
        ///     用于磁盘自动升级的主题内容修订号。较高值会替换较旧的磁盘副本。
        /// </summary>
        [JsonPropertyName("themeVersion")]
        public int? ThemeVersion { get; set; }

        /// <summary>
        ///     Theme id (lowercase identifier).
        ///     Theme id (小写 identifier)。
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        ///     Human-readable display name shown in pickers.
        ///     人类可读显示名 显示在选择器中。
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        ///     Optional parent theme id; this theme is layered on top.
        ///     可选 父主题 id; this theme is 叠加在其上。
        /// </summary>
        [JsonPropertyName("inherits")]
        public string? Inherits { get; set; }

        /// <summary>
        ///     Primitive tokens (raw values).
        ///     原始令牌（原始值）。
        /// </summary>
        [JsonPropertyName("core")]
        public JsonElement? Core { get; set; }

        /// <summary>
        ///     Semantic / alias tokens.
        ///     语义/别名令牌。
        /// </summary>
        [JsonPropertyName("semantic")]
        public JsonElement? Semantic { get; set; }

        /// <summary>
        ///     Component tokens (component → variant → state).
        ///     组件令牌（组件 → 变体 → 状态）。
        /// </summary>
        [JsonPropertyName("components")]
        public JsonElement? Components { get; set; }

        /// <summary>
        ///     Per-scope overrides (<c>shell</c> / <c>modSettings</c> / <c>mod:&lt;modId&gt;</c>).
        ///     按作用域划分的覆盖 (<c>shell</c> / <c>modSettings</c> / <c>mod:&lt;modId&gt;</c>)。
        /// </summary>
        [JsonPropertyName("scopes")]
        public Dictionary<string, JsonElement>? Scopes { get; set; }

        /// <summary>
        ///     Free-form mod extension blobs keyed by mod id.
        ///     自由格式 mod 扩展 blob 键为 mod id。
        /// </summary>
        [JsonPropertyName("extensions")]
        public Dictionary<string, JsonElement>? Extensions { get; set; }

        /// <summary>
        ///     Deserializes a <see cref="RitsuShellThemeDocument" /> from a JSON stream (case-insensitive properties).
        ///     从 JSON 流反序列化 <see cref="RitsuShellThemeDocument" />（属性不区分大小写）。
        /// </summary>
        /// <param name="stream">
        ///     Input JSON stream.
        ///     输入 JSON 流。
        /// </param>
        /// <returns>
        ///     The deserialized document, or <see langword="null" /> on failure.
        ///     反序列化后的文档；失败时为 <see langword="null" />。
        /// </returns>
        public static RitsuShellThemeDocument? Deserialize(Stream stream)
        {
            return JsonSerializer.Deserialize<RitsuShellThemeDocument>(stream, DefaultJsonOptions.Value);
        }
    }
}
