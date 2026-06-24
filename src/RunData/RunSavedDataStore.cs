namespace STS2RitsuLib.RunData
{
    /// <summary>
    ///     Per-mod registry for run saved data slots.
    ///     每个 mod 的跑局保存数据槽位注册表。
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
        ///     Owning mod id for this store.
        ///     此存储所属的 mod ID。
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     Returns the process-wide store for <paramref name="modId" />.
        ///     返回 <paramref name="modId" /> 的进程级存储。
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
        ///     Registers shared per-run saved data.
        ///     注册共享跑局保存数据。
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
        ///     Registers per-player run saved data.
        ///     注册按玩家分桶的跑局保存数据。
        /// </summary>
        public PlayerRunSavedData<T> RegisterPerPlayer<T>(
            string key,
            Func<T>? defaultFactory = null,
            RunSavedDataOptions? options = null)
            where T : class, new()
        {
            var slot = new RunSavedDataPlayerSlot<T>(ModId, key, defaultFactory, options);
            RegisterSlot(slot);
            return new(slot, defaultFactory);
        }

        private void RegisterSlot(IRunSavedDataSlot slot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(slot.Key);
            lock (_slots)
            {
                if (!_slots.TryAdd(slot.Key, slot))
                    throw new InvalidOperationException($"RunSavedData key is already registered: {ModId}::{slot.Key}");
            }

            RunSavedDataRegistry.Register(slot);
        }
    }
}
