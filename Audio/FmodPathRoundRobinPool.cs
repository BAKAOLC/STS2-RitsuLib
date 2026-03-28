namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     In-memory pool of event or file paths with simple no-repeat random selection.
    /// </summary>
    public sealed class FmodPathRoundRobinPool
    {
        private readonly List<string> _entries;
        private readonly Random _rng = new();
        private int _lastIndex = -1;

        /// <summary>
        ///     Copies <paramref name="paths" /> into an internal list (may be empty).
        /// </summary>
        public FmodPathRoundRobinPool(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            _entries = [.. paths];
        }

        /// <summary>
        ///     Snapshot of configured paths.
        /// </summary>
        public IReadOnlyList<string> Entries => _entries;

        /// <summary>
        ///     Picks a random path, avoiding the same index as the previous pick when more than one entry exists.
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
