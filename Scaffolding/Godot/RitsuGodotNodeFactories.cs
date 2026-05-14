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
    }
}
