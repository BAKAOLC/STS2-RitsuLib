using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeFactories
{
    /// <summary>
    ///     Converts plain Godot orb / misc scenes whose root is (or should become) a <see cref="Node2D" /> into a typed
    ///     <see cref="Node2D" /> for <see cref="RitsuGodotNodeFactories" /> (mirrors baselib flexible root handling with an
    ///     empty named-node set).
    ///     将普通 Godot 充能球 / 杂项场景转换为 <see cref="RitsuGodotNodeFactories" /> 使用的强类型
    ///     <see cref="Node2D" />；这些场景的根节点本身是（或应变为）<see cref="Node2D" />（这与 baselib 的灵活根节点处理一致，使用
    ///     空命名节点集合）。
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
