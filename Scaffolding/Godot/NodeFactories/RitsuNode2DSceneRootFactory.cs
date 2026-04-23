using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Converts plain Godot orb / misc scenes whose root is (or should become) a <see cref="Node2D" /> into a typed
    ///     <see cref="Node2D" /> for <see cref="RitsuGodotNodeFactories" /> (mirrors baselib flexible root handling with an
    ///     empty named-node set).
    /// </summary>
    internal sealed class RitsuNode2DSceneRootFactory() : RitsuGodotNodeFactory<Node2D>([])
    {
        protected override Node2D CreateBareFromResourceImpl(object resource)
        {
            throw new NotSupportedException(
                "RitsuNode2DSceneRootFactory only supports scene conversion via RitsuGodotNodeFactories.CreateFromScene / CreateFromScenePath<Node2D>(...).");
        }

        protected override void GenerateNode(Node2D target, IRitsuGodotNodeSlot required)
        {
        }
    }
}
