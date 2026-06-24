using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Json;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Keyed JSON DOM synchronization between a <see cref="ReflectionStaticChannel" /> and an in-memory
    ///     <see cref="JsonObject" /> document root (ModData, RPC payloads, replicas, …).
    ///     <see cref="ReflectionStaticChannel" /> 与内存中的
    ///     <see cref="JsonObject" /> document root 之间的 keyed JSON DOM 同步（ModData、RPC 载荷、replica 等）。
    /// </summary>
    public static class KeyedJsonDomTransport
    {
        /// <summary>
        ///     Default serializer options aligned with ModData interop (compact JSON).
        ///     与 ModData interop 对齐的默认 serializer options（紧凑 JSON）。
        /// </summary>
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = false,
        };

        /// <summary>
        ///     Applies provider → document pull semantics and returns the updated root node.
        ///     应用提供方到文档的拉取语义，并返回更新后的根节点。
        /// </summary>
        /// <param name="key">
        ///     Interop key passed to provider static methods.
        ///     传给提供方静态方法的互操作键。
        /// </param>
        /// <param name="channel">
        ///     Bound reflection channel for the provider.
        ///     提供方的已绑定反射通道。
        /// </param>
        /// <param name="documentRoot">
        ///     In-memory document root to update.
        ///     要更新的内存 document root。
        /// </param>
        /// <param name="pathRouting">
        ///     Optional pull/push/merge pointer lists; required when using node getters with partial paths.
        ///     可选的拉取/推送/合并 pointer 列表；使用带部分路径的节点 getter 时必需。
        /// </param>
        /// <param name="jsonOptions">
        ///     Serializer options when falling back to object round-trip; defaults to
        ///     <see cref="DefaultJsonSerializerOptions" />.
        ///     <see cref="DefaultJsonSerializerOptions" />。
        ///     回退到 object round-trip 时使用的 serializer options；默认使用
        ///     <see cref="DefaultJsonSerializerOptions" />。
        ///     <see cref="DefaultJsonSerializerOptions" />。
        /// </param>
        public static JsonNode? PullFromProviderIntoRoot(
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
                if (documentRoot is not JsonObject docObj)
                    docObj = new();

                foreach (var rawPath in paths)
                {
                    var ptr = JsonPointer.Normalize(rawPath);
                    var n = json.GetNode(key, ptr);
                    if (n != null)
                        JsonPointer.Set(docObj, ptr, n);
                }

                return docObj;
            }

            if (json.GetJson != null) return JsonNode.Parse(json.GetJson(key) ?? "{}") ?? new JsonObject();

            var obj = channel.GetObject(key);
            var jsonText = obj == null ? "{}" : JsonSerializer.Serialize(obj, opts);
            return JsonNode.Parse(jsonText) ?? new JsonObject();
        }

        /// <summary>
        ///     Applies document → provider push semantics from <paramref name="documentRoot" />.
        ///     从 <paramref name="documentRoot" /> 应用文档到提供方的推送语义。
        /// </summary>
        /// <param name="key">
        ///     Interop key passed to provider static methods.
        ///     传给提供方静态方法的互操作键。
        /// </param>
        /// <param name="channel">
        ///     Bound reflection channel for the provider.
        ///     提供方的已绑定反射通道。
        /// </param>
        /// <param name="documentRoot">
        ///     In-memory document root to read from.
        ///     要读取的内存 document root。
        /// </param>
        /// <param name="pathRouting">
        ///     Optional pull/push/merge pointer lists; required when using node or merge-at setters with partial paths.
        ///     可选的拉取/推送/合并 pointer 列表；使用带部分路径的节点 setter 或 merge-at setter 时必需。
        /// </param>
        /// <param name="jsonOptions">
        ///     Serializer options when using the JSON text setter tier; defaults to
        ///     <see cref="DefaultJsonSerializerOptions" />.
        ///     <see cref="DefaultJsonSerializerOptions" />。
        ///     使用 JSON text setter tier 时的 serializer options；默认使用
        ///     <see cref="DefaultJsonSerializerOptions" />。
        ///     <see cref="DefaultJsonSerializerOptions" />。
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
                var clone = documentRoot.DeepClone() as JsonObject ?? new JsonObject();
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

            channel.SetObject(key, documentRoot);
        }
    }
}
