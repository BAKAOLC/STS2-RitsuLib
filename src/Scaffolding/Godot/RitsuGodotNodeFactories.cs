using Godot;
using MegaCrit.Sts2.Core.Assets;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Provides explicitly invoked Godot node construction APIs. These methods do not patch
    ///         <c>PackedScene.Instantiate</c>, so BaseLib scene conversion and base-game loading retain control of their
    ///         own hooks.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         提供需要显式调用的 Godot 节点构造 API。这些方法不会修补 <c>PackedScene.Instantiate</c>，
    ///         因此 BaseLib 的场景转换和游戏的资源加载仍由各自的钩子控制。
    ///     </para>
    /// </summary>
    public static class RitsuGodotNodeFactories
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Registers a typed factory used by explicit <see cref="CreateFromScene{TNode}(PackedScene)" /> and
        ///         <see cref="CreateFromResource{TNode}(object)" /> calls.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         注册供显式调用 <see cref="CreateFromScene{TNode}(PackedScene)" /> 和
        ///         <see cref="CreateFromResource{TNode}(object)" /> 时使用的强类型工厂。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Registers delegate-based conversion for <typeparamref name="TNode" />. If
        ///         <paramref name="createFromResource" /> is omitted, <see cref="CreateFromResource{TNode}(object)" /> throws
        ///         <see cref="NotSupportedException" /> for this node type.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         为 <typeparamref name="TNode" /> 注册基于委托的转换。如果省略
        ///         <paramref name="createFromResource" />，则对此节点类型调用
        ///         <see cref="CreateFromResource{TNode}(object)" /> 时会抛出 <see cref="NotSupportedException" />。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Creates <typeparamref name="TNode" /> from a loaded resource, such as the <see cref="Texture2D" />
        ///         accepted by creature and merchant factories.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从已加载的资源创建 <typeparamref name="TNode" />，例如生物和商人工厂支持的
        ///         <see cref="Texture2D" />。
        ///     </para>
        /// </summary>
        public static TNode CreateFromResource<TNode>(object resource) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromResource<TNode>(resource);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates <typeparamref name="TNode" /> from a loaded resource and applies optional style overrides to the
        ///         factory's default visual target.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         从已加载的资源创建 <typeparamref name="TNode" />，并将可选的样式覆盖应用到工厂的默认视觉目标。
        ///     </para>
        /// </summary>
        public static TNode CreateFromResource<TNode>(object resource, VisualNodeStyle? style) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromResource<TNode>(resource, style);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Instantiates <paramref name="scene" /> and uses the registered factory to create
        ///         <typeparamref name="TNode" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         实例化 <paramref name="scene" />，并使用已注册的工厂创建 <typeparamref name="TNode" />。
        ///     </para>
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Instantiates <paramref name="scene" />, converts it through the registered factory, and applies optional
        ///         style overrides to the factory's default visual target.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         实例化 <paramref name="scene" />，通过已注册的工厂转换，并将可选的样式覆盖应用到工厂的默认视觉目标。
        ///     </para>
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene, VisualNodeStyle? style) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene, style);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Creates a node as in <see cref="CreateFromScene{TNode}(PackedScene)" />, using the specified Godot
        ///         instantiation edit state to match call sites such as <c>PackedScene.GenEditState.Disabled</c>.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         与 <see cref="CreateFromScene{TNode}(PackedScene)" /> 相同，但使用指定的 Godot 实例化编辑状态，
        ///         以匹配 <c>PackedScene.GenEditState.Disabled</c> 等调用点。
        ///     </para>
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
        ///     <para xml:lang="en">
        ///         Loads <paramref name="scenePath" /> through <see cref="PreloadManager.Cache" /> and then calls
        ///         <see cref="CreateFromScene{TNode}(PackedScene)" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         通过 <see cref="PreloadManager.Cache" /> 加载 <paramref name="scenePath" />，然后调用
        ///         <see cref="CreateFromScene{TNode}(PackedScene)" />。
        ///     </para>
        /// </summary>
        public static TNode CreateFromScenePath<TNode>(string scenePath) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Loads <paramref name="scenePath" />, converts it through the registered factory, and applies optional style
        ///         overrides to the factory's default visual target.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         加载 <paramref name="scenePath" />，通过已注册的工厂转换，并将可选的样式覆盖应用到工厂的默认视觉目标。
        ///     </para>
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
            if (GodotObject.IsInstanceValid(node))
                return node;

            throw new InvalidOperationException(
                $"Registered Godot node factory member '{factoryMember}' returned a null or invalid node for " +
                $"{typeof(TNode).FullName}.");
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
