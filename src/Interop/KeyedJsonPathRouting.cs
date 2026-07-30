namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Optional JSON Pointer lists used by <see cref="KeyedJsonDomTransport" /> for subtree synchronization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         <see cref="KeyedJsonDomTransport" /> 用于同步子树的可选 JSON Pointer 列表。
    ///     </para>
    /// </summary>
    /// <param name="PullPaths">
    ///     <para xml:lang="en">Pointers read from the keyed provider through node getters.</para>
    ///     <para xml:lang="zh-CN">通过节点读取器从键控提供方拉取数据时使用的指针。</para>
    /// </param>
    /// <param name="PushPaths">
    ///     <para xml:lang="en">Pointers written through node setters when pushing document subtrees.</para>
    ///     <para xml:lang="zh-CN">通过节点写入器推送文档子树时使用的指针。</para>
    /// </param>
    /// <param name="MergePushPaths">
    ///     <para xml:lang="en">
    ///         Pointers written through merge-at hooks when pushing RFC 7386 merge payloads.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过指定位置合并钩子推送 RFC 7386 合并载荷时使用的指针。
    ///     </para>
    /// </param>
    public sealed record KeyedJsonPathRouting(string[]? PullPaths, string[]? PushPaths, string[]? MergePushPaths);
}
