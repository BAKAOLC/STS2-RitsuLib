using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Per-mod registry for model-saved data slots.
    ///     每个 mod 的模型保存数据槽位注册表。
    /// </summary>
    public sealed class ModelSavedDataStore
    {
        private static readonly Lock StoresLock = new();

        private static readonly Dictionary<string, ModelSavedDataStore> Stores =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IModelSavedDataSlot> _slots =
            new(StringComparer.OrdinalIgnoreCase);

        private ModelSavedDataStore(string modId)
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
        public static ModelSavedDataStore For(string modId)
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
        ///     Registers saved data attached to mutable model instances.
        ///     注册附加到可变模型实例上的保存数据。
        /// </summary>
        public ModelSavedData<TTarget, TPayload> Register<TTarget, TPayload>(
            string key,
            Func<TPayload>? defaultFactory = null,
            ModelSavedDataOptions? options = null)
            where TTarget : AbstractModel
            where TPayload : class, new()
        {
            var slot = new StoredModelSavedDataSlot<TTarget, TPayload>(ModId, key, defaultFactory, options);
            RegisterSlot(slot);
            return new(slot);
        }

        /// <summary>
        ///     Registers computed saved data whose value is exported from and imported into the model directly.
        ///     注册从模型直接导出并导入的计算型保存数据。
        /// </summary>
        public void RegisterComputed<TTarget, TPayload>(
            string key,
            Func<TTarget, TPayload?> exporter,
            Action<TTarget, TPayload?> importer,
            Func<TPayload>? defaultFactory = null,
            ModelSavedDataOptions? options = null)
            where TTarget : AbstractModel
            where TPayload : class, new()
        {
            ArgumentNullException.ThrowIfNull(exporter);
            ArgumentNullException.ThrowIfNull(importer);

            RegisterSlot(new ComputedModelSavedDataSlot<TTarget, TPayload>(
                ModId,
                key,
                exporter,
                importer,
                defaultFactory,
                options));
        }

        private void RegisterSlot(IModelSavedDataSlot slot)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(slot.Key);
            lock (_slots)
            {
                if (!_slots.TryAdd(slot.Key, slot))
                    throw new InvalidOperationException(
                        $"ModelSavedData key is already registered: {ModId}::{slot.Key}");
            }

            ModelSavedDataRegistry.Register(slot);
        }
    }
}
