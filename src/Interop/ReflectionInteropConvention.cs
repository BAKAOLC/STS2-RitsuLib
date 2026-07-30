namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Names of static provider methods bound by the reflection-based keyed-data channel.
    ///     </para>
    ///     <para xml:lang="zh-CN">由反射式键控数据通道绑定的静态提供方方法名。</para>
    /// </summary>
    public sealed class ReflectionInteropConvention
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Required static object-read method name. Its signature is <c>(string key) → object?</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         必需的静态对象读取方法名；其签名为 <c>(string key) → object?</c>。
        ///     </para>
        /// </summary>
        public required string ObjectGetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Required static object-write method name. Its signature is
        ///         <c>(string key, object? value) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         必需的静态对象写入方法名；其签名为 <c>(string key, object? value) → void</c>。
        ///     </para>
        /// </summary>
        public required string ObjectSetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional RFC 7386 JSON Merge Patch getter name:
        ///         <c>(string key) → JsonObject?</c> or <c>JsonNode?</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 RFC 7386 JSON 合并补丁读取方法名：
        ///         <c>(string key) → JsonObject?</c> 或 <c>JsonNode?</c>。
        ///     </para>
        /// </summary>
        public string? MergePatchGetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional RFC 7386 JSON Merge Patch application method name:
        ///         <c>(string key, JsonNode? patch) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 RFC 7386 JSON 合并补丁应用方法名：
        ///         <c>(string key, JsonNode? patch) → void</c>。
        ///     </para>
        /// </summary>
        public string? MergePatchApplyMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional RFC 6902 JSON Patch getter name:
        ///         <c>(string key) → JsonArray?</c> or another <c>JsonNode?</c> subtype.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 RFC 6902 JSON Patch 读取方法名：
        ///         <c>(string key) → JsonArray?</c> 或其他 <c>JsonNode?</c> 子类型。
        ///     </para>
        /// </summary>
        public string? JsonPatchGetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional RFC 6902 JSON Patch application method name:
        ///         <c>(string key, JsonArray patch) → void</c> or
        ///         <c>(string key, JsonNode? patch) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 RFC 6902 JSON Patch 应用方法名：
        ///         <c>(string key, JsonArray patch) → void</c> 或
        ///         <c>(string key, JsonNode? patch) → void</c>。
        ///     </para>
        /// </summary>
        public string? JsonPatchApplyMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional JSON Pointer node-read method name:
        ///         <c>(string key, string jsonPointer) → JsonNode?</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 JSON Pointer 节点读取方法名：
        ///         <c>(string key, string jsonPointer) → JsonNode?</c>。
        ///     </para>
        /// </summary>
        public string? NodeGetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional JSON Pointer node-write method name:
        ///         <c>(string key, string jsonPointer, JsonNode? node) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的 JSON Pointer 节点写入方法名：
        ///         <c>(string key, string jsonPointer, JsonNode? node) → void</c>。
        ///     </para>
        /// </summary>
        public string? NodeSetMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional method name for merging a <see cref="System.Text.Json.Nodes.JsonObject" /> at a pointer:
        ///         <c>(string key, string jsonPointer, JsonObject value) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的指定位置对象合并方法名：
        ///         <c>(string key, string jsonPointer, JsonObject value) → void</c>。
        ///     </para>
        /// </summary>
        public string? ObjectMergeAtMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional complete-document JSON text getter name: <c>(string key) → string?</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的完整文档 JSON 文本读取方法名：<c>(string key) → string?</c>。
        ///     </para>
        /// </summary>
        public string? TypedGetJsonMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional complete-document JSON text setter name:
        ///         <c>(string key, string json) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的完整文档 JSON 文本写入方法名：
        ///         <c>(string key, string json) → void</c>。
        ///     </para>
        /// </summary>
        public string? TypedSetJsonMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional root-object getter name:
        ///         <c>(string key) → JsonObject?</c>, or <c>(string key) → JsonNode?</c> whose returned value must
        ///         be a <c>JsonObject</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的根对象读取方法名：
        ///         <c>(string key) → JsonObject?</c>，或返回值必须是 <c>JsonObject</c> 的
        ///         <c>(string key) → JsonNode?</c>。
        ///     </para>
        /// </summary>
        public string? TypedGetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Optional root-object setter name:
        ///         <c>(string key, JsonObject root) → void</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的根对象写入方法名：
        ///         <c>(string key, JsonObject root) → void</c>。
        ///     </para>
        /// </summary>
        public string? TypedSetJsonObjectMethodName { get; init; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Convention used by <c>CreateRitsuLibModDataSchema</c> and ModData runtime interop providers.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         <c>CreateRitsuLibModDataSchema</c> 和 ModData 运行时互操作提供方使用的约定。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Object-access convention for settings runtime interop. Optional JSON document method names remain
        ///         unset for compatibility; typed Boolean, integer, floating-point, and string accessors are handled
        ///         by the settings runtime mirror.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         设置运行时互操作使用的对象访问约定。为保持兼容，可选 JSON 文档方法名均未设置；
        ///         布尔值、整数、浮点数和字符串的类型化访问仍由设置运行时镜像处理。
        ///     </para>
        /// </summary>
        public static ReflectionInteropConvention SettingsRuntimeInterop { get; } = new()
        {
            ObjectGetMethodName = "GetRitsuLibSettingValue",
            ObjectSetMethodName = "SetRitsuLibSettingValue",
        };
    }
}
