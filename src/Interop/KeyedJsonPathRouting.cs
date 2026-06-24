namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Optional JSON Pointer lists used by <see cref="KeyedJsonDomTransport" /> for subtree sync.
    ///     <see cref="KeyedJsonDomTransport" /> 用于 subtree sync 的可选 JSON Pointer 列表。
    /// </summary>
    /// <param name="PullPaths">
    ///     Pointers consumed when pulling from a keyed provider via node getters.
    ///     通过节点 getter 从 keyed 提供方拉取时使用的 pointer。
    /// </param>
    /// <param name="PushPaths">
    ///     Pointers consumed when pushing document subtrees via node setters.
    ///     通过节点 setter 推送文档子树时使用的 pointer。
    /// </param>
    /// <param name="MergePushPaths">
    ///     Pointers consumed when pushing RFC 7386 merge payloads via merge-at hooks.
    ///     通过 merge-at hook 推送 RFC 7386 合并载荷时使用的 pointer。
    /// </param>
    public sealed record KeyedJsonPathRouting(string[]? PullPaths, string[]? PushPaths, string[]? MergePushPaths);
}
