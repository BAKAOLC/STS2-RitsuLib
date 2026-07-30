using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Provides a per-mod registry for model-saved-data slots.</para>
    ///     <para xml:lang="zh-CN">提供按模组划分的模型保存数据槽位注册表。</para>
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
        ///     <para xml:lang="en">Gets the ID of the mod that owns this store.</para>
        ///     <para xml:lang="zh-CN">获取拥有此存储的模组 ID。</para>
        /// </summary>
        public string ModId { get; }

        /// <summary>
        ///     <para xml:lang="en">Gets the process-wide store for <paramref name="modId" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="modId" /> 的进程级存储。</para>
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
        ///     <para xml:lang="en">Registers saved data attached to mutable model instances.</para>
        ///     <para xml:lang="zh-CN">注册附加到可变模型实例的保存数据。</para>
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
        ///     <para xml:lang="en">
        ///         Registers computed saved data whose value is exported from and imported into the model directly.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册直接从模型导出值并将值导入模型的计算型保存数据。
        ///     </para>
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
                if (_slots.ContainsKey(slot.Key))
                    throw new InvalidOperationException(
                        $"ModelSavedData key is already registered: {ModId}::{slot.Key}");

                ModelSavedDataRegistry.Register(slot);
                _slots.Add(slot.Key, slot);
            }
        }
    }
}
