using System.Collections.Immutable;

namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     <para xml:lang="en">Carries extensible storage-addressing values alongside persistence operations.</para>
    ///     <para xml:lang="zh-CN">在持久化操作中携带可扩展的存储寻址值。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         This type is intentionally generic because some <see cref="SaveScope" /> values may require
    ///         additional addressing information, such as a run fingerprint. Add each new value as a
    ///         <see cref="StorageContextKey{TValue}" /> rather than another method parameter.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         此类型刻意保持通用，因为部分 <see cref="SaveScope" /> 值可能需要额外的寻址信息，例如一局游戏的指纹。应以
    ///         <see cref="StorageContextKey{TValue}" /> 表示每个新增值，而不是继续增加方法参数。
    ///     </para>
    /// </remarks>
    public sealed class StorageContext
    {
        private readonly ImmutableDictionary<string, object?> _values;

        private StorageContext(ImmutableDictionary<string, object?> values)
        {
            _values = values;
        }

        /// <summary>
        ///     <para xml:lang="en">Empty context.</para>
        ///     <para xml:lang="zh-CN">空上下文。</para>
        /// </summary>
        public static StorageContext Empty { get; } = new(ImmutableDictionary<string, object?>.Empty);

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns <see langword="true" /> and assigns <paramref name="value" /> when the key exists and
        ///         its value is a <typeparamref name="TValue" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         键存在且对应值为 <typeparamref name="TValue" /> 时，返回 <see langword="true" /> 并为
        ///         <paramref name="value" /> 赋值。
        ///     </para>
        /// </summary>
        public bool TryGet<TValue>(StorageContextKey<TValue> key, out TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (_values.TryGetValue(key.Id, out var raw) && raw is TValue typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Returns a new <see cref="StorageContext" /> with <paramref name="value" /> stored under
        ///         <paramref name="key" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         返回一个新的 <see cref="StorageContext" />，其中 <paramref name="value" /> 存储在
        ///         <paramref name="key" /> 下。
        ///     </para>
        /// </summary>
        public StorageContext With<TValue>(StorageContextKey<TValue> key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.SetItem(key.Id, value));
        }

        /// <summary>
        ///     <para xml:lang="en">Returns a new <see cref="StorageContext" /> without <paramref name="key" />.</para>
        ///     <para xml:lang="zh-CN">返回一个不含 <paramref name="key" /> 的新 <see cref="StorageContext" />。</para>
        /// </summary>
        public StorageContext Without<TValue>(StorageContextKey<TValue> key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.Remove(key.Id));
        }
    }
}
