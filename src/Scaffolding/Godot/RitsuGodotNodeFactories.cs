using Godot;
using MegaCrit.Sts2.Core.Assets;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Explicit-only Godot node construction: call these from your own code paths. Does not patch global
    ///     <c>PackedScene.Instantiate</c>, so baselib scene conversion and vanilla loading keep exclusive control of their
    ///     hooks.
    ///     仅显式使用的 Godot 节点构造：从你自己的代码路径调用这些方法。它不会修补全局
    ///     <c>PackedScene.Instantiate</c>，因此 baselib 场景转换和原版加载会继续独占控制自己的
    ///     钩子。
    /// </summary>
    public static class RitsuGodotNodeFactories
    {
        /// <summary>
        ///     Registers a typed factory for explicit <see cref="CreateFromScene{TNode}(PackedScene)" /> and
        ///     <see cref="CreateFromResource{TNode}(object)" /> calls.
        ///     为显式 <see cref="CreateFromScene{TNode}(PackedScene)" /> 和
        ///     <see cref="CreateFromResource{TNode}(object)" /> 调用注册强类型工厂。
        /// </summary>
        public static void RegisterFactory<TNode>(
            IRitsuGodotNodeFactory<TNode> factory,
            bool replaceExisting = false)
            where TNode : Node, new()
        {
            ArgumentNullException.ThrowIfNull(factory);
            RitsuGodotNodeFactoryRegistry.RegisterFactory<TNode>(
                new PublicRitsuGodotNodeFactoryAdapter<TNode>(factory),
                replaceExisting);
        }

        /// <summary>
        ///     Registers delegate-based conversion for <typeparamref name="TNode" />. When
        ///     <paramref name="createFromResource" /> is omitted, <see cref="CreateFromResource{TNode}(object)" /> throws
        ///     <see cref="NotSupportedException" /> for this node type.
        ///     为 <typeparamref name="TNode" /> 注册基于委托的转换。当省略 <paramref name="createFromResource" /> 时，
        ///     此节点类型的 <see cref="CreateFromResource{TNode}(object)" /> 会抛出
        ///     <see cref="NotSupportedException" />。
        /// </summary>
        public static void RegisterFactory<TNode>(
            Func<Node, VisualNodeStyle?, TNode> createFromNode,
            Func<object, VisualNodeStyle?, TNode>? createFromResource = null,
            bool replaceExisting = false)
            where TNode : Node, new()
        {
            ArgumentNullException.ThrowIfNull(createFromNode);
            RitsuGodotNodeFactoryRegistry.RegisterFactory<TNode>(
                new DelegateRitsuGodotNodeFactory<TNode>(createFromNode, createFromResource),
                replaceExisting);
        }

        /// <summary>
        ///     Creates <typeparamref name="TNode" /> from a loaded resource (e.g. <see cref="Texture2D" /> for creature /
        ///     merchant factories).
        ///     <see cref="Texture2D" />）。
        ///     从已加载资源创建 <typeparamref name="TNode" />（例如用于生物 /
        ///     商人工厂的 <see cref="Texture2D" />）。
        ///     <see cref="Texture2D" />）。
        /// </summary>
        public static TNode CreateFromResource<TNode>(object resource) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromResource<TNode>(resource);
        }

        /// <summary>
        ///     Creates <typeparamref name="TNode" /> from a loaded resource and applies optional style overrides to the
        ///     factory's default visual target.
        ///     从已加载资源创建 <typeparamref name="TNode" />，并将可选样式覆盖应用到工厂的默认视觉目标。
        /// </summary>
        public static TNode CreateFromResource<TNode>(object resource, VisualNodeStyle? style) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromResource<TNode>(resource, style);
        }

        /// <summary>
        ///     Instantiates <paramref name="scene" /> and runs the registered factory to produce <typeparamref name="TNode" />.
        ///     实例化 <paramref name="scene" /> 并运行已注册工厂来生成 <typeparamref name="TNode" />。
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene);
        }

        /// <summary>
        ///     Instantiates <paramref name="scene" />, converts it through the registered factory, and applies optional
        ///     style overrides to the factory's default visual target.
        ///     实例化 <paramref name="scene" />，通过已注册工厂转换，并将可选样式覆盖应用到工厂的默认视觉目标。
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene, VisualNodeStyle? style) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene, style);
        }

        /// <summary>
        ///     Same as <see cref="CreateFromScene{TNode}(PackedScene)" /> but uses the given Godot instantiate edit
        ///     state (match vanilla callsites such as <c>PackedScene.GenEditState.Disabled</c>).
        ///     与 <see cref="CreateFromScene{TNode}(PackedScene)" /> 相同，但使用给定的 Godot 实例化编辑
        ///     状态（匹配原版调用点，例如 <c>PackedScene.GenEditState.Disabled</c>）。
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState editState)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene, editState);
        }

        /// <inheritdoc cref="CreateFromScene{TNode}(PackedScene, PackedScene.GenEditState)" />
        public static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState editState,
            VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene, editState, style);
        }

        /// <summary>
        ///     Loads <paramref name="scenePath" /> via <see cref="PreloadManager.Cache" /> then
        ///     <see>
        ///         <cref>CreateFromScene{TNode}</cref>
        ///     </see>
        ///     .
        ///     <see>
        ///         <cref>CreateFromScene{TNode}</cref>
        ///     </see>
        ///     。
        ///     通过 <see cref="PreloadManager.Cache" /> 加载 <paramref name="scenePath" />，然后调用
        ///     <see>
        ///         <cref>CreateFromScene{TNode}</cref>
        ///     </see>
        ///     。
        ///     <see>
        ///         <cref>CreateFromScene{TNode}</cref>
        ///     </see>
        ///     。
        /// </summary>
        public static TNode CreateFromScenePath<TNode>(string scenePath) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath);
        }

        /// <summary>
        ///     Loads <paramref name="scenePath" />, converts it through the registered factory, and applies optional style
        ///     overrides to the factory's default visual target.
        ///     加载 <paramref name="scenePath" />，通过已注册工厂转换，并将可选样式覆盖应用到工厂的默认视觉目标。
        /// </summary>
        public static TNode CreateFromScenePath<TNode>(string scenePath, VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath, style);
        }

        /// <inheritdoc cref="CreateFromScene{TNode}(PackedScene, PackedScene.GenEditState)" />
        public static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState editState)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath, editState);
        }

        /// <inheritdoc cref="CreateFromScenePath{TNode}(string, PackedScene.GenEditState)" />
        public static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState editState,
            VisualNodeStyle? style)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath, editState, style);
        }

        private static TNode RequireCreatedNode<TNode>(TNode? node, string factoryMember) where TNode : Node
        {
            return node ?? throw new InvalidOperationException(
                $"Registered Godot node factory member '{factoryMember}' returned null for {typeof(TNode).FullName}.");
        }

        private sealed class PublicRitsuGodotNodeFactoryAdapter<TNode>(IRitsuGodotNodeFactory<TNode> factory)
            : RitsuGodotNodeFactory
            where TNode : Node
        {
            public override Node CreateFromNode(Node source)
            {
                return CreateFromNode(source, null);
            }

            public override Node CreateFromNode(Node source, VisualNodeStyle? style)
            {
                return RequireCreatedNode(factory.CreateFromNode(source, style),
                    nameof(IRitsuGodotNodeFactory<TNode>.CreateFromNode));
            }

            public override Node CreateBareFromResource(object resource)
            {
                return CreateFromResource(resource, null);
            }

            public override Node CreateFromResource(object resource, VisualNodeStyle? style)
            {
                return RequireCreatedNode(
                    factory.CreateFromResource(resource, style),
                    nameof(IRitsuGodotNodeFactory<TNode>.CreateFromResource));
            }

            public override void CompleteBareRoot(Node bare)
            {
            }
        }

        private sealed class DelegateRitsuGodotNodeFactory<TNode>(
            Func<Node, VisualNodeStyle?, TNode> createFromNode,
            Func<object, VisualNodeStyle?, TNode>? createFromResource)
            : RitsuGodotNodeFactory
            where TNode : Node
        {
            public override Node CreateFromNode(Node source)
            {
                return CreateFromNode(source, null);
            }

            public override Node CreateFromNode(Node source, VisualNodeStyle? style)
            {
                return RequireCreatedNode(createFromNode(source, style), nameof(createFromNode));
            }

            public override Node CreateBareFromResource(object resource)
            {
                return CreateFromResource(resource, null);
            }

            public override Node CreateFromResource(object resource, VisualNodeStyle? style)
            {
                if (createFromResource == null)
                    throw new NotSupportedException(
                        $"No resource factory was registered for {typeof(TNode).FullName}.");

                return RequireCreatedNode(createFromResource(resource, style), nameof(createFromResource));
            }

            public override void CompleteBareRoot(Node bare)
            {
            }
        }
    }
}
