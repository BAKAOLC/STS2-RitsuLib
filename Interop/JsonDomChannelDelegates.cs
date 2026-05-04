using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Optional reflection-bound delegates for JSON DOM tiers (merge, pointer, UTF-16 text, root object).
    /// </summary>
    /// <param name="GetMergePatch">Merge-patch document for a key, or <c>null</c> when unbound.</param>
    /// <param name="GetRootObject">Full document root as <see cref="JsonObject" />, or <c>null</c> when unbound.</param>
    /// <param name="GetNode">Sub-tree read by JSON Pointer, or <c>null</c> when unbound.</param>
    /// <param name="ApplyMergePatch">Apply merge patch to a key, or <c>null</c> when unbound.</param>
    /// <param name="SetRootObject">Replace root object for a key, or <c>null</c> when unbound.</param>
    /// <param name="SetNode">Write a node at a JSON Pointer, or <c>null</c> when unbound.</param>
    /// <param name="MergeObjectAt">Merge an object at a JSON Pointer, or <c>null</c> when unbound.</param>
    /// <param name="GetJson">Whole document as JSON text, or <c>null</c> when unbound.</param>
    /// <param name="SetJson">Write whole document from JSON text, or <c>null</c> when unbound.</param>
    public sealed record JsonDomChannelDelegates(
        Func<string, JsonObject?>? GetMergePatch,
        Func<string, JsonObject?>? GetRootObject,
        Func<string, string, JsonNode?>? GetNode,
        Action<string, JsonObject>? ApplyMergePatch,
        Action<string, JsonObject>? SetRootObject,
        Action<string, string, JsonNode?>? SetNode,
        Action<string, string, JsonObject>? MergeObjectAt,
        Func<string, string?>? GetJson,
        Action<string, string>? SetJson);
}
