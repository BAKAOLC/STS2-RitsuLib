using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Data
{
    /// <summary>
    ///     Lazy cache for a single <see cref="ModDataStore" /> key.
    ///     Invalidates automatically when the backing entry is reloaded or the active profile data is invalidated.
    ///     单个 <see cref="ModDataStore" /> key 的惰性缓存。
    ///     当后备条目重新加载或活动档案数据失效时自动失效。
    /// </summary>
    /// <typeparam name="T">
    ///     Registered data model type.
    ///     已注册的数据模型类型。
    /// </typeparam>
    public sealed class ModDataStoreCache<T> : IDisposable where T : class, new()
    {
        private readonly IDisposable _profileInvalidatedSubscription;
        private readonly ModDataStore _store;
        private readonly Lock _sync = new();
        private bool _disposed;
        private T? _value;

        internal ModDataStoreCache(ModDataStore store, string key)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            _store = store;
            Key = key;
            _store.EntryReloaded += OnEntryReloaded;
            _profileInvalidatedSubscription =
                RitsuLibFramework.SubscribeLifecycle<ProfileDataInvalidatedEvent>(_ => Invalidate(), false);
        }

        /// <summary>
        ///     Logical data key for this cache.
        ///     此缓存对应的逻辑数据 key。
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     Returns the cached value, loading it from the store on first access or after invalidation.
        ///     返回缓存值；首次访问或失效后会从存储重新读取。
        /// </summary>
        public T Value
        {
            get
            {
                ThrowIfDisposed();

                lock (_sync)
                {
                    return _value ??= _store.Get<T>(Key);
                }
            }
        }

        /// <summary>
        ///     Whether this wrapper currently holds a cached instance.
        ///     此包装器当前是否持有缓存实例。
        /// </summary>
        public bool HasValue
        {
            get
            {
                lock (_sync)
                {
                    return _value != null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _store.EntryReloaded -= OnEntryReloaded;
            _profileInvalidatedSubscription.Dispose();
            Invalidate();
        }

        /// <summary>
        ///     Drops the cached instance; the next <see cref="Value" /> access re-reads the store.
        ///     丢弃缓存实例；下次访问 <see cref="Value" /> 时重新读取存储。
        /// </summary>
        public void Invalidate()
        {
            lock (_sync)
            {
                _value = null;
            }
        }

        /// <summary>
        ///     Forces a re-read from the store and returns the refreshed instance.
        ///     强制从存储重新读取并返回刷新后的实例。
        /// </summary>
        public T Refresh()
        {
            ThrowIfDisposed();

            lock (_sync)
            {
                _value = _store.Get<T>(Key);
                return _value;
            }
        }

        /// <summary>
        ///     Mutates the backing store entry without requiring the caller to touch the cached instance directly.
        ///     修改后备存储条目，调用方无需直接接触缓存实例。
        /// </summary>
        public void Modify(Action<T> modifier)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(modifier);

            _store.Modify(Key, modifier);
        }

        /// <summary>
        ///     Persists the backing store entry.
        ///     持久化后备存储条目。
        /// </summary>
        public void Save()
        {
            ThrowIfDisposed();
            _store.Save(Key);
        }

        private void OnEntryReloaded(string key)
        {
            if (string.Equals(Key, key, StringComparison.OrdinalIgnoreCase))
                Invalidate();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
