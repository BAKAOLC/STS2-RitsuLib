namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     In-memory pool of event or file paths with simple no-repeat random selection.
    ///     事件或文件路径的内存池，带简单的不重复随机选择。
    /// </summary>
    public sealed class FmodPathRoundRobinPool
    {
        private readonly List<string> _entries;
        private readonly Random _rng = new();
        private int _lastIndex = -1;

        /// <summary>
        ///     Copies <paramref name="paths" /> into an internal list (may be empty).
        ///     将 <paramref name="paths" /> 复制到内部列表（可为空）。
        /// </summary>
        public FmodPathRoundRobinPool(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            _entries = [.. paths];
        }

        /// <summary>
        ///     Snapshot of configured paths.
        ///     已配置路径的快照。
        /// </summary>
        public IReadOnlyList<string> Entries => _entries;

        /// <summary>
        ///     Picks a random path, avoiding the same index as the previous pick when more than one entry exists.
        ///     随机选取路径；存在多个条目时避免与上次选取相同索引。
        /// </summary>
        public bool TryPickNext(out string path)
        {
            path = "";
            switch (_entries.Count)
            {
                case 0:
                    return false;
                case 1:
                    path = _entries[0];
                    return true;
            }

            int index;
            do
            {
                index = _rng.Next(_entries.Count);
            } while (index == _lastIndex);

            _lastIndex = index;
            path = _entries[index];
            return true;
        }
    }
}
