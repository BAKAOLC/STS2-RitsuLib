using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides internal factory lookup for <see cref="RitsuGodotNodeFactories" />. Conversion runs only through
    ///         explicit factory calls; no global <c>PackedScene.Instantiate</c> postfix is installed, so BaseLib and
    ///         base-game scene loading are unaffected.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为 <see cref="RitsuGodotNodeFactories" /> 提供内部工厂查找。转换仅在显式调用工厂 API 时运行；
    ///         此注册表不会安装全局 <c>PackedScene.Instantiate</c> 后缀，因此不会影响 BaseLib 和游戏的场景加载。
    ///     </para>
    /// </summary>
    internal static class RitsuGodotNodeFactoryRegistry
    {
        private static readonly ConcurrentDictionary<Type, RitsuGodotNodeFactory> Factories = new();

        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a factory instance for <typeparamref name="TNode" />, normally from the factory constructor.
        ///         Existing registrations are replaced.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TNode" /> 注册工厂实例，通常由工厂构造函数调用。已有注册会被替换。
        ///     </para>
        /// </summary>
        public static void RegisterFactory<TNode>(RitsuGodotNodeFactory factory) where TNode : Node
        {
            RegisterFactory<TNode>(factory, true);
        }

        internal static void RegisterFactory<TNode>(RitsuGodotNodeFactory factory, bool replaceExisting)
            where TNode : Node
        {
            ArgumentNullException.ThrowIfNull(factory);

            if (replaceExisting)
            {
                Factories[typeof(TNode)] = factory;
                return;
            }

            if (!Factories.TryAdd(typeof(TNode), factory))
                throw new InvalidOperationException(
                    $"A node factory is already registered for {typeof(TNode).FullName}. Pass replaceExisting: true to replace it.");
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
            RitsuLibFramework.Logger.Debug($"[Godot] Creating {typeof(TNode).Name} from scene {scene.ResourcePath}");
            if (!Factories.TryGetValue(typeof(TNode), out var factory))
                throw new InvalidOperationException($"No node factory registered for {typeof(TNode).Name}");

            var root = editState is { } state ? scene.Instantiate(state) : scene.Instantiate();
            if (root == null)
                throw new InvalidOperationException(
                    $"PackedScene.Instantiate returned null for '{scene.ResourcePath}'.");

            try
            {
                var created = factory.CreateFromNode(root, style);
                if (!GodotObject.IsInstanceValid(created))
                    throw new InvalidOperationException(
                        $"Factory for {typeof(TNode).FullName} returned a null or invalid Godot node.");

                FreeUnusedSceneRoot(root, created);
                return (TNode)created;
            }
            catch
            {
                if (GodotObject.IsInstanceValid(root))
                    root.Free();
                throw;
            }
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
            if (resource is GodotObject godotResource && !GodotObject.IsInstanceValid(godotResource))
                throw new ArgumentException(
                    "The supplied Godot resource native instance is invalid (freed).",
                    nameof(resource));

            RequireMainThread(nameof(CreateFromResource));
            if (!Factories.TryGetValue(typeof(TNode), out var factory))
                throw new InvalidOperationException($"No node factory registered for {typeof(TNode).Name}");

            if (resource is string s && ResourceLoader.Exists(s))
            {
                var loaded = ResourceLoader.Load(s);

                resource = loaded ??
                           throw new InvalidOperationException($"ResourceLoader.Load returned null for path: {s}");
            }

            RitsuLibFramework.Logger.Debug($"[Godot] Creating {typeof(TNode).Name} from {resource.GetType().Name}");
            var created = factory.CreateFromResource(resource, style);
            if (!GodotObject.IsInstanceValid(created))
                throw new InvalidOperationException(
                    $"Factory for {typeof(TNode).FullName} returned a null or invalid Godot node.");

            return (TNode)created;
        }

        private static void FreeUnusedSceneRoot(Node root, Node created)
        {
            if (!GodotObject.IsInstanceValid(root) ||
                root.GetInstanceId() == created.GetInstanceId() ||
                root.IsAncestorOf(created) ||
                root.GetParent() != null)
                return;

            root.Free();
        }

        private static void RequireMainThread(string operation)
        {
            if (!NGame.IsMainThread())
                throw new InvalidOperationException($"[Godot] {operation} must run on the Godot main thread.");
        }
    }
}
