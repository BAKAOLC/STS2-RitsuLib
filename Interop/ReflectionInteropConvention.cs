namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Names of static provider methods bound by <c>ReflectionStaticChannelBinder</c>.
    ///     由 <c>ReflectionStaticChannelBinder</c> 绑定的静态提供方方法名。
    /// </summary>
    public sealed class ReflectionInteropConvention
    {
        /// <summary>
        ///     Name of the required static method <c>(string key) → object?</c> that reads a keyed payload from the
        ///     provider.
        ///     从提供方读取 keyed 载荷的必需静态方法名：<c>(string key) → object?</c>。
        /// </summary>
        public required string ObjectGetMethodName { get; init; }

        /// <summary>
        ///     Name of the required static method <c>(string key, object? value) → void</c> that writes a keyed
        ///     payload to the provider.
        ///     向提供方写入 keyed 载荷的必需静态方法名：<c>(string key, object? value) → void</c>。
        /// </summary>
        public required string ObjectSetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for RFC 7386 merge-patch retrieval: <c>(string key) → JsonNode?</c>.
        ///     https://www.rfc-editor.org/rfc/rfc7386
        ///     用于获取 RFC 7386 merge-patch 的可选静态方法名：<c>(string key) → JsonNode?</c>。
        /// </summary>
        public string? MergePatchGetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for applying a merge patch: <c>(string key, JsonNode? patch) → void</c>.
        ///     https://www.rfc-editor.org/rfc/rfc7386
        ///     用于应用 merge patch 的可选静态方法名：<c>(string key, JsonNode? patch) → void</c>。
        /// </summary>
        public string? MergePatchApplyMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for JSON Patch (RFC 6902) retrieval: <c>(string key) → JsonNode?</c>.
        ///     https://www.rfc-editor.org/rfc/rfc6902
        ///     用于获取 JSON Patch（RFC 6902）的可选静态方法名：<c>(string key) → JsonNode?</c>。
        /// </summary>
        public string? JsonPatchGetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for applying a JSON Patch (RFC 6902):
        ///     <c>(string key, JsonNode? patch) → void</c>.
        ///     https://www.rfc-editor.org/rfc/rfc6902
        ///     用于应用 JSON Patch（RFC 6902）的可选静态方法名：
        /// </summary>
        public string? JsonPatchApplyMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for JSON Pointer node read:
        ///     <c>(string key, string jsonPointer) → JsonNode?</c>.
        ///     用于 JSON Pointer 节点读取的可选静态方法名：
        /// </summary>
        public string? NodeGetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for JSON Pointer node write:
        ///     <c>(string key, string jsonPointer, JsonNode? node) → void</c>.
        ///     用于 JSON Pointer 节点写入的可选静态方法名：
        /// </summary>
        public string? NodeSetMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for merging a <see cref="System.Text.Json.Nodes.JsonObject" /> at a
        ///     pointer: <c>(string key, string jsonPointer, JsonObject value) → void</c>.
        ///     <c>(string key, string jsonPointer, JsonObject value) → void</c>。
        ///     用于在 pointer 位置合并 <see cref="System.Text.Json.Nodes.JsonObject" /> 的可选静态方法名：
        /// </summary>
        public string? ObjectMergeAtMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for whole-document JSON text read: <c>(string key) → string?</c>.
        ///     用于读取整份文档 JSON 文本的可选静态方法名：<c>(string key) → string?</c>。
        /// </summary>
        public string? TypedGetJsonMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for whole-document JSON text write:
        ///     <c>(string key, string json) → void</c>.
        ///     用于写入整份文档 JSON 文本的可选静态方法名：
        /// </summary>
        public string? TypedSetJsonMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for root <see cref="System.Text.Json.Nodes.JsonObject" /> read:
        ///     <c>(string key) → JsonObject?</c>.
        ///     <c>(string key) → JsonObject?</c>。
        ///     用于读取根 <see cref="System.Text.Json.Nodes.JsonObject" /> 的可选静态方法名：
        ///     <c>(string key) → JsonObject?</c>。
        ///     <c>(string key) → JsonObject?</c>。
        /// </summary>
        public string? TypedGetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     Optional static method name for root <see cref="System.Text.Json.Nodes.JsonObject" /> write:
        ///     <c>(string key, JsonObject root) → void</c>.
        ///     用于写入根 <see cref="System.Text.Json.Nodes.JsonObject" /> 的可选静态方法名：
        /// </summary>
        public string? TypedSetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     Convention for <c>CreateRitsuLibModDataSchema</c> / ModData runtime interop providers.
        ///     <c>CreateRitsuLibModDataSchema</c>
        ///     ModData 运行时互操作提供方使用的约定。
        /// </summary>
        public static ReflectionInteropConvention ModData { get; } = new()
        {
            ObjectGetMethodName = "GetRitsuLibModDataValue",
            ObjectSetMethodName = "SetRitsuLibModDataValue",
            MergePatchGetMethodName = "GetRitsuLibModDataMergePatch",
            MergePatchApplyMethodName = "ApplyRitsuLibModDataMergePatch",
            JsonPatchGetMethodName = "GetRitsuLibModDataJsonPatch",
            JsonPatchApplyMethodName = "ApplyRitsuLibModDataJsonPatch",
            NodeGetMethodName = "GetRitsuLibModDataNode",
            NodeSetMethodName = "SetRitsuLibModDataNode",
            ObjectMergeAtMethodName = "MergeRitsuLibModDataObject",
            TypedGetJsonMethodName = "GetRitsuLibModDataJson",
            TypedSetJsonMethodName = "SetRitsuLibModDataJson",
            TypedGetJsonObjectMethodName = "GetRitsuLibModDataJsonObject",
            TypedSetJsonObjectMethodName = "SetRitsuLibModDataJsonObject",
        };

        /// <summary>
        ///     Object resolvers for settings runtime interop. Optional JSON DOM tier names are left unset so
        ///     existing providers keep working; add a new <see cref="ReflectionInteropConvention" /> with
        ///     custom method names if a settings provider later opts into merge / pointer / text tiers.
        ///     Typed bool/int/double/string accessors remain handled by the settings runtime mirror.
        ///     settings 运行时互操作使用的对象解析器。可选 JSON DOM 层名称保持未设置，以便现有
        ///     provider 继续工作；如果之后 settings provider 选择接入合并/pointer/文本层，请新增带自定义
        ///     方法名的 <see cref="ReflectionInteropConvention" />。类型化 bool/int/double/string 访问器仍由
        ///     settings 运行时镜像处理。
        /// </summary>
        public static ReflectionInteropConvention SettingsRuntimeInterop { get; } = new()
        {
            ObjectGetMethodName = "GetRitsuLibSettingValue",
            ObjectSetMethodName = "SetRitsuLibSettingValue",
        };
    }
}
