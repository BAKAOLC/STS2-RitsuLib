using Godot;
using MegaCrit.Sts2.Core.Assets;

namespace STS2RitsuLib.Scaffolding.Godot
{
    /// <summary>
    ///     Explicit-only Godot node construction: call these from your own code paths. Does not patch global
    ///     <c>PackedScene.Instantiate</c>, so baselib scene conversion and vanilla loading keep exclusive control of their
    ///     hooks.
    /// </summary>
    public static class RitsuGodotNodeFactories
    {
        /// <summary>
        ///     Creates <typeparamref name="TNode" /> from a loaded resource (e.g. <see cref="Texture2D" /> for creature /
        ///     merchant factories).
        /// </summary>
        public static TNode CreateFromResource<TNode>(object resource) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromResource<TNode>(resource);
        }

        /// <summary>
        ///     Instantiates <paramref name="scene" /> and runs the registered factory to produce <typeparamref name="TNode" />.
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene);
        }

        /// <summary>
        ///     Same as <see cref="CreateFromScene{TNode}(PackedScene)" /> but uses the given Godot instantiate edit
        ///     state (match vanilla callsites such as <c>PackedScene.GenEditState.Disabled</c>).
        /// </summary>
        public static TNode CreateFromScene<TNode>(PackedScene scene, PackedScene.GenEditState editState)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScene<TNode>(scene, editState);
        }

        /// <summary>
        ///     Loads <paramref name="scenePath" /> via <see cref="PreloadManager.Cache" /> then
        ///     <see>
        ///         <cref>CreateFromScene{TNode}</cref>
        ///     </see>
        ///     .
        /// </summary>
        public static TNode CreateFromScenePath<TNode>(string scenePath) where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath);
        }

        /// <inheritdoc cref="CreateFromScene{TNode}(PackedScene, PackedScene.GenEditState)" />
        public static TNode CreateFromScenePath<TNode>(string scenePath, PackedScene.GenEditState editState)
            where TNode : Node, new()
        {
            return RitsuGodotNodeFactoryRegistry.CreateFromScenePath<TNode>(scenePath, editState);
        }
    }
}
