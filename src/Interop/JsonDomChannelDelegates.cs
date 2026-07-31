using System.Text.Json.Nodes;

namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional reflection-bound operations for keyed JSON documents, including merge patch, JSON Patch,
    ///         JSON Pointer, full-text, and root-object access.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         键控 JSON 文档的可选反射绑定操作，包括合并补丁、JSON Patch、JSON Pointer、完整文本和根对象访问。
    ///     </para>
    /// </summary>
    /// <param name="GetMergePatch">
    ///     <para xml:lang="en">
    ///         Gets an <see href="https://www.rfc-editor.org/rfc/rfc7386">RFC 7386 JSON Merge Patch</see>
    ///         for a key, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         获取指定键的 <see href="https://www.rfc-editor.org/rfc/rfc7386">RFC 7386 JSON 合并补丁</see>；
    ///         未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="GetRootObject">
    ///     <para xml:lang="en">
    ///         Gets the complete document root as a <see cref="JsonObject" />, or is
    ///         <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         以 <see cref="JsonObject" /> 获取完整文档根节点；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="GetNode">
    ///     <para xml:lang="en">
    ///         Gets a subtree selected by JSON Pointer, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         获取 JSON Pointer 选定的子树；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="ApplyMergePatch">
    ///     <para xml:lang="en">
    ///         Applies an RFC 7386 JSON Merge Patch to a key, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 RFC 7386 JSON 合并补丁应用到指定键；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="GetJsonPatch">
    ///     <para xml:lang="en">
    ///         Gets an <see href="https://www.rfc-editor.org/rfc/rfc6902">RFC 6902 JSON Patch</see>
    ///         document for a key, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         获取指定键的 <see href="https://www.rfc-editor.org/rfc/rfc6902">RFC 6902 JSON Patch</see>
    ///         文档；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="SetRootObject">
    ///     <para xml:lang="en">
    ///         Replaces the root object for a key, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         替换指定键的根对象；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="SetNode">
    ///     <para xml:lang="en">
    ///         Writes a node at a JSON Pointer, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 JSON Pointer 指定位置写入节点；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="MergeObjectAt">
    ///     <para xml:lang="en">
    ///         Merges an object at a JSON Pointer, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         在 JSON Pointer 指定位置合并对象；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="GetJson">
    ///     <para xml:lang="en">
    ///         Gets the complete document as JSON text, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         以 JSON 文本获取完整文档；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="SetJson">
    ///     <para xml:lang="en">
    ///         Replaces the complete document from JSON text, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         从 JSON 文本替换完整文档；未绑定时为 <see langword="null" />。
    ///     </para>
    /// </param>
    /// <param name="ApplyJsonPatch">
    ///     <para xml:lang="en">
    ///         Applies an RFC 6902 JSON Patch document to a key, or is <see langword="null" /> when unbound.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 RFC 6902 JSON Patch 文档应用到指定键；未绑定时为 <see langword="null" />。
    ///     </para>
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
