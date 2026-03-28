using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     Stores mod-attached state on arbitrary reference objects without subclassing or boxing through object APIs.
    /// </summary>
    /// <param name="valueFactory">
    ///     Optional per-key factory; when null, lazily created values use <c>default(TValue)</c>.
    /// </param>
    public sealed class AttachedState<TKey, TValue>(Func<TKey, TValue>? valueFactory)
        where TKey : class
    {
        private readonly ConditionalWeakTable<TKey, Box> _table = [];
        private readonly Func<TKey, TValue> _valueFactory = valueFactory ?? (_ => default!);

        /// <summary>
        ///     Creates state storage using an optional parameterless factory for default values.
        /// </summary>
        public AttachedState(Func<TValue>? defaultValueFactory = null)
            : this(_ => defaultValueFactory != null ? defaultValueFactory() : default!)
        {
        }

        /// <summary>
        ///     Gets or sets the attached value for <paramref name="key" />.
        /// </summary>
        public TValue this[TKey key]
        {
            get => GetOrCreate(key);
            set => Set(key, value);
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or creates and stores one.
        /// </summary>
        public TValue GetOrCreate(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, k => new(_valueFactory(k))).Value;
        }

        /// <summary>
        ///     Attempts to read the attached value without creating it.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (_table.TryGetValue(key, out var box))
            {
                value = box.Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        ///     Replaces the stored value for <paramref name="key" /> and returns <paramref name="value" />.
        /// </summary>
        public TValue Set(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            _table.Remove(key);
            _table.Add(key, new(value));
            return value;
        }

        /// <summary>
        ///     Mutates the stored value in place using <paramref name="updater" />.
        /// </summary>
        public TValue Update(TKey key, Func<TValue, TValue> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);
            var updated = updater(GetOrCreate(key));
            return Set(key, updated);
        }

        /// <summary>
        ///     Removes any value attached to <paramref name="key" />.
        /// </summary>
        /// <returns>True if an entry was removed.</returns>
        public bool Remove(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.Remove(key);
        }

        private sealed class Box(TValue value)
        {
            public TValue Value { get; } = value;
        }
    }
}
