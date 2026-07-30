using System.Text.Json;
using System.Text.Json.Serialization;

namespace STS2RitsuLib.Ui.Shell.Theme
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Represents a shell-theme document based on the W3C Design Tokens Format Module. Token groups are
    ///         stored as nested JSON objects, and leaf tokens contain <c>$value</c>, <c>$type</c>, and optional
    ///         <c>$description</c> properties.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         表示基于 W3C 设计令牌格式模块的外壳主题文档。令牌组存储为嵌套 JSON 对象，叶令牌包含
    ///         <c>$value</c>、<c>$type</c> 及可选的 <c>$description</c> 属性。
    ///     </para>
    /// </summary>
    public sealed class RitsuShellThemeDocument
    {
        private static readonly Lazy<JsonSerializerOptions> DefaultJsonOptions = new(() => new()
        {
            PropertyNameCaseInsensitive = true,
        });

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the optional <c>$schema</c> URL used by editors.</para>
        ///     <para xml:lang="zh-CN">获取或设置供编辑器使用的可选 <c>$schema</c> URL。</para>
        /// </summary>
        [JsonPropertyName("$schema")]
        public string? SchemaReference { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the theme format version, currently <c>1</c>.</para>
        ///     <para xml:lang="zh-CN">获取或设置主题格式版本，当前为 <c>1</c>。</para>
        /// </summary>
        [JsonPropertyName("themeFormatVersion")]
        public int? ThemeFormatVersion { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets the content revision used to upgrade extracted theme files. A newer embedded
        ///         revision replaces an older disk copy.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置用于升级已提取主题文件的内容修订号。内嵌主题的修订号较新时会替换磁盘上的旧副本。
        ///     </para>
        /// </summary>
        [JsonPropertyName("themeVersion")]
        public int? ThemeVersion { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the theme's lowercase identifier.</para>
        ///     <para xml:lang="zh-CN">获取或设置主题的小写标识符。</para>
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the human-readable name shown in theme selectors.</para>
        ///     <para xml:lang="zh-CN">获取或设置在主题选择器中显示的易读名称。</para>
        /// </summary>
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the optional parent theme over which this theme is layered.</para>
        ///     <para xml:lang="zh-CN">获取或设置可选的父主题；当前主题将叠加在该主题之上。</para>
        /// </summary>
        [JsonPropertyName("inherits")]
        public string? Inherits { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the primitive token group.</para>
        ///     <para xml:lang="zh-CN">获取或设置基础令牌组。</para>
        /// </summary>
        [JsonPropertyName("core")]
        public JsonElement? Core { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the semantic and alias token group.</para>
        ///     <para xml:lang="zh-CN">获取或设置语义令牌及别名令牌组。</para>
        /// </summary>
        [JsonPropertyName("semantic")]
        public JsonElement? Semantic { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets component tokens organized by component, variant, and state.</para>
        ///     <para xml:lang="zh-CN">获取或设置按组件、变体及状态组织的组件令牌。</para>
        /// </summary>
        [JsonPropertyName("components")]
        public JsonElement? Components { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets or sets overrides keyed by scope, including <c>shell</c>, <c>modSettings</c>, and
        ///         <c>mod:&lt;modId&gt;</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取或设置按作用域索引的覆盖，包括 <c>shell</c>、<c>modSettings</c> 和
        ///         <c>mod:&lt;modId&gt;</c>。
        ///     </para>
        /// </summary>
        [JsonPropertyName("scopes")]
        public Dictionary<string, JsonElement>? Scopes { get; set; }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets free-form extension data keyed by mod identifier.</para>
        ///     <para xml:lang="zh-CN">获取或设置按模组标识符索引的自由格式扩展数据。</para>
        /// </summary>
        [JsonPropertyName("extensions")]
        public Dictionary<string, JsonElement>? Extensions { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Deserializes a <see cref="RitsuShellThemeDocument" /> from JSON, matching property names
        ///         case-insensitively.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从 JSON 反序列化 <see cref="RitsuShellThemeDocument" />，匹配属性名时不区分大小写。
        ///     </para>
        /// </summary>
        /// <param name="stream">
        ///     <para xml:lang="en">The readable stream containing the JSON document.</para>
        ///     <para xml:lang="zh-CN">包含 JSON 文档的可读流。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">
        ///         The deserialized document, or <see langword="null" /> if the JSON root is <c>null</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         反序列化后的文档；JSON 根值为 <c>null</c> 时为 <see langword="null" />。
        ///     </para>
        /// </returns>
        /// <exception cref="JsonException">
        ///     <para xml:lang="en">The JSON is invalid or cannot be converted to a theme document.</para>
        ///     <para xml:lang="zh-CN">JSON 无效或无法转换为主题文档。</para>
        /// </exception>
        public static RitsuShellThemeDocument? Deserialize(Stream stream)
        {
            return JsonSerializer.Deserialize<RitsuShellThemeDocument>(stream, DefaultJsonOptions.Value);
        }
    }
}
