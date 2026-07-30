using System.Collections.ObjectModel;

namespace STS2RitsuLib.Audio
{
    /// <summary>
    ///     <para xml:lang="en">A thread-safe in-memory path pool that randomly avoids selecting the same entry index twice in succession.</para>
    ///     <para xml:lang="zh-CN">线程安全的内存路径池；随机选择时避免连续两次选中同一条目索引。</para>
    /// </summary>
    public sealed class FmodPathRoundRobinPool
    {
        private readonly List<string> _entries;
        private readonly Lock _gate = new();
        private readonly Random _rng = new();
        private int _lastIndex = -1;

        /// <summary>
        ///     <para xml:lang="en">Copies the supplied path sequence into a fixed internal list.</para>
        ///     <para xml:lang="zh-CN">将提供的路径序列复制到固定的内部列表。</para>
        /// </summary>
        /// <param name="paths">
        ///     <para xml:lang="en">The paths to copy; the sequence may be empty and is enumerated once.</para>
        ///     <para xml:lang="zh-CN">要复制的路径；序列可以为空，并且只枚举一次。</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when <paramref name="paths" /> is null.</para>
        ///     <para xml:lang="zh-CN">当 <paramref name="paths" /> 为 <see langword="null" /> 时抛出。</para>
        /// </exception>
        public FmodPathRoundRobinPool(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            _entries = [.. paths];
            Entries = new ReadOnlyCollection<string>(_entries);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the read-only snapshot of configured path entries.</para>
        ///     <para xml:lang="zh-CN">获取已配置路径条目的只读快照。</para>
        /// </summary>
        public IReadOnlyList<string> Entries { get; }

        /// <summary>
        ///     <para xml:lang="en">Tries to pick a random entry, excluding the previously selected index when at least two entries exist.</para>
        ///     <para xml:lang="zh-CN">尝试随机选择条目；存在至少两个条目时排除上次选中的索引。</para>
        /// </summary>
        /// <param name="path">
        ///     <para xml:lang="en">Receives the selected path, or an empty string when the pool is empty.</para>
        ///     <para xml:lang="zh-CN">接收选中的路径；池为空时接收空字符串。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en"><see langword="true" /> when an entry is selected; otherwise <see langword="false" />.</para>
        ///     <para xml:lang="zh-CN">选中条目时为 <see langword="true" />；否则为 <see langword="false" />。</para>
        /// </returns>
        public bool TryPickNext(out string path)
        {
            lock (_gate)
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
}
