namespace STS2RitsuLib.Interop
{
    /// <summary>
    ///     Optional JSON Pointer lists used by <see cref="KeyedJsonDomTransport" /> for subtree sync.
    /// </summary>
    /// <param name="PullPaths">Pointers consumed when pulling from a keyed provider via node getters.</param>
    /// <param name="PushPaths">Pointers consumed when pushing document subtrees via node setters.</param>
    /// <param name="MergePushPaths">Pointers consumed when pushing RFC 7386 merge payloads via merge-at hooks.</param>
    public sealed record KeyedJsonPathRouting(string[]? PullPaths, string[]? PushPaths, string[]? MergePushPaths);
}
