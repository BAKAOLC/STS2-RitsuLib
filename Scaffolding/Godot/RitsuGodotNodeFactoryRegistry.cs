using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Internal factory lookup for <see cref="RitsuGodotNodeFactories" />. Conversion runs only when you call
    ///     <see>
    ///         <cref>CreateFromScene{TNode}</cref>
    ///     </see>
    ///     or <see cref="CreateFromResource{TNode}" /> — there is no global
    ///     <c>PackedScene.Instantiate</c> postfix, so other libraries (e.g. baselib) and vanilla loads are unaffected.
    /// </summary>
    internal static class RitsuGodotNodeFactoryRegistry
    {
        private static readonly ConcurrentDictionary<Type, RitsuGodotNodeFactory> Factories = new();

        /// <summary>
        ///     Registers a factory instance for <typeparamref name="TNode" /> (typically done once from the factory ctor).
        /// </summary>
        public static void RegisterFactory<TNode>(RitsuGodotNodeFactory factory) where TNode : Node
        {
            Factories[typeof(TNode)] = factory;
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene) where TNode : Node, new()
        {
            return CreateFromScene<TNode>(scene, null);
        }

        internal static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState? editState)
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
            return (TNode)factory.CreateFromNode(root!);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath) where TNode : Node, new()
        {
            return CreateFromScenePath<TNode>(scenePath, null);
        }

        internal static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState? editState)
            where TNode : Node, new()
        {
            return CreateFromScene<TNode>(PreloadManager.Cache.GetScene(scenePath), editState);
        }

        internal static TNode CreateFromResource<TNode>(object resource) where TNode : Node, new()
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
            factory.CompleteBareRoot(bare);
            return (TNode)bare;
        }

        private static void RequireMainThread(string operation)
        {
            if (!NGame.IsMainThread())
                throw new InvalidOperationException($"[Godot] {operation} must run on the Godot main thread.");
        }
    }
}
