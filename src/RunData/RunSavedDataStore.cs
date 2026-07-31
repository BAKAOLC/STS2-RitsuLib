namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     <para xml:lang="en">Provides a per-mod registry of run saved-data slots.</para>
    ///     <para xml:lang="zh-CN">提供按模组划分的局内保存数据槽位注册表。</para>
    /// </summary>
    public sealed class RunSavedDataStore
    {
        private static readonly Lock StoresLock = new();

        private static readonly Dictionary<string, RunSavedDataStore> Stores =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IRunSavedDataSlot> _slots =
            new(StringComparer.OrdinalIgnoreCase);

        private RunSavedDataStore(string modId)
        {
            ModId = modId;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the identifier of the mod that owns this store.</para>
        ///     <para xml:lang="zh-CN">获取此存储所属模组的标识符。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the process-wide store for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 对应的进程级存储。</para>
        /// </summary>
        public static RunSavedDataStore For(string modId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(modId);
            lock (StoresLock)
            {
                if (Stores.TryGetValue(modId, out var store))
                    return store;

                store = new(modId);
                Stores[modId] = store;
                return store;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Registers saved data shared by the whole run.</para>
        ///     <para xml:lang="zh-CN">注册由整局游戏共享的保存数据。</para>
        /// </summary>
        public RunSavedData<T> Register<T>(
            string key,
            Func<T>? defaultFactory = null,
            RunSavedDataOptions? options = null)
            where T : class, new()
        {
            var slot = new RunSavedDataRunSlot<T>(ModId, key, defaultFactory, options);
            RegisterSlot(slot);
            return new(slot);
        }

        /// <summary>
        ///     <para xml:lang="en">Registers run saved data stored separately for each player.</para>
        ///     <para xml:lang="zh-CN">注册为每名玩家分别存储的局内保存数据。</para>
        /// </summary>
        public PlayerRunSavedData<T> RegisterPerPlayer<T>(
            string key,
            Func<T>? defaultFactory = null,
            RunSavedDataOptions? options = null)
            where T : class, new()
        {
            var slot = new RunSavedDataPlayerSlot<T>(ModId, key, defaultFactory, options);
            RegisterSlot(slot);
            return new(slot);
        }

        private void RegisterSlot(IRunSavedDataSlot slot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(slot.Key);
            lock (_slots)
            {
                if (!_slots.TryAdd(slot.Key, slot))
                    throw new InvalidOperationException($"RunSavedData key is already registered: {ModId}::{slot.Key}");

                try
                {
                    RunSavedDataRegistry.Register(slot);
                }
                catch
                {
                    if (_slots.TryGetValue(slot.Key, out var current) && ReferenceEquals(current, slot))
                        _slots.Remove(slot.Key);
                    throw;
                }
            }
        }
    }
}
