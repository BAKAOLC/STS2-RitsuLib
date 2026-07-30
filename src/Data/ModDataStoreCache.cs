using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Data
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides a lazy cache for one <see cref="ModDataStore" /> key and invalidates it when the backing entry is
    ///         reloaded.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为单个 <see cref="ModDataStore" /> 键提供惰性缓存，并在重新加载底层条目时自动使缓存失效。
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The registered data model type.</para>
    ///     <para xml:lang="zh-CN">已注册的数据模型类型。</para>
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
        ///     <para xml:lang="en">Gets the logical data key associated with this cache.</para>
        ///     <para xml:lang="zh-CN">获取与此缓存关联的逻辑数据键。</para>
        /// </summary>
        public string Key { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the cached value, loading it from the store on first access or after invalidation.</para>
        ///     <para xml:lang="zh-CN">获取缓存值；首次访问或缓存失效后会从存储中重新读取。</para>
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
        ///     <para xml:lang="en">Gets whether this wrapper currently holds a cached instance.</para>
        ///     <para xml:lang="zh-CN">获取此包装器当前是否持有缓存实例。</para>
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
        ///     <para xml:lang="en">Clears the cached instance so the next <see cref="Value" /> access reads from the store.</para>
        ///     <para xml:lang="zh-CN">清除缓存实例，使下次访问 <see cref="Value" /> 时从存储中重新读取。</para>
        /// </summary>
        public void Invalidate()
        {
            lock (_sync)
            {
                _value = null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Reloads the value from the store and returns the refreshed instance.</para>
        ///     <para xml:lang="zh-CN">从存储中重新读取值，并返回刷新后的实例。</para>
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
        ///     <para xml:lang="en">Mutates the backing store entry in place.</para>
        ///     <para xml:lang="zh-CN">原地修改底层存储条目。</para>
        /// </summary>
        public void Modify(Action<T> modifier)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(modifier);

            _store.Modify(Key, modifier);
        }

        /// <summary>
        ///     <para xml:lang="en">Persists the backing store entry.</para>
        ///     <para xml:lang="zh-CN">持久化底层存储条目。</para>
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
