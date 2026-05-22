using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Capabilities
{
    /// <summary>
    ///     Entry point for model component hosts.
    ///     模型组件宿主的入口。
    /// </summary>
    public static class ModelComponents
    {
        private const string SavedDataKey = "model_components";
        private static readonly ModelSavedDataSlotKey SavedDataSlotKey = new(Const.ModId, SavedDataKey);
        private static readonly ConditionalWeakTable<AbstractModel, ModelComponentCollection> Collections = [];
        private static bool _initialized;

        /// <summary>
        ///     Initializes the built-in persistence slot for model components.
        ///     初始化模型组件的内置持久化槽位。
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;
            ModelSavedDataStore.For(Const.ModId).RegisterComputed<AbstractModel, ModelComponentSaveDocument>(
                SavedDataKey,
                Export,
                Import,
                () => new(),
                new()
                {
                    ClonePolicy = ModelSavedDataClonePolicy.Copy,
                    WritePolicy = ModelSavedDataWritePolicy.AlwaysWhenPresent,
                });
        }

        /// <summary>
        ///     Gets the component collection for <paramref name="model" />.
        ///     获取 <paramref name="model" /> 的组件集合。
        /// </summary>
        public static ModelComponentCollection Get(AbstractModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            EnsureInitialized();
            return Collections.GetValue(model, CreateCollection);
        }

        /// <summary>
        ///     Attempts to get an existing component collection without creating one.
        ///     尝试获取已有组件集合，但不创建新集合。
        /// </summary>
        public static bool TryGet(AbstractModel model, out ModelComponentCollection collection)
        {
            ArgumentNullException.ThrowIfNull(model);
            EnsureInitialized();
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
            ModelSavedDataRuntime.GetBag(model).Set(SavedDataSlotKey, new ModelComponentSaveDocument());
        }

        internal static void NotifyCloned(AbstractModel prototype, AbstractModel clone)
        {
            if (!TryGet(prototype, out var source))
                return;

            var target = Get(clone);
            source.CopyTo(target);
        }

        private static ModelComponentCollection CreateCollection(AbstractModel model)
        {
            var collection = new ModelComponentCollection(model);
            collection.Load(null);
            ModelSavedDataRuntime.GetBag(model).Set(SavedDataSlotKey, new ModelComponentSaveDocument(), false);
            return collection;
        }

        private static ModelComponentSaveDocument? Export(AbstractModel model)
        {
            return TryGet(model, out var collection) && collection.ShouldSave() ? collection.Save() : null;
        }

        private static void Import(AbstractModel model, ModelComponentSaveDocument? document)
        {
            if (ModelComponentUpgradeReplayContext.TryDeferCardComponentImport(model, document))
                return;

            ImportImmediate(model, document);
        }

        internal static void ImportImmediate(AbstractModel model, ModelComponentSaveDocument? document)
        {
            var collection = Get(model);
            collection.Load(document);
        }
    }

    /// <summary>
    ///     Convenience extension methods for model components.
    ///     模型组件的便捷扩展方法。
    /// </summary>
    public static class ModelComponentExtensions
    {
        /// <summary>
        ///     Gets the component collection for this model.
        ///     获取此模型的组件集合。
        /// </summary>
        public static ModelComponentCollection Components(this AbstractModel model)
        {
            return ModelComponents.Get(model);
        }

        /// <summary>
        ///     Gets the first component of type <typeparamref name="TComponent" /> on this model.
        ///     获取此模型上的第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public static TComponent? Component<TComponent>(this AbstractModel model)
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).Get<TComponent>();
        }

        /// <summary>
        ///     Attempts to get the first component of type <typeparamref name="TComponent" /> on this model.
        ///     尝试获取此模型上的第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public static bool TryGetComponent<TComponent>(this AbstractModel model, out TComponent component)
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).TryGet(out component);
        }

        /// <summary>
        ///     Gets an existing component, or applies a new component created by <paramref name="factory" />.
        ///     获取已有组件；不存在时应用由 <paramref name="factory" /> 创建的新组件。
        /// </summary>
        public static TComponent GetOrAddComponent<TComponent>(
            this AbstractModel model,
            Func<TComponent> factory,
            ApplyModelComponentOptions options = new())
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).GetOrAdd(factory, options);
        }

        /// <summary>
        ///     Gets an existing component, or creates one from <see cref="ModelComponentRegistry" />.
        ///     获取已有组件；不存在时通过 <see cref="ModelComponentRegistry" /> 创建。
        /// </summary>
        public static TComponent GetOrCreateRegisteredComponent<TComponent>(
            this AbstractModel model,
            ApplyModelComponentOptions options = new())
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).GetOrCreateRegistered<TComponent>(options);
        }

        /// <summary>
        ///     Gets an existing registered component, or creates it as part of this model's upgrade.
        ///     获取已有已注册组件；不存在时作为此模型升级的一部分创建。
        /// </summary>
        public static TComponent GetOrCreateRegisteredUpgradeComponent<TComponent>(
            this AbstractModel model,
            bool allowMerge = true)
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).GetOrCreateRegisteredForUpgrade<TComponent>(allowMerge);
        }

        /// <summary>
        ///     Creates a registered component and applies it as part of this model's upgrade.
        ///     创建已注册组件，并作为此模型升级的一部分应用。
        /// </summary>
        public static TComponent? AddRegisteredUpgradeComponent<TComponent>(
            this AbstractModel model,
            bool allowMerge = true)
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).AddRegisteredForUpgrade<TComponent>(allowMerge);
        }

        /// <summary>
        ///     Removes the first component of type <typeparamref name="TComponent" /> from this model.
        ///     从此模型移除第一个 <typeparamref name="TComponent" /> 类型组件。
        /// </summary>
        public static TComponent? RemoveComponent<TComponent>(this AbstractModel model)
            where TComponent : class, IModelComponent
        {
            return ModelComponents.Get(model).Remove<TComponent>();
        }
    }
}
