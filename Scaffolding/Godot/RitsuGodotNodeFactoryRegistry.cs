using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Internal factory lookup for <see cref="RitsuGodotNodeFactories" />. Conversion runs only when you call
    ///     <see>
    ///         <cref>CreateFromScene{TNode}</cref>
    ///     </see>
    ///     or <c>CreateFromResource</c> — there is no global
    ///     <c>PackedScene.Instantiate</c> postfix, so other libraries (e.g. baselib) and vanilla loads are unaffected.
    ///     <see>
    ///         <cref>CreateFromScene{TNode}</cref>
    ///     </see>
    ///     <see cref="RitsuGodotNodeFactories" /> 的内部工厂查找。只有在调用
    ///     <see>
    ///         <cref>CreateFromScene{TNode}</cref>
    ///     </see>
    ///     或 <c>CreateFromResource</c> 时才会运行转换；没有全局
    ///     <c>PackedScene.Instantiate</c> postfix，因此其他库（如 baselib）和原版加载不受影响。
    ///     <see>
    ///         <cref>CreateFromScene{TNode}</cref>
    ///     </see>
    /// </summary>
    internal static class RitsuGodotNodeFactoryRegistry
    {
        private static readonly ConcurrentDictionary<Type, RitsuGodotNodeFactory> Factories = new();

        /// <summary>
        ///     Registers a factory instance for <typeparamref name="TNode" /> (typically done once from the factory ctor).
        ///     为 <typeparamref name="TNode" /> 注册一个工厂实例（通常从工厂构造函数中执行一次）。
        /// </summary>
        public static void RegisterFactory<TNode>(RitsuGodotNodeFactory factory) where TNode : Node
        {
            Factories[typeof(TNode)] = factory;
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene) where TNode : Node, new()
        {
            return CreateFromScene<TNode>(scene, null, null);
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState? editState)
            where TNode : Node, new()
        {
            return CreateFromScene<TNode>(scene, editState, null);
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene, VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return CreateFromScene<TNode>(scene, null, style);
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState? editState,
            VisualNodeStyle? style)
            where TNode : Node, new()
        {
            if (!GodotObject.IsInstanceValid(scene))
                throw new ArgumentException(
                    "PackedScene is null or the native instance is invalid (freed).",
                    nameof(scene));

            RequireMainThread(nameof(CreateFromScene));
            RitsuLibFramework.Logger.Info($"[Godot] Creating {typeof(TNode).Name} from scene {scene.ResourcePath}");
            if (!Factories.TryGetValue(typeof(TNode), out var factory))
                throw new InvalidOperationException($"No node factory registered for {typeof(TNode).Name}");

            var root = editState is { } state ? scene.Instantiate(state) : scene.Instantiate();
            return (TNode)factory.CreateFromNode(root!, style);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath) where TNode : Node, new()
        {
            return CreateFromScenePath<TNode>(scenePath, null, null);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState? editState)
            where TNode : Node, new()
        {
            return CreateFromScenePath<TNode>(scenePath, editState, null);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath, VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return CreateFromScenePath<TNode>(scenePath, null, style);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState? editState,
            VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return CreateFromScene<TNode>(PreloadManager.Cache.GetScene(scenePath), editState, style);
        }

        internal static TNode CreateFromResource<TNode>(object resource) where TNode : Node, new()
        {
            return CreateFromResource<TNode>(resource, null);
        }

        internal static TNode CreateFromResource<TNode>(object resource, VisualNodeStyle? style)
            where TNode : Node, new()
        {
            ArgumentNullException.ThrowIfNull(resource);

            RequireMainThread(nameof(CreateFromResource));
            if (!Factories.TryGetValue(typeof(TNode), out var factory))
                throw new InvalidOperationException($"No node factory registered for {typeof(TNode).Name}");

            if (resource is string s && ResourceLoader.Exists(s))
            {
                var loaded = ResourceLoader.Load(s);

                resource = loaded ??
                           throw new InvalidOperationException($"ResourceLoader.Load returned null for path: {s}");
            }

            RitsuLibFramework.Logger.Info($"[Godot] Creating {typeof(TNode).Name} from {resource.GetType().Name}");
            var bare = factory.CreateBareFromResource(resource);
            factory.CompleteBareRoot(bare, style);
            return (TNode)bare;
        }

        private static void RequireMainThread(string operation)
        {
            if (!NGame.IsMainThread())
                throw new InvalidOperationException($"[Godot] {operation} must run on the Godot main thread.");
        }
    }
}
