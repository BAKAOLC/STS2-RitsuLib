using System.Diagnostics.CodeAnalysis;
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
        ///     Determines whether an entry exists for <paramref name="key" /> (without creating one).
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryGetValue(key, out _);
        }

        /// <summary>
        ///     Adds an entry for <paramref name="key" /> if absent.
        /// </summary>
        /// <returns>True if the entry was added; false if <paramref name="key" /> already had a value.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryAdd(key, new(value));
        }

        /// <summary>
        ///     Adds an entry for <paramref name="key" />.
        /// </summary>
        /// <exception cref="ArgumentException">An entry for <paramref name="key" /> already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!_table.TryAdd(key, new(value)))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or adds <paramref name="value" /> and returns it.
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, _ => new(value)).Value;
        }

        /// <summary>
        ///     Returns the existing value for <paramref name="key" /> or creates one with <paramref name="valueFactory" />.
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(valueFactory);
            return _table.GetValue(key, k => new(valueFactory(k))).Value;
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
        ///     Returns the value for <paramref name="key" /> if present; otherwise <c>default(TValue)</c>.
        /// </summary>
        public TValue? GetValueOrDefault(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>
        ///     Returns the value for <paramref name="key" /> if present; otherwise <paramref name="defaultValue" />.
        /// </summary>
        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     Attempts to read the attached value without creating it.
        /// </summary>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
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
            return TryRemove(key, out _);
        }

        /// <summary>
        ///     Removes the value attached to <paramref name="key" /> if present.
        /// </summary>
        /// <returns>True if an entry was removed.</returns>
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_table.TryGetValue(key, out var box))
            {
                value = default!;
                return false;
            }

            var extracted = box.Value;
            if (!_table.Remove(key))
            {
                value = default!;
                return false;
            }

            value = extracted;
            return true;
        }

        /// <summary>
        ///     Removes all entries from the table (does not affect live <typeparamref name="TKey" /> instances).
        /// </summary>
        public void Clear()
        {
            _table.Clear();
        }

        private sealed class Box(TValue value)
        {
            public TValue Value { get; } = value;
        }
    }
}
