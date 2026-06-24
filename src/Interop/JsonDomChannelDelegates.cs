using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Optional reflection-bound delegates for JSON DOM tiers (merge, pointer, UTF-16 text, root object).
    ///     JSON DOM tier 使用的可选反射绑定 delegate（merge、pointer、UTF-16 text、root object）。
    /// </summary>
    /// <param name="GetMergePatch">
    ///     RFC 7386 merge-patch for a key, or <c>null</c> when unbound.
    ///     https://www.rfc-editor.org/rfc/rfc7386
    ///     某个 key 的 RFC 7386 merge-patch；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="GetRootObject">
    ///     Full document root as <see cref="JsonObject" />, or <c>null</c> when unbound.
    ///     作为 <see cref="JsonObject" /> 的完整 document root；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="GetNode">
    ///     Sub-tree read by JSON Pointer, or <c>null</c> when unbound.
    ///     通过 JSON Pointer 读取的 sub-tree；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="ApplyMergePatch">
    ///     Apply an RFC 7386 merge patch to a key, or <c>null</c> when unbound.
    ///     对某个 key 应用 RFC 7386 merge patch；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="GetJsonPatch">
    ///     JSON Patch (RFC 6902) document for a key, or <c>null</c> when unbound.
    ///     https://www.rfc-editor.org/rfc/rfc6902
    ///     某个 key 的 JSON Patch（RFC 6902）文档；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="SetRootObject">
    ///     Replace root object for a key, or <c>null</c> when unbound.
    ///     替换某个 key 的根对象；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="SetNode">
    ///     Write a node at a JSON Pointer, or <c>null</c> when unbound.
    ///     在 JSON Pointer 位置写入节点；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="MergeObjectAt">
    ///     Merge an object at a JSON Pointer, or <c>null</c> when unbound.
    ///     在 JSON Pointer 位置 merge object；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="GetJson">
    ///     Whole document as JSON text, or <c>null</c> when unbound.
    ///     作为 JSON 文本的完整 document；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="SetJson">
    ///     Write whole document from JSON text, or <c>null</c> when unbound.
    ///     从 JSON 文本写入完整 document；未绑定时为 <c>null</c>。
    /// </param>
    /// <param name="ApplyJsonPatch">
    ///     Apply a JSON Patch (RFC 6902) document to a key, or <c>null</c> when unbound.
    ///     对某个 key 应用 JSON Patch（RFC 6902）文档；未绑定时为 <c>null</c>。
    /// </param>
    public sealed record JsonDomChannelDelegates(
        Func<string, JsonNode?>? GetMergePatch,
        Func<string, JsonObject?>? GetRootObject,
        Func<string, string, JsonNode?>? GetNode,
        Action<string, JsonNode?>? ApplyMergePatch,
        Func<string, JsonNode?>? GetJsonPatch,
        Action<string, JsonObject>? SetRootObject,
        Action<string, string, JsonNode?>? SetNode,
        Action<string, string, JsonObject>? MergeObjectAt,
        Func<string, string?>? GetJson,
        Action<string, string>? SetJson,
        Action<string, JsonNode?>? ApplyJsonPatch);
}
