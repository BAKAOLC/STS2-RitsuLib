using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace STS2RitsuLib.Utils
{
    /// <summary>
    ///     <para xml:lang="en">Stores mod-attached state on arbitrary reference objects without subclassing or boxing through object APIs.</para>
    ///     <para xml:lang="zh-CN">在任意引用对象上存储模组附加状态，无需子类化，也无需通过 object API 装箱。</para>
    /// </summary>
    /// <param name="valueFactory">
    ///     <para xml:lang="en">Optional per-key factory; when null, lazily created values use <c>default(TValue)</c>.</para>
    ///     <para xml:lang="zh-CN">可选的按键工厂；为 null 时，惰性创建的值使用 <c>default(TValue)</c>。</para>
    /// </param>
    public sealed class AttachedState<TKey, TValue>(Func<TKey, TValue>? valueFactory)
        where TKey : class
    {
        private readonly ConditionalWeakTable<TKey, Box> _table = [];
        private readonly Func<TKey, TValue> _valueFactory = valueFactory ?? (_ => default!);

        /// <summary>
        ///     <para xml:lang="en">Creates state storage using an optional parameterless factory for default values.</para>
        ///     <para xml:lang="zh-CN">使用可选的无参工厂创建具有默认值的状态存储。</para>
        /// </summary>
        public AttachedState(Func<TValue>? defaultValueFactory = null)
            : this(_ => defaultValueFactory != null ? defaultValueFactory() : default!)
        {
        }

        /// <summary>
        ///     <para xml:lang="en">Gets or sets the attached value for <paramref name="key" />.</para>
        ///     <para xml:lang="zh-CN">获取或设置 <paramref name="key" /> 的附加值。</para>
        /// </summary>
        public TValue this[TKey key]
        {
            get => GetOrCreate(key);
            set => Set(key, value);
        }

        /// <summary>
        ///     <para xml:lang="en">Determines whether an entry exists for <paramref name="key" /> without creating one.</para>
        ///     <para xml:lang="zh-CN">确定 <paramref name="key" /> 是否存在条目，但不创建条目。</para>
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryGetValue(key, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds an entry for <paramref name="key" /> if absent.</para>
        ///     <para xml:lang="zh-CN">如果 <paramref name="key" /> 缺少条目，则添加条目。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">True if the entry was added; false if <paramref name="key" /> already had a value.</para>
        ///     <para xml:lang="zh-CN">已添加条目时为 true；<paramref name="key" /> 已有值时为 false。</para>
        /// </returns>
        public bool TryAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.TryAdd(key, new(value));
        }

        /// <summary>
        ///     <para xml:lang="en">Adds an entry for <paramref name="key" />.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="key" /> 添加条目。</para>
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     <para xml:lang="en">An entry for <paramref name="key" /> already exists.</para>
        ///     <para xml:lang="zh-CN"><paramref name="key" /> 的条目已存在。</para>
        /// </exception>
        public void Add(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!_table.TryAdd(key, new(value)))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the existing value for <paramref name="key" /> or adds <paramref name="value" /> and returns it.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="key" /> 的现有值；不存在时添加并返回 <paramref name="value" />。</para>
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, _ => new(value)).Value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the existing value for <paramref name="key" /> or creates one with <paramref name="valueFactory" />.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="key" /> 的现有值；不存在时使用 <paramref name="valueFactory" /> 创建一个。</para>
        /// </summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(valueFactory);
            return _table.GetValue(key, k => new(valueFactory(k))).Value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the existing value for <paramref name="key" /> or creates and stores one.</para>
        ///     <para xml:lang="zh-CN">返回 <paramref name="key" /> 的现有值；不存在时创建并存储一个。</para>
        /// </summary>
        public TValue GetOrCreate(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _table.GetValue(key, k => new(_valueFactory(k))).Value;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the value for <paramref name="key" /> if present; otherwise <c>default(TValue)</c>.</para>
        ///     <para xml:lang="zh-CN">存在时返回 <paramref name="key" /> 的值；否则返回 <c>default(TValue)</c>。</para>
        /// </summary>
        public TValue? GetValueOrDefault(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>
        ///     <para xml:lang="en">Returns the value for <paramref name="key" /> if present; otherwise <paramref name="defaultValue" />.</para>
        ///     <para xml:lang="zh-CN">存在时返回 <paramref name="key" /> 的值；否则返回 <paramref name="defaultValue" />。</para>
        /// </summary>
        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to read the attached value without creating it.</para>
        ///     <para xml:lang="zh-CN">尝试读取附加值，但不创建它。</para>
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
        ///     <para xml:lang="en">Stores <paramref name="value" /> for <paramref name="key" />, replacing any existing entry, and returns the value.</para>
        ///     <para xml:lang="zh-CN">为 <paramref name="key" /> 存储 <paramref name="value" />，替换任何现有条目，并返回该值。</para>
        /// </summary>
        public TValue Set(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            _table.Remove(key);
            _table.Add(key, new(value));
            return value;
        }

        /// <summary>
        ///     <para xml:lang="en">Updates the stored value using <paramref name="updater" />, creating the entry first when absent.</para>
        ///     <para xml:lang="zh-CN">使用 <paramref name="updater" /> 更新已存储值；条目不存在时会先创建。</para>
        /// </summary>
        public TValue Update(TKey key, Func<TValue, TValue> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);
            var updated = updater(GetOrCreate(key));
            return Set(key, updated);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes any value attached to <paramref name="key" />.</para>
        ///     <para xml:lang="zh-CN">移除附加到 <paramref name="key" /> 的任何值。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">True if an entry was removed.</para>
        ///     <para xml:lang="zh-CN">已移除条目时为 true。</para>
        /// </returns>
        public bool Remove(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryRemove(key, out _);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the value attached to <paramref name="key" /> if present.</para>
        ///     <para xml:lang="zh-CN">存在时移除附加到 <paramref name="key" /> 的值。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">True if an entry was removed.</para>
        ///     <para xml:lang="zh-CN">已移除条目时为 true。</para>
        /// </returns>
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
        ///     <para xml:lang="en">Removes all entries from the table without affecting live <typeparamref name="TKey" /> instances.</para>
        ///     <para xml:lang="zh-CN">移除表中的所有条目，但不影响仍存活的 <typeparamref name="TKey" /> 实例。</para>
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
