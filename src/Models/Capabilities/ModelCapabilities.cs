using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     <para xml:lang="en">Entry point for model capability hosts.</para>
    ///     <para xml:lang="zh-CN">模型能力宿主的入口。</para>
    /// </summary>
    public static class ModelCapabilities
    {
        private const string SavedDataKey = "model_capabilities";
        private static readonly Lock InitializationLock = new();
        private static readonly ModelSavedDataSlotKey SavedDataSlotKey = new(Const.ModId, SavedDataKey);
        private static readonly ConditionalWeakTable<AbstractModel, ModelCapabilitySet> Collections = [];

        internal static bool IsInitialized { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Initializes the built-in persistence slot for model capabilities.</para>
        ///     <para xml:lang="zh-CN">初始化模型能力的内置持久化槽位。</para>
        /// </summary>
        public static void EnsureInitialized()
        {
            lock (InitializationLock)
            {
                if (IsInitialized)
                    return;

                ModelSavedDataStore.For(Const.ModId).RegisterComputed<AbstractModel, ModelCapabilitySaveDocument>(
                    SavedDataKey,
                    Export,
                    Import,
                    () => new(),
                    new()
                    {
                        ClonePolicy = ModelSavedDataClonePolicy.Copy,
                        WritePolicy = ModelSavedDataWritePolicy.AlwaysWhenPresent,
                    });
                IsInitialized = true;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the capability set for <paramref name="model" />.</para>
        ///     <para xml:lang="zh-CN">获取 <paramref name="model" /> 的能力集合。</para>
        /// </summary>
        public static ModelCapabilitySet Get(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return Collections.GetValue(model, CreateCollection);
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get an existing capability set without creating one.</para>
        ///     <para xml:lang="zh-CN">尝试获取已有能力集合，但不创建新集合。</para>
        /// </summary>
        public static bool TryGet(AbstractModel model, out ModelCapabilitySet collection)
        {
            ArgumentNullException.ThrowIfNull(model);
            return Collections.TryGetValue(model, out collection!);
        }

        internal static void MarkDirty(AbstractModel model)
        {
            if (Collections.TryGetValue(model, out var collection))
                collection.MarkDirtyFromHost();

            MarkSavedDataDirty(model);
        }

        internal static void MarkSavedDataDirty(AbstractModel model)
        {
            if (!IsInitialized)
                throw new InvalidOperationException(
                    "ModelCapabilities persistence has not been registered. Register a model capability or default capability modifier during mod initialization before mutating model capabilities.");

            ModelSavedDataRuntime.GetBag(model).Set(SavedDataSlotKey, new ModelCapabilitySaveDocument());
        }

        internal static void NotifyCloned(AbstractModel prototype, AbstractModel clone)
        {
            if (!TryGet(prototype, out var source))
                return;

            var target = Get(clone);
            source.CopyTo(target);
        }

        private static ModelCapabilitySet CreateCollection(AbstractModel model)
        {
            var collection = new ModelCapabilitySet(model);
            collection.Load(null);
            if (IsInitialized)
                ModelSavedDataRuntime.GetBag(model).Set(SavedDataSlotKey, new ModelCapabilitySaveDocument(), false);
            return collection;
        }

        private static ModelCapabilitySaveDocument? Export(AbstractModel model)
        {
            // ReSharper disable once InvertIf
            if (!TryGet(model, out var collection))
            {
                if (!ModelCapabilityDefaults.HasDefaultCapabilitySource(model))
                    return null;

                collection = Get(model);
            }

            return collection.ShouldSave() ? collection.Save() : null;
        }

        private static void Import(AbstractModel model, ModelCapabilitySaveDocument? document)
        {
            if (ModelCapabilityUpgradeReplayContext.TryDeferCardCapabilityImport(model, document))
                return;

            ImportImmediate(model, document);
        }

        internal static void ImportImmediate(AbstractModel model, ModelCapabilitySaveDocument? document)
        {
            var collection = Get(model);
            collection.Load(document);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Convenience extension methods for model capabilities.</para>
    ///     <para xml:lang="zh-CN">模型能力的便捷扩展方法。</para>
    /// </summary>
    public static class ModelCapabilityExtensions
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the capability set for this model.</para>
        ///     <para xml:lang="zh-CN">获取此模型的能力集合。</para>
        /// </summary>
        public static ModelCapabilitySet Capabilities(this AbstractModel model)
        {
            return ModelCapabilities.Get(model);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the first capability of type <typeparamref name="TCapability" /> on this model.</para>
        ///     <para xml:lang="zh-CN">获取此模型上的第一个 <typeparamref name="TCapability" /> 类型能力。</para>
        /// </summary>
        public static TCapability? Capability<TCapability>(this AbstractModel model)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).Get<TCapability>();
        }

        /// <summary>
        ///     <para xml:lang="en">Attempts to get the first capability of type <typeparamref name="TCapability" /> on this model.</para>
        ///     <para xml:lang="zh-CN">尝试获取此模型上的第一个 <typeparamref name="TCapability" /> 类型能力。</para>
        /// </summary>
        public static bool TryGetCapability<TCapability>(this AbstractModel model, out TCapability capability)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).TryGet(out capability);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies <paramref name="capability" /> to this model, using the set merge rules.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="capability" /> 应用到此模型，并使用能力集合的合并规则。</para>
        /// </summary>
        public static TCapability? ApplyCapability<TCapability>(
            this AbstractModel model,
            TCapability capability,
            ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).Apply(capability, options);
        }

        /// <summary>
        ///     <para xml:lang="en">Adds <paramref name="capability" /> to this model.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="capability" /> 添加到此模型。</para>
        /// </summary>
        public static TCapability? AddCapability<TCapability>(
            this AbstractModel model,
            TCapability capability,
            bool allowMerge = true,
            bool isUpgrade = false)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).Add(capability, allowMerge, isUpgrade);
        }

        /// <summary>
        ///     <para xml:lang="en">Applies <paramref name="capability" /> as a subtractive merge against this model.</para>
        ///     <para xml:lang="zh-CN">将 <paramref name="capability" /> 作为减法合并应用到此模型。</para>
        /// </summary>
        public static TCapability? SubtractCapability<TCapability>(
            this AbstractModel model,
            TCapability capability,
            bool isUpgrade = false)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).Subtract(capability, isUpgrade) as TCapability;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets an existing capability, or applies a new capability created by <paramref name="factory" />
        ///         .
        ///     </para>
        ///     <para xml:lang="zh-CN">获取已有能力；不存在时应用由 <paramref name="factory" /> 创建的新能力。</para>
        /// </summary>
        public static TCapability GetOrAddCapability<TCapability>(
            this AbstractModel model,
            Func<TCapability> factory,
            ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).GetOrAdd(factory, options);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an existing capability, or creates one from <see cref="ModelCapabilityRegistry" />.</para>
        ///     <para xml:lang="zh-CN">获取已有能力；不存在时通过 <see cref="ModelCapabilityRegistry" /> 创建。</para>
        /// </summary>
        public static TCapability GetOrCreateCapability<TCapability>(
            this AbstractModel model,
            ApplyModelCapabilityOptions options = new())
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).GetOrCreate<TCapability>(options);
        }

        /// <summary>
        ///     <para xml:lang="en">Gets an existing registered capability, or creates it as part of this model's upgrade.</para>
        ///     <para xml:lang="zh-CN">获取已有已注册能力；不存在时作为此模型升级的一部分创建。</para>
        /// </summary>
        public static TCapability GetOrCreateUpgradeCapability<TCapability>(
            this AbstractModel model,
            bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).GetOrCreateUpgrade<TCapability>(allowMerge);
        }

        /// <summary>
        ///     <para xml:lang="en">Creates a registered capability and applies it as part of this model's upgrade.</para>
        ///     <para xml:lang="zh-CN">创建已注册能力，并作为此模型升级的一部分应用。</para>
        /// </summary>
        public static TCapability? AddUpgradeCapability<TCapability>(
            this AbstractModel model,
            bool allowMerge = true)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).AddUpgrade<TCapability>(allowMerge);
        }

        /// <summary>
        ///     <para xml:lang="en">Removes the first capability of type <typeparamref name="TCapability" /> from this model.</para>
        ///     <para xml:lang="zh-CN">从此模型移除第一个 <typeparamref name="TCapability" /> 类型能力。</para>
        /// </summary>
        public static TCapability? RemoveCapability<TCapability>(this AbstractModel model)
            where TCapability : class, IModelCapability
        {
            return ModelCapabilities.Get(model).Remove<TCapability>();
        }
    }
}
