using System.Collections;
using MegaCrit.Sts2.Core.Random;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Optional contract for items that supply their own selection weight.</para>
    ///     <para xml:lang="zh-CN">供自行提供选择权重的项使用的可选契约。</para>
    /// </summary>
    public interface IWeightedValue
    {
        /// <summary>
        ///     <para xml:lang="en">Positive relative weight used by <c>WeightedList&lt;T&gt;</c> when no explicit weight is provided.</para>
        ///     <para xml:lang="zh-CN">未提供显式权重时，<c>WeightedList&lt;T&gt;</c> 使用的正相对权重。</para>
        /// </summary>
        int Weight { get; }
    }

    /// <summary>
    ///     <para xml:lang="en">Weighted random container with optional draw-without-replacement support.</para>
    ///     <para xml:lang="zh-CN">支持可选不放回抽取的加权随机容器。</para>
    /// </summary>
    public class WeightedList<T> : IList<T>
    {
        private readonly List<Entry> _entries = [];

        /// <summary>
        ///     <para xml:lang="en">Sum of all entry weights, or zero when empty.</para>
        ///     <para xml:lang="zh-CN">所有条目权重之和；为空时为零。</para>
        /// </summary>
        public int TotalWeight { get; private set; }

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public int Count => _entries.Count;

        /// <inheritdoc />
        public T this[int index]
        {
            get => _entries[index].Value;
            set => _entries[index] = new(value, _entries[index].Weight);
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            Add(item, item is IWeightedValue weighted ? weighted.Weight : 1);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _entries.Clear();
            TotalWeight = 0;
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return _entries.Any(entry => EqualityComparer<T>.Default.Equals(entry.Value, item));
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            _entries.Select(entry => entry.Value).ToList().CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _entries.Select(entry => entry.Value).GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            for (var i = 0; i < _entries.Count; i++)
                if (EqualityComparer<T>.Default.Equals(_entries[i].Value, item))
                    return i;

            return -1;
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            Insert(index, item, item is IWeightedValue weighted ? weighted.Weight : 1);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            var entry = _entries[index];
            _entries.RemoveAt(index);
            TotalWeight -= entry.Weight;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     <para xml:lang="en">Appends <paramref name="item" /> with an explicit positive <paramref name="weight" />.</para>
        ///     <para xml:lang="zh-CN">以显式的正权重 <paramref name="weight" /> 追加 <paramref name="item" />。</para>
        /// </summary>
        /// <exception cref="OverflowException">
        ///     <para xml:lang="en">The resulting total weight exceeds <see cref="int.MaxValue" />.</para>
        ///     <para xml:lang="zh-CN">相加后的总权重超过 <see cref="int.MaxValue" />。</para>
        /// </exception>
        public void Add(T item, int weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
            var nextTotalWeight = checked(TotalWeight + weight);
            _entries.Add(new(item, weight));
            TotalWeight = nextTotalWeight;
        }

        /// <summary>
        ///     <para xml:lang="en">Appends each item using <paramref name="weightSelector" />, <see cref="IWeightedValue" />, or weight 1.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="weightSelector" />、<see cref="IWeightedValue" /> 或权重 1 追加每一项。</para>
        /// </summary>
        public void AddRange(IEnumerable<T> items, Func<T, int>? weightSelector = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
                Add(item, weightSelector?.Invoke(item) ?? (item is IWeightedValue weighted ? weighted.Weight : 1));
        }

        /// <summary>
        ///     <para xml:lang="en">Rolls a weighted random entry using <paramref name="rng" />, optionally removing the chosen entry.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="rng" /> 抽取加权随机条目，并可移除选中的条目。</para>
        /// </summary>
        public T GetRandom(Rng rng, bool remove = false)
        {
            ArgumentNullException.ThrowIfNull(rng);

            if (_entries.Count == 0 || TotalWeight <= 0)
                throw new InvalidOperationException("Cannot roll from an empty weighted list.");

            var roll = rng.NextInt(TotalWeight);
            var cumulative = 0;

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                cumulative += entry.Weight;
                if (roll >= cumulative)
                    continue;

                if (remove)
                    RemoveAt(i);

                return entry.Value;
            }

            throw new InvalidOperationException($"Weighted roll {roll} exceeded total weight {TotalWeight}.");
        }

        /// <summary>
        ///     <para xml:lang="en">Returns <see langword="false" /> when the list is empty or has non-positive total weight; otherwise performs a weighted roll like <c>GetRandom</c>.</para>
        ///     <para xml:lang="zh-CN">列表为空或总权重非正时返回 <see langword="false" />；否则执行一次类似 <c>GetRandom</c> 的加权抽取。</para>
        /// </summary>
        public bool TryGetRandom(Rng rng, out T value, bool remove = false)
        {
            if (_entries.Count == 0 || TotalWeight <= 0)
            {
                value = default!;
                return false;
            }

            value = GetRandom(rng, remove);
            return true;
        }

        /// <summary>
        ///     <para xml:lang="en">Inserts <paramref name="item" /> at <paramref name="index" /> with explicit positive <paramref name="weight" />.</para>
        ///     <para xml:lang="zh-CN">以显式的正权重 <paramref name="weight" /> 将 <paramref name="item" /> 插入到 <paramref name="index" />。</para>
        /// </summary>
        /// <exception cref="OverflowException">
        ///     <para xml:lang="en">The resulting total weight exceeds <see cref="int.MaxValue" />.</para>
        ///     <para xml:lang="zh-CN">相加后的总权重超过 <see cref="int.MaxValue" />。</para>
        /// </exception>
        public void Insert(int index, T item, int weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
            if ((uint)index > (uint)_entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var nextTotalWeight = checked(TotalWeight + weight);
            _entries.Insert(index, new(item, weight));
            TotalWeight = nextTotalWeight;
        }

        private readonly record struct Entry(T Value, int Weight);
    }
}
