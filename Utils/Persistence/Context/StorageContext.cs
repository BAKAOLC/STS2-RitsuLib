using System.Collections.Immutable;

namespace STS2RitsuLib.Utils.Persistence.Context
{
    /// <summary>
    ///     Extensible storage addressing context carried alongside persistence operations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This type is intentionally generic: storage domains (see <see cref="SaveScope" />) may require additional
    ///         addressing facets (e.g. run fingerprint). New facets should be introduced as new
    ///         <see cref="StorageContextKey{TValue}" /> values rather than new method parameters.
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
        /// </summary>
        public static StorageContext Empty { get; } = new(ImmutableDictionary<string, object?>.Empty);

        /// <summary>
        ///     Returns true and assigns <paramref name="value" /> when the key exists and the stored value is of type
        ///     <typeparamref name="TValue" />.
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
        /// </summary>
        public StorageContext With<TValue>(StorageContextKey<TValue> key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.SetItem(key.Id, value));
        }

        /// <summary>
        ///     Returns a new <see cref="StorageContext" /> without <paramref name="key" />.
        /// </summary>
        public StorageContext Without<TValue>(StorageContextKey<TValue> key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new(_values.Remove(key.Id));
        }
    }
}
