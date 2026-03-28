using System.Collections;
using MegaCrit.Sts2.Core.Random;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Optional contract for items that supply their own selection weight.
    /// </summary>
    public interface IWeightedValue
    {
        /// <summary>
        ///     Relative weight used by <c>WeightedList&lt;T&gt;</c> when no explicit weight is provided.
        /// </summary>
        int Weight { get; }
    }

    /// <summary>
    ///     Weighted random container with optional draw-without-replacement support.
    /// </summary>
    public class WeightedList<T> : IList<T>
    {
        private readonly List<Entry> _entries = [];

        /// <summary>
        ///     Sum of all entry weights (zero when empty).
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
        ///     Appends <paramref name="item" /> with an explicit positive <paramref name="weight" />.
        /// </summary>
        public void Add(T item, int weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
            _entries.Add(new(item, weight));
            TotalWeight += weight;
        }

        /// <summary>
        ///     Appends each item using <paramref name="weightSelector" />, <see cref="IWeightedValue" />, or weight 1.
        /// </summary>
        public void AddRange(IEnumerable<T> items, Func<T, int>? weightSelector = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
                Add(item, weightSelector?.Invoke(item) ?? (item is IWeightedValue weighted ? weighted.Weight : 1));
        }

        /// <summary>
        ///     Rolls a weighted random entry using <paramref name="rng" />, optionally removing the chosen row.
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
        ///     Returns false when the list is empty or has non-positive total weight; otherwise performs a weighted
        ///     roll like <c>GetRandom</c>.
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
        ///     Inserts <paramref name="item" /> at <paramref name="index" /> with explicit positive
        ///     <paramref name="weight" />.
        /// </summary>
        public void Insert(int index, T item, int weight)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
            _entries.Insert(index, new(item, weight));
            TotalWeight += weight;
        }

        private readonly record struct Entry(T Value, int Weight);
    }
}
