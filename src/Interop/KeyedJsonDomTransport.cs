using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Json;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Synchronizes keyed JSON documents between a <see cref="ReflectionStaticChannel" /> and an in-memory
    ///         JSON tree, for uses such as ModData, RPC payloads, and replicas.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 <see cref="ReflectionStaticChannel" /> 与内存 JSON 树之间同步键控文档，可用于 ModData、
    ///         RPC 载荷和副本等场景。
    ///     </para>
    /// </summary>
    public static class KeyedJsonDomTransport
    {
        /// <summary>
        ///     <para xml:lang="en">Default compact serializer options aligned with ModData interop.</para>
        ///     <para xml:lang="zh-CN">与 ModData 互操作保持一致的默认紧凑序列化选项。</para>
        /// </summary>
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = false,
        };

        /// <summary>
        ///     <para xml:lang="en">
        ///         Pulls provider data into a cloned document tree and returns the resulting root node.
        ///     </para>
        ///     <para xml:lang="zh-CN">将提供方数据拉取到文档树副本中，并返回所得根节点。</para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">Key passed to the provider's static methods.</para>
        ///     <para xml:lang="zh-CN">传给提供方静态方法的键。</para>
        /// </param>
        /// <param name="channel">
        ///     <para xml:lang="en">Bound reflection channel for the provider.</para>
        ///     <para xml:lang="zh-CN">已为提供方绑定的反射通道。</para>
        /// </param>
        /// <param name="documentRoot">
        ///     <para xml:lang="en">
        ///         Existing in-memory root to clone before applying provider data, or <see langword="null" />
        ///         to start with an empty object.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         应用提供方数据前要复制的现有内存根节点；为 <see langword="null" /> 时从空对象开始。
        ///     </para>
        /// </param>
        /// <param name="pathRouting">
        ///     <para xml:lang="en">
        ///         Optional path routing. <see cref="KeyedJsonPathRouting.PullPaths" /> is required to use
        ///         a bound node getter.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的路径路由；使用已绑定节点读取器时必须提供
        ///         <see cref="KeyedJsonPathRouting.PullPaths" />。
        ///     </para>
        /// </param>
        /// <param name="jsonOptions">
        ///     <para xml:lang="en">
        ///         Serializer options used by the object fallback. Defaults to
        ///         <see cref="DefaultJsonSerializerOptions" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         对象回退通道使用的序列化选项；默认为 <see cref="DefaultJsonSerializerOptions" />。
        ///     </para>
        /// </param>
        public static JsonNode? PullFromProviderIntoRoot(
            string key,
            ReflectionStaticChannel channel,
            JsonNode? documentRoot,
            KeyedJsonPathRouting? pathRouting,
            JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            documentRoot = documentRoot?.DeepClone() ?? new JsonObject();

            var opts = jsonOptions ?? DefaultJsonSerializerOptions;
            var json = channel.Json;

            if (json.GetMergePatch != null)
            {
                var patch = json.GetMergePatch(key);
                return patch == null ? documentRoot : JsonMergePatch.Apply(documentRoot, patch);
            }

            if (json.GetJsonPatch != null)
            {
                var patch = json.GetJsonPatch(key);
                return patch == null ? documentRoot : JsonPatch.Apply(documentRoot, patch);
            }

            if (json.GetRootObject != null)
            {
                var incoming = json.GetRootObject(key) ?? new JsonObject();
                return incoming.DeepClone();
            }

            if (json.GetNode != null && pathRouting?.PullPaths is { Length: > 0 } paths)
            {
                foreach (var rawPath in paths)
                {
                    var ptr = JsonPointer.Normalize(rawPath);
                    var n = json.GetNode(key, ptr);
                    if (n == null)
                        continue;

                    if (JsonPointer.IsRoot(ptr))
                    {
                        documentRoot = n.DeepClone();
                        continue;
                    }

                    if (documentRoot is not JsonObject docObj)
                        throw new InvalidOperationException(
                            "JSON Pointer subtree pulls require an object document root for non-root paths.");

                    JsonPointer.Set(docObj, ptr, n);
                }

                return documentRoot;
            }

            if (json.GetJson != null) return JsonNode.Parse(json.GetJson(key) ?? "{}") ?? new JsonObject();

            var obj = channel.GetObject(key);
            var jsonText = obj == null ? "{}" : JsonSerializer.Serialize(obj, opts);
            return JsonNode.Parse(jsonText) ?? new JsonObject();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Pushes <paramref name="documentRoot" /> to the highest-priority operation bound by the provider.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过提供方已绑定操作中优先级最高的一项推送 <paramref name="documentRoot" />。
        ///     </para>
        /// </summary>
        /// <param name="key">
        ///     <para xml:lang="en">Key passed to the provider's static methods.</para>
        ///     <para xml:lang="zh-CN">传给提供方静态方法的键。</para>
        /// </param>
        /// <param name="channel">
        ///     <para xml:lang="en">Bound reflection channel for the provider.</para>
        ///     <para xml:lang="zh-CN">已为提供方绑定的反射通道。</para>
        /// </param>
        /// <param name="documentRoot">
        ///     <para xml:lang="en">
        ///         In-memory document root to push, or <see langword="null" /> for an empty object.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         要推送的内存文档根节点；为 <see langword="null" /> 时使用空对象。
        ///     </para>
        /// </param>
        /// <param name="pathRouting">
        ///     <para xml:lang="en">
        ///         Optional path routing required when using node setters or merge-at operations for selected subtrees.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         可选的路径路由；使用节点写入器或指定位置合并操作推送选定子树时必须提供。
        ///     </para>
        /// </param>
        /// <param name="jsonOptions">
        ///     <para xml:lang="en">
        ///         Serializer options used by the JSON-text operation. Defaults to
        ///         <see cref="DefaultJsonSerializerOptions" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         JSON 文本操作使用的序列化选项；默认为 <see cref="DefaultJsonSerializerOptions" />。
        ///     </para>
        /// </param>
        public static void PushRootToProvider(
            string key,
            ReflectionStaticChannel channel,
            JsonNode? documentRoot,
            KeyedJsonPathRouting? pathRouting,
            JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            documentRoot ??= new JsonObject();

            var opts = jsonOptions ?? DefaultJsonSerializerOptions;
            var json = channel.Json;

            if (json.SetRootObject != null)
            {
                var clone = documentRoot.DeepClone() as JsonObject
                            ?? throw new InvalidOperationException(
                                "The configured root JSON setter only accepts a JsonObject document root.");
                json.SetRootObject(key, clone);
                return;
            }

            if (json.ApplyMergePatch != null)
            {
                json.ApplyMergePatch(key, documentRoot.DeepClone());
                return;
            }

            if (json.ApplyJsonPatch != null)
            {
                var patch = new JsonArray
                {
                    new JsonObject
                    {
                        ["op"] = "replace",
                        ["path"] = "",
                        ["value"] = documentRoot.DeepClone(),
                    },
                };

                json.ApplyJsonPatch(key, patch);
                return;
            }

            if (json.SetNode != null && pathRouting?.PushPaths is { Length: > 0 } pushPaths)
            {
                if (documentRoot is not JsonObject docObj)
                    docObj = new();

                foreach (var rawPath in pushPaths)
                {
                    var ptr = JsonPointer.Normalize(rawPath);
                    var n = JsonPointer.Get(docObj, ptr);
                    json.SetNode(key, ptr, n?.DeepClone());
                }

                return;
            }

            if (json.MergeObjectAt != null && pathRouting?.MergePushPaths is { Length: > 0 } mergePaths)
            {
                if (documentRoot is not JsonObject docObj)
                    docObj = new();

                foreach (var rawPath in mergePaths)
                {
                    var ptr = JsonPointer.Normalize(rawPath);
                    if (JsonPointer.Get(docObj, ptr) is JsonObject sub)
                        json.MergeObjectAt(key, ptr, sub.DeepClone() as JsonObject ?? new JsonObject());
                }

                return;
            }

            if (json.SetJson != null)
            {
                json.SetJson(key, JsonSerializer.Serialize(documentRoot, opts));
                return;
            }

            channel.SetObject(key, documentRoot.DeepClone());
        }
    }
}
