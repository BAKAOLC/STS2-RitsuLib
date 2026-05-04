using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using STS2RitsuLib.Utils.Persistence.Interop;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Keyed JSON DOM synchronization between a <see cref="ReflectionStaticChannel" /> and an in-memory
    ///     <see cref="JsonObject" /> document root (ModData, RPC payloads, replicas, …).
    /// </summary>
    public static class KeyedJsonDomTransport
    {
        /// <summary>
        ///     Default serializer options aligned with ModData interop (compact JSON).
        /// </summary>
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = false,
        };

        /// <summary>
        ///     Applies provider → document pull semantics into <paramref name="documentRoot" />.
        /// </summary>
        /// <param name="key">Interop key passed to provider static methods.</param>
        /// <param name="channel">Bound reflection channel for the provider.</param>
        /// <param name="documentRoot">In-memory document root to mutate in place.</param>
        /// <param name="pathRouting">
        ///     Optional pull/push/merge pointer lists; required when using node getters with partial paths.
        /// </param>
        /// <param name="jsonOptions">Serializer options when falling back to object round-trip; defaults to
        ///     <see cref="DefaultJsonSerializerOptions" />.</param>
        public static void PullFromProviderIntoRoot(
            string key,
            ReflectionStaticChannel channel,
            JsonObject documentRoot,
            KeyedJsonPathRouting? pathRouting,
            JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(documentRoot);

            var opts = jsonOptions ?? DefaultJsonSerializerOptions;
            var json = channel.Json;

            if (json.GetMergePatch != null)
            {
                var patch = json.GetMergePatch(key);
                if (patch != null)
                    ModDataJsonInteropPrimitives.MergePatch7386(documentRoot,
                        patch.DeepClone() as JsonObject ?? new JsonObject());

                return;
            }

            if (json.GetRootObject != null)
            {
                var incoming = json.GetRootObject(key) ?? new JsonObject();
                documentRoot.Clear();
                foreach (var p in incoming)
                    documentRoot[p.Key] = p.Value?.DeepClone();

                return;
            }

            if (json.GetNode != null && pathRouting?.PullPaths is { Length: > 0 } paths)
            {
                foreach (var rawPath in paths)
                {
                    var ptr = NormalizeJsonPointer(rawPath);
                    var n = json.GetNode(key, ptr);
                    if (n != null)
                        ModDataJsonInteropPrimitives.SetNodeAt(documentRoot, ptr, n);
                }

                return;
            }

            if (json.GetJson != null)
            {
                var incoming = ParseIncomingJsonObject(json.GetJson(key) ?? "{}");
                documentRoot.Clear();
                foreach (var p in incoming)
                    documentRoot[p.Key] = p.Value?.DeepClone();

                return;
            }

            var obj = channel.GetObject(key);
            var jsonText = obj == null ? "{}" : JsonSerializer.Serialize(obj, opts);
            var incomingRoot = ParseIncomingJsonObject(jsonText);
            documentRoot.Clear();
            foreach (var p in incomingRoot)
                documentRoot[p.Key] = p.Value?.DeepClone();
        }

        /// <summary>
        ///     Applies document → provider push semantics from <paramref name="documentRoot" />.
        /// </summary>
        /// <param name="key">Interop key passed to provider static methods.</param>
        /// <param name="channel">Bound reflection channel for the provider.</param>
        /// <param name="documentRoot">In-memory document root to read from.</param>
        /// <param name="pathRouting">
        ///     Optional pull/push/merge pointer lists; required when using node or merge-at setters with partial paths.
        /// </param>
        /// <param name="jsonOptions">Serializer options when using the JSON text setter tier; defaults to
        ///     <see cref="DefaultJsonSerializerOptions" />.</param>
        public static void PushRootToProvider(
            string key,
            ReflectionStaticChannel channel,
            JsonObject documentRoot,
            KeyedJsonPathRouting? pathRouting,
            JsonSerializerOptions? jsonOptions = null)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(documentRoot);

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
                var clone = documentRoot.DeepClone() as JsonObject ?? new JsonObject();
                json.ApplyMergePatch(key, clone);
                return;
            }

            if (json.SetNode != null && pathRouting?.PushPaths is { Length: > 0 } pushPaths)
            {
                foreach (var rawPath in pushPaths)
                {
                    var ptr = NormalizeJsonPointer(rawPath);
                    var n = ModDataJsonInteropPrimitives.GetNodeAt(documentRoot, ptr);
                    json.SetNode(key, ptr, n?.DeepClone());
                }

                return;
            }

            if (json.MergeObjectAt != null && pathRouting?.MergePushPaths is { Length: > 0 } mergePaths)
            {
                foreach (var rawPath in mergePaths)
                {
                    var ptr = NormalizeJsonPointer(rawPath);
                    if (ModDataJsonInteropPrimitives.GetNodeAt(documentRoot, ptr) is JsonObject sub)
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

        /// <summary>
        ///     Normalizes a JSON Pointer fragment for DOM navigation (leading slash optional when authoring).
        /// </summary>
        /// <param name="rawPath">Pointer from schema or config, with or without a leading slash.</param>
        /// <returns>A normalized pointer beginning with <c>/</c>.</returns>
        public static string NormalizeJsonPointer(string rawPath)
        {
            var t = rawPath.Trim();
            if (t.Length == 0 || t == "/")
                return "/";

            return t.StartsWith('/') ? t : "/" + t;
        }

        private static JsonObject ParseIncomingJsonObject(string jsonText)
        {
            try
            {
                var node = JsonNode.Parse(jsonText);
                return node as JsonObject ?? new JsonObject();
            }
            catch
            {
                return new();
            }
        }
    }
}
