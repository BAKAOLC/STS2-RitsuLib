using System.Collections.Immutable;

namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Extensible storage addressing context carried alongside persistence operations.
    ///     Extensible 存储 addressing context carried alongside persistence operations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This type is intentionally generic: storage domains (see <see cref="SaveScope" />) may require additional
    ///         addressing facets (e.g. run fingerprint). New facets should be introduced as new
    ///         <see cref="StorageContextKey{TValue}" /> values rather than new method parameters.
    ///     </para>
    ///     <para>
    ///         此类型刻意保持通用：存储域（见 <see cref="SaveScope" />）可能需要额外的
    ///         寻址维度（例如跑局指纹）。新的维度应作为新的
    ///         <see cref="StorageContextKey{TValue}" /> 值引入，而不是新增方法参数。
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
        ///     Empty context.
        ///     空上下文。
        /// </summary>
        public static StorageContext Empty { get; } = new(ImmutableDictionary<string, object?>.Empty);

        /// <summary>
        ///     Returns true and assigns <paramref name="value" /> when the key exists and the stored value is of type
        ///     <typeparamref name="TValue" />.
        ///     当键存在且已存储值类型为
        ///     <typeparamref name="TValue" /> 时，返回 true 并赋值给 <paramref name="value" />。
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
        ///     Returns a new <see cref="StorageContext" /> with <paramref name="value" /> stored under <paramref name="key" />.
        ///     返回一个新的 <see cref="StorageContext" />，其中 <paramref name="value" /> 存储在 <paramref name="key" /> 下。
        /// </summary>
        public StorageContext With<TValue>(StorageContextKey<TValue> key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.SetItem(key.Id, value));
        }

        /// <summary>
        ///     Returns a new <see cref="StorageContext" /> without <paramref name="key" />.
        ///     返回一个不含 <paramref name="key" /> 的新 <see cref="StorageContext" />。
        /// </summary>
        public StorageContext Without<TValue>(StorageContextKey<TValue> key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.Remove(key.Id));
        }
    }
}
